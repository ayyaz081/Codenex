// Contact Page Specific JavaScript

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
const contactApiUrl = `${backendBaseUrl}/api/contact`;

// Theme is handled by shared components now
const htmlElement = document.documentElement;
const currentTheme = localStorage.getItem('theme') || 'light';
htmlElement.setAttribute('data-theme', currentTheme);

// Contact form handling
document.getElementById('contact-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const successMessage = document.getElementById('success-message');
    const errorMessage = document.getElementById('error-message');
    const errorText = document.getElementById('error-text');
    
    // Hide previous messages
    successMessage.style.display = 'none';
    errorMessage.style.display = 'none';
    
    // Show loading state
    submitBtn.disabled = true;
    btnText.innerHTML = '<span class="loading-spinner"></span> Sending...';
    
    // Get form data
    const formData = new FormData(e.target);
    const contactData = {
        name: formData.get('name'),
        email: formData.get('email'),
        subject: formData.get('subject'),
        message: formData.get('message')
    };
    
    try {
        const response = await fetch(contactApiUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(contactData)
        });
        
        if (response.ok) {
            const result = await response.json();
            // Success
            successMessage.style.display = 'block';
            document.getElementById('contact-form').reset();
            
            // Clear any validation styling
            document.querySelectorAll('.form-control').forEach(input => {
                input.style.borderColor = 'var(--glass-border)';
            });
            
            // Scroll to success message
            successMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `Server error: ${response.status}`);
        }
    } catch (error) {
        console.error('Contact form error:', error);
        let errorMsgText = 'Failed to send message. Please try again or contact us directly.';
        
        // Handle specific error cases
        if (error.message.includes('Failed to fetch')) {
            errorMsgText = 'Unable to connect to server. Please check your connection and try again.';
        } else if (error.message.includes('400')) {
            errorMsgText = 'Please check that all required fields are filled out correctly.';
        } else if (error.message.includes('500')) {
            errorMsgText = 'Server error. Please try again later or contact us directly.';
        } else if (error.message) {
            errorMsgText = error.message;
        }
        
        errorText.textContent = errorMsgText;
        errorMessage.style.display = 'block';
        
        // Scroll to error message
        errorMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } finally {
        // Reset button state
        submitBtn.disabled = false;
        btnText.innerHTML = 'Send Message';
    }
});

// Form validation and UX improvements
document.querySelectorAll('.form-control').forEach(input => {
    // Add focus/blur effects
    input.addEventListener('focus', (e) => {
        e.target.parentElement.style.transform = 'translateY(-2px)';
    });
    
    input.addEventListener('blur', (e) => {
        e.target.parentElement.style.transform = 'translateY(0)';
    });
    
    // Real-time validation
    input.addEventListener('input', (e) => {
        const isValid = e.target.checkValidity();
        if (e.target.value.length > 0) {
            e.target.style.borderColor = isValid ? 'var(--success)' : 'var(--accent)';
        } else {
            e.target.style.borderColor = 'var(--glass-border)';
        }
    });
});

// Auto-resize textarea
document.getElementById('message').addEventListener('input', function() {
    this.style.height = 'auto';
    this.style.height = Math.max(140, this.scrollHeight) + 'px';
});

// Load the Google Maps embed focused on Virtual University Gojra campus
setTimeout(() => {
    const mapContainer = document.getElementById('map-container');
    // Updated coordinates for Virtual University Gojra, Punjab, Pakistan
    mapContainer.innerHTML = `
        <iframe 
            src="https://maps.google.com/maps?q=Virtual+University+Gojra+Punjab+Pakistan+PGJR002&hl=en&z=16&output=embed" 
            width="100%" 
            height="100%" 
            style="border:0; border-radius: 8px;" 
            allowfullscreen="" 
            loading="lazy" 
            referrerpolicy="no-referrer-when-downgrade" 
            title="Virtual University Gojra Campus Location">
        </iframe>
        <div style="margin-top: 16px; text-align: center;">
            <a href="https://maps.app.goo.gl/zf8LwNbv4wSxDRBT9" 
               target="_blank" 
               style="color: var(--primary); text-decoration: none; font-weight: 600; display: inline-flex; align-items: center; gap: 8px; padding: 8px 16px; border-radius: 8px; background: var(--glass-bg); border: 1px solid var(--glass-border); transition: all 0.3s ease;" 
               onmouseover="this.style.transform='translateY(-2px)'; this.style.boxShadow='0 4px 12px rgba(59, 130, 246, 0.3)';" 
               onmouseout="this.style.transform='translateY(0)'; this.style.boxShadow='none';">
                <i class="fas fa-map-marker-alt"></i> View Virtual University Gojra Campus
            </a>
        </div>
    `;
}, 1500);
