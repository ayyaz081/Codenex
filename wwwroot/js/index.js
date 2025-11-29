// Index Page Specific JavaScript

document.addEventListener('DOMContentLoaded', function() {
    console.log('🚀 Starting initialization...');
    
    // Initialize SharedComponents which handles all header functionality
    if (typeof window !== 'undefined' && window.sharedComponents) {
        console.log('✅ Using existing SharedComponents instance');
    } else if (typeof SharedComponents !== 'undefined') {
        console.log('🔧 SharedComponents will be initialized by shared-components.js');
    } else {
        console.error('❌ SharedComponents class not found!');
    }

    // Carousel functionality
    const slides = document.querySelector('.carousel-slides');
    const dots = document.querySelectorAll('.carousel-dot');
    let currentSlide = 0;
    const totalSlides = 3;

    function goToSlide(index) {
        slides.style.transform = `translateX(-${index * 100 / totalSlides}%)`;
        dots.forEach(dot => dot.classList.remove('active'));
        dots[index].classList.add('active');
        currentSlide = index;
    }

    dots.forEach(dot => {
        dot.addEventListener('click', () => {
            goToSlide(parseInt(dot.dataset.slide));
        });
    });

    // Auto-advance carousel
    setInterval(() => {
        currentSlide = (currentSlide + 1) % totalSlides;
        goToSlide(currentSlide);
    }, 5000);

    // Parallax scroll effect
    window.addEventListener('scroll', () => {
        const card = document.querySelector('.overview-card[data-parallax]');
        if (card) {
            const scrollPosition = window.scrollY;
            const cardPosition = card.getBoundingClientRect().top + window.scrollY;
            const offset = (scrollPosition - cardPosition) * 0.1;
            card.style.transform = `perspective(1000px) translateZ(0) translateY(${offset}px)`;
        }
    });

});
