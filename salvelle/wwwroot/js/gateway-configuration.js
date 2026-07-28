// gateway-configuration.js

document.addEventListener('DOMContentLoaded', function() {
    loadGatewayConfigurations();
    setupFormHandlers();
});

function setupFormHandlers() {
    document.getElementById('mercadoPagoForm').addEventListener('submit', function(e) {
        e.preventDefault();
        saveGatewayConfig('mercadopago');
    });

    document.getElementById('pagSeguroForm').addEventListener('submit', function(e) {
        e.preventDefault();
        saveGatewayConfig('pagseguro');
    });

    document.getElementById('stripeForm').addEventListener('submit', function(e) {
        e.preventDefault();
        saveGatewayConfig('stripe');
    });

    document.getElementById('asaasForm').addEventListener('submit', function(e) {
        e.preventDefault();
        saveGatewayConfig('asaas');
    });
}

async function loadGatewayConfigurations() {
    try {
        // Carregar configurações salvas
        const response = await fetch('/api/Configuration/gateways');
        if (response.ok) {
            const result = await response.json();
            
            if (result.success && result.data) {
                populateGatewayForms(result.data);
            }
        }
    } catch (error) {
        console.error('Erro ao carregar configurações:', error);
    }
}

function populateGatewayForms(configs) {
    // Mercado Pago
    if (configs.mercadopago) {
        document.getElementById('mpEnabled').checked = configs.mercadopago.enabled;
        document.getElementById('mpPublicKey').value = configs.mercadopago.publicKey || '';
        document.getElementById('mpAccessToken').value = configs.mercadopago.accessToken || '';
        document.getElementById('mpEnvironment').value = configs.mercadopago.environment || 'sandbox';
    }

    // PagSeguro
    if (configs.pagseguro) {
        document.getElementById('psEnabled').checked = configs.pagseguro.enabled;
        document.getElementById('psEmail').value = configs.pagseguro.email || '';
        document.getElementById('psToken').value = configs.pagseguro.token || '';
        document.getElementById('psEnvironment').value = configs.pagseguro.environment || 'sandbox';
    }

    // Stripe
    if (configs.stripe) {
        document.getElementById('stripeEnabled').checked = configs.stripe.enabled;
        document.getElementById('stripePublicKey').value = configs.stripe.publicKey || '';
        document.getElementById('stripeSecretKey').value = configs.stripe.secretKey || '';
        document.getElementById('stripeWebhookSecret').value = configs.stripe.webhookSecret || '';
    }

    // Asaas
    if (configs.asaas) {
        document.getElementById('asaasEnabled').checked = configs.asaas.enabled;
        document.getElementById('asaasApiKey').value = configs.asaas.apiKey || '';
        document.getElementById('asaasEnvironment').value = configs.asaas.environment || 'sandbox';
    }
}

async function saveGatewayConfig(gateway) {
    let config = {};

    switch (gateway) {
        case 'mercadopago':
            config = {
                enabled: document.getElementById('mpEnabled').checked,
                publicKey: document.getElementById('mpPublicKey').value,
                accessToken: document.getElementById('mpAccessToken').value,
                environment: document.getElementById('mpEnvironment').value
            };
            break;

        case 'pagseguro':
            config = {
                enabled: document.getElementById('psEnabled').checked,
                email: document.getElementById('psEmail').value,
                token: document.getElementById('psToken').value,
                environment: document.getElementById('psEnvironment').value
            };
            break;

        case 'stripe':
            config = {
                enabled: document.getElementById('stripeEnabled').checked,
                publicKey: document.getElementById('stripePublicKey').value,
                secretKey: document.getElementById('stripeSecretKey').value,
                webhookSecret: document.getElementById('stripeWebhookSecret').value
            };
            break;

        case 'asaas':
            config = {
                enabled: document.getElementById('asaasEnabled').checked,
                apiKey: document.getElementById('asaasApiKey').value,
                environment: document.getElementById('asaasEnvironment').value
            };
            break;
    }

    try {
        const response = await fetch(`/api/Configuration/gateways/${gateway}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(config)
        });

        const result = await response.json();

        if (result.success) {
            showToast(`Configurações do ${formatGatewayName(gateway)} salvas com sucesso!`, 'success');
            logEvent(`SUCCESS: Configurações do ${formatGatewayName(gateway)} atualizadas`);
        } else {
            showToast(`Erro ao salvar configurações: ${result.message}`, 'error');
            logEvent(`ERROR: ${result.message}`, 'error');
        }
    } catch (error) {
        console.error('Erro ao salvar configurações:', error);
        showToast('Erro ao salvar configurações', 'error');
        logEvent(`ERROR: ${error.message}`, 'error');
    }
}

async function testMercadoPago() {
    logEvent('INFO: Testando conexão com Mercado Pago...');
    
    const config = {
        publicKey: document.getElementById('mpPublicKey').value,
        accessToken: document.getElementById('mpAccessToken').value,
        environment: document.getElementById('mpEnvironment').value
    };

    if (!config.publicKey || !config.accessToken) {
        showToast('Preencha Public Key e Access Token', 'warning');
        return;
    }

    try {
        const response = await fetch('/api/Gateways/mercadopago/test', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(config)
        });

        const result = await response.json();

        if (result.success) {
            showToast('Conexão com Mercado Pago estabelecida com sucesso!', 'success');
            logEvent('SUCCESS: Mercado Pago conectado', 'success');
        } else {
            showToast(`Erro na conexão: ${result.message}`, 'error');
            logEvent(`ERROR: ${result.message}`, 'error');
        }
    } catch (error) {
        console.error('Erro ao testar Mercado Pago:', error);
        showToast('Erro ao testar conexão', 'error');
        logEvent(`ERROR: ${error.message}`, 'error');
    }
}

async function testPagSeguro() {
    logEvent('INFO: Testando conexão com PagSeguro...');
    
    const config = {
        email: document.getElementById('psEmail').value,
        token: document.getElementById('psToken').value,
        environment: document.getElementById('psEnvironment').value
    };

    if (!config.email || !config.token) {
        showToast('Preencha Email e Token', 'warning');
        return;
    }

    // Implementar teste real quando integrar
    setTimeout(() => {
        showToast('Teste do PagSeguro - Em desenvolvimento', 'info');
        logEvent('INFO: Teste PagSeguro simulado');
    }, 1000);
}

async function testStripe() {
    logEvent('INFO: Testando conexão com Stripe...');
    
    const config = {
        publicKey: document.getElementById('stripePublicKey').value,
        secretKey: document.getElementById('stripeSecretKey').value
    };

    if (!config.publicKey || !config.secretKey) {
        showToast('Preencha Publishable Key e Secret Key', 'warning');
        return;
    }

    // Implementar teste real quando integrar
    setTimeout(() => {
        showToast('Teste do Stripe - Em desenvolvimento', 'info');
        logEvent('INFO: Teste Stripe simulado');
    }, 1000);
}

async function testAsaas() {
    logEvent('INFO: Testando conexão com Asaas...');
    
    const config = {
        apiKey: document.getElementById('asaasApiKey').value,
        environment: document.getElementById('asaasEnvironment').value
    };

    if (!config.apiKey) {
        showToast('Preencha API Key', 'warning');
        return;
    }

    // Implementar teste real quando integrar
    setTimeout(() => {
        showToast('Teste do Asaas - Em desenvolvimento', 'info');
        logEvent('INFO: Teste Asaas simulado');
    }, 1000);
}

function formatGatewayName(gateway) {
    const names = {
        'mercadopago': 'Mercado Pago',
        'pagseguro': 'PagSeguro',
        'stripe': 'Stripe',
        'asaas': 'Asaas'
    };
    return names[gateway] || gateway;
}

function logEvent(message, type = 'info') {
    const logsContainer = document.getElementById('logsContainer');
    const timestamp = new Date().toLocaleString('pt-BR');
    
    let color = 'white';
    switch (type) {
        case 'success':
            color = '#4ade80';
            break;
        case 'error':
            color = '#f87171';
            break;
        case 'warning':
            color = '#fbbf24';
            break;
        default:
            color = '#60a5fa';
    }
    
    const logEntry = document.createElement('div');
    logEntry.style.color = color;
    logEntry.textContent = `[${timestamp}] ${message}`;
    
    logsContainer.appendChild(logEntry);
    logsContainer.scrollTop = logsContainer.scrollHeight;
}

function clearLogs() {
    if (confirm('Deseja limpar todos os logs?')) {
        const logsContainer = document.getElementById('logsContainer');
        logsContainer.innerHTML = '<div class="text-muted">Logs limpos. Aguardando eventos...</div>';
        showToast('Logs limpos', 'info');
    }
}

function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer') || createToastContainer();
    
    const bgColor = type === 'error' ? 'danger' : 
                    type === 'warning' ? 'warning' : 
                    type === 'success' ? 'success' : 'info';
    
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${bgColor} border-0`;
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    setTimeout(() => toast.remove(), 5000);
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container position-fixed top-0 end-0 p-3';
    container.style.zIndex = '9999';
    document.body.appendChild(container);
    return container;
}
