// Email Verified Page Specific JavaScript

// Get URL parameters
const urlParams = new URLSearchParams(window.location.search);
const userId = urlParams.get('userId');
const token = urlParams.get('token');

if (userId && token) {
    // Call the verification API
    fetch(`/api/auth/verify-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (!response.ok) {
            throw new Error(data.message || 'Verification failed');
        }
        // Success - page already shows success message
    })
    .catch(error => {
        // Show error message
        document.getElementById('content').innerHTML = `
            <div class="error">
                <div class="success-icon">✗</div>
                <h1>Verification Failed</h1>
                <p>The verification link is invalid or has expired. Please try registering again.</p>
                <a href="/Auth" class="btn">Go to Register</a>
            </div>
        `;
    });
} else {
    // No parameters - show error
    document.getElementById('content').innerHTML = `
        <div class="error">
            <div class="success-icon">✗</div>
            <h1>Invalid Link</h1>
            <p>This verification link appears to be invalid.</p>
            <a href="/Auth" class="btn">Go to Register</a>
        </div>
    `;
}
