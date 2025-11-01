// Get backend base URL with proper protocol detection
        const getBackendBaseUrl = () => {
            // Check if PortfolioConfig is loaded
            if (typeof PortfolioConfig !== 'undefined' && PortfolioConfig.api && PortfolioConfig.api.getBaseUrl) {
                return PortfolioConfig.api.getBaseUrl();
            }
            
            // Check for API_BASE_URL from environment
            if (window.API_BASE_URL) {
                return window.API_BASE_URL;
            }
            
            if (window.WordPressConfig) {
                return window.WordPressConfig.getApiUrl().replace('/api', '');
            }
            
            // For localhost development, always use HTTP on port 7150
            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                return 'http://localhost:7150';
            }
            
            // For production, use same protocol and hostname
            return `${window.location.protocol}//${window.location.hostname}`;
        };
        const backendBaseUrl = getBackendBaseUrl();
        const solutionsApiUrl = `${backendBaseUrl}/api/solutions`;
        
        // Debug: Log the constructed URLs
        console.log('Backend Base URL:', backendBaseUrl);
        console.log('Solutions API URL:', solutionsApiUrl);

        let solutions = [];
        let filteredSolutions = [];
        let currentPage = 1;
        let pageSize = 8;
        let deepLinkHandled = false;

        // Helper function to get full image URL
        function getFullImageUrl(imageUrl) {
            if (!imageUrl || imageUrl.trim() === '') return null;
            // If URL starts with http/https, return as is
            if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) {
                return imageUrl;
            }
            // Otherwise, prepend the backend base URL
            return `${backendBaseUrl}${imageUrl}`;
        }

        // Theme is handled by shared-components.js
        // Load solutions and populate filters
        async function loadSolutions() {
            try {
                const response = await fetch(`${solutionsApiUrl}?pageSize=50`);
                if (response.ok) {
                    solutions = await response.json();
                    filteredSolutions = [...solutions];
                    
                    // Load categories for filter dropdown
                    await loadCategoryFilter();
                    
                    updateStats();
                    renderSolutions();
                    handleDeepLink();
                } else {
                    throw new Error('Failed to load solutions');
                }
            } catch (error) {
                console.error('Error loading solutions:', error);
                showErrorState();
            }
        }
        
        // Load problem areas dynamically from the API
        async function loadCategoryFilter() {
            try {
                const response = await fetch(`${solutionsApiUrl}/problem-areas`);
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

        // Update statistics
        function updateStats() {
            const totalCount = filteredSolutions.length;
            const categories = [...new Set(filteredSolutions.map(solution => solution.problemArea))].filter(Boolean).length;
            const activeCount = filteredSolutions.filter(solution => solution.isActive).length;
            
            document.getElementById('total-count').textContent = totalCount;
            document.getElementById('categories-count').textContent = categories;
            document.getElementById('active-count').textContent = activeCount;
            
            // Find the most recent update date
            const lastUpdate = filteredSolutions.reduce((latest, solution) => {
                const solutionDate = new Date(solution.createdAt);
                return solutionDate > latest ? solutionDate : latest;
            }, new Date(0));
            
            if (lastUpdate.getTime() > 0) {
                document.getElementById('last-updated').textContent = lastUpdate.toLocaleDateString();
            }
        }

        // Render solutions with pagination
        function renderSolutions() {
            const container = document.getElementById('solutions-container');
            
            if (filteredSolutions.length === 0) {
                container.innerHTML = `
                    <div class="empty-state">
                        <i class="fas fa-search"></i>
                        <h3>No solutions found</h3>
                        <p>Try adjusting your search criteria or filters.</p>
                    </div>
                `;
                hidePagination();
                return;
            }

            // Calculate pagination
            const totalPages = Math.ceil(filteredSolutions.length / pageSize);
            const startIndex = (currentPage - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            const currentPageSolutions = filteredSolutions.slice(startIndex, endIndex);

            const solutionsHtml = currentPageSolutions.map(solution => {
                const fullImageUrl = getFullImageUrl(solution.demoImageUrl);
                return `
                <div class="solution-card" id="solution-${solution.id}">
                    <div class="solution-content-area">
                        ${fullImageUrl ? 
                            `<img src="${fullImageUrl}" alt="${solution.title}" class="solution-image" onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';">` : 
                            ''
                        }
                        ${!fullImageUrl ? 
                            `<div class="solution-placeholder"><i class="fas fa-lightbulb"></i></div>` : 
                            `<div class="solution-placeholder" style="display:none;"><i class="fas fa-lightbulb"></i></div>`
                        }
                        
                        <div class="solution-header">
                            <span class="solution-category">${solution.problemArea || 'Solution'}</span>
                            <span class="solution-domain ${solution.isActive ? 'active' : 'inactive'}">${solution.isActive ? 'Active' : 'Inactive'}</span>
                        </div>
                        
                        <h3 class="solution-title">${solution.title}</h3>
                        <p class="solution-description">${solution.summary || solution.description || ''}</p>
                        
                        <div class="solution-technologies">
                            ${(solution.technologiesUsed || '').split(',').map(tech => 
                                tech.trim() ? `<span class="tech-tag">${tech.trim()}</span>` : ''
                            ).join('')}
                        </div>
                        
                        <ul class="solution-features">
                            ${(solution.keyFeatures || '').split(',').slice(0, 3).map(feature => 
                                feature.trim() ? `<li>${feature.trim()}</li>` : ''
                            ).join('')}
                        </ul>
                        
                        <div class="solution-spacer"></div>
                    </div>
                    
                    <div class="solution-meta">
                        <span style="display: flex; align-items: center; gap: 4px;">
                            <i class="fas fa-calendar" style="color: var(--primary); font-size: 0.8rem;"></i>
                            ${new Date(solution.createdAt).toLocaleDateString()}
                        </span>
                        <span style="display: flex; align-items: center; gap: 4px;">
                            <i class="fas ${solution.isActive ? 'fa-check-circle' : 'fa-pause-circle'}" style="color: ${solution.isActive ? 'var(--success)' : 'var(--warning)'}; font-size: 0.8rem;"></i>
                            ${solution.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </div>
                    
                    <div class="solution-actions">
                        <button class="btn btn-demo" ${!solution.demoVideoUrl ? 'disabled' : ''} onclick="${solution.demoVideoUrl ? `viewDemo('${solution.demoVideoUrl}', '${solution.title.replace(/'/g, "\\'")}')` : 'showNoDemoMessage()'}">
                            <i class="fas fa-play"></i> View Demo
                        </button>
                        <button class="btn btn-outline" onclick="viewSolution(${solution.id})">
                            <i class="fas fa-eye"></i>
                            View Details
                        </button>
                        <button class="btn btn-primary" onclick="requestSolution(${solution.id})">
                            <i class="fas fa-envelope"></i>
                            Request Quote
                        </button>
                    </div>
                </div>
            `;
            }).join('');

            container.innerHTML = `
                <div class="solutions-grid">
                    ${solutionsHtml}
                </div>
            `;

            // If deep-linked, ensure target card is visible on the current page
            if (!deepLinkHandled) {
                const urlParams = new URLSearchParams(window.location.search);
                const idParam = urlParams.get('id');
                if (idParam) {
                    const targetId = parseInt(idParam, 10);
                    const idx = filteredSolutions.findIndex(s => s.id === targetId || s.Id === targetId);
                    if (idx >= 0) {
                        currentPage = Math.floor(idx / pageSize) + 1;
                    }
                }
            }

            // Deep link handling
            if (!deepLinkHandled) {
                const urlParams = new URLSearchParams(window.location.search);
                const targetId = urlParams.get('id');
                if (targetId) {
                    const el = document.getElementById(`solution-${targetId}`);
                    if (el) {
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        try { viewSolution(parseInt(targetId, 10)); } catch {}
                        deepLinkHandled = true;
                    }
                }
            }

            // Render pagination
            renderPagination();
            
            // Handle deep linking after rendering
            if (!deepLinkHandled) {
                const urlParams = new URLSearchParams(window.location.search);
                const targetId = urlParams.get('id');
                if (targetId) {
                    const el = document.getElementById(`solution-${targetId}`);
                    if (el) {
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        try { viewSolution(parseInt(targetId, 10)); } catch {}
                        deepLinkHandled = true;
                    }
                }
            }
        }
        
        function handleDeepLink() {
            const urlParams = new URLSearchParams(window.location.search);
            
            // Handle individual solution ID
            const id = urlParams.get('id');
            if (id) {
                // Find the page that contains this solution
                const solutionIndex = filteredSolutions.findIndex(s => s.id === parseInt(id, 10));
                if (solutionIndex >= 0) {
                    currentPage = Math.floor(solutionIndex / pageSize) + 1;
                    renderSolutions();
                }
                return;
            }
            
            // Handle problemArea filtering
            const problemArea = urlParams.get('problemArea');
            if (problemArea) {
                console.log('Deep link problemArea filter detected:', problemArea);
                const categoryFilter = document.getElementById('category-filter');
                if (categoryFilter) {
                    // Set the category filter value (maps to problemArea)
                    categoryFilter.value = problemArea;
                    // Trigger filtering
                    applyFilters();
                    console.log('Applied problemArea filter from URL:', problemArea);
                } else {
                    console.warn('Category filter element not found, will retry after DOM loads');
                    // Retry after a short delay to ensure DOM is ready
                    setTimeout(() => {
                        const retryCategoryFilter = document.getElementById('category-filter');
                        if (retryCategoryFilter) {
                            retryCategoryFilter.value = problemArea;
                            applyFilters();
                            console.log('Applied problemArea filter from URL (retry):', problemArea);
                        }
                    }, 500);
                }
            }
        }

        // View solution details
        function applyFilters() {
            const searchTerm = document.getElementById('search-input').value.toLowerCase();
            const categoryFilter = document.getElementById('category-filter').value;
            const statusFilter = document.getElementById('status-filter').value;
            const sortFilter = document.getElementById('sort-filter').value;

            console.log('Applying filters:', { searchTerm, categoryFilter, statusFilter, sortFilter });

            filteredSolutions = solutions.filter(solution => {
                // Search matches title, summary, or problem area
                const matchesSearch = !searchTerm || 
                    solution.title.toLowerCase().includes(searchTerm) ||
                    (solution.summary && solution.summary.toLowerCase().includes(searchTerm)) ||
                    (solution.problemArea && solution.problemArea.toLowerCase().includes(searchTerm));
                
                // Category filter matches problemArea (since that's the field we have)
                const matchesCategory = !categoryFilter || solution.problemArea === categoryFilter;
                
                // Status filter based on isActive field
                const matchesStatus = !statusFilter || 
                    (statusFilter === 'active' && solution.isActive) ||
                    (statusFilter === 'inactive' && !solution.isActive);

                const matches = matchesSearch && matchesCategory && matchesStatus;
                if (searchTerm && matches) {
                    console.log('Solution matches filters:', solution.title);
                }
                return matches;
            });

            console.log('Filtered results:', filteredSolutions.length, 'out of', solutions.length);

            // Sort results
            filteredSolutions.sort((a, b) => {
                switch (sortFilter) {
                    case 'newest':
                        return new Date(b.createdAt) - new Date(a.createdAt);
                    case 'oldest':
                        return new Date(a.createdAt) - new Date(b.createdAt);
                    case 'title':
                        return a.title.localeCompare(b.title);
                    case 'category':
                        return (a.problemArea || '').localeCompare(b.problemArea || '');
                    default:
                        return 0;
                }
            });

            // Reset to first page when filters change
            currentPage = 1;
            updateStats();
            renderSolutions();
        }

        // Pagination Functions
        function renderPagination(totalPages) {
            const paginationContainer = document.getElementById('pagination');
            
            if (totalPages <= 1) {
                paginationContainer.style.display = 'none';
                return;
            }

            paginationContainer.style.display = 'flex';
            
            let paginationHtml = '';
            
            // Previous button
            const prevDisabled = currentPage === 1 ? 'disabled' : '';
            paginationHtml += `
                <button class="pagination-btn ${prevDisabled}" onclick="goToPage(${currentPage - 1})" ${prevDisabled ? 'disabled' : ''}>
                    <i class="fas fa-chevron-left"></i>
                </button>
            `;
            
            // Page numbers logic
            let startPage = Math.max(1, currentPage - 2);
            let endPage = Math.min(totalPages, currentPage + 2);
            
            // Adjust the range if we're near the beginning or end
            if (currentPage <= 3) {
                endPage = Math.min(totalPages, 5);
            } else if (currentPage > totalPages - 3) {
                startPage = Math.max(1, totalPages - 4);
            }
            
            // First page and ellipsis
            if (startPage > 1) {
                paginationHtml += `<button class="pagination-btn" onclick="goToPage(1)">1</button>`;
                if (startPage > 2) {
                    paginationHtml += `<span class="pagination-info">...</span>`;
                }
            }
            
            // Page numbers
            for (let i = startPage; i <= endPage; i++) {
                const activeClass = i === currentPage ? 'active' : '';
                paginationHtml += `<button class="pagination-btn ${activeClass}" onclick="goToPage(${i})">${i}</button>`;
            }
            
            // Last page and ellipsis
            if (endPage < totalPages) {
                if (endPage < totalPages - 1) {
                    paginationHtml += `<span class="pagination-info">...</span>`;
                }
                paginationHtml += `<button class="pagination-btn" onclick="goToPage(${totalPages})">${totalPages}</button>`;
            }
            
            // Next button
            const nextDisabled = currentPage === totalPages ? 'disabled' : '';
            paginationHtml += `
                <button class="pagination-btn ${nextDisabled}" onclick="goToPage(${currentPage + 1})" ${nextDisabled ? 'disabled' : ''}>
                    <i class="fas fa-chevron-right"></i>
                </button>
            `;
            
            // Page info
            const startItem = (currentPage - 1) * pageSize + 1;
            const endItem = Math.min(currentPage * pageSize, filteredSolutions.length);
            paginationHtml += `
                <span class="pagination-info">
                    <i class="fas fa-info-circle"></i>
                    ${startItem}-${endItem} of ${filteredSolutions.length}
                </span>
            `;
            
            paginationContainer.innerHTML = paginationHtml;
        }

        function goToPage(page) {
            const totalPages = Math.ceil(filteredSolutions.length / pageSize);
            if (page < 1 || page > totalPages) return;
            
            currentPage = page;
            renderSolutions();
            
            // Scroll to top of solutions container
            document.getElementById('solutions-container').scrollIntoView({ 
                behavior: 'smooth', 
                block: 'start' 
            });
        }

        function hidePagination() {
            document.getElementById('pagination').style.display = 'none';
        }

        // View solution details
        async function viewSolution(solutionId) {
            try {
                const response = await fetch(`${solutionsApiUrl}/${solutionId}`);
                if (response.ok) {
                    const solution = await response.json();
                    showSolutionModal(solution);
                } else {
                    throw new Error('Failed to load solution details');
                }
            } catch (error) {
                console.error('Error loading solution details:', error);
                alert('Failed to load solution details. Please try again.');
            }
        }

        // Show solution modal
        function showSolutionModal(solution) {
            document.getElementById('modal-title').textContent = solution.title;
            document.getElementById('modal-body').innerHTML = `
                <div style="margin-bottom: 20px;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; flex-wrap: wrap; gap: 12px;">
                        <span style="background: var(--primary); color: var(--bg-light); padding: 4px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 600;">${solution.problemArea || 'Solution'}</span>
                        <span style="background: ${solution.isActive ? 'var(--success)' : 'var(--warning)'}; color: var(--bg-light); padding: 4px 8px; border-radius: 12px; font-size: 0.75rem; font-weight: 600;">${solution.isActive ? 'Active' : 'Inactive'}</span>
                    </div>
                    <p style="color: var(--text-light); margin-bottom: 20px; line-height: 1.6;">${solution.summary || solution.description || ''}</p>
                    
                    ${solution.technologiesUsed ? `
                        <h4 style="color: var(--text); margin-bottom: 12px;">Technologies Used:</h4>
                        <div style="display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 20px;">
                            ${solution.technologiesUsed.split(',').map(tech => 
                                `<span style="background: var(--neutral-light); color: var(--neutral); font-size: 0.8rem; font-weight: 500; padding: 4px 10px; border-radius: 12px; border: 1px solid var(--glass-border);">${tech.trim()}</span>`
                            ).join('')}
                        </div>
                    ` : ''}
                    
                    ${solution.keyFeatures ? `
                        <h4 style="color: var(--text); margin-bottom: 12px;">Key Features:</h4>
                        <ul style="list-style: none; margin-bottom: 20px;">
                            ${solution.keyFeatures.split(',').map(feature => 
                                `<li style="margin-bottom: 8px; display: flex; align-items: center; gap: 8px; color: var(--text-light);">
                                    <span style="color: var(--success); font-weight: bold;">âœ“</span>
                                    ${feature.trim()}
                                </li>`
                            ).join('')}
                        </ul>
                    ` : ''}
                    
                    ${solution.targetAudience ? `
                        <h4 style="color: var(--text); margin-bottom: 12px;">Target Audience:</h4>
                        <p style="color: var(--text-light); margin-bottom: 20px; line-height: 1.6;">${solution.targetAudience}</p>
                    ` : ''}
                    
                    ${solution.caseStudyLink ? `
                        <h4 style="color: var(--text); margin-bottom: 12px;">Case Study:</h4>
                        <p style="margin-bottom: 20px;">
                            <a href="${solution.caseStudyLink}" target="_blank" style="color: var(--primary); text-decoration: none;">
                                <i class="fas fa-external-link-alt"></i> View detailed case study
                            </a>
                        </p>
                    ` : ''}
                    
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px;">
                        <div>
                            <strong style="color: var(--text);">Problem Area:</strong><br>
                            <span style="color: var(--text-light);">${solution.problemArea || 'Not specified'}</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Created:</strong><br>
                            <span style="color: var(--text-light);">${new Date(solution.createdAt).toLocaleDateString()}</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Status:</strong><br>
                            <span style="color: var(--success);">Active</span>
                        </div>
                        <div>
                            <strong style="color: var(--text);">Delivery:</strong><br>
                            <span style="color: var(--text-light);">Custom Timeline</span>
                        </div>
                    </div>
                    
                    <div style="display: flex; gap: 12px; margin-top: 24px;">
                        <button class="btn btn-outline" onclick="viewPublications(${solution.id})" style="flex: 1;">
                            <i class="fas fa-book"></i>
                            View Publications
                        </button>
                        <button class="btn btn-primary" onclick="requestSolution(${solution.id})" style="flex: 1;">
                            <i class="fas fa-envelope"></i>
                            Request Quote
                        </button>
                        <button class="btn btn-outline" onclick="scheduleMeeting(${solution.id})" style="flex: 1;">
                            <i class="fas fa-calendar"></i>
                            Schedule Meeting
                        </button>
                    </div>
                </div>
            `;
            
            document.getElementById('solution-modal').classList.add('show');
        }

        // Close modal
        function closeModal() {
            document.getElementById('solution-modal').classList.remove('show');
        }

        // View publications for solution
        function viewPublications(solutionId) {
            // Redirect to Publications page filtered by solution
            window.location.href = `Publications.html?solutionId=${solutionId}`;
        }

        // Request solution
        function requestSolution(solutionId) {
            // Redirect to contact page with solution information
            window.location.href = `Contact.html?solution=${solutionId}&action=quote`;
        }
        // Schedule meeting
        function scheduleMeeting(solutionId) {
            // Find the solution details for context
            let solution = filteredSolutions.find(s => s.id === solutionId || s.Id === solutionId) || 
                          solutions.find(s => s.id === solutionId || s.Id === solutionId);
            
            // If still not found, try to get it from the modal
            if (!solution) {
                const modalTitle = document.getElementById('modal-title');
                if (modalTitle && modalTitle.textContent) {
                    solution = filteredSolutions.find(s => s.title === modalTitle.textContent) || 
                              solutions.find(s => s.title === modalTitle.textContent);
                }
            }
            
            // Open Google Calendar booking page
            const bookingUrl = 'https://calendar.app.google/xta6b1fwfCN2Bkgc9';
            
            try {
                // Open in new tab/window
                window.open(bookingUrl, '_blank', 'noopener,noreferrer');
                
                // Optional: Show confirmation message with solution context
                if (solution) {
                    setTimeout(() => {
                        alert(`Opening booking page for "${solution.title}". Please mention this solution in your meeting request.`);
                    }, 500);
                }
            } catch (error) {
                console.error('Error opening booking page:', error);
                // Fallback: copy URL to clipboard and show instructions
                navigator.clipboard.writeText(bookingUrl).then(() => {
                    alert(`Could not open booking page automatically. The URL has been copied to your clipboard: ${bookingUrl}`);
                }).catch(() => {
                    alert(`Please visit this URL to schedule a meeting: ${bookingUrl}`);
                });
            }
        }
        
        // Show no demo message
        function showNoDemoMessage() {
            alert('Demo video not available for this solution. Please contact us for more information.');
        }

        // Show error state
        function showErrorState() {
            document.getElementById('solutions-container').innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-exclamation-triangle" style="color: var(--accent);"></i>
                    <h3>Failed to load solutions</h3>
                    <p>Please try again later or contact support if the problem persists.</p>
                </div>
            `;
        }

        // Event listeners
        document.getElementById('search-input').addEventListener('input', debounce(applyFilters, 300));
        document.getElementById('category-filter').addEventListener('change', applyFilters);
        document.getElementById('status-filter').addEventListener('change', applyFilters);
        document.getElementById('sort-filter').addEventListener('change', applyFilters);

        // Demo Video Functions
        function viewDemo(videoUrl, solutionTitle) {
            console.log('Opening demo video:', videoUrl, 'for solution:', solutionTitle);
            
            if (!videoUrl || videoUrl.trim() === '') {
                alert('No demo video available for this solution');
                return;
            }

            // Set modal title
            document.getElementById('demo-modal-title').innerHTML = `<i class="fas fa-play-circle"></i> ${solutionTitle} - Demo`;
            document.getElementById('demo-video-url').textContent = videoUrl;
            
            // Show loading state
            showDemoVideoLoader();
            
            // Show modal
            document.getElementById('demo-modal').classList.add('show');
            document.body.style.overflow = 'hidden';
            
            // Load video after a short delay to ensure modal is visible
            setTimeout(() => {
                loadDemoVideo(videoUrl);
            }, 300);
        }

        function showDemoVideoLoader() {
            document.getElementById('demo-video-loader').style.display = 'block';
            document.getElementById('demo-video-frame').style.display = 'none';
            document.getElementById('demo-video-player').style.display = 'none';
            document.getElementById('demo-video-error').style.display = 'none';
        }

        function hideDemoVideoLoader() {
            document.getElementById('demo-video-loader').style.display = 'none';
        }

        function showDemoVideoError() {
            document.getElementById('demo-video-loader').style.display = 'none';
            document.getElementById('demo-video-frame').style.display = 'none';
            document.getElementById('demo-video-player').style.display = 'none';
            document.getElementById('demo-video-error').style.display = 'block';
        }

        function loadDemoVideo(videoUrl) {
            const iframe = document.getElementById('demo-video-frame');
            const videoPlayer = document.getElementById('demo-video-player');
            
            try {
                // Clean the URL
                const cleanUrl = videoUrl.trim();
                
                // Check if it's a YouTube URL
                if (isDemoYouTubeUrl(cleanUrl)) {
                    const embedUrl = convertToDemoYouTubeEmbed(cleanUrl);
                    iframe.src = embedUrl;
                    iframe.onload = () => {
                        hideDemoVideoLoader();
                        iframe.style.display = 'block';
                    };
                    iframe.onerror = () => showDemoVideoError();
                }
                // Check if it's a Vimeo URL
                else if (isDemoVimeoUrl(cleanUrl)) {
                    const embedUrl = convertToDemoVimeoEmbed(cleanUrl);
                    iframe.src = embedUrl;
                    iframe.onload = () => {
                        hideDemoVideoLoader();
                        iframe.style.display = 'block';
                    };
                    iframe.onerror = () => showDemoVideoError();
                }
                // Check if it's a direct video file
                else if (isDemoDirectVideoUrl(cleanUrl)) {
                    videoPlayer.src = cleanUrl;
                    videoPlayer.onloadeddata = () => {
                        hideDemoVideoLoader();
                        videoPlayer.style.display = 'block';
                    };
                    videoPlayer.onerror = () => showDemoVideoError();
                }
                // Try as iframe for other platforms
                else {
                    iframe.src = cleanUrl;
                    iframe.onload = () => {
                        hideDemoVideoLoader();
                        iframe.style.display = 'block';
                    };
                    iframe.onerror = () => showDemoVideoError();
                }
                
                // Set timeout to show error if video doesn't load within 10 seconds
                setTimeout(() => {
                    if (document.getElementById('demo-video-loader').style.display !== 'none') {
                        showDemoVideoError();
                    }
                }, 10000);
                
            } catch (error) {
                console.error('Error loading demo video:', error);
                showDemoVideoError();
            }
        }

        function isDemoYouTubeUrl(url) {
            const youtubeRegex = /(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?\/ ]{11})/;
            return youtubeRegex.test(url);
        }

        function convertToDemoYouTubeEmbed(url) {
            const videoIdRegex = /(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?\/ ]{11})/;
            const match = url.match(videoIdRegex);
            if (match && match[1]) {
                return `https://www.youtube.com/embed/${match[1]}?autoplay=1&rel=0&modestbranding=1`;
            }
            return url;
        }

        function isDemoVimeoUrl(url) {
            const vimeoRegex = /(?:vimeo\.com\/)([0-9]+)/;
            return vimeoRegex.test(url);
        }

        function convertToDemoVimeoEmbed(url) {
            const videoIdRegex = /(?:vimeo\.com\/)([0-9]+)/;
            const match = url.match(videoIdRegex);
            if (match && match[1]) {
                return `https://player.vimeo.com/video/${match[1]}?autoplay=1`;
            }
            return url;
        }

        function isDemoDirectVideoUrl(url) {
            const videoExtensions = /\.(mp4|webm|ogg|avi|mov|wmv|flv|mkv)$/i;
            return videoExtensions.test(url);
        }

        function closeDemoModal() {
            const iframe = document.getElementById('demo-video-frame');
            const videoPlayer = document.getElementById('demo-video-player');
            
            // Stop video playback
            iframe.src = '';
            videoPlayer.src = '';
            videoPlayer.pause();
            
            // Reset modal state
            showDemoVideoLoader();
            
            // Close modal
            document.getElementById('demo-modal').classList.remove('show');
            document.body.style.overflow = 'auto';
        }

        function toggleDemoFullscreen() {
            const videoContainer = document.getElementById('demo-video-container');
            const fullscreenBtn = document.getElementById('demo-fullscreen-btn');
            
            if (!document.fullscreenElement) {
                videoContainer.requestFullscreen().then(() => {
                    fullscreenBtn.innerHTML = '<i class="fas fa-compress"></i> Exit Fullscreen';
                }).catch(err => {
                    console.log('Error attempting to enable fullscreen:', err);
                    alert('Fullscreen not supported by your browser');
                });
            } else {
                document.exitFullscreen().then(() => {
                    fullscreenBtn.innerHTML = '<i class="fas fa-expand"></i> Fullscreen';
                }).catch(err => {
                    console.log('Error attempting to exit fullscreen:', err);
                });
            }
        }

        // Listen for fullscreen changes
        document.addEventListener('fullscreenchange', function() {
            const fullscreenBtn = document.getElementById('demo-fullscreen-btn');
            if (fullscreenBtn) {
                if (document.fullscreenElement) {
                    fullscreenBtn.innerHTML = '<i class="fas fa-compress"></i> Exit Fullscreen';
                } else {
                    fullscreenBtn.innerHTML = '<i class="fas fa-expand"></i> Fullscreen';
                }
            }
        });

        // Close modal when clicking outside
        document.getElementById('solution-modal').addEventListener('click', (e) => {
            if (e.target.id === 'solution-modal') {
                closeModal();
            }
        });

        document.getElementById('demo-modal').addEventListener('click', (e) => {
            if (e.target.id === 'demo-modal') {
                closeDemoModal();
            }
        });

        // Close modals with Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                const solutionModal = document.getElementById('solution-modal');
                const demoModal = document.getElementById('demo-modal');
                
                if (demoModal.classList.contains('show')) {
                    closeDemoModal();
                } else if (solutionModal.classList.contains('show')) {
                    closeModal();
                }
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
            
            if (userAvatar && userName) {
                const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();
                userAvatar.textContent = initials;
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
        
        // Initialize authentication event listeners (called after components load)
        function initializeAuthEventListeners() {
            const logoutBtn = document.getElementById('logout-btn');
            if (logoutBtn) {
                logoutBtn.addEventListener('click', () => {
                    if (confirm('Are you sure you want to logout?')) {
                        clearAuthData();
                        showLoggedOutState();
                    }
                });
            }
        }
        
        // Initialize application
        document.addEventListener('DOMContentLoaded', () => {
            checkAuthState();
            loadSolutions();
        });
        
        // Check auth state on page focus (in case user logged in/out in another tab)
        window.addEventListener('focus', () => {
            checkAuthState();
        });