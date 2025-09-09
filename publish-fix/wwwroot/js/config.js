/**
 * Portfolio Frontend Configuration
 * Centralized configuration for API endpoints, SSL settings, and deployment environments
 */

window.PortfolioConfig = {
    // Environment detection
    environment: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1' ? 'development' : 'production',
    
    // API Configuration
    api: {
        // Auto-detect backend URL based on current protocol and environment
        getBaseUrl: function() {
            // Check for WordPress environment first
            if (window.WordPressConfig) {
                return window.WordPressConfig.getApiUrl().replace('/api', '');
            }
            
            // Environment-specific configuration
            if (this.parent.environment === 'development') {
                // Development: Support both HTTP and HTTPS
                if (window.location.protocol === 'https:') {
                    return 'https://localhost:7151';  // HTTPS development port
                } else {
                    return 'http://localhost:7150';   // HTTP development port
                }
            } else {
                // Production: Always use HTTPS
                const hostname = window.location.hostname;
                const port = window.location.port;
                
                if (port && port !== '80' && port !== '443') {
                    return `https://${hostname}:${port}`;
                } else {
                    return `https://${hostname}`;
                }
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
    
    // SSL/Security Configuration
    ssl: {
        // Force HTTPS in production
        enforceHttps: true,
        
        // Redirect HTTP to HTTPS
        redirectHttpToHttps: function() {
            if (this.enforceHttps && 
                window.location.protocol === 'http:' && 
                PortfolioConfig.environment === 'production') {
                window.location.href = window.location.href.replace('http:', 'https:');
                return true;
            }
            return false;
        },
        
        // Check if connection is secure
        isSecureConnection: function() {
            return window.location.protocol === 'https:' || 
                   window.location.hostname === 'localhost' || 
                   window.location.hostname === '127.0.0.1';
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
        if (this.environment === 'production') {
            // Production settings
            console.log('Portfolio running in production mode');
            
            // Hide debug information
            if (!this.features.enableDebugMode()) {
                console.log = function() {}; // Disable console.log in production
            }
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
        
        // Warn about insecure connections in production
        if (this.environment === 'production' && !this.ssl.isSecureConnection()) {
            console.warn('⚠️ Insecure connection detected in production environment!');
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
                    if (!window.location.pathname.includes('Auth.html')) {
                        window.location.href = 'Auth.html';
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
            return window.location.protocol === 'https:' || 
                   (PortfolioConfig.environment === 'development' && 
                    (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'));
        }
    }
};

// Initialize configuration when script loads
PortfolioConfig.init();
