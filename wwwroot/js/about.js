// About Page - Dynamic Team & Testimonials Loader
// This script loads team members and testimonials from the backend API

// Configuration
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
    const hostname = window.location.hostname;
    const port = window.location.port;
    
    if (port && port !== '80' && port !== '443') {
        return `${window.location.protocol}//${hostname}:${port}`;
    } else {
        return `${window.location.protocol}//${hostname}`;
    }
}

const API_BASE_URL = getBackendBaseUrl() + '/api';

// Helper function to get full image URL
function getFullImageUrl(relativePath) {
    if (!relativePath) return null;
    if (relativePath.startsWith('http://') || relativePath.startsWith('https://')) {
        return relativePath;
    }
    const baseUrl = getBackendBaseUrl();
    const cleanPath = relativePath.startsWith('/') ? relativePath : '/' + relativePath;
    return baseUrl + cleanPath;
}

// Load Team Members
async function loadTeamMembers() {
    const container = document.getElementById('dynamicTeamGrid');
    if (!container) {
        console.error('Team grid container not found');
        return;
    }

    // Show loading state
    container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-spinner fa-spin" style="font-size: 2rem; margin-bottom: 10px;"></i><p>Loading team members...</p></div>';

    try {
        const response = await fetch(`${API_BASE_URL}/About/team`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch team members: ${response.status}`);
        }

        let teamMembers = await response.json();
        console.log('Team members loaded:', teamMembers);

        // Filter only active members and sort by display order
        teamMembers = teamMembers
            .filter(member => member.isActive)
            .sort((a, b) => (a.displayOrder || 999) - (b.displayOrder || 999));

        if (teamMembers.length === 0) {
            container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-users" style="font-size: 2rem; margin-bottom: 10px;"></i><p>No team members to display.</p></div>';
            return;
        }

        // Render team members - create cards
        const teamCards = teamMembers.map((member, index) => {
            const fullImageUrl = getFullImageUrl(member.photoUrl);
            const fullName = `${member.firstName} ${member.lastName}`;
            const bioText = member.bio || member.department || '';
            const maxLength = 120;
            const needsTruncation = bioText.length > maxLength;
            const truncatedBio = needsTruncation ? bioText.substring(0, maxLength) + '...' : bioText;
            
            // Handle image or emoji/initials fallback
            let imageContent;
            if (fullImageUrl) {
                imageContent = `<img src="${fullImageUrl}" alt="${fullName}" onerror="this.style.display='none'; this.parentElement.querySelector('.team-emoji').style.display='flex';">
                    <div class="team-emoji" style="display: none;">${member.firstName.charAt(0)}${member.lastName.charAt(0)}</div>`;
            } else {
                // Use emoji if available, otherwise initials
                const emoji = member.emoji || '👤';
                imageContent = `<div class="team-emoji">${emoji}</div>`;
            }

            // Build social links if available
            let socialLinks = '';
            if (member.linkedInUrl || member.twitterUrl || member.email) {
                socialLinks = '<div class="social-links">';
                if (member.linkedInUrl) {
                    socialLinks += `<a href="${member.linkedInUrl}" target="_blank" rel="noopener noreferrer" aria-label="LinkedIn"><i class="fab fa-linkedin"></i></a>`;
                }
                if (member.twitterUrl) {
                    socialLinks += `<a href="${member.twitterUrl}" target="_blank" rel="noopener noreferrer" aria-label="Twitter"><i class="fab fa-twitter"></i></a>`;
                }
                if (member.email) {
                    socialLinks += `<a href="mailto:${member.email}" aria-label="Email"><i class="fas fa-envelope"></i></a>`;
                }
                socialLinks += '</div>';
            }

            return `
                <div class="team-card" id="team-card-${index}">
                    ${imageContent}
                    <h3>${fullName}</h3>
                    <p class="role">${member.position || 'Team Member'}</p>
                    <p class="bio-text" id="team-bio-${index}">
                        <span class="bio-short">${truncatedBio}</span>
                        ${needsTruncation ? `<span class="bio-full" style="display: none;">${bioText}</span>` : ''}
                    </p>
                    ${needsTruncation ? `<a href="#" class="show-more-link" onclick="toggleTeamBio(${index}); return false;">Show more</a>` : ''}
                    ${socialLinks}
                </div>
            `;
        }).join('');
        
        // Render cards wrapped in scroller div
        container.innerHTML = `<div class="team-scroller">${teamCards}</div>`;

    } catch (error) {
        console.error('Error loading team members:', error);
        container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-exclamation-triangle" style="font-size: 2rem; margin-bottom: 10px; color: var(--danger);"></i><p>Failed to load team members. Please try again later.</p></div>';
    }
}

// Load Client Testimonials
async function loadTestimonials() {
    const container = document.getElementById('dynamicReviewsGrid');
    if (!container) {
        console.error('Reviews grid container not found');
        return;
    }

    // Show loading state
    container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-spinner fa-spin" style="font-size: 2rem; margin-bottom: 10px;"></i><p>Loading testimonials...</p></div>';

    try {
        const response = await fetch(`${API_BASE_URL}/About/testimonials`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch testimonials: ${response.status}`);
        }

        let testimonials = await response.json();
        console.log('Testimonials loaded:', testimonials);

        // Filter only active and approved testimonials, sort by display order
        testimonials = testimonials
            .filter(t => t.isActive && (t.isApproved !== false))
            .sort((a, b) => (a.displayOrder || 999) - (b.displayOrder || 999));

        if (testimonials.length === 0) {
            container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-comments" style="font-size: 2rem; margin-bottom: 10px;"></i><p>No testimonials to display.</p></div>';
            return;
        }

        // Render testimonials - create cards
        const reviewCards = testimonials.map((testimonial, index) => {
            const fullImageUrl = getFullImageUrl(testimonial.clientPhotoUrl);
            const rating = testimonial.rating || 5;
            const message = testimonial.message || '';
            const maxLength = 150;
            const needsTruncation = message.length > maxLength;
            const truncatedMessage = needsTruncation ? message.substring(0, maxLength) + '...' : message;
            
            // Build star rating HTML
            let starsHtml = '<div class="rating">';
            for (let i = 0; i < 5; i++) {
                if (i < Math.floor(rating)) {
                    starsHtml += '<i class="fas fa-star"></i>';
                } else if (i < rating) {
                    starsHtml += '<i class="fas fa-star-half-alt"></i>';
                } else {
                    starsHtml += '<i class="far fa-star"></i>';
                }
            }
            starsHtml += '</div>';

            // Handle company display
            const companyDisplay = testimonial.companyName ? 
                `<span class="company">${testimonial.companyName}</span>` : '';

            return `
                <div class="review-card" id="review-card-${index}">
                    <i class="fas fa-quote-left quote-icon"></i>
                    ${fullImageUrl ? 
                        `<img src="${fullImageUrl}" alt="${testimonial.clientName}" onerror="this.style.display='none';">` : 
                        ''}
                    ${starsHtml}
                    <p class="testimonial-text" id="testimonial-text-${index}">
                        "<span class="text-short">${truncatedMessage}</span>${needsTruncation ? `<span class="text-full" style="display: none;">${message}</span>` : ''}"
                    </p>
                    ${needsTruncation ? `<a href="#" class="show-more-link" onclick="toggleTestimonial(${index}); return false;">Show more</a>` : ''}
                    <h3>${testimonial.clientName} ${companyDisplay}</h3>
                </div>
            `;
        }).join('');
        
        // Render cards wrapped in scroller div
        container.innerHTML = `<div class="reviews-scroller">${reviewCards}</div>`;

    } catch (error) {
        console.error('Error loading testimonials:', error);
        container.innerHTML = '<div style="width: 100%; text-align: center; padding: 40px; color: var(--text-muted);"><i class="fas fa-exclamation-triangle" style="font-size: 2rem; margin-bottom: 10px; color: var(--danger);"></i><p>Failed to load testimonials. Please try again later.</p></div>';
    }
}

// Toggle functions for show more/less
function toggleTeamBio(index) {
    const bioElement = document.getElementById(`team-bio-${index}`);
    const shortText = bioElement.querySelector('.bio-short');
    const fullText = bioElement.querySelector('.bio-full');
    const link = bioElement.parentElement.querySelector('.show-more-link');
    
    if (fullText.style.display === 'none') {
        shortText.style.display = 'none';
        fullText.style.display = 'inline';
        link.textContent = 'Show less';
    } else {
        shortText.style.display = 'inline';
        fullText.style.display = 'none';
        link.textContent = 'Show more';
    }
}

function toggleTestimonial(index) {
    const textElement = document.getElementById(`testimonial-text-${index}`);
    const shortText = textElement.querySelector('.text-short');
    const fullText = textElement.querySelector('.text-full');
    const link = textElement.parentElement.querySelector('.show-more-link');
    
    if (fullText.style.display === 'none') {
        shortText.style.display = 'none';
        fullText.style.display = 'inline';
        link.textContent = 'Show less';
    } else {
        shortText.style.display = 'inline';
        fullText.style.display = 'none';
        link.textContent = 'Show more';
    }
}

// Make toggle functions globally available
window.toggleTeamBio = toggleTeamBio;
window.toggleTestimonial = toggleTestimonial;

// Auto-scroll functionality - EXACT same as home page feature scroller
let teamScrollPosition = 0;
let teamScrollDirection = 1;
let testimonialScrollPosition = 0;
let testimonialScrollDirection = 1;
let isMobile = window.innerWidth < 1280;

function autoScrollTeam() {
    if (isMobile) return;
    
    const teamGrid = document.getElementById('dynamicTeamGrid');
    const teamScroller = teamGrid ? teamGrid.querySelector('.team-scroller') : null;
    if (!teamGrid || !teamScroller) return;
    
    // Check if paused
    if (teamGrid.dataset.paused === 'true') {
        requestAnimationFrame(autoScrollTeam);
        return;
    }
    
    const visibleWidth = teamGrid.clientWidth;
    const maxScroll = teamScroller.scrollWidth - visibleWidth;
    
    if (maxScroll <= 0) return; // Nothing to scroll
    
    if (teamScrollPosition <= 0) {
        teamScrollDirection = 1;
    } else if (teamScrollPosition >= maxScroll) {
        teamScrollDirection = -1;
    }
    
    teamScrollPosition += teamScrollDirection * 1;
    teamScroller.style.transform = `translateX(-${teamScrollPosition}px)`;
    
    requestAnimationFrame(autoScrollTeam);
}

function autoScrollTestimonials() {
    if (isMobile) return;
    
    const reviewsGrid = document.getElementById('dynamicReviewsGrid');
    const reviewsScroller = reviewsGrid ? reviewsGrid.querySelector('.reviews-scroller') : null;
    if (!reviewsGrid || !reviewsScroller) return;
    
    // Check if paused
    if (reviewsGrid.dataset.paused === 'true') {
        requestAnimationFrame(autoScrollTestimonials);
        return;
    }
    
    const visibleWidth = reviewsGrid.clientWidth;
    const maxScroll = reviewsScroller.scrollWidth - visibleWidth;
    
    if (maxScroll <= 0) return; // Nothing to scroll
    
    if (testimonialScrollPosition <= 0) {
        testimonialScrollDirection = 1;
    } else if (testimonialScrollPosition >= maxScroll) {
        testimonialScrollDirection = -1;
    }
    
    testimonialScrollPosition += testimonialScrollDirection * 1;
    reviewsScroller.style.transform = `translateX(-${testimonialScrollPosition}px)`;
    
    requestAnimationFrame(autoScrollTestimonials);
}

// Pause auto-scroll on hover
function setupAutoScrollPause() {
    const teamGrid = document.getElementById('dynamicTeamGrid');
    const reviewsGrid = document.getElementById('dynamicReviewsGrid');
    
    if (teamGrid) {
        teamGrid.addEventListener('mouseenter', () => {
            teamGrid.dataset.paused = 'true';
        });
        teamGrid.addEventListener('mouseleave', () => {
            teamGrid.dataset.paused = 'false';
            if (!isMobile) requestAnimationFrame(autoScrollTeam);
        });
    }
    
    if (reviewsGrid) {
        reviewsGrid.addEventListener('mouseenter', () => {
            reviewsGrid.dataset.paused = 'true';
        });
        reviewsGrid.addEventListener('mouseleave', () => {
            reviewsGrid.dataset.paused = 'false';
            if (!isMobile) requestAnimationFrame(autoScrollTestimonials);
        });
    }
}

// Handle window resize
window.addEventListener('resize', () => {
    const wasMobile = isMobile;
    isMobile = window.innerWidth < 1280;
    
    // Only restart animations when transitioning from mobile to desktop
    if (wasMobile && !isMobile) {
        requestAnimationFrame(autoScrollTeam);
        requestAnimationFrame(autoScrollTestimonials);
    }
});

// Timeline animation functionality
let timelineAnimated = false;

function animateTimeline() {
    const timeline = document.querySelector('.timeline');
    if (!timeline || timelineAnimated) return;
    
    const timelineRect = timeline.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    
    // Check if timeline section is in view (at least 20% visible)
    if (timelineRect.top < windowHeight * 0.8 && timelineRect.bottom > 0) {
        timelineAnimated = true;
        
        // Get all timeline items
        const timelineItems = timeline.querySelectorAll('.timeline-item');
        const totalItems = timelineItems.length;
        
        // Calculate timing for synchronized animation
        const totalAnimationTime = 2000; // 2 seconds for full line
        const timePerItem = totalAnimationTime / totalItems;
        
        // Start line animation
        timeline.classList.add('animate-line');
        
        // Animate each timeline item synchronized with line progression
        timelineItems.forEach((item, index) => {
            setTimeout(() => {
                item.classList.add('animate-in');
            }, index * timePerItem); // Synchronize with line animation
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('About page loaded - initializing dynamic content');
    
    // Load content first
    Promise.all([loadTeamMembers(), loadTestimonials()]).then(() => {
        // Start auto-scroll after content is loaded
        setupAutoScrollPause();
        
        if (!isMobile) {
            setTimeout(() => {
                requestAnimationFrame(autoScrollTeam);
                requestAnimationFrame(autoScrollTestimonials);
            }, 500); // Small delay to ensure content is fully rendered
        }
    });
    
    // Set up timeline animation on scroll
    window.addEventListener('scroll', animateTimeline);
    // Check immediately in case already in view
    animateTimeline();
});
