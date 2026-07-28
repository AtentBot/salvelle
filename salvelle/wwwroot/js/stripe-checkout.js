/**
 * Salvelle - Stripe Integration JavaScript
 * Handles Stripe Checkout and Customer Portal
 */

// Initialize Stripe (publishable key will be injected from backend)
let stripe = null;

document.addEventListener('DOMContentLoaded', function() {
    // Get publishable key from meta tag
    const stripeKeyMeta = document.querySelector('meta[name="stripe-publishable-key"]');
    if (stripeKeyMeta) {
        const publishableKey = stripeKeyMeta.content;
        stripe = Stripe(publishableKey);
    }

    initializePaymentPage();
    initializeCustomerPortal();
});

// ============================================
// PAYMENT PAGE - STRIPE CHECKOUT
// ============================================

function initializePaymentPage() {
    const startTrialBtn = document.getElementById('startTrialBtn');
    
    if (startTrialBtn) {
        startTrialBtn.addEventListener('click', async function(e) {
            e.preventDefault();
            
            const establishmentId = this.dataset.establishmentId;
            const planId = this.dataset.planId;
            const billingCycle = document.querySelector('input[name="billingCycle"]:checked')?.value || 'monthly';

            if (!establishmentId || !planId) {
                showAlert('danger', 'Dados de pagamento incompletos. Por favor, volte ao cadastro.');
                return;
            }

            await createCheckoutSession(establishmentId, planId, billingCycle);
        });
    }

    // Billing cycle toggle
    const billingCycleInputs = document.querySelectorAll('input[name="billingCycle"]');
    billingCycleInputs.forEach(input => {
        input.addEventListener('change', function() {
            updatePriceDisplay(this.value);
        });
    });
}

async function createCheckoutSession(establishmentId, planId, billingCycle) {
    const btn = document.getElementById('startTrialBtn');
    const originalText = btn.innerHTML;
    
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processando...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/stripe/create-checkout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                establishmentId: establishmentId,
                planId: planId,
                billingCycle: billingCycle,
                successUrl: `${window.location.origin}/api/stripe/success?session_id={CHECKOUT_SESSION_ID}`,
                cancelUrl: `${window.location.origin}/signup/payment?establishmentId=${establishmentId}&planId=${planId}&canceled=true`
            })
        });

        const data = await response.json();

        if (response.ok && data.sessionId) {
            // Initialize Stripe if not already done
            if (!stripe && data.publishableKey) {
                stripe = Stripe(data.publishableKey);
            }

            if (!stripe) {
                throw new Error('Stripe não inicializado corretamente');
            }

            // Redirect to Stripe Checkout
            const result = await stripe.redirectToCheckout({
                sessionId: data.sessionId
            });

            if (result.error) {
                throw new Error(result.error.message);
            }
        } else {
            throw new Error(data.message || 'Erro ao criar sessão de pagamento');
        }
    } catch (error) {
        console.error('Checkout error:', error);
        showAlert('danger', error.message || 'Erro ao processar pagamento. Tente novamente.');
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

function updatePriceDisplay(billingCycle) {
    const monthlyPrice = document.querySelector('.price-display.monthly');
    const yearlyPrice = document.querySelector('.price-display.yearly');
    const savingsNote = document.querySelector('.savings-note');

    if (monthlyPrice && yearlyPrice) {
        if (billingCycle === 'yearly') {
            monthlyPrice.style.display = 'none';
            yearlyPrice.style.display = 'block';
            if (savingsNote) savingsNote.style.display = 'block';
        } else {
            monthlyPrice.style.display = 'block';
            yearlyPrice.style.display = 'none';
            if (savingsNote) savingsNote.style.display = 'none';
        }
    }
}

// ============================================
// CUSTOMER PORTAL - MANAGE SUBSCRIPTION
// ============================================

function initializeCustomerPortal() {
    const manageSubBtn = document.getElementById('manageSubscriptionBtn');
    
    if (manageSubBtn) {
        manageSubBtn.addEventListener('click', async function(e) {
            e.preventDefault();
            
            const establishmentId = this.dataset.establishmentId;
            
            if (!establishmentId) {
                showAlert('danger', 'Dados não encontrados.');
                return;
            }

            await openCustomerPortal(establishmentId);
        });
    }
}

async function openCustomerPortal(establishmentId) {
    const btn = document.getElementById('manageSubscriptionBtn');
    const originalText = btn.innerHTML;
    
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Abrindo...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/stripe/create-portal-session', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                establishmentId: establishmentId,
                returnUrl: window.location.href
            })
        });

        const data = await response.json();

        if (response.ok && data.url) {
            // Redirect to Stripe Customer Portal
            window.location.href = data.url;
        } else {
            throw new Error(data.message || 'Erro ao abrir portal do cliente');
        }
    } catch (error) {
        console.error('Portal error:', error);
        showAlert('danger', error.message || 'Erro ao abrir portal. Tente novamente.');
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

// ============================================
// SUBSCRIPTION STATUS INDICATOR
// ============================================

function updateSubscriptionStatus() {
    const statusBadge = document.querySelector('.subscription-status-badge');
    if (!statusBadge) return;

    const status = statusBadge.dataset.status;
    const statusMap = {
        'TRIALING': {
            class: 'bg-info',
            icon: 'bi-clock-history',
            text: 'Em Período de Teste'
        },
        'ACTIVE': {
            class: 'bg-success',
            icon: 'bi-check-circle',
            text: 'Ativa'
        },
        'PAST_DUE': {
            class: 'bg-warning',
            icon: 'bi-exclamation-triangle',
            text: 'Pagamento Pendente'
        },
        'CANCELED': {
            class: 'bg-danger',
            icon: 'bi-x-circle',
            text: 'Cancelada'
        },
        'INCOMPLETE': {
            class: 'bg-secondary',
            icon: 'bi-hourglass',
            text: 'Incompleta'
        }
    };

    const statusInfo = statusMap[status] || statusMap['INCOMPLETE'];
    
    statusBadge.className = `badge ${statusInfo.class} subscription-status-badge`;
    statusBadge.innerHTML = `<i class="bi ${statusInfo.icon} me-1"></i>${statusInfo.text}`;
}

// Call on page load
document.addEventListener('DOMContentLoaded', updateSubscriptionStatus);

// ============================================
// INVOICE MANAGEMENT
// ============================================

async function downloadInvoice(invoiceId) {
    const btn = event.target.closest('button');
    const originalText = btn.innerHTML;
    
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
    btn.disabled = true;

    try {
        const response = await fetch(`/api/stripe/invoices/${invoiceId}/download`, {
            method: 'GET'
        });

        if (response.ok) {
            const data = await response.json();
            if (data.pdfUrl) {
                window.open(data.pdfUrl, '_blank');
            } else {
                throw new Error('URL do PDF não disponível');
            }
        } else {
            throw new Error('Erro ao obter fatura');
        }
    } catch (error) {
        console.error('Invoice download error:', error);
        showAlert('danger', 'Erro ao baixar fatura. Tente novamente.');
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

// ============================================
// TRIAL COUNTDOWN
// ============================================

function initializeTrialCountdown() {
    const countdownElement = document.getElementById('trialCountdown');
    if (!countdownElement) return;

    const trialEndDate = new Date(countdownElement.dataset.trialEnd);
    
    function updateCountdown() {
        const now = new Date();
        const diff = trialEndDate - now;

        if (diff <= 0) {
            countdownElement.innerHTML = '<span class="text-danger">Período de teste expirado</span>';
            return;
        }

        const days = Math.floor(diff / (1000 * 60 * 60 * 24));
        const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

        let countdownText = '';
        if (days > 0) {
            countdownText = `${days} dia${days > 1 ? 's' : ''}`;
        } else if (hours > 0) {
            countdownText = `${hours} hora${hours > 1 ? 's' : ''}`;
        } else {
            countdownText = `${minutes} minuto${minutes > 1 ? 's' : ''}`;
        }

        countdownElement.innerHTML = `<i class="bi bi-clock me-1"></i>${countdownText} restantes no período de teste`;
    }

    updateCountdown();
    setInterval(updateCountdown, 60000); // Update every minute
}

document.addEventListener('DOMContentLoaded', initializeTrialCountdown);

// ============================================
// PLAN COMPARISON MODAL
// ============================================

function showPlanComparison() {
    const modal = new bootstrap.Modal(document.getElementById('planComparisonModal'));
    modal.show();
}

// ============================================
// UPGRADE/DOWNGRADE PLAN
// ============================================

async function changePlan(newPlanId) {
    const confirmMsg = 'Deseja realmente alterar seu plano? As mudanças serão aplicadas imediatamente.';
    
    if (!confirm(confirmMsg)) {
        return;
    }

    const btn = event.target.closest('button');
    const originalText = btn.innerHTML;
    
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processando...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/subscriptions/change-plan', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                newPlanId: newPlanId
            })
        });

        const data = await response.json();

        if (response.ok) {
            showAlert('success', 'Plano alterado com sucesso! Recarregando...');
            setTimeout(() => window.location.reload(), 2000);
        } else {
            throw new Error(data.message || 'Erro ao alterar plano');
        }
    } catch (error) {
        console.error('Plan change error:', error);
        showAlert('danger', error.message || 'Erro ao alterar plano. Tente novamente.');
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

// ============================================
// PAYMENT METHOD MANAGEMENT
// ============================================

async function updatePaymentMethod() {
    // Opens Stripe Customer Portal to payment method section
    const establishmentId = document.querySelector('[data-establishment-id]')?.dataset.establishmentId;
    
    if (establishmentId) {
        await openCustomerPortal(establishmentId);
    }
}

// ============================================
// HANDLE STRIPE ERRORS
// ============================================

function handleStripeError(error) {
    const errorMessages = {
        'card_declined': 'Seu cartão foi recusado. Tente outro cartão.',
        'expired_card': 'Seu cartão está vencido. Use outro cartão.',
        'incorrect_cvc': 'Código de segurança incorreto.',
        'processing_error': 'Erro ao processar pagamento. Tente novamente.',
        'incorrect_number': 'Número do cartão inválido.',
        'insufficient_funds': 'Saldo insuficiente.',
        'generic_decline': 'Pagamento recusado. Entre em contato com seu banco.'
    };

    const message = errorMessages[error.code] || error.message || 'Erro ao processar pagamento';
    showAlert('danger', message);
}

// ============================================
// UTILITY FUNCTIONS
// ============================================

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

function formatCurrency(amount) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(amount / 100); // Stripe amounts are in cents
}

// ============================================
// HANDLE URL PARAMETERS
// ============================================

document.addEventListener('DOMContentLoaded', function() {
    const urlParams = new URLSearchParams(window.location.search);
    
    // Show canceled message
    if (urlParams.get('canceled') === 'true') {
        showAlert('warning', 'Pagamento cancelado. Você pode tentar novamente quando quiser.');
    }

    // Show success message
    if (urlParams.get('success') === 'true') {
        showAlert('success', 'Pagamento processado com sucesso! Bem-vindo ao Salvelle.');
    }
});

// ============================================
// EXPORT FUNCTIONS FOR GLOBAL ACCESS
// ============================================

window.StripeIntegration = {
    createCheckoutSession,
    openCustomerPortal,
    downloadInvoice,
    changePlan,
    updatePaymentMethod,
    handleStripeError
};
