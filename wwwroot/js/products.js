// Backend URL configuration - use HTTP on localhost:7150
        const getBackendBaseUrl = () => {
            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                return 'http://localhost:7150';
            }
            // For production, use same protocol and hostname
            return `${window.location.protocol}//${window.location.hostname}`;
        };
        const backendBaseUrl = getBackendBaseUrl();
        const productsApiUrl = `${backendBaseUrl}/api/products`;

        let currentPage = 1;
        let pageSize = 12;
        let products = [];
        let filteredProducts = [];
        let deepLinkHandled = false;

        // Helper function to get full image URL with mixed content handling
        function getFullImageUrl(imageUrl) {
            if (!imageUrl || imageUrl.trim() === '') return null;
            // If URL starts with http/https, return as is
            if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) {
                return imageUrl;
            }
            // For development: Try to use relative URL first to avoid mixed content
            // If the image exists on the frontend server, use relative path
            // Otherwise, fall back to backend URL (will trigger mixed content warning)
            return `${backendBaseUrl}${imageUrl}`;
        }
        
        // Helper function to handle image loading errors and fallbacks
        function handleImageError(imgElement, originalSrc) {
            // Try to load from frontend server as fallback
            const relativePath = originalSrc.replace(`${backendBaseUrl}`, '');
            const frontendUrl = window.location.origin + relativePath;
            
            // Create a test image to check if it exists on frontend
            const testImg = new Image();
            testImg.onload = function() {
                imgElement.src = frontendUrl;
                imgElement.style.display = 'block';
            };
            testImg.onerror = function() {
                // If neither backend nor frontend has the image, show placeholder
                imgElement.style.display = 'none';
                if (imgElement.nextElementSibling) {
                    imgElement.nextElementSibling.style.display = 'flex';
                }
            };
            testImg.src = frontendUrl;
        }

        // Theme is handled by shared components
        // No theme initialization needed here

        // Load products
        async function loadProducts() {
            try {
                const response = await fetch(productsApiUrl);
                if (response.ok) {
                    products = await response.json();
                    filteredProducts = [...products];
                    populateDomainFilter();
                    updateStats();
                    handleDeepLinkPaging();
                    renderProducts();
                    renderPagination();
                } else {
                    throw new Error('Failed to load products');
                }
            } catch (error) {
                console.error('Error loading products:', error);
                showErrorState();
            }
        }

        // Ensure deep-linked product appears on the current page before initial render
        function handleDeepLinkPaging() {
            const urlParams = new URLSearchParams(window.location.search);
            console.log('Products handleDeepLinkPaging called. URL params:', window.location.search);
            
            // Handle individual product ID
            const idParam = urlParams.get('id');
            if (idParam) {
                console.log('Found product ID parameter:', idParam);
                const id = parseInt(idParam, 10);
                const idx = filteredProducts.findIndex(p => p.id === id || p.Id === id);
                if (idx >= 0) {
                    currentPage = Math.floor(idx / pageSize) + 1;
                }
                return;
            }
            
            // Handle domain filtering
            const domain = urlParams.get('domain');
            console.log('Checking for domain parameter. Found:', domain);
            if (domain) {
                console.log('Deep link domain filter detected:', domain);
                const domainFilter = document.getElementById('category-filter');
                console.log('Domain filter element found:', domainFilter);
                if (domainFilter) {
                    console.log('Setting domain filter value to:', domain);
                    // Set the domain filter value
                    domainFilter.value = domain;
                    console.log('Filter value set. Current value:', domainFilter.value);
                    // Trigger filtering
                    console.log('Calling applyFilters...');
                    applyFilters();
                    console.log('Applied domain filter from URL:', domain);
                } else {
                    console.warn('Domain filter element not found, will retry after DOM loads');
                    // Retry after a short delay to ensure DOM is ready
                    setTimeout(() => {
                        console.log('Retrying domain filter application...');
                        const retryDomainFilter = document.getElementById('category-filter');
                        if (retryDomainFilter) {
                            retryDomainFilter.value = domain;
                            applyFilters();
                            console.log('Applied domain filter from URL (retry):', domain);
                        } else {
                            console.error('Still could not find category-filter element after retry');
                        }
                    }, 500);
                }
            } else {
                console.log('No domain parameter found in URL');
            }
        }

        // Helper function to extract key features from description
        function extractKeyFeatures(longDescription) {
            if (!longDescription) return [];
            
            // Look for bullet points, numbered lists, or key phrases
            const features = [];
            const lines = longDescription.split(/[.!?\n]/);
            
            for (const line of lines) {
                const trimmed = line.trim();
                if (trimmed.length > 10 && trimmed.length < 100) {
                    // Look for feature-like phrases
                    if (trimmed.match(/^(\d+\.|â€¢|-|\*|Features?:|Benefits?:|Includes?:)/i) || 
                        trimmed.includes('support') || 
                        trimmed.includes('design') ||
                        trimmed.includes('manage') ||
                        trimmed.includes('integrate') ||
                        trimmed.includes('custom') ||
                        trimmed.includes('secure') ||
                        trimmed.includes('responsive') ||
                        trimmed.includes('real-time')) {
                        features.push(trimmed.replace(/^(\d+\.|â€¢|-|\*|Features?:|Benefits?:|Includes?:)\s*/i, ''));
                        if (features.length >= 4) break;
                    }
                }
            }
            
            return features;
        }

        // Update statistics
        function updateStats() {
            const totalCount = filteredProducts.length;
            const domains = [...new Set(filteredProducts.map(product => product.domain))].length;
            
            document.getElementById('total-count').textContent = totalCount;
            document.getElementById('categories-count').textContent = domains;
            
            // Find the most recent update date
            const lastUpdate = filteredProducts.reduce((latest, product) => {
                const productDate = new Date(product.createdAt);
                return productDate > latest ? productDate : latest;
            }, new Date(0));
            
            if (lastUpdate.getTime() > 0) {
                document.getElementById('last-updated').textContent = lastUpdate.toLocaleDateString();
            }
        }

        // Render products
        function renderProducts() {
            const container = document.getElementById('products-container');
            
            if (filteredProducts.length === 0) {
                container.innerHTML = `
                    <div class="empty-state">
                        <i class="fas fa-search"></i>
                        <h3>No products found</h3>
                        <p>Try adjusting your search criteria or filters.</p>
                    </div>
                `;
                document.getElementById('pagination').style.display = 'none';
                return;
            }

            const startIndex = (currentPage - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            const pageItems = filteredProducts.slice(startIndex, endIndex);

                const productsHtml = pageItems.map(product => {
                const fullImageUrl = getFullImageUrl(product.imageUrl);
                const imageHtml = fullImageUrl ? `
                    <div class="product-image-container" style="position: relative; width: 100%; height: 220px; margin-bottom: 16px;">
                        <img src="${fullImageUrl}" alt="${product.title}" style="width: 100%; height: 100%; object-fit: cover; border-radius: 12px; border: 1px solid var(--glass-border);" 
                             onerror="handleImageError(this, '${fullImageUrl}')"
                             onload="this.style.display='block'; if(this.nextElementSibling) this.nextElementSibling.style.display='none';"
                        >
                        <div style="display: none; width: 100%; height: 100%; background: linear-gradient(135deg, var(--glass-bg), var(--bg-light)); border-radius: 12px; flex-direction: column; align-items: center; justify-content: center; border: 1px solid var(--glass-border); position: absolute; top: 0; left: 0;">
                            <i class="fas fa-image" style="color: var(--primary); font-size: 3rem; margin-bottom: 8px; opacity: 0.6;"></i>
                            <span style="color: var(--text-muted); font-size: 0.9rem;">Image Unavailable</span>
                            <span style="color: var(--text-muted); font-size: 0.8rem; margin-top: 4px;">Mixed content blocked</span>
                        </div>
                    </div>
                ` : `
                    <div class="product-image-container" style="width: 100%; height: 220px; background: linear-gradient(135deg, var(--glass-bg), var(--bg-light)); border-radius: 12px; display: flex; flex-direction: column; align-items: center; justify-content: center; border: 1px solid var(--glass-border); margin-bottom: 16px;">
                        <i class="fas fa-cube" style="color: var(--primary); font-size: 3rem; margin-bottom: 8px; opacity: 0.6;"></i>
                        <span style="color: var(--text-muted); font-size: 0.9rem;">Product Image</span>
                    </div>
                `;
                
                // Extract key points from long description
                const keyFeatures = extractKeyFeatures(product.longDescription);
                
                return `
                <div class="product-card" id="product-${product.id}">
                    ${imageHtml}
                    
                    <div class="product-card-inner">
                        <div class="product-content-area">
                            <div class="product-header">
                                <span class="product-category">${product.domain}</span>
                                <span class="product-status" style="background: var(--success); color: var(--bg-light); font-size: 0.75rem; font-weight: 600; padding: 4px 8px; border-radius: 12px;">
                                    <i class="fas fa-check-circle" style="margin-right: 4px;"></i>Available
                                </span>
                            </div>
                            
                            <h3 class="product-title">${product.title}</h3>
                            <p class="product-description">${product.shortDescription}</p>
                            
                            ${keyFeatures.length > 0 ? `
                                <ul class="product-features">
                                    ${keyFeatures.slice(0, 3).map(feature => 
                                        `<li>${feature}</li>`
                                    ).join('')}
                                </ul>
                            ` : `
                                <div class="product-preview" style="background: var(--glass-bg); padding: 12px; border-radius: 8px; margin-bottom: 16px; border: 1px solid var(--glass-border);">
                                    <p style="color: var(--text-light); font-size: 0.9rem; margin: 0; line-height: 1.4;">
                                        ${product.longDescription.substring(0, 120)}${product.longDescription.length > 120 ? '...' : ''}
                                    </p>
                                </div>
                            `}
                            
                            <div class="product-spacer"></div>
                        </div>
                        
                        <div class="product-meta">
                            <span style="display: flex; align-items: center; gap: 4px;">
                                <i class="fas fa-calendar" style="color: var(--primary); font-size: 0.8rem;"></i>
                                ${new Date(product.createdAt).toLocaleDateString()}
                            </span>
                            <span style="display: flex; align-items: center; gap: 4px;">
                                <i class="fas fa-building" style="color: var(--info); font-size: 0.8rem;"></i>
                                ${product.domain}
                            </span>
                        </div>
                        
                        <div class="product-actions">
                            <button class="btn btn-outline" onclick="viewProduct(${product.id})">
                                <i class="fas fa-info-circle"></i>
                                Learn More
                            </button>
                            <button class="btn btn-primary" onclick="viewCodebase(${product.id})">
                                <i class="fas fa-code-branch"></i>
                                View Codebase
                            </button>
                        </div>
                    </div>
                </div>
                `;
            }).join('');

            container.innerHTML = `
                <div class="products-grid">
                    ${productsHtml}
                </div>
            `;

            // Deep link handling: open and scroll to specific product if provided
            if (!deepLinkHandled) {
                const urlParams = new URLSearchParams(window.location.search);
                const targetId = urlParams.get('id');
                if (targetId) {
                    const el = document.getElementById(`product-${targetId}`);
                    if (el) {
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        try { viewProduct(parseInt(targetId, 10)); } catch {}
                        deepLinkHandled = true;
                    }
                }
            }

            document.getElementById('pagination').style.display = filteredProducts.length > pageSize ? 'flex' : 'none';
        }

        // Render pagination with enhanced features
        function renderPagination() {
            const totalPages = Math.ceil(filteredProducts.length / pageSize);
            const paginationContainer = document.getElementById('pagination');
            
            if (totalPages <= 1) {
                hidePagination();
                return;
            }

            let paginationHtml = '';
            
            // Previous button with better styling
            paginationHtml += `
                <button class="pagination-btn ${currentPage === 1 ? 'disabled' : ''}" 
                        onclick="changePage(${currentPage - 1})" 
                        aria-label="Previous page"
                        ${currentPage === 1 ? 'disabled tabindex="-1"' : ''}>
                    <i class="fas fa-chevron-left"></i>
                    <span class="pagination-text">Prev</span>
                </button>
            `;

            // Calculate visible page range
            const maxVisiblePages = window.innerWidth <= 768 ? 3 : 5;
            let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
            let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

            // Adjust range if needed
            if (endPage - startPage + 1 < maxVisiblePages) {
                startPage = Math.max(1, endPage - maxVisiblePages + 1);
            }

            // First page and ellipsis
            if (startPage > 1) {
                paginationHtml += `<button class="pagination-btn" onclick="changePage(1)" aria-label="Page 1">1</button>`;
                if (startPage > 2) {
                    paginationHtml += `<span class="pagination-ellipsis" aria-hidden="true">...</span>`;
                }
            }

            // Page number buttons
            for (let i = startPage; i <= endPage; i++) {
                paginationHtml += `
                    <button class="pagination-btn ${i === currentPage ? 'active' : ''}" 
                            onclick="changePage(${i})"
                            aria-label="Page ${i}"
                            ${i === currentPage ? 'aria-current="page"' : ''}>${i}</button>
                `;
            }

            // Last page and ellipsis
            if (endPage < totalPages) {
                if (endPage < totalPages - 1) {
                    paginationHtml += `<span class="pagination-ellipsis" aria-hidden="true">...</span>`;
                }
                paginationHtml += `<button class="pagination-btn" onclick="changePage(${totalPages})" aria-label="Page ${totalPages}">${totalPages}</button>`;
            }

            // Next button with better styling
            paginationHtml += `
                <button class="pagination-btn ${currentPage === totalPages ? 'disabled' : ''}" 
                        onclick="changePage(${currentPage + 1})"
                        aria-label="Next page" 
                        ${currentPage === totalPages ? 'disabled tabindex="-1"' : ''}>
                    <span class="pagination-text">Next</span>
                    <i class="fas fa-chevron-right"></i>
                </button>
            `;

            // Add pagination info on larger screens
            if (window.innerWidth > 768) {
                const startItem = ((currentPage - 1) * pageSize) + 1;
                const endItem = Math.min(currentPage * pageSize, filteredProducts.length);
                const totalItems = filteredProducts.length;
                
                paginationHtml += `
                    <div class="pagination-info">
                        <i class="fas fa-info-circle"></i>
                        <span>Showing ${startItem}-${endItem} of ${totalItems} products</span>
                    </div>
                `;
            }

            paginationContainer.innerHTML = paginationHtml;
            paginationContainer.style.display = 'flex';
        }

        // Hide pagination
        function hidePagination() {
            document.getElementById('pagination').style.display = 'none';
        }

        // Enhanced page change function
        function changePage(page) {
            const totalPages = Math.ceil(filteredProducts.length / pageSize);
            if (page < 1 || page > totalPages || page === currentPage) return;
            
            // Add loading state briefly
            const container = document.getElementById('products-container');
            const originalContent = container.innerHTML;
            container.innerHTML = `
                <div class="loading-container">
                    <div class="loading-spinner"></div>
                </div>
            `;
            
            // Smooth transition
            setTimeout(() => {
                currentPage = page;
                renderProducts();
                renderPagination();
                
                // Smooth scroll to top with offset for navbar
                const navbarHeight = document.querySelector('.navbar')?.offsetHeight || 70;
                window.scrollTo({
                    top: Math.max(0, document.querySelector('.section-title')?.offsetTop - navbarHeight - 20 || 0),
                    behavior: 'smooth'
                });
            }, 200);
        }

        // Apply filters
        function applyFilters() {
            const searchTerm = document.getElementById('search-input').value.toLowerCase();
            const categoryFilter = document.getElementById('category-filter').value;
            const sortFilter = document.getElementById('sort-filter').value;

            filteredProducts = products.filter(product => {
                const matchesSearch = !searchTerm || 
                    product.title.toLowerCase().includes(searchTerm) ||
                    product.shortDescription.toLowerCase().includes(searchTerm) ||
                    product.longDescription.toLowerCase().includes(searchTerm) ||
                    product.domain.toLowerCase().includes(searchTerm);
                
                const matchesCategory = !categoryFilter || product.domain === categoryFilter;

                return matchesSearch && matchesCategory;
            });

            // Sort results
            filteredProducts.sort((a, b) => {
                switch (sortFilter) {
                    case 'newest':
                        return new Date(b.createdAt) - new Date(a.createdAt);
                    case 'oldest':
                        return new Date(a.createdAt) - new Date(b.createdAt);
                    case 'name':
                        return a.title.localeCompare(b.title);
                    case 'price-low':
                    case 'price-high':
                        return a.title.localeCompare(b.title); // Fallback to name sorting
                    default:
                        return 0;
                }
            });

            currentPage = 1;
            updateStats();
            renderProducts();
            renderPagination();
        }

        // Populate domain filter options
        function populateDomainFilter() {
            const domainFilter = document.getElementById('category-filter');
            const uniqueDomains = [...new Set(products.map(product => product.domain))].sort();
            
            // Clear existing options (except "All Domains")
            while (domainFilter.children.length > 1) {
                domainFilter.removeChild(domainFilter.lastChild);
            }
            
            // Add domain options
            uniqueDomains.forEach(domain => {
                const option = document.createElement('option');
                option.value = domain;
                option.textContent = domain;
                domainFilter.appendChild(option);
            });
        }

        // View product details
        async function viewProduct(productId) {
            try {
                const response = await fetch(`${productsApiUrl}/${productId}`);
                if (response.ok) {
                    const product = await response.json();
                    showProductModal(product);
                } else {
                    throw new Error('Failed to load product details');
                }
            } catch (error) {
                console.error('Error loading product details:', error);
                alert('Failed to load product details. Please try again.');
            }
        }

        // Show product modal
        function showProductModal(product) {
            const fullImageUrl = getFullImageUrl(product.imageUrl);
            const keyFeatures = extractKeyFeatures(product.longDescription);
            
            document.getElementById('modal-title').textContent = product.title;
            document.getElementById('modal-body').innerHTML = `
                <div style="margin-bottom: 20px;">
                    ${fullImageUrl ? `
                        <div style="margin-bottom: 20px; position: relative;">
                            <img src="${fullImageUrl}" alt="${product.title}" style="width: 100%; height: 250px; object-fit: cover; border-radius: 12px; border: 1px solid var(--glass-border);" 
                                 onerror="handleImageError(this, '${fullImageUrl}')"
                                 onload="this.style.display='block'; if(this.nextElementSibling) this.nextElementSibling.style.display='none';"
                            >
                            <div style="display: none; width: 100%; height: 250px; background: linear-gradient(135deg, var(--glass-bg), var(--bg-light)); border-radius: 12px; flex-direction: column; align-items: center; justify-content: center; border: 1px solid var(--glass-border); position: absolute; top: 0; left: 0;">
                                <i class="fas fa-image" style="color: var(--primary); font-size: 4rem; margin-bottom: 12px; opacity: 0.6;"></i>
                                <span style="color: var(--text-muted); font-size: 1rem; margin-bottom: 4px;">Image Unavailable</span>
                                <span style="color: var(--text-muted); font-size: 0.9rem;">Mixed content blocked by browser</span>
                            </div>
                        </div>
                    ` : ''}
                    
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; flex-wrap: wrap; gap: 12px;">
                        <span style="background: var(--primary); color: var(--bg-light); padding: 4px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 600; text-transform: uppercase;">${product.domain}</span>
                        <span style="background: var(--success); color: var(--bg-light); font-size: 0.8rem; font-weight: 600; padding: 4px 8px; border-radius: 12px;">
                            <i class="fas fa-check-circle" style="margin-right: 4px;"></i>Available
                        </span>
                    </div>
                    
                    <h4 style="color: var(--text); margin-bottom: 12px; font-size: 1.1rem;">Short Description:</h4>
                    <p style="color: var(--text-light); margin-bottom: 20px; line-height: 1.6;">${product.shortDescription}</p>
                    
                    <h4 style="color: var(--text); margin-bottom: 12px; font-size: 1.1rem;">Detailed Description:</h4>
                    <div style="background: var(--glass-bg); padding: 16px; border-radius: 12px; margin-bottom: 20px; border: 1px solid var(--glass-border);">
                        <p style="color: var(--text-light); line-height: 1.6; margin: 0;">${product.longDescription}</p>
                    </div>
                    
                    ${keyFeatures.length > 0 ? `
                        <h4 style="color: var(--text); margin-bottom: 12px; font-size: 1.1rem;">Key Features:</h4>
                        <ul style="list-style: none; margin-bottom: 20px;">
                            ${keyFeatures.map(feature => 
                                `<li style="margin-bottom: 8px; display: flex; align-items: center; gap: 8px; color: var(--text-light);">
                                    <span style="color: var(--success); font-weight: bold;">âœ“</span>
                                    ${feature}
                                </li>`
                            ).join('')}
                        </ul>
                    ` : ''}
                    
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px;">
                        <div>
                            <strong style="color: var(--text);">Domain:</strong><br>
                            <span style="color: var(--text-light);">${product.domain}</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Created:</strong><br>
                            <span style="color: var(--text-light);">${new Date(product.createdAt).toLocaleDateString()}</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Last Updated:</strong><br>
                            <span style="color: var(--text-light);">${product.updatedAt ? new Date(product.updatedAt).toLocaleDateString() : 'Not updated'}</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Delivery:</strong><br>
                            <span style="color: var(--text-light);">Custom Timeline</span>
                        </div>
                    </div>
                    
                    <div style="display: flex; gap: 12px; margin-top: 24px;">
                        <button class="btn btn-primary" onclick="requestProduct(${product.id})" style="width: 100%;">
                            <i class="fas fa-envelope"></i>
                            Contact Admin
                        </button>
                    </div>
                </div>
            `;
            
            document.getElementById('product-modal').classList.add('show');
        }

        // Close modal
        function closeModal() {
            document.getElementById('product-modal').classList.remove('show');
        }

        // Purchase product
        function purchaseProduct(productId) {
            // In a real application, this would integrate with a payment system
            alert(`Redirecting to payment for product #${productId}. This would integrate with a payment processor like Stripe or PayPal.`);
        }

        // Request product quote
        function requestProduct(productId) {
            // Redirect to contact page with product information
            window.location.href = `Contact.html?product=${productId}&action=quote`;
        }

        // View codebase for product
        function viewCodebase(productId) {
            // Redirect to Repository page filtered by product
            window.location.href = `Repository.html?productId=${productId}`;
        }

        // Show error state
        function showErrorState() {
            document.getElementById('products-container').innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-exclamation-triangle" style="color: var(--accent);"></i>
                    <h3>Failed to load products</h3>
                    <p>Please try again later or contact support if the problem persists.</p>
                </div>
            `;
            document.getElementById('pagination').style.display = 'none';
        }

        // Event listeners
        document.getElementById('search-input').addEventListener('input', debounce(applyFilters, 300));
        document.getElementById('category-filter').addEventListener('change', applyFilters);
        document.getElementById('sort-filter').addEventListener('change', applyFilters);

        // Close modal when clicking outside
        document.getElementById('product-modal').addEventListener('click', (e) => {
            if (e.target.id === 'product-modal') {
                closeModal();
            }
        });

        // Debounce function
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

        // Authentication state management
        const authApiUrl = `${backendBaseUrl}/api/auth`;
        
        function checkAuthState() {
            const token = localStorage.getItem('authToken');
            const userInfo = localStorage.getItem('userInfo');
            
            if (token && userInfo) {
                try {
                    const user = JSON.parse(userInfo);
                    const expiresAt = new Date(user.expiresAt);
                    
                    if (expiresAt > new Date()) {
                        showLoggedInState(user);
                        return true;
                    } else {
                        clearAuthData();
                    }
                } catch (error) {
                    clearAuthData();
                }
            }
            
            showLoggedOutState();
            return false;
        }
        
        function showLoggedInState(user) {
            const loggedOutSection = document.getElementById('logged-out-section');
            const loggedInSection = document.getElementById('logged-in-section');
            const userAvatar = document.getElementById('user-avatar');
            const userName = document.getElementById('user-name');
            
            if (loggedOutSection) loggedOutSection.style.display = 'none';
            if (loggedInSection) loggedInSection.style.display = 'flex';
            
            if (userAvatar) {
                const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();
                userAvatar.textContent = initials;
            }
            if (userName) {
                userName.textContent = `${user.firstName || ''} ${user.lastName || ''}`;
            }
        }
        
        function showLoggedOutState() {
            const loggedOutSection = document.getElementById('logged-out-section');
            const loggedInSection = document.getElementById('logged-in-section');
            
            if (loggedOutSection) loggedOutSection.style.display = 'block';
            if (loggedInSection) loggedInSection.style.display = 'none';
        }
        
        function clearAuthData() {
            localStorage.removeItem('authToken');
            localStorage.removeItem('userInfo');
        }
        
        // Logout functionality - handled by shared components
        // The logout button is part of the shared navigation and will be handled there
        
        // Global Search Functionality
        // Search state management
        let searchTimeout;
        let currentSearchIndex = -1;
        let searchResults = [];
        
        // Initialize application
        document.addEventListener('DOMContentLoaded', () => {
            checkAuthState();
            loadProducts();
            initializeGlobalSearch();
        });
        
        // Check auth state on page focus (in case user logged in/out in another tab)
        window.addEventListener('focus', () => {
            checkAuthState();
        });
        
        function initializeGlobalSearch() {
            const searchInput = document.getElementById('global-search-input');
            const suggestionsContainer = document.getElementById('search-suggestions');
            
            if (!searchInput || !suggestionsContainer) {
                // Global search elements not found - this is expected for standalone pages
                // The search functionality will be handled by the shared navigation component
                return;
            }
            
            // Search input event listeners
            searchInput.addEventListener('input', handleSearchInput);
            searchInput.addEventListener('keydown', handleSearchKeydown);
            searchInput.addEventListener('focus', handleSearchFocus);
            
            // Hide suggestions when clicking outside
            document.addEventListener('click', function(event) {
                if (!searchInput.contains(event.target) && !suggestionsContainer.contains(event.target)) {
                    hideSuggestions();
                }
            });
        }
        
        // Handle search input with debouncing
        function handleSearchInput(event) {
            const query = event.target.value.trim();
            
            // Clear previous timeout
            if (searchTimeout) {
                clearTimeout(searchTimeout);
            }
            
            // Reset search index
            currentSearchIndex = -1;
            
            if (query.length === 0) {
                hideSuggestions();
                return;
            }
            
            if (query.length < 2) {
                showMinimumCharsMessage();
                return;
            }
            
            // Debounce search requests
            searchTimeout = setTimeout(() => {
                performSearch(query);
            }, 300);
        }
        
        // Handle keyboard navigation in search
        function handleSearchKeydown(event) {
            const suggestionsContainer = document.getElementById('search-suggestions');
            const suggestions = suggestionsContainer.querySelectorAll('.suggestion-item');
            
            switch(event.key) {
                case 'ArrowDown':
                    event.preventDefault();
                    currentSearchIndex = Math.min(currentSearchIndex + 1, suggestions.length - 1);
                    updateSelectedSuggestion(suggestions);
                    break;
                    
                case 'ArrowUp':
                    event.preventDefault();
                    currentSearchIndex = Math.max(currentSearchIndex - 1, -1);
                    updateSelectedSuggestion(suggestions);
                    break;
                    
                case 'Enter':
                    event.preventDefault();
                    if (currentSearchIndex >= 0 && suggestions[currentSearchIndex]) {
                        suggestions[currentSearchIndex].click();
                    } else {
                        // Perform fallback search
                        performFallbackSearch(event.target.value.trim());
                    }
                    break;
                    
                case 'Escape':
                    event.preventDefault();
                    hideSuggestions();
                    event.target.blur();
                    break;
            }
        }
        
        // Handle search input focus
        function handleSearchFocus(event) {
            const query = event.target.value.trim();
            if (query.length >= 2 && searchResults.length > 0) {
                showSuggestions();
            }
        }
        
        // Perform search API call
        async function performSearch(query) {
            try {
                showLoadingState();
                
                const response = await fetch(`${backendBaseUrl}/api/GlobalSearch?query=${encodeURIComponent(query)}&pageSize=8`);
                
                if (!response.ok) {
                    throw new Error(`Search request failed: ${response.status}`);
                }
                
                const data = await response.json();
                searchResults = data.results || [];
                
                if (searchResults.length === 0) {
                    showNoResultsMessage(query);
                } else {
                    displaySearchResults(searchResults, query);
                }
                
            } catch (error) {
                console.error('Search error:', error);
                showErrorMessage();
            }
        }
        
        // Display search results
        function displaySearchResults(results, query) {
            const suggestionsContainer = document.getElementById('search-suggestions');
            
            const groupedResults = groupResultsByType(results);
            let html = '';
            
            // Display results by category
            Object.keys(groupedResults).forEach(type => {
                const items = groupedResults[type];
                if (items.length > 0) {
                    items.forEach(item => {
                        const icon = getIconForType(item.type);
                        const highlightedTitle = highlightText(item.title, query);
                        const meta = getMetaForItem(item);
                        
                        html += `
                            <div class="suggestion-item" onclick="navigateToResult('${item.type}', '${item.id}', '${item.url || ''}')">
                                <div class="suggestion-icon">
                                    <i class="${icon}"></i>
                                </div>
                                <div class="suggestion-content">
                                    <div class="suggestion-title">${highlightedTitle}</div>
                                    <div class="suggestion-meta">${meta}</div>
                                </div>
                            </div>
                        `;
                    });
                }
            });
            
            // Add "View All Results" option
            html += `
                <div class="suggestion-item" onclick="performFallbackSearch('${query}')" style="border-top: 1px solid var(--glass-border); margin-top: 8px; padding-top: 12px;">
                    <div class="suggestion-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">View all results for "${query}"</div>
                        <div class="suggestion-meta">See complete search results</div>
                    </div>
                </div>
            `;
            
            suggestionsContainer.innerHTML = html;
            showSuggestions();
        }
        
        // Group results by type
        function groupResultsByType(results) {
            const grouped = {};
            results.forEach(result => {
                if (!grouped[result.type]) {
                    grouped[result.type] = [];
                }
                grouped[result.type].push(result);
            });
            return grouped;
        }
        
        // Get icon for result type
        function getIconForType(type) {
            const icons = {
                'product': 'fas fa-cube',
                'publication': 'fas fa-book',
                'solution': 'fas fa-lightbulb',
                'repository': 'fas fa-code-branch',
                'page': 'fas fa-file-alt'
            };
            return icons[type] || 'fas fa-search';
        }
        
        // Get meta information for item
        function getMetaForItem(item) {
            switch(item.type) {
                case 'product':
                    return `Product â€¢ ${item.domain || 'Software'}`;
                case 'publication':
                    return `Publication â€¢ ${item.publishDate ? new Date(item.publishDate).getFullYear() : ''}`;
                case 'solution':
                    return `Solution â€¢ ${item.category || 'Business'}`;
                case 'repository':
                    return `Repository â€¢ ${item.language || 'Code'}`;
                case 'page':
                    return `Page â€¢ ${item.section || 'Content'}`;
                default:
                    return item.description || '';
            }
        }
        
        // Highlight matching text
        function highlightText(text, query) {
            if (!query || !text) return text;
            
            const regex = new RegExp(`(${escapeRegex(query)})`, 'gi');
            return text.replace(regex, '<strong>$1</strong>');
        }
        
        // Escape regex special characters
        function escapeRegex(string) {
            return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
        
        // Navigate to search result
        function navigateToResult(type, id, url) {
            hideSuggestions();
            
            if (url) {
                window.location.href = url;
                return;
            }
            
            // Default navigation based on type
            switch(type) {
                case 'product':
                    window.location.href = `Products#product-${id}`;
                    break;
                case 'publication':
                    window.location.href = `Publications#publication-${id}`;
                    break;
                case 'solution':
                    window.location.href = `/solutions?id=${id}`;
                    break;
                case 'repository':
                    window.location.href = `Repository#repo-${id}`;
                    break;
                default:
                    window.location.href = `SearchResults?q=${encodeURIComponent(document.getElementById('global-search-input').value)}&type=${type}&id=${id}`;
            }
        }
        
        // Perform fallback search (navigate to search results page)
        function performFallbackSearch(query) {
            if (!query || query.trim().length === 0) return;
            
            hideSuggestions();
            window.location.href = `SearchResults?q=${encodeURIComponent(query.trim())}`;
        }
        
        // Update selected suggestion highlighting
        function updateSelectedSuggestion(suggestions) {
            suggestions.forEach((suggestion, index) => {
                if (index === currentSearchIndex) {
                    suggestion.classList.add('selected');
                } else {
                    suggestion.classList.remove('selected');
                }
            });
        }
        
        // Show suggestions container
        function showSuggestions() {
            document.getElementById('search-suggestions').classList.add('show');
        }
        
        // Hide suggestions container
        function hideSuggestions() {
            document.getElementById('search-suggestions').classList.remove('show');
            currentSearchIndex = -1;
        }
        
        // Show loading state
        function showLoadingState() {
            const suggestionsContainer = document.getElementById('search-suggestions');
            suggestionsContainer.innerHTML = `
                <div class="suggestion-item" style="justify-content: center; cursor: default;">
                    <div class="suggestion-icon">
                        <i class="fas fa-spinner fa-spin"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">Searching...</div>
                    </div>
                </div>
            `;
            showSuggestions();
        }
        
        // Show minimum characters message
        function showMinimumCharsMessage() {
            const suggestionsContainer = document.getElementById('search-suggestions');
            suggestionsContainer.innerHTML = `
                <div class="suggestion-item" style="justify-content: center; cursor: default; opacity: 0.7;">
                    <div class="suggestion-icon">
                        <i class="fas fa-keyboard"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">Type at least 2 characters to search</div>
                    </div>
                </div>
            `;
            showSuggestions();
        }
        
        // Show no results message
        function showNoResultsMessage(query) {
            const suggestionsContainer = document.getElementById('search-suggestions');
            suggestionsContainer.innerHTML = `
                <div class="suggestion-item" style="cursor: default; opacity: 0.7;">
                    <div class="suggestion-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">No results found for "${query}"</div>
                        <div class="suggestion-meta">Try different keywords or browse our content</div>
                    </div>
                </div>
                <div class="suggestion-item" onclick="performFallbackSearch('${query}')" style="border-top: 1px solid var(--glass-border); margin-top: 8px; padding-top: 12px;">
                    <div class="suggestion-icon">
                        <i class="fas fa-external-link-alt"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">Search our full catalog</div>
                        <div class="suggestion-meta">View detailed search results</div>
                    </div>
                </div>
            `;
            showSuggestions();
        }
        
        // Show error message
        function showErrorMessage() {
            const suggestionsContainer = document.getElementById('search-suggestions');
            suggestionsContainer.innerHTML = `
                <div class="suggestion-item" style="cursor: default; opacity: 0.7;">
                    <div class="suggestion-icon">
                        <i class="fas fa-exclamation-triangle" style="color: var(--accent);"></i>
                    </div>
                    <div class="suggestion-content">
                        <div class="suggestion-title">Search temporarily unavailable</div>
                        <div class="suggestion-meta">Please try again in a moment</div>
                    </div>
                </div>
            `;
            showSuggestions();
        }