/**
 * Portfolio Frontend Configuration
 * Centralized configuration for API endpoints, SSL settings, and deployment environments
 */

window.PortfolioConfig = {
    // Enhanced environment detection - works with any deployment scenario
    environment: (function() {
        // Check for explicit environment override
        if (window.PORTFOLIO_ENV) {
            return window.PORTFOLIO_ENV;
        }
        
        // Auto-detect based on hostname patterns
        const hostname = window.location.hostname;
        
        // Development indicators
        if (hostname === 'localhost' || 
            hostname === '127.0.0.1' || 
            hostname.endsWith('.local') ||
            hostname.startsWith('192.168.') ||
            hostname.startsWith('10.') ||
            hostname.includes('dev') ||
            window.location.port && ['3000', '5000', '8080', '8000', '7150'].includes(window.location.port))
            return 'development';
        }
        
        // Everything else is production
        return 'production';
    })(),
    
    // API Configuration
    api: {
        // Auto-detect backend URL with multiple fallback strategies
        getBaseUrl: function() {
            // Priority 1: Explicit API_BASE_URL override from environment/injection
            if (window.API_BASE_URL) {
                console.log('🔧 Using API Base URL from environment override:', window.API_BASE_URL);
                return window.API_BASE_URL.replace(/\/$/, ''); // Remove trailing slash
            }
            
            // Priority 2: Check for API_URL in meta tags (alternative injection method)
            const metaApiUrl = document.querySelector('meta[name="api-base-url"]');
            if (metaApiUrl && metaApiUrl.content) {
                console.log('🔧 Using API Base URL from meta tag:', metaApiUrl.content);
                return metaApiUrl.content.replace(/\/$/, '');
            }
            
            // Priority 3: Environment-based auto-detection
            if (PortfolioConfig.environment === 'development' || 
                window.location.hostname === 'localhost' || 
                window.location.hostname === '127.0.0.1') {
                // Development: Always use HTTP on port 7150
                return 'http://localhost:7150';
            } else {
                // Production: Intelligent URL construction
                const protocol = window.location.protocol; // Use current protocol
                const hostname = window.location.hostname;
                const port = window.location.port;
                
                // For Azure Web Apps and most cloud platforms, API is served from same origin
                let baseUrl;
                if (port && port !== '80' && port !== '443') {
                    baseUrl = `${protocol}//${hostname}:${port}`;
                } else {
                    baseUrl = `${protocol}//${hostname}`;
                }
                
                console.log('🔧 Auto-detected production API base URL:', baseUrl);
                return baseUrl;
            }
        },
        
        // API endpoints
        endpoints: {
            auth: '/api/auth',
            products: '/api/products',
            publications: '/api/publications',
            solutions: '/api/solutions',
            repositories: '/api/repository',
            search: '/api/GlobalSearch',
            contact: '/api/contact',
            health: '/health'
        }
    },
    
    // SSL/Security Configuration - adaptive to deployment environment
    ssl: {
        // Dynamically determine if HTTPS should be enforced
        shouldEnforceHttps: function() {
            // Never enforce HTTPS - use HTTP for localhost, let deployment handle redirects
            return false;
        },
        
        // No HTTP to HTTPS redirect - deployment will handle this
        redirectHttpToHttps: function() {
            return false;
        },
        
        // Check if connection is secure or acceptable
        isSecureConnection: function() {
            return window.location.protocol === 'https:' || 
                   PortfolioConfig.environment === 'development' ||
                   window.location.hostname === 'localhost' || 
                   window.location.hostname === '127.0.0.1' ||
                   window.location.hostname.endsWith('.local');
        }
    },
    
    // Feature flags
    features: {
        // Enable development features only in dev environment
        enableDebugMode: function() {
            return PortfolioConfig.environment === 'development';
        },
        
        // Enable service worker for PWA features
        enableServiceWorker: true,
        
        // Enable analytics (only in production)
        enableAnalytics: function() {
            return PortfolioConfig.environment === 'production';
        }
    },
    
    // Initialize configuration
    init: function() {
        // Set up environment-specific settings
        console.log('🌍 Environment Detection:');
        console.log('  - Hostname:', window.location.hostname);
        console.log('  - Environment:', this.environment);
        console.log('  - API Base URL:', this.api.getBaseUrl());
        
        if (this.environment === 'production') {
            // Production settings
            console.log('✅ Portfolio running in production mode');
            
            // Don't disable console.log in production for now (for debugging)
            // if (!this.features.enableDebugMode()) {
            //     console.log = function() {}; // Disable console.log in production
            // }
        } else {
            // Development settings
            console.log('Portfolio running in development mode');
            console.log('API Base URL:', this.api.getBaseUrl());
            console.log('SSL Configuration:', this.ssl);
        }
        
        // Check and redirect to HTTPS if needed
        if (this.ssl.redirectHttpToHttps()) {
            return; // Page will redirect, don't continue initialization
        }
        
        // Warn about insecure connections when they might be a problem
        if (this.environment === 'production' && !this.ssl.isSecureConnection()) {
            console.warn('⚠️ Insecure connection detected in production environment!');
            console.warn('🔒 Consider using HTTPS for better security');
        }
        
        // Log SSL configuration
        if (this.environment === 'development') {
            console.log('🔒 SSL Configuration:', {
                enforceHttps: this.ssl.shouldEnforceHttps(),
                isSecure: this.ssl.isSecureConnection(),
                protocol: window.location.protocol
            });
        }
        
        // Set up global error handling for API requests
        this.setupGlobalErrorHandling();
        
        // Initialize service worker if enabled
        if (this.features.enableServiceWorker && 'serviceWorker' in navigator) {
            this.initServiceWorker();
        }
    },
    
    // Set up global error handling for fetch requests
    setupGlobalErrorHandling: function() {
        // Store original fetch function
        const originalFetch = window.fetch;
        
        window.fetch = function(...args) {
            return originalFetch.apply(this, args)
                .catch(error => {
                    // Handle network errors
                    if (error.name === 'TypeError' && error.message.includes('Failed to fetch')) {
                        console.error('🔗 Network error - Check your connection and SSL certificates');
                        
                        // If on HTTPS and getting network error, suggest HTTP fallback for development
                        if (window.location.protocol === 'https:' && PortfolioConfig.environment === 'development') {
                            console.warn('💡 Try accessing via HTTP for development: http://localhost:7150');
                        }
                    }
                    throw error;
                });
        };
    },
    
    // Initialize service worker
    initServiceWorker: function() {
        navigator.serviceWorker.register('/sw.js')
            .then(registration => {
                console.log('✅ Service Worker registered:', registration);
            })
            .catch(error => {
                console.log('❌ Service Worker registration failed:', error);
            });
    },
    
    // Utility functions
    utils: {
        // Build full API URL
        buildApiUrl: function(endpoint) {
            const baseUrl = PortfolioConfig.api.getBaseUrl();
            const apiPath = PortfolioConfig.api.endpoints[endpoint] || endpoint;
            return baseUrl + apiPath;
        },
        
        // Make authenticated API request
        apiRequest: async function(endpoint, options = {}) {
            const url = this.buildApiUrl(endpoint);
            const token = localStorage.getItem('authToken');
            
            const defaultOptions = {
                headers: {
                    'Content-Type': 'application/json',
                    ...(token && { 'Authorization': `Bearer ${token}` })
                }
            };
            
            const finalOptions = {
                ...defaultOptions,
                ...options,
                headers: {
                    ...defaultOptions.headers,
                    ...options.headers
                }
            };
            
            try {
                const response = await fetch(url, finalOptions);
                
                // Handle authentication errors
                if (response.status === 401) {
                    // Token expired or invalid
                    localStorage.removeItem('authToken');
                    localStorage.removeItem('userInfo');
                    
                    // Redirect to auth page if not already there
                    if (!window.location.pathname.includes('/Auth')) {
                        window.location.href = '/Auth';
                        return;
                    }
                }
                
                return response;
            } catch (error) {
                console.error('API Request failed:', error);
                throw error;
            }
        },
        
        // Check if current environment supports HTTPS
        supportsHttps: function() {
            return PortfolioConfig.ssl.isSecureConnection();
        },
        
        // Get the appropriate protocol for API requests
        getApiProtocol: function() {
            // In production, always try to use HTTPS first
            if (PortfolioConfig.environment === 'production') {
                return 'https:';
            }
            
            // In development, use the current page protocol
            return window.location.protocol;
        }
    }
};

// Initialize configuration when script loads
PortfolioConfig.init();
