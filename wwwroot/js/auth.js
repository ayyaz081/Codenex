/**
 * Authentication JavaScript - Clean and minimal
 * Handles all auth functionality without redundancy
 */

class AuthManager {
    constructor() {
        this.backendBaseUrl = this.getBackendBaseUrl();
        this.authApiUrl = `${this.backendBaseUrl}/api/auth`;
        this.init();
    }

    getBackendBaseUrl() {
        if (typeof PortfolioConfig !== 'undefined' && PortfolioConfig.api && PortfolioConfig.api.getBaseUrl) {
            return PortfolioConfig.api.getBaseUrl();
        }
        if (window.API_BASE_URL) {
            return window.API_BASE_URL;
        }
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            return 'http://localhost:7150';
        }
        
        const hostname = window.location.hostname;
        const port = window.location.port;
        const protocol = hostname === 'localhost' || hostname === '127.0.0.1' 
                        ? window.location.protocol 
                        : 'https:';
        
        if (port && port !== '80' && port !== '443') {
            return `${protocol}//${hostname}:${port}`;
        } else {
            return `${protocol}//${hostname}`;
        }
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.setupTabs();
            this.setupEventListeners();
            this.checkAuthState();
            this.handleUrlParams();
        });
    }

    setupTabs() {
        const tabBtns = document.querySelectorAll('.tab-btn');
        tabBtns.forEach(btn => {
            if (btn?.dataset?.tab) {
                btn.addEventListener('click', () => this.switchTab(btn.dataset.tab));
            }
        });
    }

    switchTab(tabName) {
        // Remove active from all tabs and forms
        document.querySelectorAll('.tab-btn').forEach(btn => btn?.classList?.remove('active'));
        document.querySelectorAll('.auth-form').forEach(form => form?.classList?.remove('active'));
        
        // Activate target tab and form
        const targetBtn = document.querySelector(`[data-tab="${tabName}"]`);
        const targetForm = document.getElementById(`${tabName}-form`) || document.getElementById(`${tabName}-section`);
        
        if (targetBtn && targetForm) {
            targetBtn.classList.add('active');
            targetForm.classList.add('active');
        }
    }

    setupEventListeners() {
        // Auth forms
        document.getElementById('login-form')?.addEventListener('submit', (e) => this.handleLogin(e));
        document.getElementById('register-form')?.addEventListener('submit', (e) => this.handleRegister(e));
        document.getElementById('forgot-form')?.addEventListener('submit', (e) => this.handleForgotPassword(e));
        document.getElementById('verify-form')?.addEventListener('submit', (e) => this.handleEmailVerification(e));
        
        // Buttons
        document.getElementById('reset-password-btn')?.addEventListener('click', () => this.handleResetPassword());
        document.getElementById('resend-btn')?.addEventListener('click', () => this.handleResendVerification());
        document.getElementById('logout-btn')?.addEventListener('click', () => this.handleLogout());
        document.getElementById('dashboard-btn')?.addEventListener('click', () => this.handleDashboard());

        // Password strength indicators
        document.getElementById('reg-password')?.addEventListener('input', (e) => {
            this.updatePasswordStrength(e.target.value, 'reg-strength-fill', 'reg-strength-text');
        });
        document.getElementById('reset-password')?.addEventListener('input', (e) => {
            this.updatePasswordStrength(e.target.value, 'reset-strength-fill', 'reset-strength-text');
        });
    }

    async checkAuthState() {
        const token = localStorage.getItem('authToken');
        const userInfo = localStorage.getItem('userInfo');
        
        if (token && userInfo) {
            try {
                const user = JSON.parse(userInfo);
                const expiresAt = new Date(user.expiresAt);
                
                if (expiresAt > new Date()) {
                    await this.loadUserProfile();
                    this.showAuthenticatedTabs();
                    this.switchTab('profile');
                    return true;
                } else {
                    this.clearAuthData();
                }
            } catch (error) {
                this.clearAuthData();
            }
        }
        
        this.showUnauthenticatedTabs();
        return false;
    }

    showAuthenticatedTabs() {
        document.querySelectorAll('.tab-btn').forEach(btn => {
            if (btn?.dataset?.tab) {
                if (['login', 'register'].includes(btn.dataset.tab)) {
                    btn.style.display = 'none';
                } else if (btn.dataset.tab === 'profile') {
                    btn.style.display = 'block';
                }
            }
        });
    }

    showUnauthenticatedTabs() {
        document.querySelectorAll('.tab-btn').forEach(btn => {
            if (btn?.dataset?.tab) {
                if (['login', 'register', 'forgot', 'verify'].includes(btn.dataset.tab)) {
                    btn.style.display = 'block';
                } else if (btn.dataset.tab === 'profile') {
                    btn.style.display = 'none';
                }
            }
        });
    }

    async loadUserProfile() {
        try {
            const token = localStorage.getItem('authToken');
            const response = await fetch(`${this.authApiUrl}/profile`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const user = await response.json();
                this.displayUserProfile(user);
            } else {
                const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}');
                this.displayUserProfile(userInfo);
            }
        } catch (error) {
            console.error('Error loading profile:', error);
            const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}');
            this.displayUserProfile(userInfo);
        }
    }

    displayUserProfile(user) {
        const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();
        
        const elements = {
            avatar: document.getElementById('user-avatar'),
            name: document.getElementById('user-name'),
            email: document.getElementById('user-email'),
            role: document.getElementById('user-role'),
            status: document.getElementById('user-status'),
            dashboardBtn: document.getElementById('dashboard-btn')
        };

        if (elements.avatar) elements.avatar.textContent = initials;
        if (elements.name) elements.name.textContent = `${user.firstName || ''} ${user.lastName || ''}`;
        if (elements.email) elements.email.textContent = user.email || '';
        if (elements.role) elements.role.textContent = user.role || '';
        
        if (elements.status) {
            if (user.emailVerified) {
                elements.status.textContent = 'Email Verified';
                elements.status.className = 'status-badge status-verified';
            } else {
                elements.status.textContent = 'Email Unverified';
                elements.status.className = 'status-badge status-unverified';
                
                const resendField = document.getElementById('resend-email');
                if (resendField && user.email) {
                    resendField.value = user.email;
                }
            }
        }

        // Show/hide dashboard button based on role
        if (elements.dashboardBtn) {
            elements.dashboardBtn.style.display = 
                (user.role === 'Admin' || user.role === 'Manager') ? 'inline-flex' : 'none';
        }
    }

    handleUrlParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const userId = urlParams.get('userId');
        const token = urlParams.get('token');
        const action = urlParams.get('action');
        const returnUrl = urlParams.get('returnUrl');
        
        if (action === 'verify' && userId && token) {
            this.switchTab('verify');
            const tokenInput = document.getElementById('verify-token');
            if (tokenInput) tokenInput.value = token;
            sessionStorage.setItem('verifyUserId', userId);
            this.showMessage('Click "Verify Email" to confirm your email address', 'info');
        } else if (action === 'reset' && userId && token) {
            this.switchTab('forgot');
            const resetSection = document.getElementById('reset-section');
            const tokenInput = document.getElementById('reset-token');
            if (resetSection) resetSection.style.display = 'block';
            if (tokenInput) tokenInput.value = token;
            sessionStorage.setItem('resetUserId', userId);
            this.showMessage('Enter your new password below', 'info');
        } else if (returnUrl && ['download', 'comment', 'rate'].includes(action)) {
            const actionText = {
                'download': 'download the publication',
                'comment': 'post a comment', 
                'rate': 'rate the publication'
            }[action] || action;
            this.showMessage(`Please log in to ${actionText}. You'll be redirected back after authentication.`, 'info');
        }
    }

    async handleLogin(e) {
        e.preventDefault();
        const email = document.getElementById('login-email')?.value.trim();
        const password = document.getElementById('login-password')?.value;

        if (!this.validateEmail(email) || !password) {
            this.showMessage('Please enter valid email and password', 'error');
            return;
        }

        const btn = document.getElementById('login-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Signing In...';
            }
            
            const response = await fetch(`${this.authApiUrl}/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password, rememberMe: false })
            });

            const result = await response.json();

            if (response.ok) {
                localStorage.setItem('authToken', result.token);
                localStorage.setItem('userInfo', JSON.stringify({
                    email: result.email,
                    firstName: result.firstName,
                    lastName: result.lastName,
                    role: result.role,
                    userId: result.userId,
                    emailVerified: result.emailVerified,
                    expiresAt: result.expiresAt
                }));

                this.showMessage('Login successful!', 'success');
                await this.loadUserProfile();
                this.showAuthenticatedTabs();
                this.switchTab('profile');
                this.handlePostLoginRedirect();
            } else {
                this.showMessage(result.message || 'Login failed', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred during login', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    async handleRegister(e) {
        e.preventDefault();
        const firstName = document.getElementById('reg-firstname')?.value.trim();
        const lastName = document.getElementById('reg-lastname')?.value.trim();
        const email = document.getElementById('reg-email')?.value.trim();
        const password = document.getElementById('reg-password')?.value;
        const confirmPassword = document.getElementById('reg-confirm')?.value;

        if (!firstName || !lastName || !this.validateEmail(email) || !password || password !== confirmPassword) {
            this.showMessage('Please fill all fields correctly and ensure passwords match', 'error');
            return;
        }

        const btn = document.getElementById('register-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Creating Account...';
            }
            
            const response = await fetch(`${this.authApiUrl}/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ firstName, lastName, email, password, confirmPassword })
            });

            const result = await response.json();

            if (response.ok) {
                localStorage.setItem('authToken', result.token);
                localStorage.setItem('userInfo', JSON.stringify({
                    email: result.email,
                    firstName: result.firstName,
                    lastName: result.lastName,
                    role: result.role,
                    userId: result.userId,
                    emailVerified: result.emailVerified,
                    expiresAt: result.expiresAt
                }));

                this.showMessage('Account created successfully!', 'success');
                await this.loadUserProfile();
                this.showAuthenticatedTabs();
                this.switchTab('profile');
                this.handlePostLoginRedirect();
            } else {
                this.showMessage(result.message || 'Registration failed', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred during registration', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    async handleForgotPassword(e) {
        e.preventDefault();
        const email = document.getElementById('forgot-email')?.value.trim();

        if (!this.validateEmail(email)) {
            this.showMessage('Please enter a valid email address', 'error');
            return;
        }

        const btn = document.getElementById('forgot-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Sending...';
            }
            
            const response = await fetch(`${this.authApiUrl}/forgot-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });

            const result = await response.json();

            if (response.ok) {
                this.showMessage(result.message, 'success');
                const resetSection = document.getElementById('reset-section');
                if (resetSection) resetSection.style.display = 'block';
            } else {
                this.showMessage(result.message || 'Failed to send reset email', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred. Please try again.', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    async handleResetPassword() {
        const token = document.getElementById('reset-token')?.value.trim();
        const newPassword = document.getElementById('reset-password')?.value;
        const confirmPassword = document.getElementById('reset-confirm')?.value;
        const resetUserId = sessionStorage.getItem('resetUserId');

        if (!token || !newPassword || newPassword !== confirmPassword) {
            this.showMessage('Please fill all fields and ensure passwords match', 'error');
            return;
        }

        const btn = document.getElementById('reset-password-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Resetting...';
            }
            
            const response = await fetch(`${this.authApiUrl}/reset-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    userId: resetUserId || '',
                    token,
                    newPassword,
                    confirmPassword
                })
            });

            const result = await response.json();

            if (response.ok) {
                this.showMessage('Password reset successfully! You can now log in.', 'success');
                const resetSection = document.getElementById('reset-section');
                if (resetSection) resetSection.style.display = 'none';
                sessionStorage.removeItem('resetUserId');
                this.switchTab('login');
            } else {
                this.showMessage(result.message || 'Failed to reset password', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred. Please try again.', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    async handleEmailVerification(e) {
        e.preventDefault();
        const token = document.getElementById('verify-token')?.value.trim();
        const verifyUserId = sessionStorage.getItem('verifyUserId');

        if (!token) {
            this.showMessage('Please enter the verification token', 'error');
            return;
        }

        const btn = document.getElementById('verify-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Verifying...';
            }
            
            const response = await fetch(`${this.authApiUrl}/verify-email?userId=${verifyUserId || ''}&token=${encodeURIComponent(token)}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            const result = await response.json();

            if (response.ok) {
                this.showMessage('Email verified successfully!', 'success');
                sessionStorage.removeItem('verifyUserId');
                
                const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}');
                if (userInfo.userId || verifyUserId) {
                    userInfo.emailVerified = true;
                    localStorage.setItem('userInfo', JSON.stringify(userInfo));
                    await this.loadUserProfile();
                }
            } else {
                this.showMessage(result.message || 'Email verification failed', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred during verification', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    async handleResendVerification() {
        const email = document.getElementById('resend-email')?.value.trim();

        if (!this.validateEmail(email)) {
            this.showMessage('Please enter a valid email address', 'error');
            return;
        }

        // Rate limiting
        const lastResendTime = localStorage.getItem(`lastResend_${email}`);
        const now = Date.now();
        if (lastResendTime && (now - parseInt(lastResendTime)) < 60000) {
            const remainingTime = Math.ceil((60000 - (now - parseInt(lastResendTime))) / 1000);
            this.showMessage(`Please wait ${remainingTime} seconds before requesting another verification email.`, 'error');
            return;
        }

        const btn = document.getElementById('resend-btn');
        const originalContent = btn?.innerHTML;
        
        try {
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<div class="loading-spinner"></div> Sending...';
            }
            
            const response = await fetch(`${this.authApiUrl}/resend-verification`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });

            const result = await response.json();

            if (response.ok) {
                this.showMessage('Verification email sent! Please check your inbox.', 'success');
                localStorage.setItem(`lastResend_${email}`, now.toString());
                const resendEmailInput = document.getElementById('resend-email');
                if (resendEmailInput) resendEmailInput.value = '';
            } else {
                this.showMessage(result.message || 'Failed to resend verification email', 'error');
            }
        } catch (error) {
            this.showMessage('An error occurred. Please try again.', 'error');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = originalContent;
            }
        }
    }

    handleLogout() {
        if (confirm('Are you sure you want to sign out?')) {
            this.clearAuthData();
            this.showMessage('You have been signed out successfully', 'success');
            this.showUnauthenticatedTabs();
            this.switchTab('login');
        }
    }

    handleDashboard() {
        const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}');
        if (userInfo.role === 'Admin' || userInfo.role === 'Manager') {
            window.location.href = '/Admin';
        } else {
            window.location.href = '/';
        }
    }

    handlePostLoginRedirect() {
        const urlParams = new URLSearchParams(window.location.search);
        const returnUrl = urlParams.get('returnUrl');
        const action = urlParams.get('action');
        const publicationId = urlParams.get('publicationId');
        
        if (returnUrl && action && publicationId) {
            sessionStorage.setItem('pendingAction', JSON.stringify({
                action: action,
                publicationId: publicationId,
                returnUrl: decodeURIComponent(returnUrl)
            }));
            
            setTimeout(() => {
                window.location.href = decodeURIComponent(returnUrl);
            }, 1500);
        } else if (returnUrl) {
            setTimeout(() => {
                window.location.href = decodeURIComponent(returnUrl);
            }, 1500);
        }
    }

    validateEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    updatePasswordStrength(password, fillId, textId) {
        const strengthFill = document.getElementById(fillId);
        const strengthText = document.getElementById(textId);
        
        if (!strengthFill || !strengthText) return;

        let score = 0;
        let strength = 'None';
        
        if (password.length >= 8) score++;
        if (password.match(/[a-z]/)) score++;
        if (password.match(/[A-Z]/)) score++;
        if (password.match(/[0-9]/)) score++;
        if (password.match(/[^A-Za-z0-9]/)) score++;
        
        strengthFill.className = 'strength-fill';
        
        switch (score) {
            case 0:
            case 1:
                strength = 'Weak';
                strengthFill.classList.add('strength-weak');
                break;
            case 2:
                strength = 'Fair';
                strengthFill.classList.add('strength-fair');
                break;
            case 3:
            case 4:
                strength = 'Good';
                strengthFill.classList.add('strength-good');
                break;
            case 5:
                strength = 'Strong';
                strengthFill.classList.add('strength-strong');
                break;
        }
        
        strengthText.textContent = `Password strength: ${strength}`;
    }

    showMessage(message, type) {
        const messageEl = document.getElementById('message');
        if (messageEl) {
            messageEl.textContent = message;
            messageEl.className = `message ${type}`;
            messageEl.style.display = 'block';
            
            setTimeout(() => {
                messageEl.style.display = 'none';
            }, 5000);
        }
    }

    clearAuthData() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userInfo');
    }
}

// Initialize auth manager
new AuthManager();
