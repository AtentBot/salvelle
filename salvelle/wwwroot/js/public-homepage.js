/**
 * Salvelle - Public Homepage JavaScript
 * Handles homepage interactivity, animations, and contact form
 */

document.addEventListener('DOMContentLoaded', function() {
    
    // ============================================
    // SMOOTH SCROLL FOR NAVIGATION LINKS
    // ============================================
    const smoothScrollLinks = document.querySelectorAll('a[href^="#"]');
    smoothScrollLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            const targetId = this.getAttribute('href');
            if (targetId !== '#') {
                e.preventDefault();
                const targetElement = document.querySelector(targetId);
                if (targetElement) {
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });

    // ============================================
    // NAVBAR SCROLL EFFECT
    // ============================================
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        window.addEventListener('scroll', function() {
            if (window.scrollY > 50) {
                navbar.classList.add('navbar-scrolled');
            } else {
                navbar.classList.remove('navbar-scrolled');
            }
        });
    }

    // ============================================
    // ANIMATE ON SCROLL
    // ============================================
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    const animatedElements = document.querySelectorAll('.animate-on-scroll');
    animatedElements.forEach(el => observer.observe(el));

    // ============================================
    // PRICING TOGGLE (MONTHLY/YEARLY)
    // ============================================
    const pricingToggle = document.getElementById('pricingToggle');
    const monthlyPrices = document.querySelectorAll('.price-monthly');
    const yearlyPrices = document.querySelectorAll('.price-yearly');
    
    if (pricingToggle) {
        pricingToggle.addEventListener('change', function() {
            const isYearly = this.checked;
            
            monthlyPrices.forEach(el => {
                el.style.display = isYearly ? 'none' : 'block';
            });
            
            yearlyPrices.forEach(el => {
                el.style.display = isYearly ? 'block' : 'none';
            });

            // Update button links
            const pricingButtons = document.querySelectorAll('.btn-pricing');
            pricingButtons.forEach(btn => {
                const planId = btn.dataset.planId;
                if (planId) {
                    const cycle = isYearly ? 'yearly' : 'monthly';
                    btn.href = `/signup?plan=${planId}&cycle=${cycle}`;
                }
            });
        });
    }

    // ============================================
    // CONTACT FORM VALIDATION & SUBMISSION
    // ============================================
    const contactForm = document.getElementById('contactForm');
    if (contactForm) {
        contactForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            clearFormErrors(contactForm);
            
            const formData = {
                name: document.getElementById('contactName').value.trim(),
                email: document.getElementById('contactEmail').value.trim(),
                phone: document.getElementById('contactPhone')?.value.trim() || '',
                subject: document.getElementById('contactSubject').value.trim(),
                message: document.getElementById('contactMessage').value.trim()
            };

            const errors = validateContactForm(formData);
            if (errors.length > 0) {
                displayFormErrors(contactForm, errors);
                return;
            }

            const submitBtn = contactForm.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enviando...';
            submitBtn.disabled = true;

            try {
                await new Promise(resolve => setTimeout(resolve, 1500));
                showAlert('success', 'Mensagem enviada com sucesso! Entraremos em contato em breve.');
                contactForm.reset();
            } catch (error) {
                showAlert('danger', 'Erro ao enviar mensagem. Por favor, tente novamente.');
            } finally {
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        });
    }

    // ============================================
    // STATISTICS COUNTER ANIMATION
    // ============================================
    const counters = document.querySelectorAll('.counter');
    counters.forEach(counter => {
        const target = parseInt(counter.dataset.target);
        const duration = 2000;
        const increment = target / (duration / 16);
        let current = 0;

        const counterObserver = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const updateCounter = () => {
                        current += increment;
                        if (current < target) {
                            counter.textContent = Math.floor(current).toLocaleString('pt-BR');
                            requestAnimationFrame(updateCounter);
                        } else {
                            counter.textContent = target.toLocaleString('pt-BR');
                        }
                    };
                    updateCounter();
                    counterObserver.unobserve(counter);
                }
            });
        });

        counterObserver.observe(counter);
    });
});

// ============================================
// UTILITY FUNCTIONS
// ============================================

function validateContactForm(data) {
    const errors = [];
    
    if (!data.name || data.name.length < 3) {
        errors.push({ field: 'contactName', message: 'Nome deve ter pelo menos 3 caracteres' });
    }
    
    if (!isValidEmail(data.email)) {
        errors.push({ field: 'contactEmail', message: 'E-mail inválido' });
    }
    
    if (!data.subject || data.subject.length < 3) {
        errors.push({ field: 'contactSubject', message: 'Assunto é obrigatório' });
    }
    
    if (!data.message || data.message.length < 10) {
        errors.push({ field: 'contactMessage', message: 'Mensagem deve ter pelo menos 10 caracteres' });
    }
    
    return errors;
}

function isValidEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

function displayFormErrors(form, errors) {
    errors.forEach(error => {
        const field = form.querySelector(`#${error.field}`);
        if (field) {
            field.classList.add('is-invalid');
            
            let feedback = field.nextElementSibling;
            if (!feedback || !feedback.classList.contains('invalid-feedback')) {
                feedback = document.createElement('div');
                feedback.className = 'invalid-feedback';
                field.parentNode.insertBefore(feedback, field.nextSibling);
            }
            feedback.textContent = error.message;
        }
    });
}

function clearFormErrors(form) {
    const invalidFields = form.querySelectorAll('.is-invalid');
    invalidFields.forEach(field => field.classList.remove('is-invalid'));
    
    const feedbacks = form.querySelectorAll('.invalid-feedback');
    feedbacks.forEach(feedback => feedback.textContent = '');
}

function showAlert(type, message) {
    const existingAlerts = document.querySelectorAll('.alert-floating');
    existingAlerts.forEach(alert => alert.remove());
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-floating alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alert);
    
    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 300);
    }, 5000);
}
