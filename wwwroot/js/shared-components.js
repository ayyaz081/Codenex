// Shared components file - authorization is now handled in individual pages

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
     * Get backend base URL with proper protocol handling
     */
    getBackendBaseUrl() {
        // First check if PortfolioConfig is available and use its API base URL
        if (typeof PortfolioConfig !== 'undefined' && PortfolioConfig.api && PortfolioConfig.api.getBaseUrl) {
            return PortfolioConfig.api.getBaseUrl();
        }
        
        // Check for API_BASE_URL override from Azure environment variables
        if (window.API_BASE_URL) {
            return window.API_BASE_URL;
        }
        
        // Fallback logic for when PortfolioConfig is not available
        if (window.WordPressConfig) {
            return window.WordPressConfig.getApiUrl().replace('/api', '');
        }
        
        // For localhost development, always use HTTP on port 7150
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            return 'http://localhost:7150';
        }
        
        // For production, force HTTPS for non-localhost domains
        const hostname = window.location.hostname;
        const port = window.location.port;
        
        // Force HTTPS for production domains (non-localhost)
        const protocol = hostname === 'localhost' || hostname === '127.0.0.1' 
                        ? window.location.protocol 
                        : 'https:';
        
        if (port && port !== '80' && port !== '443') {
            return `${protocol}//${hostname}:${port}`;
        } else {
            return `${protocol}//${hostname}`;
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
            this.initializeNavDropdowns();
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
            console.log('Loading header component...');
            const headerResponse = await fetch('/components/header.html');
            if (!headerResponse.ok) {
                throw new Error(`Header fetch failed: ${headerResponse.status}`);
            }
            const headerHtml = await headerResponse.text();
            
            // Load footer
            console.log('Loading footer component...');
            const footerResponse = await fetch('/components/footer.html');
            if (!footerResponse.ok) {
                console.warn('Footer fetch failed, continuing without footer');
            } else {
                const footerHtml = await footerResponse.text();
                
                // Replace footer placeholder
                const footerPlaceholder = document.getElementById('footer-placeholder');
                if (footerPlaceholder) {
                    const footerContainer = document.createElement('div');
                    footerContainer.innerHTML = footerHtml;
                    footerPlaceholder.replaceWith(footerContainer.firstElementChild);
                }
            }

            // Replace header placeholder
            const headerPlaceholder = document.getElementById('header-placeholder');
            if (headerPlaceholder) {
                console.log('Replacing header placeholder...');
                const headerContainer = document.createElement('div');
                headerContainer.innerHTML = headerHtml;
                headerPlaceholder.replaceWith(headerContainer.firstElementChild);
            } else {
                console.log('Header placeholder not found, inserting at body start...');
                // Fallback: Insert header at the beginning of body
                const headerContainer = document.createElement('div');
                headerContainer.innerHTML = headerHtml;
                document.body.insertBefore(headerContainer.firstElementChild, document.body.firstChild);
            }
            
            console.log('Header component loaded successfully');

        } catch (error) {
            console.error('Error loading components:', error);
            // Create a fallback header if loading fails
            this.createFallbackHeader();
        }
    }
    
    /**
     * Create a fallback header if the main header fails to load
     */
    createFallbackHeader() {
        console.log('Creating fallback header...');
        const fallbackHeader = `
            <nav class="navbar">
                <div class="nav-container">
                    <div class="nav-left">
                        <a href="/" class="nav-logo">
                            <span class="nav-logo-text">Codenex Solutions</span>
                        </a>
                    </div>
                    <div class="nav-center">
                        <ul class="nav-links">
                            <li><a href="/"><i class="fas fa-home"></i> Home</a></li>
                            <li><a href="/About"><i class="fas fa-user-circle"></i> About</a></li>
                            <li><a href="/Publications"><i class="fas fa-file-alt"></i> Publications</a></li>
                            <li><a href="/Products"><i class="fas fa-cube"></i> Products</a></li>
                            <li><a href="/Repository"><i class="fab fa-github"></i> Repository</a></li>
                            <li><a href="/solutions"><i class="fas fa-lightbulb"></i> Solutions</a></li>
                            <li><a href="/Contact"><i class="fas fa-envelope"></i> Contact</a></li>
                        </ul>
                    </div>
                    <div class="nav-right">
                        <div class="nav-actions">
                            <div id="auth-section">
                                <div id="logged-out-section">
                                    <a href="/Auth" class="auth-btn login-btn" title="Login">
                                        <i class="fas fa-sign-in-alt"></i>
                                    </a>
                                </div>
                                <div id="logged-in-section" style="display: none;">
                                    <div class="user-info">
                                        <a href="/Auth" class="user-link" id="user-link" title="User Account">
                                            <div class="user-avatar" id="user-avatar">U</div>
                                        </a>
                                    </div>
                                    <button class="auth-btn logout-btn" id="logout-btn" title="Logout">
                                        <i class="fas fa-sign-out-alt"></i>
                                    </button>
                                </div>
                            </div>
                            <button class="theme-toggle" id="theme-toggle">
                                <i class="fas fa-moon"></i>
                            </button>
                            <div class="nav-mobile">
                                <button class="nav-toggle" id="nav-toggle">
                                    <i class="fas fa-bars"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </nav>
        `;
        
        const headerPlaceholder = document.getElementById('header-placeholder');
        if (headerPlaceholder) {
            headerPlaceholder.innerHTML = fallbackHeader;
        } else {
            const headerContainer = document.createElement('div');
            headerContainer.innerHTML = fallbackHeader;
            document.body.insertBefore(headerContainer.firstElementChild, document.body.firstChild);
        }
    }

    /**
     * Initialize all event handlers
     */
    initializeEventHandlers() {
        console.log('🔧 Initializing event handlers...');
        
        // Use setTimeout to ensure DOM is fully loaded
        setTimeout(() => {
            // Theme toggle with enhanced event handling
            this.attachButtonHandler('theme-toggle', this.toggleTheme.bind(this), 'Theme toggle');
    
            // Logout button
            this.attachButtonHandler('logout-btn', this.handleLogout.bind(this), 'Logout button');
    
            // Mobile navigation toggle
            this.initializeMobileNavigation();
            
            console.log('✅ Event handlers initialized');
        }, 100);

        // Check auth state on window focus
        window.addEventListener('focus', this.checkAuthState.bind(this));
    }
    
    /**
     * Enhanced button handler attachment with fallbacks
     */
    attachButtonHandler(elementId, handler, description) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.warn(`⚠️ ${description}: Element #${elementId} not found`);
            return false;
        }
        
        // Remove existing handlers to prevent duplicates
        element.onclick = null;
        
        // Store handler reference for potential cleanup
        if (!element._sharedComponentHandlers) {
            element._sharedComponentHandlers = [];
        }
        
        // Create wrapped handler with proper event handling
        const wrappedHandler = (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log(`🖱️ ${description} clicked`);
            
            try {
                handler(e);
            } catch (error) {
                console.error(`❌ Error in ${description} handler:`, error);
            }
        };
        
        // Add click event listener
        element.addEventListener('click', wrappedHandler, { passive: false });
        element._sharedComponentHandlers.push({ type: 'click', handler: wrappedHandler });
        
        // Add touch support for mobile devices
        if ('ontouchstart' in window) {
            const touchHandler = (e) => {
                // Only handle if not already handled by click
                if (e.type === 'touchstart') {
                    e.preventDefault();
                    e.stopPropagation();
                    console.log(`👆 ${description} touched`);
                    
                    try {
                        handler(e);
                    } catch (error) {
                        console.error(`❌ Error in ${description} touch handler:`, error);
                    }
                }
            };
            
            element.addEventListener('touchstart', touchHandler, { passive: false });
            element._sharedComponentHandlers.push({ type: 'touchstart', handler: touchHandler });
        }
        
        // Ensure element is accessible
        if (!element.tabIndex || element.tabIndex < 0) {
            element.tabIndex = 0;
        }
        
        // Add keyboard support for accessibility
        const keyHandler = (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                e.stopPropagation();
                console.log(`⌨️ ${description} activated via keyboard`);
                
                try {
                    handler(e);
                } catch (error) {
                    console.error(`❌ Error in ${description} keyboard handler:`, error);
                }
            }
        };
        
        element.addEventListener('keydown', keyHandler);
        element._sharedComponentHandlers.push({ type: 'keydown', handler: keyHandler });
        
        console.log(`✅ ${description}: Handler attached successfully`);
        return true;
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
        const userLink = document.getElementById('user-link');
        
        if (loggedOutSection) loggedOutSection.style.display = 'none';
        if (loggedInSection) loggedInSection.style.display = 'flex';
        
        if (userAvatar) {
            const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();
            userAvatar.textContent = initials || 'U';
            
            // Set tooltip with full name
            if (userLink) {
                const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
                userLink.title = fullName || 'User Account';
            }
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
     * Initialize mobile navigation functionality
     */
    initializeMobileNavigation() {
        console.log('📱 Initializing mobile navigation...');
        
        // Use enhanced button handler for nav toggle
        this.attachButtonHandler('nav-toggle', this.toggleMobileMenu.bind(this), 'Mobile navigation toggle');
        
        // Create mobile menu initially if on mobile/tablet
        this.handleMobileMenuCreation();
        
        // Handle window resize to show/hide mobile menu appropriately
        window.addEventListener('resize', () => {
            this.handleMobileMenuCreation();
        });
        
        // Handle clicks outside mobile menu to close it
        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 1024) { // Only on mobile/tablet
                const mobileMenu = document.querySelector('.mobile-menu');
                const navToggle = document.getElementById('nav-toggle');
                
                if (mobileMenu && mobileMenu.classList.contains('show') && 
                    !mobileMenu.contains(e.target) && !navToggle?.contains(e.target)) {
                    this.closeMobileMenu();
                }
            }
        });
        
        // Handle escape key to close mobile menu
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && window.innerWidth <= 1024) {
                this.closeMobileMenu();
            }
        });
    }

    /**
     * Handle mobile menu creation based on screen size
     */
    handleMobileMenuCreation() {
        const isMobileTablet = window.innerWidth <= 1024;
        const existingMenu = document.querySelector('.mobile-menu');
        const existingOverlay = document.querySelector('.mobile-menu-overlay');
        
        if (isMobileTablet && !existingMenu) {
            // Create mobile menu on mobile/tablet
            this.createMobileMenu();
        } else if (!isMobileTablet && existingMenu) {
            // Remove mobile menu on desktop
            this.removeMobileMenu();
        }
        
        // Also ensure the mobile nav toggle is properly visible/hidden
        const navMobile = document.querySelector('.nav-mobile');
        if (navMobile) {
            if (isMobileTablet) {
                navMobile.style.display = 'flex';
            } else {
                navMobile.style.display = 'none';
            }
        }
    }

    /**
     * Create mobile menu structure
     */
    createMobileMenu() {
        // Check if mobile menu already exists
        if (document.querySelector('.mobile-menu')) {
            return;
        }
        
        // Create mobile menu overlay
        const overlay = document.createElement('div');
        overlay.className = 'mobile-menu-overlay';
        
        // Create mobile menu
        const mobileMenu = document.createElement('div');
        mobileMenu.className = 'mobile-menu';
        
        // Create mobile menu content
        const menuContent = document.createElement('div');
        menuContent.className = 'mobile-menu-content';
        
        // Create mobile search container
        const searchContainer = document.createElement('div');
        searchContainer.className = 'mobile-search-container';
        searchContainer.innerHTML = `
            <div class="mobile-search">
                <input type="text" class="mobile-search-input" id="mobile-search-input" 
                       placeholder="Search products, publications, solutions..." autocomplete="off">
                <i class="mobile-search-icon fas fa-search"></i>
            </div>
            <div class="search-suggestions" id="mobile-search-suggestions"></div>
        `;
        
        // Create mobile navigation links
        const navLinks = document.createElement('ul');
        navLinks.className = 'mobile-nav-links';
        
        // Get navigation links from desktop menu
        const desktopNavLinks = document.querySelectorAll('.nav-links a');
        desktopNavLinks.forEach(link => {
            const li = document.createElement('li');
            const a = document.createElement('a');
            a.href = link.href;
            a.innerHTML = link.innerHTML;
            a.addEventListener('click', () => {
                this.closeMobileMenu();
            });
            li.appendChild(a);
            navLinks.appendChild(li);
        });
        
        // Add authentication link to mobile menu
        const authLi = document.createElement('li');
        const authA = document.createElement('a');
        authA.href = '/Auth';
        authA.innerHTML = '<i class="fas fa-sign-in-alt"></i> Login / Account';
        authA.addEventListener('click', () => {
            this.closeMobileMenu();
        });
        authLi.appendChild(authA);
        navLinks.appendChild(authLi);
        
        // Assemble mobile menu
        menuContent.appendChild(searchContainer);
        menuContent.appendChild(navLinks);
        mobileMenu.appendChild(menuContent);
        
        // Add to page
        document.body.appendChild(overlay);
        document.body.appendChild(mobileMenu);
        
        // Initialize mobile search
        this.initializeMobileSearch();
    }

    /**
     * Remove mobile menu
     */
    removeMobileMenu() {
        const mobileMenu = document.querySelector('.mobile-menu');
        const overlay = document.querySelector('.mobile-menu-overlay');
        
        if (mobileMenu) {
            mobileMenu.remove();
        }
        if (overlay) {
            overlay.remove();
        }
        
        // Ensure body class is removed
        document.body.classList.remove('mobile-menu-open');
    }

    /**
     * Toggle mobile menu
     */
    toggleMobileMenu() {
        // Only work on mobile/tablet
        if (window.innerWidth > 1024) {
            return;
        }
        
        const mobileMenu = document.querySelector('.mobile-menu');
        const overlay = document.querySelector('.mobile-menu-overlay');
        
        if (mobileMenu && overlay) {
            const isOpen = mobileMenu.classList.contains('show');
            
            if (isOpen) {
                this.closeMobileMenu();
            } else {
                this.openMobileMenu();
            }
        }
    }

    /**
     * Open mobile menu
     */
    openMobileMenu() {
        // Only work on mobile/tablet
        if (window.innerWidth > 1024) {
            return;
        }
        
        const mobileMenu = document.querySelector('.mobile-menu');
        const overlay = document.querySelector('.mobile-menu-overlay');
        const body = document.body;
        
        if (mobileMenu && overlay) {
            mobileMenu.classList.add('show');
            overlay.classList.add('show');
            body.classList.add('mobile-menu-open');
        }
    }

    /**
     * Close mobile menu
     */
    closeMobileMenu() {
        const mobileMenu = document.querySelector('.mobile-menu');
        const overlay = document.querySelector('.mobile-menu-overlay');
        const body = document.body;
        
        if (mobileMenu && overlay) {
            mobileMenu.classList.remove('show');
            overlay.classList.remove('show');
            body.classList.remove('mobile-menu-open');
        }
    }

    /**
     * Initialize mobile search functionality
     */
    initializeMobileSearch() {
        const mobileSearchInput = document.getElementById('mobile-search-input');
        const mobileSuggestions = document.getElementById('mobile-search-suggestions');
        
        if (mobileSearchInput) {
            mobileSearchInput.addEventListener('input', (e) => {
                const query = e.target.value.trim();
                this.selectedSuggestionIndex = -1;
                
                if (this.searchTimeout) {
                    clearTimeout(this.searchTimeout);
                }
                
                if (query.length < 2) {
                    this.hideMobileSuggestions();
                    return;
                }
                
                this.searchTimeout = setTimeout(() => {
                    this.performMobileSearch(query);
                }, 300);
            });
            
            mobileSearchInput.addEventListener('keydown', (e) => {
                this.handleMobileSearchKeydown(e);
            });
        }
    }

    /**
     * Perform mobile search
     */
    async performMobileSearch(query) {
        try {
            let response = await fetch(`${this.backendBaseUrl}/api/GlobalSearch?query=${encodeURIComponent(query)}&pageSize=8`);
            if (!response.ok) {
                console.log('Global search API not available, using fallback');
                this.performMobileFallbackSearch(query);
                return;
            }

            const data = await response.json();
            const suggestions = Array.isArray(data?.results) ? data.results : Array.isArray(data) ? data : [];
            const normalizedSuggestions = suggestions.map(item => this.normalizeApiResult(item));
            this.displayMobileSuggestions(normalizedSuggestions);

        } catch (error) {
            console.error('Mobile search error:', error);
            this.performMobileFallbackSearch(query);
        }
    }

    /**
     * Perform mobile fallback search
     */
    performMobileFallbackSearch(query) {
        const fallbackSuggestions = [
            {
                type: 'page',
                title: 'Publications',
                description: 'Research papers and publications',
                url: '/Publications'
            },
            {
                type: 'page',
                title: 'Products',
                description: 'Our product offerings',
                url: '/Products'
            },
            {
                type: 'page',
                title: 'Solutions',
                description: 'Our solution catalog',
                url: '/solutions'
            },
            {
                type: 'page',
                title: 'Repository',
                description: 'Code repositories and resources',
                url: '/Repository'
            }
        ].filter(item => 
            item.title.toLowerCase().includes(query.toLowerCase()) ||
            item.description.toLowerCase().includes(query.toLowerCase())
        ).slice(0, 4);

        this.displayMobileSuggestions(fallbackSuggestions);
    }

    /**
     * Display mobile search suggestions
     */
    displayMobileSuggestions(suggestions) {
        const suggestionsContainer = document.getElementById('mobile-search-suggestions');
        
        if (!suggestions || suggestions.length === 0) {
            this.hideMobileSuggestions();
            return;
        }

        const suggestionsHtml = suggestions.map((suggestion, index) => {
            const icon = this.getTypeIcon(suggestion.type);
            const urlAttr = suggestion.url ? `data-url="${this.escapeForHtmlAttribute(suggestion.url)}"` : '';
            const idAttr = suggestion.id != null ? `data-id="${suggestion.id}"` : '';
            const typeAttr = `data-type="${this.escapeForHtmlAttribute(suggestion.type)}"`;
            return `
                <div class="suggestion-item" data-index="${index}" ${typeAttr} ${idAttr} ${urlAttr}
                     onclick="sharedComponents.handleMobileSuggestionClick(this)">
                    <i class="suggestion-icon ${icon}"></i>
                    <div class="suggestion-content">
                        <div class="suggestion-title">${this.escapeHtml(suggestion.title)}</div>
                        <div class="suggestion-meta">${this.escapeHtml(suggestion.description || suggestion.type)}</div>
                    </div>
                </div>
            `;
        }).join('');

        const searchQuery = document.getElementById('mobile-search-input').value.trim();
        const searchAllHtml = `
            <div class="suggestion-item" onclick="sharedComponents.performMobileSearchNavigation('${this.escapeForHtmlAttribute(searchQuery)}')">
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
     * Handle mobile search keydown
     */
    handleMobileSearchKeydown(e) {
        const suggestionsContainer = document.getElementById('mobile-search-suggestions');
        const suggestions = suggestionsContainer.querySelectorAll('.suggestion-item');

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.min(this.selectedSuggestionIndex + 1, suggestions.length - 1);
                this.updateMobileSuggestionSelection();
                break;

            case 'ArrowUp':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.max(this.selectedSuggestionIndex - 1, -1);
                this.updateMobileSuggestionSelection();
                break;

            case 'Enter':
                e.preventDefault();
                if (this.selectedSuggestionIndex >= 0 && suggestions[this.selectedSuggestionIndex]) {
                    this.handleMobileSuggestionClick(suggestions[this.selectedSuggestionIndex]);
                } else {
                    this.performMobileSearchNavigation(e.target.value.trim());
                }
                break;

            case 'Escape':
                this.hideMobileSuggestions();
                e.target.blur();
                break;
        }
    }

    /**
     * Update mobile suggestion selection
     */
    updateMobileSuggestionSelection() {
        const suggestions = document.querySelectorAll('#mobile-search-suggestions .suggestion-item');
        
        suggestions.forEach((suggestion, index) => {
            if (suggestion && suggestion.classList) {
                if (index === this.selectedSuggestionIndex) {
                    suggestion.classList.add('selected');
                } else {
                    suggestion.classList.remove('selected');
                }
            }
        });
    }

    /**
     * Handle mobile suggestion click
     */
    handleMobileSuggestionClick(element) {
        this.hideMobileSuggestions();
        this.closeMobileMenu();
        
        const type = element.getAttribute('data-type');
        const id = element.getAttribute('data-id');
        const url = element.getAttribute('data-url');

        if (url) {
            window.location.href = url;
            return;
        }

        switch (type) {
            case 'product':
                window.location.href = id ? `/Products.html?id=${id}` : '/Products.html';
                break;
            case 'publication':
                window.location.href = id ? `/Publications.html?id=${id}` : '/Publications.html';
                break;
            case 'solution':
                window.location.href = id ? `/solutions.html?id=${id}` : '/solutions.html';
                break;
            case 'repository':
                window.location.href = id ? `/Repository.html?id=${id}` : '/Repository.html';
                break;
            default:
                const query = document.getElementById('mobile-search-input')?.value.trim() || '';
                this.navigateToSearchResults(query);
        }
    }

    /**
     * Perform mobile search navigation
     */
    performMobileSearchNavigation(query) {
        this.hideMobileSuggestions();
        this.closeMobileMenu();
        if (query) {
            this.navigateToSearchResults(query);
        }
    }

    /**
     * Hide mobile suggestions
     */
    hideMobileSuggestions() {
        const suggestionsContainer = document.getElementById('mobile-search-suggestions');
        if (suggestionsContainer) {
            suggestionsContainer.classList.remove('show');
        }
    }

    /**
     * Initialize navigation dropdowns
     */
    initializeNavDropdowns() {
        console.log('🔽 Initializing navigation dropdowns...');
        
        // Load dropdown data
        this.loadNavigationData();
        
        // Initialize dropdown event listeners
        this.initializeDropdownEvents();
    }
    
    /**
     * Load navigation dropdown data from API
     */
    async loadNavigationData() {
        try {
            const response = await fetch(`${this.backendBaseUrl}/api/Navigation/all`);
            if (!response.ok) {
                console.warn('Navigation API not available, using fallback');
                this.setupFallbackDropdowns();
                return;
            }
            
            const data = await response.json();
            this.populateDropdowns(data);
            
        } catch (error) {
            console.error('Error loading navigation data:', error);
            this.setupFallbackDropdowns();
        }
    }
    
    /**
     * Populate dropdowns with data from API
     */
    populateDropdowns(data) {
        console.log('🔽 Populating dropdowns with data:', data);
        
        // Publications dropdown
        console.log('📚 Publications domains:', data.publications?.domains || []);
        this.populatePublicationsDropdown(data.publications?.domains || []);
        
        // Products dropdown
        console.log('📦 Products domains:', data.products?.domains || []);
        this.populateProductsDropdown(data.products?.domains || []);
        
        // Repository dropdown
        console.log('📁 Repository categories:', data.repositories?.categories || []);
        this.populateRepositoryDropdown(data.repositories?.categories || []);
        
        // Solutions dropdown
        console.log('💡 Solutions problem areas:', data.solutions?.problemAreas || []);
        this.populateSolutionsDropdown(data.solutions?.problemAreas || []);
        
    }
    
    /**
     * Populate publications dropdown
     */
    populatePublicationsDropdown(domains) {
        console.log('📚 populatePublicationsDropdown called with:', domains);
        const dropdown = document.getElementById('publications-dropdown');
        console.log('📚 Publications dropdown element:', dropdown);
        if (!dropdown) {
            console.error('❌ Publications dropdown element not found!');
            return;
        }
        
        if (domains.length === 0) {
            console.log('📚 Publications: No domains available');
            dropdown.innerHTML = `
                <div class="dropdown-item">No domains available</div>
                <a href="/Publications" class="dropdown-item dropdown-view-all">View All Publications</a>
            `;
            return;
        }
        
        console.log('📚 Publications: Generating HTML for domains:', domains);
        const domainsHtml = domains.map(domain => 
            `<a href="/Publications?domain=${encodeURIComponent(domain)}" class="dropdown-item">${this.escapeHtml(domain)}</a>`
        ).join('');
        
        const finalHtml = `
            <div class="dropdown-section-title">By Domain</div>
            ${domainsHtml}
            <a href="/Publications" class="dropdown-item dropdown-view-all">View All Publications</a>
        `;
        
        console.log('📚 Publications: Setting innerHTML to:', finalHtml);
        dropdown.innerHTML = finalHtml;
    }
    
    /**
     * Populate products dropdown
     */
    populateProductsDropdown(domains) {
        const dropdown = document.getElementById('products-dropdown');
        if (!dropdown) return;
        
        if (domains.length === 0) {
            dropdown.innerHTML = `
                <div class="dropdown-item">No domains available</div>
                <a href="/Products" class="dropdown-item dropdown-view-all">View All Products</a>
            `;
            return;
        }
        
        const domainsHtml = domains.map(domain => 
            `<a href="/Products?domain=${encodeURIComponent(domain)}" class="dropdown-item">${this.escapeHtml(domain)}</a>`
        ).join('');
        
        dropdown.innerHTML = `
            <div class="dropdown-section-title">By Domain</div>
            ${domainsHtml}
            <a href="/Products" class="dropdown-item dropdown-view-all">View All Products</a>
        `;
    }
    
    /**
     * Populate repository dropdown
     */
    populateRepositoryDropdown(categories) {
        console.log('📁 populateRepositoryDropdown called with:', categories);
        const dropdown = document.getElementById('repository-dropdown');
        console.log('📁 Repository dropdown element:', dropdown);
        if (!dropdown) {
            console.error('❌ Repository dropdown element not found!');
            return;
        }
        
        if (categories.length === 0) {
            console.log('📁 Repository: No categories available');
            dropdown.innerHTML = `
                <div class="dropdown-item">No categories available</div>
                <a href="/Repository" class="dropdown-item dropdown-view-all">View All Repositories</a>
            `;
            return;
        }
        
        console.log('📁 Repository: Generating HTML for categories:', categories);
        const categoriesHtml = categories.map(category => 
            `<a href="/Repository?category=${encodeURIComponent(category)}" class="dropdown-item">${this.escapeHtml(category)}</a>`
        ).join('');
        
        const finalHtml = `
            <div class="dropdown-section-title">By Category</div>
            ${categoriesHtml}
            <a href="/Repository" class="dropdown-item dropdown-view-all">View All Repositories</a>
        `;
        
        console.log('📁 Repository: Setting innerHTML to:', finalHtml);
        dropdown.innerHTML = finalHtml;
    }
    
    /**
     * Populate solutions dropdown
     */
    populateSolutionsDropdown(problemAreas) {
        const dropdown = document.getElementById('solutions-dropdown');
        if (!dropdown) return;
        
        if (problemAreas.length === 0) {
            dropdown.innerHTML = `
                <div class="dropdown-item">No problem areas available</div>
                <a href="/solutions" class="dropdown-item dropdown-view-all">View All Solutions</a>
            `;
            return;
        }
        
        const problemAreasHtml = problemAreas.map(area => 
            `<a href="/solutions?problemArea=${encodeURIComponent(area)}" class="dropdown-item">${this.escapeHtml(area)}</a>`
        ).join('');
        
        dropdown.innerHTML = `
            <div class="dropdown-section-title">By Problem Area</div>
            ${problemAreasHtml}
            <a href="/solutions" class="dropdown-item dropdown-view-all">View All Solutions</a>
        `;
    }
    
    /**
     * Setup fallback dropdowns when API is not available
     */
    setupFallbackDropdowns() {
        const dropdowns = {
            'publications-dropdown': '<a href="/Publications" class="dropdown-item dropdown-view-all">View All Publications</a>',
            'products-dropdown': '<a href="/Products" class="dropdown-item dropdown-view-all">View All Products</a>',
            'repository-dropdown': '<a href="/Repository" class="dropdown-item dropdown-view-all">View All Repositories</a>',
            'solutions-dropdown': '<a href="/solutions" class="dropdown-item dropdown-view-all">View All Solutions</a>'
        };
        
        Object.entries(dropdowns).forEach(([id, html]) => {
            const dropdown = document.getElementById(id);
            if (dropdown) {
                dropdown.innerHTML = html;
            }
        });
    }
    
    /**
     * Initialize dropdown event listeners
     */
    initializeDropdownEvents() {
        const dropdowns = document.querySelectorAll('.nav-dropdown');
        
        dropdowns.forEach(dropdown => {
            const toggle = dropdown.querySelector('.nav-dropdown-toggle');
            const menu = dropdown.querySelector('.nav-dropdown-menu');
            
            if (!toggle || !menu) return;
            
            let hideTimeout;
            
            // Show dropdown on hover with immediate response
            dropdown.addEventListener('mouseenter', () => {
                clearTimeout(hideTimeout);
                this.showDropdown(menu, toggle);
            });
            
            // Hide dropdown on mouse leave with delay
            dropdown.addEventListener('mouseleave', () => {
                hideTimeout = setTimeout(() => {
                    this.hideDropdown(menu);
                }, 150); // 150ms delay
            });
            
            // Keep dropdown open when hovering over the menu itself
            menu.addEventListener('mouseenter', () => {
                clearTimeout(hideTimeout);
            });
            
            // Hide when leaving the menu
            menu.addEventListener('mouseleave', () => {
                hideTimeout = setTimeout(() => {
                    this.hideDropdown(menu);
                }, 100);
            });
        });
        
        // Hide all dropdowns when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.nav-dropdown')) {
                this.hideAllDropdowns();
            }
        });
    }
    
    /**
     * Show dropdown menu
     */
    showDropdown(menu, toggle) {
        // Hide all other dropdowns first
        this.hideAllDropdowns();
        
        // Position the dropdown
        this.positionDropdown(menu, toggle);
        
        // Show the dropdown
        menu.classList.add('show');
    }
    
    /**
     * Hide dropdown menu
     */
    hideDropdown(menu) {
        menu.classList.remove('show');
    }
    
    /**
     * Hide all dropdown menus
     */
    hideAllDropdowns() {
        const dropdowns = document.querySelectorAll('.nav-dropdown-menu');
        dropdowns.forEach(dropdown => {
            dropdown.classList.remove('show');
        });
    }
    
    /**
     * Position dropdown menu using fixed positioning
     */
    positionDropdown(menu, toggle) {
        const rect = toggle.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const menuWidth = 180;
        
        // Position below the toggle with minimal gap
        const top = rect.bottom + 2;
        let left = rect.left;
        
        // Simple left alignment with screen boundary checks
        if (left + menuWidth > viewportWidth - 20) {
            left = viewportWidth - menuWidth - 20;
        }
        if (left < 20) {
            left = 20;
        }
        
        // Apply positioning
        menu.style.top = `${top}px`;
        menu.style.left = `${left}px`;
        
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
                
                // Search expansion event listeners with enhanced handling
                this.attachButtonHandler('search-icon-container', this.handleSearchExpand.bind(this), 'Search expand');
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
                
                // Reposition dropdown on window resize
                window.addEventListener('resize', () => {
                    const suggestionsContainer = document.getElementById('search-suggestions');
                    if (suggestionsContainer && suggestionsContainer.classList.contains('show')) {
                        this.positionSuggestionsDropdown(suggestionsContainer);
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
            // Use the GlobalSearch endpoint
            let response = await fetch(`${this.backendBaseUrl}/api/GlobalSearch?query=${encodeURIComponent(query)}&pageSize=8`);
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
                url: '/Publications'
            },
            {
                type: 'page',
                title: 'Products',
                description: 'Our product offerings',
                url: '/Products'
            },
            {
                type: 'page',
                title: 'Solutions',
                description: 'Our solution catalog',
                url: '/solutions'
            },
            {
                type: 'page',
                title: 'Repository',
                description: 'Code repositories and resources',
                url: '/Repository'
            }
        ].filter(item => 
            item.title.toLowerCase().includes(query.toLowerCase()) ||
            item.description.toLowerCase().includes(query.toLowerCase())
        ).slice(0, 4);

        this.currentSuggestions = fallbackSuggestions;
        this.displaySuggestions(fallbackSuggestions);
    }

    /**
     * Position suggestions dropdown using fixed positioning
     */
    positionSuggestionsDropdown(suggestionsContainer) {
        const searchContainer = document.getElementById('global-search');
        if (!searchContainer) return;
        
        const rect = searchContainer.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const dropdownWidth = 280; // min-width from CSS
        
        // Position below the search container with some margin
        const top = rect.bottom + 8;
        let left = rect.left;
        
        // Ensure dropdown doesn't go off screen on the right
        if (left + dropdownWidth > viewportWidth - 20) {
            left = viewportWidth - dropdownWidth - 20;
        }
        
        // Ensure dropdown doesn't go off screen on the left
        if (left < 20) {
            left = 20;
        }
        
        // Apply positioning
        suggestionsContainer.style.top = `${top}px`;
        suggestionsContainer.style.left = `${left}px`;
        suggestionsContainer.style.width = `${Math.max(dropdownWidth, rect.width)}px`;
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
        
        // Position the dropdown using fixed positioning to break out of any stacking context
        this.positionSuggestionsDropdown(suggestionsContainer);

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
                    url = id != null ? `/Products.html?id=${id}` : '/Products.html';
                    break;
                case 'solution':
                    // File is lowercase on disk
                    url = id != null ? `/solutions.html?id=${id}` : '/solutions.html';
                    break;
                case 'publication':
                    url = id != null ? `/Publications.html?id=${id}` : '/Publications.html';
                    break;
                case 'repository':
                    url = id != null ? `/Repository.html?id=${id}` : '/Repository.html';
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
        const currentPath = window.location.pathname.toLowerCase();
        if (id) {
            if (type === 'publication' && (currentPath === '/publications' || currentPath === '/publications.html') && typeof window.viewPublication === 'function') {
                try { window.viewPublication(parseInt(id, 10)); return; } catch {}
            }
            if (type === 'product' && (currentPath === '/products' || currentPath === '/products.html') && typeof window.viewProduct === 'function') {
                try { window.viewProduct(parseInt(id, 10)); return; } catch {}
            }
            if (type === 'solution' && (currentPath === '/solutions' || currentPath === '/solutions.html') && typeof window.viewSolution === 'function') {
                try { window.viewSolution(parseInt(id, 10)); return; } catch {}
            }
        }

        switch (type) {
            case 'product':
                window.location.href = id ? `/Products.html?id=${id}` : '/Products.html';
                break;
            case 'publication':
                window.location.href = id ? `/Publications.html?id=${id}` : '/Publications.html';
                break;
            case 'solution':
                window.location.href = id ? `/solutions.html?id=${id}` : '/solutions.html';
                break;
            case 'repository':
                window.location.href = id ? `/Repository.html?id=${id}` : '/Repository.html';
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
            if (suggestion && suggestion.classList) {
                if (index === this.selectedSuggestionIndex) {
                    suggestion.classList.add('selected');
                } else {
                    suggestion.classList.remove('selected');
                }
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
        window.location.href = `/SearchResults?${params.toString()}`;
    }

    /**
     * Handle search bar expansion
     */
    handleSearchExpand() {
        const searchContainer = document.getElementById('global-search');
        const searchInput = document.getElementById('global-search-input');
        const navCenter = document.querySelector('.nav-center');
        
        if (searchContainer && !searchContainer.classList.contains('expanded')) {
            searchContainer.classList.add('expanded');
            
            // Collapse nav links to prevent overlap
            if (navCenter) {
                navCenter.classList.add('search-expanded');
            }
            
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
        const navCenter = document.querySelector('.nav-center');
        
        if (searchContainer) {
            searchContainer.classList.remove('expanded');
        }
        
        // Restore nav links visibility
        if (navCenter) {
            navCenter.classList.remove('search-expanded');
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
        navLinks.forEach(link => {
            if (link && link.classList) {
                link.classList.remove('active');
            }
        });
        
        // Add active class to current page link
        navLinks.forEach(link => {
            if (link && link.getAttribute && link.classList) {
                const href = link.getAttribute('href');
                const linkPage = href ? href.split('/').pop() : '';
                
                // Match exact filename or handle index/home page
                if (linkPage === currentPage || 
                    (currentPage === '' && linkPage === 'index.html') ||
                    (currentPage === 'index.html' && linkPage === 'index.html')) {
                    link.classList.add('active');
                }
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
// Expose globally for inline handlers in generated HTML (e.g., suggestion items)
if (typeof window !== 'undefined') {
    window.sharedComponents = sharedComponents;
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    sharedComponents.init();
});
