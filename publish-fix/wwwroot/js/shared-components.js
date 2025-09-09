/**
 * Shared Components Manager
 * Handles loading and initialization of shared header/footer components
 */

class SharedComponents {
    constructor() {
        this.backendBaseUrl = this.getBackendBaseUrl();
        this.authApiUrl = `${this.backendBaseUrl}/api/auth`;
        this.searchTimeout = null;
        this.selectedSuggestionIndex = -1;
        this.currentSuggestions = [];
    }

    /**
     * Get backend base URL with proper protocol handling for localhost
     */
    getBackendBaseUrl() {
        if (window.WordPressConfig) {
            return window.WordPressConfig.getApiUrl().replace('/api', '');
        }
        // For localhost development, use correct port based on protocol
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            if (window.location.protocol === 'https:') {
                return 'https://localhost:7151';  // HTTPS backend port
            } else {
                return 'http://localhost:7150';   // HTTP backend port
            }
        }
        // For production, use same protocol as page with default HTTPS port
        if (window.location.protocol === 'https:') {
            return 'https://localhost:7151';
        } else {
            return 'http://localhost:7150';
        }
    }

    /**
     * Initialize all shared components
     */
    async init() {
        try {
            await this.loadComponents();
            
            // Small delay to ensure DOM is ready after header loading
            await new Promise(resolve => setTimeout(resolve, 50));
            
            this.initializeEventHandlers();
            this.checkAuthState();
            this.initializeGlobalSearch();
            this.initializeTheme();
            this.setActiveNavigation();
            this.updateNavigationLinks();
            
            // Call page-specific initialization functions if they exist
            if (typeof initializeTheme === 'function') {
                initializeTheme();
            }
            if (typeof initializeAuthEventListeners === 'function') {
                initializeAuthEventListeners();
            }
        } catch (error) {
            console.error('Failed to initialize shared components:', error);
        }
    }

    /**
     * Load header and footer components
     */
    async loadComponents() {
        try {
            // Load header
            const headerResponse = await fetch('/components/header.html');
            const headerHtml = await headerResponse.text();
            
            // Load footer
            const footerResponse = await fetch('/components/footer.html');
            const footerHtml = await footerResponse.text();

            // Replace header placeholder
            const headerPlaceholder = document.getElementById('header-placeholder');
            if (headerPlaceholder) {
                const headerContainer = document.createElement('div');
                headerContainer.innerHTML = headerHtml;
                headerPlaceholder.replaceWith(headerContainer.firstElementChild);
            } else {
                // Fallback: Insert header at the beginning of body
                const headerContainer = document.createElement('div');
                headerContainer.innerHTML = headerHtml;
                document.body.insertBefore(headerContainer.firstElementChild, document.body.firstChild);
            }

            // Replace footer placeholder
            const footerPlaceholder = document.getElementById('footer-placeholder');
            if (footerPlaceholder) {
                const footerContainer = document.createElement('div');
                footerContainer.innerHTML = footerHtml;
                footerPlaceholder.replaceWith(footerContainer.firstElementChild);
            } else {
                // Fallback: Insert footer at the end of body
                const footerContainer = document.createElement('div');
                footerContainer.innerHTML = footerHtml;
                document.body.appendChild(footerContainer.firstElementChild);
            }

        } catch (error) {
            console.error('Error loading components:', error);
            throw error;
        }
    }

    /**
     * Initialize all event handlers
     */
    initializeEventHandlers() {
        // Theme toggle
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', this.toggleTheme.bind(this));
        }

        // Logout button
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', this.handleLogout.bind(this));
        }

        // Check auth state on window focus
        window.addEventListener('focus', this.checkAuthState.bind(this));
    }

    /**
     * Initialize theme functionality
     */
    initializeTheme() {
        const themeToggle = document.getElementById('theme-toggle');
        const htmlElement = document.documentElement;
        const currentTheme = localStorage.getItem('theme') || 'light';
        
        htmlElement.setAttribute('data-theme', currentTheme);
        if (themeToggle) {
            themeToggle.innerHTML = `<i class="fas fa-${currentTheme === 'dark' ? 'sun' : 'moon'}"></i>`;
        }
    }

    /**
     * Toggle theme between light and dark
     */
    toggleTheme() {
        const htmlElement = document.documentElement;
        const themeToggle = document.getElementById('theme-toggle');
        const newTheme = htmlElement.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
        
        htmlElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        if (themeToggle) {
            themeToggle.innerHTML = `<i class="fas fa-${newTheme === 'dark' ? 'sun' : 'moon'}"></i>`;
        }
    }

    /**
     * Check authentication state
     */
    checkAuthState() {
        const token = localStorage.getItem('authToken');
        const userInfo = localStorage.getItem('userInfo');
        
        if (token && userInfo) {
            try {
                const user = JSON.parse(userInfo);
                const expiresAt = new Date(user.expiresAt);
                
                if (expiresAt > new Date()) {
                    this.showLoggedInState(user);
                    return true;
                } else {
                    this.clearAuthData();
                }
            } catch (error) {
                this.clearAuthData();
            }
        }
        
        this.showLoggedOutState();
        return false;
    }

    /**
     * Show logged in state
     */
    showLoggedInState(user) {
        const loggedOutSection = document.getElementById('logged-out-section');
        const loggedInSection = document.getElementById('logged-in-section');
        const userAvatar = document.getElementById('user-avatar');
        const userName = document.getElementById('user-name');
        
        if (loggedOutSection) loggedOutSection.style.display = 'none';
        if (loggedInSection) loggedInSection.style.display = 'flex';
        
        if (userAvatar && userName) {
            const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();
            userAvatar.textContent = initials;
            userName.textContent = `${user.firstName || ''} ${user.lastName || ''}`;
        }
    }

    /**
     * Show logged out state
     */
    showLoggedOutState() {
        const loggedOutSection = document.getElementById('logged-out-section');
        const loggedInSection = document.getElementById('logged-in-section');
        
        if (loggedOutSection) loggedOutSection.style.display = 'block';
        if (loggedInSection) loggedInSection.style.display = 'none';
    }

    /**
     * Clear authentication data
     */
    clearAuthData() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userInfo');
    }

    /**
     * Handle logout
     */
    handleLogout() {
        if (confirm('Are you sure you want to logout?')) {
            this.clearAuthData();
            this.showLoggedOutState();
        }
    }

    /**
     * Initialize global search functionality
     */
    initializeGlobalSearch() {
        // Try to find elements with a retry mechanism
        let retryCount = 0;
        const maxRetries = 5;
        
        const tryInit = () => {
            const searchContainer = document.getElementById('global-search');
            const searchIconContainer = document.getElementById('search-icon-container');
            const searchInput = document.getElementById('global-search-input');
            const suggestionsContainer = document.getElementById('search-suggestions');

            if (searchContainer && searchIconContainer && searchInput && suggestionsContainer) {
                // All elements found, proceed with initialization
                
                // Search expansion event listeners
                searchIconContainer.addEventListener('click', this.handleSearchExpand.bind(this));
                searchInput.addEventListener('focus', this.handleSearchExpand.bind(this));
                searchInput.addEventListener('blur', this.handleSearchCollapse.bind(this));
                
                // Search input event listeners
                searchInput.addEventListener('input', this.handleSearchInput.bind(this));
                searchInput.addEventListener('keydown', this.handleSearchKeydown.bind(this));

                // Hide suggestions and collapse when clicking outside
                document.addEventListener('click', (e) => {
                    if (!searchContainer.contains(e.target)) {
                        this.hideSuggestions();
                        // Only collapse if input is empty
                        if (!searchInput.value.trim()) {
                            this.collapseSearchBar();
                        }
                    }
                });
                
                // Force initial state to be collapsed
                searchContainer.classList.remove('expanded');
                
                console.log('Global search initialized successfully');
                return true;
            } else {
                retryCount++;
                if (retryCount < maxRetries) {
                    // Retry after a short delay
                    setTimeout(tryInit, 100 * retryCount);
                } else {
                    console.error('Failed to initialize global search after', maxRetries, 'attempts. Missing elements:', {
                        searchContainer: searchContainer ? 'found' : 'missing',
                        searchIconContainer: searchIconContainer ? 'found' : 'missing',
                        searchInput: searchInput ? 'found' : 'missing',
                        suggestionsContainer: suggestionsContainer ? 'found' : 'missing'
                    });
                }
                return false;
            }
        };
        
        tryInit();
    }

    /**
     * Handle search input
     */
    handleSearchInput(e) {
        const query = e.target.value.trim();
        this.selectedSuggestionIndex = -1;

        if (this.searchTimeout) {
            clearTimeout(this.searchTimeout);
        }

        if (query.length < 2) {
            this.hideSuggestions();
            return;
        }

        // Debounce search requests
        this.searchTimeout = setTimeout(() => {
            this.performGlobalSearch(query);
        }, 300);
    }

    /**
     * Handle search keydown events
     */
    handleSearchKeydown(e) {
        const suggestionsContainer = document.getElementById('search-suggestions');
        const suggestions = suggestionsContainer.querySelectorAll('.suggestion-item');

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.min(this.selectedSuggestionIndex + 1, suggestions.length - 1);
                this.updateSuggestionSelection();
                break;

            case 'ArrowUp':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.max(this.selectedSuggestionIndex - 1, -1);
                this.updateSuggestionSelection();
                break;

            case 'Enter':
                e.preventDefault();
                if (this.selectedSuggestionIndex >= 0 && suggestions[this.selectedSuggestionIndex]) {
                    this.selectSuggestion(this.currentSuggestions[this.selectedSuggestionIndex]);
                } else {
                    this.performGlobalSearchNavigation(e.target.value.trim());
                }
                break;

            case 'Escape':
                this.hideSuggestions();
                e.target.blur();
                break;
        }
    }

    /**
     * Handle search focus
     */
    handleSearchFocus(e) {
        const query = e.target.value.trim();
        if (query.length >= 2) {
            this.performGlobalSearch(query);
        }
    }

    /**
     * Perform global search
     */
    async performGlobalSearch(query) {
        try {
            // Prefer the newer GlobalSearch endpoint which returns rich DTOs
            let response = await fetch(`${this.backendBaseUrl}/api/GlobalSearch?query=${encodeURIComponent(query)}&pageSize=8`);
            if (!response.ok) {
                // Fallback to legacy search endpoint
                response = await fetch(`${this.backendBaseUrl}/api/search/global?query=${encodeURIComponent(query)}`);
            }
            if (!response.ok) {
                console.log('Global search API not available, using fallback');
                this.performFallbackSearch(query);
                return;
            }

            const data = await response.json();
            // Normalize to an array of unified suggestion items
            const suggestions = Array.isArray(data?.results) ? data.results : Array.isArray(data) ? data : [];
            this.currentSuggestions = suggestions.map(item => this.normalizeApiResult(item));
            this.displaySuggestions(this.currentSuggestions);

        } catch (error) {
            console.error('Global search error:', error);
            this.performFallbackSearch(query);
        }
    }

    /**
     * Perform fallback search when API is not available
     */
    performFallbackSearch(query) {
        const fallbackSuggestions = [
            {
                type: 'page',
                title: 'Publications',
                description: 'Research papers and publications',
                url: 'Publications.html'
            },
            {
                type: 'page',
                title: 'Products',
                description: 'Our product offerings',
                url: 'Products.html'
            },
            {
                type: 'page',
                title: 'Solutions',
                description: 'Our solution catalog',
                url: 'Solutions.html'
            },
            {
                type: 'page',
                title: 'Repository',
                description: 'Code repositories and resources',
                url: 'Repository.html'
            }
        ].filter(item => 
            item.title.toLowerCase().includes(query.toLowerCase()) ||
            item.description.toLowerCase().includes(query.toLowerCase())
        ).slice(0, 4);

        this.currentSuggestions = fallbackSuggestions;
        this.displaySuggestions(fallbackSuggestions);
    }

    /**
     * Display search suggestions
     */
    displaySuggestions(suggestions) {
        const suggestionsContainer = document.getElementById('search-suggestions');
        
        if (!suggestions || suggestions.length === 0) {
            this.hideSuggestions();
            return;
        }

        const suggestionsHtml = suggestions.map((suggestion, index) => {
            const icon = this.getTypeIcon(suggestion.type);
            const urlAttr = suggestion.url ? `data-url=\"${this.escapeForHtmlAttribute(suggestion.url)}\"` : '';
            const idAttr = suggestion.id != null ? `data-id=\"${suggestion.id}\"` : '';
            const typeAttr = `data-type=\"${this.escapeForHtmlAttribute(suggestion.type)}\"`;
            return `
                <div class="suggestion-item" data-index="${index}" ${typeAttr} ${idAttr} ${urlAttr}
                     onclick="sharedComponents.handleSuggestionClick(this)">
                    <i class="suggestion-icon ${icon}"></i>
                    <div class="suggestion-content">
                        <div class="suggestion-title">${this.escapeHtml(suggestion.title)}</div>
                        <div class="suggestion-meta">${this.escapeHtml(suggestion.description || suggestion.type)}</div>
                    </div>
                </div>
            `;
        }).join('');

        // Add "Search for everything" option
        const searchQuery = document.getElementById('global-search-input').value.trim();
        const searchAllHtml = `
            <div class="suggestion-item" onclick="sharedComponents.performGlobalSearchNavigation('${this.escapeForHtmlAttribute(searchQuery)}')">
                <i class="suggestion-icon fas fa-search"></i>
                <div class="suggestion-content">
                    <div class="suggestion-title">Search for "${this.escapeHtml(searchQuery)}"</div>
                    <div class="suggestion-meta">View all results</div>
                </div>
            </div>
        `;

        suggestionsContainer.innerHTML = suggestionsHtml + searchAllHtml;
        suggestionsContainer.classList.add('show');
        this.selectedSuggestionIndex = -1;
    }

    /**
     * Normalize backend API result to unified suggestion shape with front-end URLs
     */
    normalizeApiResult(item) {
        const type = item.type || item.Type || 'page';
        const id = item.id ?? item.Id;
        let url = item.url || item.Url || '';

        // Normalize backend-style paths or empty URLs into frontend pages with deep-link id
        const typeLower = (type || '').toString().toLowerCase();
        const startsWith = (u, prefix) => typeof u === 'string' && u.startsWith(prefix);

        if (!url ||
            startsWith(url, '/products/') ||
            startsWith(url, '/solutions/') ||
            startsWith(url, '/publications/') ||
            startsWith(url, '/repositories/')) {
            switch (typeLower) {
                case 'product':
                    url = id != null ? `Products.html?id=${id}` : 'Products.html';
                    break;
                case 'solution':
                    // File is lowercase on disk
                    url = id != null ? `solutions.html?id=${id}` : 'solutions.html';
                    break;
                case 'publication':
                    url = id != null ? `Publications.html?id=${id}` : 'Publications.html';
                    break;
                case 'repository':
                    url = id != null ? `Repository.html?id=${id}` : 'Repository.html';
                    break;
                default:
                    // Default to search results if we only have a title/description
                    url = '';
                    break;
            }
        }

        return {
            type,
            id,
            title: item.title || item.Title || '',
            description: item.description || item.Description || '',
            url
        };
    }

    /**
     * Handle suggestion click to support deep linking
     */
    handleSuggestionClick(element) {
        this.hideSuggestions();
        const type = element.getAttribute('data-type');
        const id = element.getAttribute('data-id');
        const url = element.getAttribute('data-url');

        if (url) {
            window.location.href = url;
            return;
        }

        // If already on the destination page and a modal open function exists, open it directly
        const currentFile = (window.location.pathname.split('/').pop() || '').toLowerCase();
        if (id) {
            if (type === 'publication' && currentFile === 'publications.html' && typeof window.viewPublication === 'function') {
                try { window.viewPublication(parseInt(id, 10)); return; } catch {}
            }
            if (type === 'product' && currentFile === 'products.html' && typeof window.viewProduct === 'function') {
                try { window.viewProduct(parseInt(id, 10)); return; } catch {}
            }
            if (type === 'solution' && currentFile === 'solutions.html' && typeof window.viewSolution === 'function') {
                try { window.viewSolution(parseInt(id, 10)); return; } catch {}
            }
        }

        switch (type) {
            case 'product':
                window.location.href = id ? `Products.html?id=${id}` : 'Products.html';
                break;
            case 'publication':
                window.location.href = id ? `Publications.html?id=${id}` : 'Publications.html';
                break;
            case 'solution':
                window.location.href = id ? `solutions.html?id=${id}` : 'solutions.html';
                break;
            case 'repository':
                window.location.href = id ? `Repository.html?id=${id}` : 'Repository.html';
                break;
            default:
                const query = document.getElementById('global-search-input')?.value.trim() || '';
                this.navigateToSearchResults(query);
        }
    }

    /**
     * Update suggestion selection
     */
    updateSuggestionSelection() {
        const suggestions = document.querySelectorAll('.suggestion-item');
        
        suggestions.forEach((suggestion, index) => {
            if (index === this.selectedSuggestionIndex) {
                suggestion.classList.add('selected');
            } else {
                suggestion.classList.remove('selected');
            }
        });
    }

    /**
     * Select a suggestion
     */
    selectSuggestion(suggestion) {
        this.hideSuggestions();
        
        if (suggestion.url) {
            // Navigate to the URL
            window.location.href = suggestion.url;
        } else {
            // Navigate to search results page
            const query = document.getElementById('global-search-input').value.trim();
            this.navigateToSearchResults(query);
        }
    }

    /**
     * Perform global search navigation
     */
    performGlobalSearchNavigation(query) {
        this.hideSuggestions();
        if (query) {
            this.navigateToSearchResults(query);
        }
    }

    /**
     * Navigate to search results page
     */
    navigateToSearchResults(query) {
        const params = new URLSearchParams();
        params.append('q', query);
        window.location.href = `SearchResults.html?${params.toString()}`;
    }

    /**
     * Handle search bar expansion
     */
    handleSearchExpand() {
        const searchContainer = document.getElementById('global-search');
        const searchInput = document.getElementById('global-search-input');
        
        if (searchContainer && !searchContainer.classList.contains('expanded')) {
            searchContainer.classList.add('expanded');
            
            // Focus the input after expansion animation
            setTimeout(() => {
                if (searchInput && searchInput !== document.activeElement) {
                    searchInput.focus();
                }
            }, 200);
        }
    }

    /**
     * Handle search bar collapse
     */
    handleSearchCollapse() {
        // Use a timeout to allow for click events on suggestions
        setTimeout(() => {
            const searchInput = document.getElementById('global-search-input');
            const suggestionsContainer = document.getElementById('search-suggestions');
            
            // Only collapse if input is empty and suggestions are not showing
            if (searchInput && !searchInput.value.trim() && 
                (!suggestionsContainer || !suggestionsContainer.classList.contains('show'))) {
                this.collapseSearchBar();
            }
        }, 150);
    }

    /**
     * Collapse the search bar
     */
    collapseSearchBar() {
        const searchContainer = document.getElementById('global-search');
        if (searchContainer) {
            searchContainer.classList.remove('expanded');
        }
        this.hideSuggestions();
    }

    /**
     * Hide search suggestions
     */
    hideSuggestions() {
        const suggestionsContainer = document.getElementById('search-suggestions');
        if (suggestionsContainer) {
            suggestionsContainer.classList.remove('show');
        }
        this.selectedSuggestionIndex = -1;
    }

    /**
     * Get icon for suggestion type
     */
    getTypeIcon(type) {
        switch (type) {
            case 'publication':
                return 'fas fa-file-alt';
            case 'product':
                return 'fas fa-cube';
            case 'solution':
                return 'fas fa-lightbulb';
            case 'repository':
                return 'fab fa-github';
            case 'page':
                return 'fas fa-file';
            default:
                return 'fas fa-search';
        }
    }

    /**
     * Escape HTML
     */
    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function(m) { return map[m]; });
    }

    /**
     * Escape for HTML attributes
     */
    escapeForHtmlAttribute(text) {
        return text.replace(/"/g, '&quot;').replace(/'/g, '&#039;');
    }

    /**
     * Set active navigation based on current page
     */
    setActiveNavigation() {
        // Get current page filename
        const currentPage = window.location.pathname.split('/').pop() || 'index.html';
        
        // Remove active class from all nav links
        const navLinks = document.querySelectorAll('.nav-links a');
        navLinks.forEach(link => link.classList.remove('active'));
        
        // Add active class to current page link
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            const linkPage = href ? href.split('/').pop() : '';
            
            // Match exact filename or handle index/home page
            if (linkPage === currentPage || 
                (currentPage === '' && linkPage === 'index.html') ||
                (currentPage === 'index.html' && linkPage === 'index.html')) {
                link.classList.add('active');
            }
        });
    }

    /**
     * Update navigation links for WordPress compatibility
     */
    updateNavigationLinks() {
        if (window.WordPressConfig && window.WordPressConfig.isWordPressWidget()) {
            console.log('Updating navigation links for WordPress environment');
            
            // Update all navigation links
            const navLinks = document.querySelectorAll('.nav-links a[href$=".html"], .footer-section a[href$=".html"]');
            navLinks.forEach(link => {
                const href = link.getAttribute('href');
                const fileName = href.split('/').pop();
                const newUrl = window.WordPressConfig.getNavigationUrl(fileName);
                
                if (newUrl !== fileName) {
                    link.setAttribute('href', newUrl);
                    link.setAttribute('data-original-href', href);
                    console.log(`Updated link: ${href} -> ${newUrl}`);
                }
            });
            
            // Update search results navigation
            this.navigateToSearchResults = function(query) {
                const params = new URLSearchParams();
                params.append('q', query);
                const searchUrl = window.WordPressConfig.getNavigationUrl('SearchResults.html');
                window.location.href = `${searchUrl}?${params.toString()}`;
            };
            
            // Update suggestion navigation
            this.selectSuggestion = function(suggestion) {
                this.hideSuggestions();
                
                if (suggestion.url) {
                    const originalUrl = suggestion.url;
                    const [pathOnly, queryString] = originalUrl.split('?');
                    const fileName = pathOnly.split('/').pop();
                    const mappedBase = window.WordPressConfig.getNavigationUrl(fileName);
                    const finalUrl = queryString ? `${mappedBase}?${queryString}` : mappedBase;
                    window.location.href = finalUrl;
                } else {
                    const query = document.getElementById('global-search-input').value.trim();
                    this.navigateToSearchResults(query);
                }
            }.bind(this);
        }
    }
}

// Global instance
const sharedComponents = new SharedComponents();

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    sharedComponents.init();
});
