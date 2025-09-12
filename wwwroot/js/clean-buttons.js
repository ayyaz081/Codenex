/**
 * 🧹 CLEAN BUTTON HANDLERS
 * Universal solution for mobile and desktop button functionality
 * No conflicts, no duplicates, just working buttons
 */

console.log('🚀 Loading clean button handlers...');

// 🎯 UNIVERSAL BUTTON HANDLER - Works with any header (shared-components or fallback)
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM loaded - initializing buttons...');
    
    // Initialize theme
    initializeTheme();
    
    // Wait for any header to load, then attach handlers
    setTimeout(() => {
        console.log('⚡ Attaching universal button handlers...');
        attachUniversalHandlers();
    }, 500);
});

/**
 * Attach universal click handlers
 */
function attachUniversalHandlers() {
    // Remove any existing handlers to prevent duplicates
    document.body.removeEventListener('click', universalClickHandler);
    document.body.removeEventListener('touchstart', universalTouchHandler);
    
    // Add fresh handlers
    document.body.addEventListener('click', universalClickHandler);
    document.body.addEventListener('touchstart', universalTouchHandler, { passive: false });
}

/**
 * Universal click handler for all buttons
 */
function universalClickHandler(e) {
    // Mobile menu toggle
    if (e.target.closest('#nav-toggle')) {
        e.preventDefault();
        e.stopPropagation();
        console.log('📱 Mobile menu clicked!');
        handleMobileMenuToggle();
        return;
    }
    
    // Theme toggle (both mobile and desktop)
    if (e.target.closest('#theme-toggle')) {
        e.preventDefault();
        e.stopPropagation();
        console.log('🎨 Theme toggle clicked!');
        handleThemeToggle();
        return;
    }
    
    // Search icon (desktop) - only if shared-components search is not working
    if (e.target.closest('#search-icon-container')) {
        // Let shared-components handle it first
        setTimeout(() => {
            const globalSearch = document.getElementById('global-search');
            if (globalSearch && !globalSearch.classList.contains('expanded')) {
                // Shared-components didn't handle it, use fallback
                console.log('🔍 Search clicked - using fallback!');
                handleSearchToggle();
            }
        }, 50);
        return;
    }
}

/**
 * Universal touch handler for mobile devices
 */
function universalTouchHandler(e) {
    if (e.target.closest('#nav-toggle') || e.target.closest('#theme-toggle')) {
        // Prevent default to avoid double-tap issues
        e.preventDefault();
    }
}

/**
 * 🎨 THEME TOGGLE HANDLER
 */
function handleThemeToggle() {
    const htmlElement = document.documentElement;
    const themeToggle = document.getElementById('theme-toggle');
    const currentTheme = htmlElement.getAttribute('data-theme') || 'light';
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    
    htmlElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    
    if (themeToggle) {
        themeToggle.innerHTML = `<i class="fas fa-${newTheme === 'dark' ? 'sun' : 'moon'}"></i>`;
    }
    
    console.log('✅ Theme switched to:', newTheme);
}

/**
 * Initialize theme on page load
 */
function initializeTheme() {
    const htmlElement = document.documentElement;
    const savedTheme = localStorage.getItem('theme') || 'light';
    
    htmlElement.setAttribute('data-theme', savedTheme);
    
    // Update theme toggle icon when it becomes available
    const updateThemeIcon = () => {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.innerHTML = `<i class="fas fa-${savedTheme === 'dark' ? 'sun' : 'moon'}"></i>`;
        } else {
            // Retry if theme toggle not found yet
            setTimeout(updateThemeIcon, 200);
        }
    };
    
    updateThemeIcon();
}

/**
 * 📱 MOBILE MENU HANDLER
 */
function handleMobileMenuToggle() {
    if (window.innerWidth > 1024) {
        console.log('Not on mobile, ignoring menu toggle');
        return;
    }
    
    let mobileMenu = document.querySelector('.mobile-menu');
    let overlay = document.querySelector('.mobile-menu-overlay');
    
    // Create mobile menu if it doesn't exist
    if (!mobileMenu) {
        console.log('Creating mobile menu...');
        createMobileMenu();
        mobileMenu = document.querySelector('.mobile-menu');
        overlay = document.querySelector('.mobile-menu-overlay');
    }
    
    if (mobileMenu && overlay) {
        const isOpen = mobileMenu.classList.contains('show');
        
        if (isOpen) {
            // Close menu
            mobileMenu.classList.remove('show');
            overlay.classList.remove('show');
            document.body.classList.remove('mobile-menu-open');
            console.log('✅ Mobile menu closed');
        } else {
            // Open menu
            mobileMenu.classList.add('show');
            overlay.classList.add('show');
            document.body.classList.add('mobile-menu-open');
            console.log('✅ Mobile menu opened');
        }
    }
}

/**
 * Create mobile menu structure
 */
function createMobileMenu() {
    // Create overlay
    const overlay = document.createElement('div');
    overlay.className = 'mobile-menu-overlay';
    overlay.addEventListener('click', handleMobileMenuToggle);
    
    // Create menu
    const menu = document.createElement('div');
    menu.className = 'mobile-menu';
    
    // Set position based on navbar height
    const navbar = document.querySelector('.navbar');
    const navbarHeight = navbar ? navbar.offsetHeight : 70;
    menu.style.top = navbarHeight + 'px';
    
    menu.innerHTML = `
        <div class="mobile-menu-content">
            <div class="mobile-search-container">
                <div class="mobile-search">
                    <input type="text" class="mobile-search-input" placeholder="Search...">
                    <i class="mobile-search-icon fas fa-search"></i>
                </div>
            </div>
            <ul class="mobile-nav-links">
                <li><a href="/"><i class="fas fa-home"></i> Home</a></li>
                <li><a href="/About"><i class="fas fa-user-circle"></i> About</a></li>
                <li><a href="/Publications"><i class="fas fa-file-alt"></i> Publications</a></li>
                <li><a href="/Products"><i class="fas fa-cube"></i> Products</a></li>
                <li><a href="/Repository"><i class="fab fa-github"></i> Repository</a></li>
                <li><a href="/solutions"><i class="fas fa-lightbulb"></i> Solutions</a></li>
                <li><a href="/Contact"><i class="fas fa-envelope"></i> Contact</a></li>
                <li><a href="/Auth"><i class="fas fa-sign-in-alt"></i> Login / Account</a></li>
            </ul>
        </div>
    `;
    
    // Add click handlers to menu links
    const links = menu.querySelectorAll('.mobile-nav-links a');
    links.forEach(link => {
        link.addEventListener('click', handleMobileMenuToggle);
    });
    
    document.body.appendChild(overlay);
    document.body.appendChild(menu);
    
    console.log('✅ Mobile menu created');
}

/**
 * 🔍 SEARCH TOGGLE HANDLER (Fallback)
 * Only used if shared-components search fails
 */
function handleSearchToggle() {
    const globalSearch = document.getElementById('global-search');
    const searchInput = document.getElementById('global-search-input');
    const navCenter = document.querySelector('.nav-center');
    
    if (!globalSearch) {
        console.log('❌ Global search element not found');
        return;
    }
    
    const isExpanded = globalSearch.classList.contains('expanded');
    
    if (isExpanded) {
        // Collapse search
        globalSearch.classList.remove('expanded');
        globalSearch.style.width = '40px';
        
        // Restore nav links visibility
        if (navCenter) {
            navCenter.classList.remove('search-expanded');
        }
        
        const inputContainer = globalSearch.querySelector('.search-input-container');
        if (inputContainer) {
            inputContainer.style.opacity = '0';
            inputContainer.style.width = '0';
        }
        
        const searchIcon = globalSearch.querySelector('#search-icon-container');
        if (searchIcon) {
            searchIcon.style.borderRadius = '50%';
            searchIcon.style.borderRight = '1px solid var(--glass-border)';
        }
        
        if (searchInput) {
            searchInput.blur();
        }
        
        console.log('✅ Search collapsed');
    } else {
        // Expand search
        globalSearch.classList.add('expanded');
        globalSearch.style.width = '160px';
        
        // Collapse nav links to prevent overlap
        if (navCenter) {
            navCenter.classList.add('search-expanded');
        }
        
        setTimeout(() => {
            const inputContainer = globalSearch.querySelector('.search-input-container');
            if (inputContainer) {
                inputContainer.style.opacity = '1';
                inputContainer.style.width = 'calc(100% - 40px)';
            }
            
            const searchIcon = globalSearch.querySelector('#search-icon-container');
            if (searchIcon) {
                searchIcon.style.borderRadius = '20px 0 0 20px';
                searchIcon.style.borderRight = 'none';
            }
            
            if (searchInput) {
                searchInput.focus();
            }
        }, 50);
        
        console.log('✅ Search expanded');
    }
}

console.log('✅ Clean button handlers loaded');
