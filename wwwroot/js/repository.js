// Backend API Configuration
        function getBackendBaseUrl() {
            // Check if PortfolioConfig is loaded
            if (typeof PortfolioConfig !== 'undefined' && PortfolioConfig.api && PortfolioConfig.api.getBaseUrl) {
                return PortfolioConfig.api.getBaseUrl();
            }
            
            // Check for API_BASE_URL from environment
            if (window.API_BASE_URL) {
                return window.API_BASE_URL;
            }
            
            // Fallback to dynamic detection
            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                return 'http://localhost:7150';
            }
            
            // Production: use same protocol and hostname
            return `${window.location.protocol}//${window.location.hostname}`;
        }
        
        const backendBaseUrl = getBackendBaseUrl();
        const repositoryApiUrl = `${backendBaseUrl}/api/repository`;

        let currentPage = 1;
        let pageSize = 12;
        let totalItems = 0;
        let repositories = [];
        let filteredRepositories = [];
        let deepLinkHandled = false;
        let currentUser = null;

        // Authentication Management
        class InlineAuthManager {
            constructor() {
                this.baseUrl = getBackendBaseUrl();
                this.user = null;
                this.init();
            }

            init() {
                this.user = this.getCurrentUser();
                this.checkTokenExpiration();
                
                setInterval(() => this.checkTokenExpiration(), 60000);
                
                this.initAuthDisplay();
            }

            getCurrentUser() {
                const userInfo = localStorage.getItem('userInfo');
                const token = localStorage.getItem('authToken');
                
                if (userInfo && token) {
                    try {
                        const user = JSON.parse(userInfo);
                        const expiresAt = new Date(user.expiresAt);
                        
                        if (expiresAt > new Date()) {
                            return user;
                        } else {
                            this.clearAuthData();
                            return null;
                        }
                    } catch (error) {
                        console.error('Error parsing user info:', error);
                        this.clearAuthData();
                        return null;
                    }
                }
                return null;
            }

            getAuthHeaders() {
                const token = localStorage.getItem('authToken');
                return token ? { 'Authorization': `Bearer ${token}` } : {};
            }

            isAuthenticated() {
                return this.user !== null;
            }

            clearAuthData() {
                localStorage.removeItem('authToken');
                localStorage.removeItem('userInfo');
                this.user = null;
            }

            checkTokenExpiration() {
                const userInfo = localStorage.getItem('userInfo');
                if (userInfo) {
                    try {
                        const user = JSON.parse(userInfo);
                        const expiresAt = new Date(user.expiresAt);
                        
                        if (expiresAt <= new Date()) {
                            console.log('Token expired, clearing auth data');
                            this.clearAuthData();
                            this.onTokenExpired();
                        }
                    } catch (error) {
                        console.error('Error checking token expiration:', error);
                        this.clearAuthData();
                    }
                }
            }

            onTokenExpired() {
                this.showNotification('Your session has expired. Please log in again.', 'warning');
                this.initAuthDisplay();
                setTimeout(() => {
                    if (window.confirm('Your session has expired. Would you like to go to the login page?')) {
                        window.location.href = './Auth.html';
                    }
                }, 3000);
            }

            showLoginPrompt() {
                const message = 'You need to log in to access this feature. Would you like to go to the login page?';
                if (window.confirm(message)) {
                    window.location.href = './Auth.html';
                }
            }

            async makeAuthenticatedRequest(url, options = {}) {
                const headers = {
                    ...this.getAuthHeaders(),
                    ...(options.headers || {})
                };

                const response = await fetch(url, {
                    ...options,
                    headers
                });

                if (response.status === 401 || response.status === 403) {
                    this.clearAuthData();
                    this.initAuthDisplay();
                    
                    if (response.status === 401) {
                        this.showNotification('Please log in to access this feature.', 'warning');
                        this.showLoginPrompt();
                    } else {
                        this.showNotification('Access denied. You don\'t have permission to perform this action.', 'error');
                    }
                    
                    throw new Error(`Authentication error: ${response.status}`);
                }

                return response;
            }

            initAuthDisplay() {
                console.log('Auth display handled by navbar');
            }

            logout() {
                if (window.confirm('Are you sure you want to logout?')) {
                    this.clearAuthData();
                    this.showNotification('You have been logged out successfully.', 'success');
                    this.initAuthDisplay();
                    
                    setTimeout(() => {
                        if (window.confirm('Would you like to go to the login page?')) {
                            window.location.href = './Auth.html';
                        }
                    }, 1500);
                }
            }

            showNotification(message, type = 'info') {
                if (typeof showNotification === 'function') {
                    showNotification(message, type);
                    return;
                }
                
                const icons = {
                    success: 'âœ…',
                    error: 'âŒ',
                    warning: 'âš ï¸',
                    info: 'â„¹ï¸'
                };
                
                alert(`${icons[type] || icons.info} ${message}`);
            }
        }

        const authManager = new InlineAuthManager();

        // No HTTP to HTTPS redirection - using HTTP on localhost:7150

        // Theme handling is managed by shared components

        async function loadRepositories() {
            try {
                const response = await fetch(repositoryApiUrl);
                if (response.ok) {
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        repositories = await response.json();
                    } else {
                        throw new Error('API returned non-JSON response');
                    }
                } else {
                    throw new Error(`API request failed with status: ${response.status}`);
                }
            } catch (error) {
                console.error('Failed to load repositories from API:', error);
                showErrorState();
                return;
            }
            
            // Load categories for filter dropdown
            await loadCategoryFilter();
            
            filteredRepositories = [...repositories];
            updateStats();
            await renderRepositories();
            renderPagination();
        }
        
        // Load categories dynamically from the API
        async function loadCategoryFilter() {
            try {
                const response = await fetch(`${repositoryApiUrl}/categories`);
                if (response.ok) {
                    const categories = await response.json();
                    populateCategoryFilter(categories);
                } else {
                    console.warn('Could not load categories from API, keeping hardcoded values');
                }
            } catch (error) {
                console.warn('Error loading categories from API:', error, '- keeping hardcoded values');
            }
        }
        
        // Populate category filter dropdown with API data
        function populateCategoryFilter(categories) {
            const categoryFilter = document.getElementById('category-filter');
            
            // Clear existing options except "All Categories"
            while (categoryFilter.children.length > 1) {
                categoryFilter.removeChild(categoryFilter.lastChild);
            }
            
            // Add categories from API
            categories.forEach(category => {
                const option = document.createElement('option');
                option.value = category;
                option.textContent = category;
                categoryFilter.appendChild(option);
            });
        }

        function updateStats() {
            const totalCount = filteredRepositories.length;
            const freeCount = filteredRepositories.filter(repo => !repo.isPremium).length;
            const premiumCount = filteredRepositories.filter(repo => repo.isPremium).length;
            
            document.getElementById('total-count').textContent = totalCount;
            document.getElementById('free-count').textContent = freeCount;
            document.getElementById('premium-count').textContent = premiumCount;
            
            const lastUpdate = filteredRepositories.reduce((latest, repo) => {
                const repoDate = new Date(repo.updatedAt || repo.createdAt);
                return repoDate > latest ? repoDate : latest;
            }, new Date(0));
            
            if (lastUpdate.getTime() > 0) {
                document.getElementById('last-updated').textContent = lastUpdate.toLocaleDateString();
            }
        }

        async function renderRepositories() {
            const container = document.getElementById('repositories-container');
            
            if (filteredRepositories.length === 0) {
                container.innerHTML = `
                    <div class="empty-state">
                        <i class="fas fa-search"></i>
                        <h3>No repositories found</h3>
                        <p>Try adjusting your search criteria or filters.</p>
                    </div>
                `;
                document.getElementById('pagination').style.display = 'none';
                return;
            }

            const startIndex = (currentPage - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            const pageItems = filteredRepositories.slice(startIndex, endIndex);

            // Generate action buttons for all repos (async)
            const repositoriesWithButtons = await Promise.all(pageItems.map(async (repo) => {
                const actionButtons = await generateActionButtons(repo);
                const techTags = (repo.technicalStack || '').split(',').map(tech => 
                    tech.trim() ? `<span class="tech-tag">${tech.trim()}</span>` : ''
                ).join('');
                
                return `
                <div class="repository-card ${repo.isPremium ? 'premium' : ''}" id="repo-${repo.id}">
                    <div class="repository-header">
                        <div class="repository-type">${repo.category || 'General'}</div>
                        <div class="repository-badges">
                            ${repo.isPremium ? '<span class="premium-badge">Premium</span>' : '<span class="free-badge">Free</span>'}
                        </div>
                    </div>
                    
                    <h3 class="repository-title">
                        <i class="fab fa-github"></i>
                        ${repo.title}
                    </h3>
                    
                    <p class="repository-description">${repo.description}</p>
                    
                    <div class="repository-meta">
                        <div class="meta-item">
                            <i class="fas fa-download meta-icon"></i>
                            <span>${repo.downloadCount || 0} downloads</span>
                        </div>
                        <div class="meta-item">
                            <i class="fas fa-calendar meta-icon"></i>
                            <span>${new Date(repo.createdAt).toLocaleDateString()}</span>
                        </div>
                    </div>
                    
                    <div class="technology-tags">
                        ${techTags}
                    </div>
                    
                    <div class="repository-actions">
                        ${actionButtons}
                    </div>
                </div>
            `;
            }));

            const repositoriesHtml = repositoriesWithButtons.join('');

            container.innerHTML = `
                <div class="repositories-grid">
                    ${repositoriesHtml}
                </div>
            `;

            // Deep link handling: compute page for repo id and scroll, or handle category filtering
            if (!deepLinkHandled) {
                const urlParams = new URLSearchParams(window.location.search);
                
                // Handle individual repository ID
                const idParam = urlParams.get('id');
                if (idParam) {
                    const targetId = parseInt(idParam, 10);
                    const idx = filteredRepositories.findIndex(r => (r.id || r.Id) === targetId);
                    if (idx >= 0) {
                        currentPage = Math.floor(idx / pageSize) + 1;
                    }
                    const el = document.getElementById(`repo-${targetId}`);
                    if (el) {
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        try { viewRepository(targetId); } catch {}
                    }
                } else {
                    // Handle category filtering
                    const category = urlParams.get('category');
                    if (category) {
                        console.log('Deep link category filter detected:', category);
                        const categoryFilter = document.getElementById('category-filter');
                        if (categoryFilter) {
                            // Set the category filter value
                            categoryFilter.value = category;
                            // Trigger filtering
                            applyFilters();
                            console.log('Applied category filter from URL:', category);
                        } else {
                            console.warn('Category filter element not found');
                        }
                    }
                }
                deepLinkHandled = true;
            }
            document.getElementById('pagination').style.display = filteredRepositories.length > pageSize ? 'flex' : 'none';
        }

        function renderPagination() {
            const totalPages = Math.ceil(filteredRepositories.length / pageSize);
            if (totalPages <= 1) {
                document.getElementById('pagination').style.display = 'none';
                return;
            }

            let paginationHtml = '';
            
            paginationHtml += `
                <button class="pagination-btn ${currentPage === 1 ? 'disabled' : ''}" 
                        onclick="changePage(${currentPage - 1})" 
                        ${currentPage === 1 ? 'disabled' : ''}>
                    <i class="fas fa-chevron-left"></i> Previous
                </button>
            `;

            const maxVisiblePages = 5;
            let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
            let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

            if (endPage - startPage + 1 < maxVisiblePages) {
                startPage = Math.max(1, endPage - maxVisiblePages + 1);
            }

            if (startPage > 1) {
                paginationHtml += `<button class="pagination-btn" onclick="changePage(1)">1</button>`;
                if (startPage > 2) {
                    paginationHtml += `<span class="pagination-btn disabled">...</span>`;
                }
            }

            for (let i = startPage; i <= endPage; i++) {
                paginationHtml += `
                    <button class="pagination-btn ${i === currentPage ? 'active' : ''}" 
                            onclick="changePage(${i})">${i}</button>
                `;
            }

            if (endPage < totalPages) {
                if (endPage < totalPages - 1) {
                    paginationHtml += `<span class="pagination-btn disabled">...</span>`;
                }
                paginationHtml += `<button class="pagination-btn" onclick="changePage(${totalPages})">${totalPages}</button>`;
            }

            paginationHtml += `
                <button class="pagination-btn ${currentPage === totalPages ? 'disabled' : ''}" 
                        onclick="changePage(${currentPage + 1})" 
                        ${currentPage === totalPages ? 'disabled' : ''}>
                    Next <i class="fas fa-chevron-right"></i>
                </button>
            `;

            document.getElementById('pagination').innerHTML = paginationHtml;
        }

        async function changePage(page) {
            const totalPages = Math.ceil(filteredRepositories.length / pageSize);
            if (page < 1 || page > totalPages) return;
            
            currentPage = page;
            await renderRepositories();
            renderPagination();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        async function applyFilters() {
            const searchTerm = document.getElementById('search-input').value.toLowerCase();
            const categoryFilter = document.getElementById('category-filter').value;
            const typeFilter = document.getElementById('type-filter').value;
            const sortFilter = document.getElementById('sort-filter').value;

            filteredRepositories = repositories.filter(repo => {
                const matchesSearch = !searchTerm || 
                    repo.title.toLowerCase().includes(searchTerm) ||
                    repo.description.toLowerCase().includes(searchTerm) ||
                    (repo.technicalStack && repo.technicalStack.toLowerCase().includes(searchTerm));
                
                const matchesCategory = !categoryFilter || repo.category === categoryFilter;
                
                const matchesType = !typeFilter || 
                    (typeFilter === 'free' && !repo.isPremium) ||
                    (typeFilter === 'premium' && repo.isPremium);

                return matchesSearch && matchesCategory && matchesType;
            });

            filteredRepositories.sort((a, b) => {
                switch (sortFilter) {
                    case 'newest':
                        return new Date(b.createdAt) - new Date(a.createdAt);
                    case 'oldest':
                        return new Date(a.createdAt) - new Date(b.createdAt);
                    case 'name':
                        return a.title.localeCompare(b.title);
                    case 'stars':
                        return (b.stars || 0) - (a.stars || 0);
                    default:
                        return 0;
                }
            });

            currentPage = 1;
            updateStats();
            await renderRepositories();
            renderPagination();
        }

        function showErrorState() {
            document.getElementById('repositories-container').innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-exclamation-triangle" style="color: var(--accent);"></i>
                    <h3>Failed to load repositories</h3>
                    <p>Please try again later or contact support if the problem persists.</p>
                </div>
            `;
            document.getElementById('pagination').style.display = 'none';
        }

        document.getElementById('search-input').addEventListener('input', debounce(applyFilters, 300));
        document.getElementById('category-filter').addEventListener('change', applyFilters);
        document.getElementById('type-filter').addEventListener('change', applyFilters);
        document.getElementById('sort-filter').addEventListener('change', applyFilters);

        function debounce(func, wait) {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        }

        async function generateActionButtons(repo) {
            let buttons = [];
            
            if (!repo.isPremium) {
                // Free repository
                if (repo.githubUrl) {
                    buttons.push(`
                        <a href="${repo.githubUrl}" target="_blank" class="action-btn btn-primary">
                            <i class="fab fa-github"></i>
                            View on GitHub
                        </a>
                    `);
                } else {
                    buttons.push(`
                        <button class="action-btn btn-primary" onclick="downloadRepository('${repo.id}')">
                            <i class="fas fa-download"></i>
                            Download Free
                        </button>
                    `);
                }
            } else {
                // Premium repository
                const price = repo.price ? `$${repo.price.toFixed(2)}` : '$29.99';
                
                // Check if user already purchased this repository
                if (authManager.isAuthenticated()) {
                    try {
                        const response = await fetch(`${backendBaseUrl}/api/payment/verify-purchase/${repo.id}`, {
                            headers: authManager.getAuthHeaders()
                        });
                        
                        if (response.ok) {
                            const purchase = await response.json();
                            
                            if (purchase.hasPurchased) {
                                // User owns this repo
                                const githubUrl = repo.githubRepoFullName ? 
                                    `https://github.com/${repo.githubRepoFullName}` : 
                                    repo.githubUrl;
                                    
                                buttons.push(`
                                    <div style="text-align: center; width: 100%;">
                                        <div style="background: linear-gradient(135deg, var(--success), var(--info)); color: white; padding: 8px 12px; border-radius: 8px; margin-bottom: 8px; font-weight: 600; font-size: 0.9rem;">
                                            <i class="fas fa-check-circle"></i> You own this repository
                                        </div>
                                        ${githubUrl ? `
                                            <a href="${githubUrl}" target="_blank" class="action-btn btn-primary" style="width: 100%;">
                                                <i class="fab fa-github"></i>
                                                Open on GitHub
                                            </a>
                                        ` : '<p style="color: var(--text-muted); font-size: 0.9rem;">Check your email for GitHub invitation</p>'}
                                    </div>
                                `);
                                return buttons.join('');
                            }
                        }
                    } catch (error) {
                        console.error('Error checking purchase status:', error);
                    }
                }
                
                // User hasn't purchased yet
                buttons.push(`
                    <button class="action-btn btn-premium" onclick="purchaseRepository(${repo.id}, '${repo.title}', ${repo.price || 29.99})" data-repo-id="${repo.id}">
                        <i class="fas fa-lock"></i>
                        Purchase ${price}
                    </button>
                `);
            }
            
            return buttons.join('');
        }

        function getAuthHeaders() {
            return authManager.getAuthHeaders();
        }

        function getCurrentUser() {
            return authManager.getCurrentUser();
        }

        async function downloadRepository(repositoryId) {
            try {
                console.log('Starting download for repository:', repositoryId);
                
                if (!authManager.isAuthenticated()) {
                    authManager.showNotification('Please log in to download repositories.', 'warning');
                    authManager.showLoginPrompt();
                    return;
                }
                
                const response = await authManager.makeAuthenticatedRequest(`${repositoryApiUrl}/${repositoryId}/download`, {
                    method: 'GET'
                });
                
                if (response.ok) {
                    const contentDisposition = response.headers.get('Content-Disposition');
                    let filename = `repository-${repositoryId}.zip`;
                    if (contentDisposition) {
                        const filenameMatch = contentDisposition.match(/filename[*]?=[\"']?([^\"'\;]+)[\"']?/);
                        if (filenameMatch && filenameMatch[1]) {
                            filename = filenameMatch[1];
                        }
                    }
                    
                    const blob = await response.blob();
                    if (blob.size === 0) {
                        console.error('Empty blob received');
                        showNotification('Received empty file. Please try again.', 'error');
                        return;
                    }
                    
                    const downloadUrl = window.URL.createObjectURL(blob);
                    const downloadLink = document.createElement('a');
                    downloadLink.href = downloadUrl;
                    downloadLink.download = filename;
                    downloadLink.style.display = 'none';
                    
                    document.body.appendChild(downloadLink);
                    downloadLink.click();
                    
                    setTimeout(() => {
                        try {
                            if (downloadLink.parentNode) {
                                document.body.removeChild(downloadLink);
                            }
                            window.URL.revokeObjectURL(downloadUrl);
                        } catch (cleanupError) {
                            console.warn('Cleanup error (non-critical):', cleanupError);
                        }
                    }, 200);
                    
                    showNotification('Download started successfully!', 'success');
                } else if (response.status === 401) {
                    showNotification('Please log in to download this repository.', 'warning');
                    showLoginPrompt();
                } else if (response.status === 403) {
                    showNotification('Premium access required for this repository.', 'warning');
                } else if (response.status === 404) {
                    showNotification('Repository file not found.', 'error');
                } else {
                    const errorText = await response.text();
                    showNotification(`Download failed (${response.status}). Please try again.`, 'error');
                }
            } catch (networkError) {
                if (networkError.name === 'TypeError') {
                    showNotification('Network error: Unable to connect to server.', 'error');
                } else {
                    showNotification('Download failed. Please check your connection and try again.', 'error');
                }
            }
        }

        function showLoginPrompt() {
            const shouldRedirect = confirm('You need to log in to access this feature. Would you like to go to the login page?');
            if (shouldRedirect) {
                window.location.href = './Auth.html';
            }
        }

        // Purchase repository with Stripe payment integration
        async function purchaseRepository(repositoryId, repositoryTitle, price) {
            // Check if user is logged in
            if (!authManager.isAuthenticated()) {
                showNotification('Please log in to purchase repositories.', 'warning');
                authManager.showLoginPrompt();
                return;
            }
            
            // Prompt for GitHub username
            const githubUsername = prompt(`Enter your GitHub username to receive repository access:\n\nRepository: ${repositoryTitle}\nPrice: $${price.toFixed(2)}\n\nYou will receive a GitHub invitation email after payment.`);
            
            if (!githubUsername || githubUsername.trim() === '') {
                return; // User cancelled or entered empty string
            }
            
            try {
                showNotification('Processing payment request...', 'info');
                
                // Verify GitHub username first
                const verifyResponse = await fetch(`${backendBaseUrl}/api/payment/verify-github-username`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(githubUsername.trim())
                });
                
                if (!verifyResponse.ok) {
                    showNotification('Failed to verify GitHub username. Please try again.', 'error');
                    return;
                }
                
                const isValid = await verifyResponse.json();
                if (!isValid) {
                    showNotification(`GitHub username "${githubUsername}" not found. Please enter a valid username.`, 'error');
                    return;
                }
                
                // Create Stripe checkout session
                const response = await fetch(`${backendBaseUrl}/api/payment/create-checkout-session`, {
                    method: 'POST',
                    headers: {
                        ...authManager.getAuthHeaders(),
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        repositoryId: repositoryId,
                        githubUsername: githubUsername.trim()
                    })
                });
                
                if (!response.ok) {
                    const error = await response.text();
                    throw new Error(error || 'Failed to create checkout session');
                }
                
                const data = await response.json();
                
                // Redirect to Stripe Checkout
                if (typeof Stripe === 'undefined') {
                    console.error('Stripe.js not loaded. Check console for errors.');
                    showNotification('Payment system not loaded. Please check your internet connection and refresh the page.', 'error');
                    return;
                }
                
                console.log('Initializing Stripe with publishable key...');
                const stripe = Stripe(data.publishableKey);
                const result = await stripe.redirectToCheckout({
                    sessionId: data.sessionId
                });
                
                if (result.error) {
                    showNotification(result.error.message, 'error');
                }
                
            } catch (error) {
                console.error('Purchase error:', error);
                showNotification(`Purchase failed: ${error.message}`, 'error');
            }
        }
        
        // Legacy function kept for backwards compatibility (not used anymore)
        function contactAdmin(repositoryId, repositoryTitle) {
            showNotification('This repository now uses Stripe payment integration. Please use the Purchase button.', 'info');
        }

        function showNotification(message, type = 'info') {
            const notification = document.createElement('div');
            notification.className = `notification notification-${type}`;
            notification.innerHTML = `
                <i class="fas fa-${getNotificationIcon(type)}"></i>
                <span>${message}</span>
                <button class="notification-close" onclick="this.parentElement.remove()">
                    <i class="fas fa-times"></i>
                </button>
            `;
            
            if (!document.getElementById('notification-styles')) {
                const style = document.createElement('style');
                style.id = 'notification-styles';
                style.textContent = `
                    .notification {
                        position: fixed;
                        top: 20px;
                        right: 20px;
                        background: var(--bg-light);
                        border: 1px solid var(--glass-border);
                        border-radius: 8px;
                        padding: 16px 20px;
                        display: flex;
                        align-items: center;
                        gap: 12px;
                        box-shadow: var(--glass-shadow);
                        backdrop-filter: var(--glass-blur);
                        z-index: 9999;
                        min-width: 300px;
                        animation: slideInRight 0.3s ease;
                    }
                    .notification-success { border-color: var(--success); color: var(--success); }
                    .notification-error { border-color: var(--accent); color: var(--accent); }
                    .notification-warning { border-color: var(--warning); color: var(--warning); }
                    .notification-info { border-color: var(--info); color: var(--info); }
                    .notification-close {
                        background: none;
                        border: none;
                        color: var(--text-muted);
                        cursor: pointer;
                        padding: 4px;
                        margin-left: auto;
                    }
                    @keyframes slideInRight {
                        from { transform: translateX(100%); opacity: 0; }
                        to { transform: translateX(0); opacity: 1; }
                    }
                `;
                document.head.appendChild(style);
            }
            
            document.body.appendChild(notification);
            
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, 5000);
        }

        function getNotificationIcon(type) {
            switch (type) {
                case 'success': return 'check-circle';
                case 'error': return 'exclamation-circle';
                case 'warning': return 'exclamation-triangle';
                case 'info': 
                default: return 'info-circle';
            }
        }

        // Global search is handled by shared components
        // Removed duplicate GlobalSearch class

        // Navbar auth is handled by shared components
        function initNavbarAuth() {
            // Wait for shared components to load, then sync auth state
            setTimeout(() => {
                const currentUser = authManager.getCurrentUser();
                console.log('Auth state synced with shared components:', currentUser ? 'logged in' : 'logged out');
            }, 500);
        }

        document.addEventListener('DOMContentLoaded', () => {
            currentUser = getCurrentUser();
            loadRepositories();
            
            // Global search is handled by shared components
            // new GlobalSearch();
            
            initNavbarAuth();
        });
