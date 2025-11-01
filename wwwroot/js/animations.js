/**
 * Professional Animations System
 * Handles scroll-triggered animations, parallax effects, and interactive animations
 */

class AnimationController {
    constructor() {
        this.observerOptions = {
            root: null,
            rootMargin: '0px',
            threshold: 0.1
        };
        
        this.init();
    }

    init() {
        // Initialize all animation features
        this.setupScrollAnimations();
        this.setupParallaxEffects();
        this.setupCardAnimations();
        this.setupButtonRipples();
        this.setupCounterAnimations();
        this.setupImageReveal();
    }

    /**
     * Scroll-triggered animations using Intersection Observer
     */
    setupScrollAnimations() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    // Add staggered delay for multiple elements
                    setTimeout(() => {
                        entry.target.classList.add('animated');
                        
                        // Apply specific animation classes
                        const animationType = entry.target.dataset.animation || 'fade-in-up';
                        entry.target.classList.add(animationType);
                    }, index * 100);
                    
                    // Unobserve after animation
                    observer.unobserve(entry.target);
                }
            });
        }, this.observerOptions);

        // Observe all elements with animation classes
        const animateElements = document.querySelectorAll(
            '.animate-on-scroll, .feature-card, .overview-card, .expertise-card, .team-card, .product-card'
        );
        
        animateElements.forEach(el => {
            el.classList.add('animate-on-scroll');
            observer.observe(el);
        });
    }

    /**
     * Parallax scrolling effect for hero sections and backgrounds
     */
    setupParallaxEffects() {
        const parallaxElements = document.querySelectorAll('[data-parallax]');
        
        if (parallaxElements.length === 0) return;

        let ticking = false;

        const updateParallax = () => {
            const scrolled = window.pageYOffset;

            parallaxElements.forEach(element => {
                const speed = element.dataset.parallaxSpeed || 0.5;
                const yPos = -(scrolled * speed);
                element.style.transform = `translateY(${yPos}px)`;
            });

            ticking = false;
        };

        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(updateParallax);
                ticking = true;
            }
        });
    }

    /**
     * Enhanced card hover animations
     */
    setupCardAnimations() {
        const cards = document.querySelectorAll(
            '.feature-card, .overview-card, .expertise-card, .team-card, .product-card'
        );

        cards.forEach(card => {
            // Add hover lift effect
            card.classList.add('card-hover-lift');

            // 3D tilt effect on mouse move
            card.addEventListener('mousemove', (e) => {
                const rect = card.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                const centerX = rect.width / 2;
                const centerY = rect.height / 2;

                const rotateX = (y - centerY) / 10;
                const rotateY = (centerX - x) / 10;

                card.style.transform = `
                    translateY(-8px) 
                    perspective(1000px) 
                    rotateX(${rotateX}deg) 
                    rotateY(${rotateY}deg)
                    scale(1.02)
                `;
            });

            card.addEventListener('mouseleave', () => {
                card.style.transform = 'translateY(0) perspective(1000px) rotateX(0) rotateY(0) scale(1)';
            });
        });
    }

    /**
     * Material Design ripple effect for buttons
     */
    setupButtonRipples() {
        const buttons = document.querySelectorAll('button, .btn, .cta-button, a[class*="btn"]');

        buttons.forEach(button => {
            button.classList.add('ripple-effect');
            
            button.addEventListener('click', function(e) {
                const ripple = document.createElement('span');
                const rect = this.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;

                ripple.style.width = ripple.style.height = size + 'px';
                ripple.style.left = x + 'px';
                ripple.style.top = y + 'px';
                ripple.classList.add('ripple');

                this.appendChild(ripple);

                setTimeout(() => {
                    ripple.remove();
                }, 600);
            });
        });

        // Add ripple animation
        const style = document.createElement('style');
        style.textContent = `
            .ripple {
                position: absolute;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.4);
                transform: scale(0);
                animation: rippleEffect 0.6s ease-out;
                pointer-events: none;
            }
            
            @keyframes rippleEffect {
                to {
                    transform: scale(4);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }

    /**
     * Animated counters for statistics
     */
    setupCounterAnimations() {
        const counters = document.querySelectorAll('[data-counter]');

        const animateCounter = (element) => {
            const target = parseInt(element.dataset.counter);
            const duration = 2000;
            const step = target / (duration / 16);
            let current = 0;

            const updateCounter = () => {
                current += step;
                if (current < target) {
                    element.textContent = Math.floor(current);
                    requestAnimationFrame(updateCounter);
                } else {
                    element.textContent = target;
                }
            };

            updateCounter();
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, this.observerOptions);

        counters.forEach(counter => observer.observe(counter));
    }

    /**
     * Progressive image reveal with blur effect
     */
    setupImageReveal() {
        const images = document.querySelectorAll('img[data-animate]');

        images.forEach(img => {
            img.style.filter = 'blur(10px)';
            img.style.transition = 'filter 0.5s ease';

            if (img.complete) {
                img.style.filter = 'blur(0)';
            } else {
                img.addEventListener('load', () => {
                    img.style.filter = 'blur(0)';
                });
            }
        });
    }

    /**
     * Smooth scroll to anchor links
     */
    setupSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                if (href === '#') return;

                e.preventDefault();
                const target = document.querySelector(href);
                
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    /**
     * Add loading animation to page transitions
     */
    setupPageTransitions() {
        // Add fade-in animation on page load
        document.body.classList.add('page-transition-enter');

        // Add fade-out animation on page unload
        window.addEventListener('beforeunload', () => {
            document.body.classList.add('page-transition-exit');
        });
    }

    /**
     * Stagger animation for lists and grids
     */
    staggerAnimation(selector, animationClass = 'fade-in-up', delay = 100) {
        const elements = document.querySelectorAll(selector);
        
        elements.forEach((element, index) => {
            element.style.opacity = '0';
            setTimeout(() => {
                element.style.opacity = '1';
                element.classList.add(animationClass);
            }, index * delay);
        });
    }

    /**
     * Text reveal animation (typewriter effect)
     */
    setupTextReveal() {
        const textElements = document.querySelectorAll('[data-text-reveal]');

        textElements.forEach(element => {
            const text = element.textContent;
            element.textContent = '';
            element.style.opacity = '1';

            let i = 0;
            const typeWriter = () => {
                if (i < text.length) {
                    element.textContent += text.charAt(i);
                    i++;
                    setTimeout(typeWriter, 50);
                }
            };

            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        typeWriter();
                        observer.unobserve(entry.target);
                    }
                });
            }, this.observerOptions);

            observer.observe(element);
        });
    }

    /**
     * Progress bar animation
     */
    animateProgressBars() {
        const progressBars = document.querySelectorAll('[data-progress]');

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const bar = entry.target;
                    const progress = bar.dataset.progress;
                    bar.style.width = progress + '%';
                    observer.unobserve(bar);
                }
            });
        }, this.observerOptions);

        progressBars.forEach(bar => {
            bar.style.width = '0%';
            bar.style.transition = 'width 1.5s ease-out';
            observer.observe(bar);
        });
    }
}

// Initialize animations when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new AnimationController();
    });
} else {
    new AnimationController();
}

// Export for potential module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AnimationController;
}
