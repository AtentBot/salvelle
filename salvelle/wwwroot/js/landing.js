// Landing Page JavaScript
let currentBillingCycle = 'monthly';
let plansData = [];

// Load plans on page load
document.addEventListener('DOMContentLoaded', async () => {
    await loadPlans();
    setupBillingToggle();
});

// Load subscription plans from API
async function loadPlans() {
    try {
        const response = await fetch('/api/subscriptionplans?activeOnly=true');
        if (!response.ok) {
            throw new Error('Failed to load plans');
        }

        plansData = await response.json();
        renderPricingCards(currentBillingCycle);
    } catch (error) {
        console.error('Error loading plans:', error);
        showErrorMessage();
    }
}

// Setup billing cycle toggle
function setupBillingToggle() {
    const toggle = document.getElementById('billingCycleToggle');
    if (toggle) {
        toggle.addEventListener('change', (e) => {
            currentBillingCycle = e.target.checked ? 'yearly' : 'monthly';
            renderPricingCards(currentBillingCycle);
        });
    }
}

// Render pricing cards
function renderPricingCards(billingCycle) {
    const container = document.getElementById('pricingCards');
    if (!container) return;

    if (plansData.length === 0) {
        container.innerHTML = '<div class="col-12 text-center"><p class="text-muted">Nenhum plano disponível no momento.</p></div>';
        return;
    }

    container.innerHTML = plansData.map((plan, index) => {
        const price = billingCycle === 'yearly' ? plan.priceYearly : plan.priceMonthly;
        const monthlyPrice = billingCycle === 'yearly' ? (plan.priceYearly / 12) : plan.priceMonthly;
        const savings = billingCycle === 'yearly' ? calculateSavings(plan) : 0;
        const features = parseFeatures(plan.features);
        const isFeatured = index === 1; // Middle plan is featured

        return `
            <div class="col-lg-4 col-md-6">
                <div class="pricing-card ${isFeatured ? 'featured' : ''}">
                    ${isFeatured ? '<div class="pricing-badge">Mais Popular</div>' : ''}

                    <div class="pricing-header">
                        <h3 class="plan-name">${escapeHtml(plan.name)}</h3>
                        <p class="plan-description">${escapeHtml(plan.description || 'Plano completo para sua farmácia')}</p>

                        <div class="plan-price">
                            <span class="currency">R$</span>
                            ${formatPrice(monthlyPrice)}
                            <small>/mês</small>
                        </div>

                        ${billingCycle === 'yearly' ? `
                            <div class="plan-cycle">
                                R$ ${formatPrice(price)} cobrado anualmente
                                ${savings > 0 ? `<br><span class="text-success fw-bold">Economize R$ ${formatPrice(savings)}</span>` : ''}
                            </div>
                        ` : '<div class="plan-cycle">Cobrado mensalmente</div>'}
                    </div>

                    <div class="pricing-body">
                        <ul class="features-list">
                            ${plan.maxEmployees ? `
                                <li>
                                    <i class="bi bi-check-circle-fill"></i>
                                    <span>Até ${plan.maxEmployees} funcionários</span>
                                </li>
                            ` : '<li><i class="bi bi-check-circle-fill"></i><span>Funcionários ilimitados</span></li>'}

                            ${plan.maxMonthlyOrders ? `
                                <li>
                                    <i class="bi bi-check-circle-fill"></i>
                                    <span>Até ${plan.maxMonthlyOrders} pedidos/mês</span>
                                </li>
                            ` : '<li><i class="bi bi-check-circle-fill"></i><span>Pedidos ilimitados</span></li>'}

                            ${features.map(feature => `
                                <li>
                                    <i class="bi bi-check-circle-fill"></i>
                                    <span>${escapeHtml(feature)}</span>
                                </li>
                            `).join('')}
                        </ul>
                    </div>

                    <div class="pricing-footer">
                        <a href="/Signup?planId=${plan.id}&cycle=${billingCycle}"
                           class="btn ${isFeatured ? 'btn-plan' : 'btn-outline-primary'} btn-plan">
                            Começar Agora
                        </a>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

// Parse features from JSON string
function parseFeatures(featuresJson) {
    try {
        if (!featuresJson) return getDefaultFeatures();

        const features = JSON.parse(featuresJson);
        const featuresList = [];

        for (const [key, value] of Object.entries(features)) {
            if (value === true || value === 'true') {
                featuresList.push(formatFeatureName(key));
            }
        }

        return featuresList.length > 0 ? featuresList : getDefaultFeatures();
    } catch (error) {
        console.error('Error parsing features:', error);
        return getDefaultFeatures();
    }
}

// Get default features
function getDefaultFeatures() {
    return [
        'Gestão de produção completa',
        'Controle de estoque',
        'Precificação automática',
        'Prescrições digitais',
        'Relatórios e dashboards',
        'Suporte por email'
    ];
}

// Format feature name from key
function formatFeatureName(key) {
    const featureNames = {
        'production_management': 'Gestão de produção',
        'inventory_control': 'Controle de estoque',
        'automatic_pricing': 'Precificação automática',
        'digital_prescriptions': 'Prescrições digitais',
        'reports_dashboard': 'Relatórios e dashboards',
        'email_support': 'Suporte por email',
        'phone_support': 'Suporte telefônico',
        'priority_support': 'Suporte prioritário',
        'advanced_reports': 'Relatórios avançados',
        'multi_location': 'Múltiplas filiais',
        'api_access': 'Acesso à API',
        'custom_integrations': 'Integrações personalizadas',
        'dedicated_account_manager': 'Gerente de conta dedicado'
    };

    return featureNames[key] || key.replace(/_/g, ' ');
}

// Calculate savings for yearly plan
function calculateSavings(plan) {
    const yearlyTotal = plan.priceYearly;
    const monthlyTotal = plan.priceMonthly * 12;
    return monthlyTotal - yearlyTotal;
}

// Format price
function formatPrice(price) {
    return price.toLocaleString('pt-BR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Show error message
function showErrorMessage() {
    const container = document.getElementById('pricingCards');
    if (container) {
        container.innerHTML = `
            <div class="col-12 text-center">
                <div class="alert alert-warning" role="alert">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    Não foi possível carregar os planos. Por favor, tente novamente mais tarde.
                </div>
            </div>
        `;
    }
}

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});
