// 404 Page Specific JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Add some interactivity
    const errorCode = document.querySelector('.error-code');
    const floatingElements = document.querySelectorAll('.floating-element');
    
    // Add click animation to error code
    errorCode.addEventListener('click', function() {
        this.style.animation = 'none';
        setTimeout(() => {
            this.style.animation = 'bounce 2s infinite';
        }, 100);
    });
    
    // Randomize floating element positions
    floatingElements.forEach((element, index) => {
        const randomDelay = Math.random() * 5;
        element.style.animationDelay = randomDelay + 's';
    });
});
