// payment-management.js

function escapeHtml(str) {
    if (str == null) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
}

let paymentsChart = null;
let methodsChart = null;

document.addEventListener('DOMContentLoaded', function() {
    initializeCharts();
    loadDashboardData();
    loadPaymentMethods();
    setupEventListeners();
});

function setupEventListeners() {
    document.getElementById('reportPeriod').addEventListener('change', function() {
        if (this.value === 'custom') {
            document.getElementById('customPeriod').classList.remove('d-none');
        } else {
            document.getElementById('customPeriod').classList.add('d-none');
        }
    });
}

async function loadDashboardData() {
    try {
        const today = new Date().toISOString().split('T')[0];
        const response = await fetch(`/api/Payments/daily-cash-flow?date=${today}`);
        const result = await response.json();

        if (result.success) {
            const data = result.data;
            
            // Atualizar KPIs
            document.getElementById('totalReceivedToday').textContent = formatCurrency(data.grandTotal);
            document.getElementById('completedSales').textContent = data.totalSales;
            
            const avgTicket = data.totalSales > 0 ? data.grandTotal / data.totalSales : 0;
            document.getElementById('avgTicket').textContent = `Ticket médio: ${formatCurrency(avgTicket)}`;

            // Atualizar método mais usado
            if (data.paymentMethodBreakdown.length > 0) {
                const topMethod = data.paymentMethodBreakdown[0];
                document.getElementById('topMethod').textContent = formatPaymentMethod(topMethod.paymentMethod);
                document.getElementById('topMethodPercentage').textContent = `${topMethod.percentage.toFixed(1)}%`;
            }

            // Atualizar gráficos
            updateMethodsChart(data.paymentMethodBreakdown);
            
            // Carregar transações recentes
            loadRecentTransactions();
        }
    } catch (error) {
        console.error('Erro ao carregar dados do dashboard:', error);
        showToast('Erro ao carregar dados', 'error');
    }
}

function initializeCharts() {
    // Gráfico de Recebimentos (Linha)
    const paymentsCtx = document.getElementById('paymentsChart').getContext('2d');
    paymentsChart = new Chart(paymentsCtx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: 'Recebimentos',
                data: [],
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return 'R$ ' + context.parsed.y.toLocaleString('pt-BR', {minimumFractionDigits: 2});
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return 'R$ ' + value.toLocaleString('pt-BR');
                        }
                    }
                }
            }
        }
    });

    // Gráfico de Métodos (Pizza)
    const methodsCtx = document.getElementById('methodsChart').getContext('2d');
    methodsChart = new Chart(methodsCtx, {
        type: 'doughnut',
        data: {
            labels: [],
            datasets: [{
                data: [],
                backgroundColor: [
                    'rgba(255, 99, 132, 0.8)',
                    'rgba(54, 162, 235, 0.8)',
                    'rgba(255, 206, 86, 0.8)',
                    'rgba(75, 192, 192, 0.8)',
                    'rgba(153, 102, 255, 0.8)'
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: R$ ${value.toLocaleString('pt-BR', {minimumFractionDigits: 2})} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });

    loadLast7DaysData();
}

async function loadLast7DaysData() {
    try {
        const labels = [];
        const data = [];
        
        for (let i = 6; i >= 0; i--) {
            const date = new Date();
            date.setDate(date.getDate() - i);
            const dateStr = date.toISOString().split('T')[0];
            
            const response = await fetch(`/api/Payments/daily-cash-flow?date=${dateStr}`);
            const result = await response.json();
            
            labels.push(date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }));
            data.push(result.success ? result.data.grandTotal : 0);
        }
        
        paymentsChart.data.labels = labels;
        paymentsChart.data.datasets[0].data = data;
        paymentsChart.update();
    } catch (error) {
        console.error('Erro ao carregar dados dos últimos 7 dias:', error);
    }
}

function updateMethodsChart(breakdown) {
    const labels = breakdown.map(m => formatPaymentMethod(m.paymentMethod));
    const data = breakdown.map(m => m.totalAmount);
    
    methodsChart.data.labels = labels;
    methodsChart.data.datasets[0].data = data;
    methodsChart.update();
}

async function loadRecentTransactions() {
    try {
        // Buscar vendas recentes com pagamentos
        const today = new Date().toISOString().split('T')[0];
        const response = await fetch(`/api/Sales?date=${today}`);
        const result = await response.json();

        const tbody = document.getElementById('recentTransactionsBody');
        tbody.innerHTML = '';

        if (result.success && result.data.length > 0) {
            const sales = result.data.slice(0, 10); // Últimas 10
            
            for (const sale of sales) {
                // Buscar pagamentos de cada venda
                const paymentsResponse = await fetch(`/api/Payments/sales/${sale.id}`);
                const paymentsResult = await paymentsResponse.json();
                
                if (paymentsResult.success && paymentsResult.data.length > 0) {
                    paymentsResult.data.forEach(payment => {
                        const row = createTransactionRow(sale, payment);
                        tbody.appendChild(row);
                    });
                }
            }
        } else {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center text-muted py-4">
                        <i class="bi bi-inbox fs-3 d-block mb-2"></i>
                        Nenhuma transação encontrada hoje
                    </td>
                </tr>
            `;
        }
    } catch (error) {
        console.error('Erro ao carregar transações recentes:', error);
    }
}

function createTransactionRow(sale, payment) {
    const tr = document.createElement('tr');
    
    const statusBadge = getStatusBadge(payment.paymentStatus);
    const methodIcon = getMethodIcon(payment.paymentMethod);
    
    tr.innerHTML = `
        <td>${formatDateTime(payment.paymentDate)}</td>
        <td><a href="/Sales/Details/${sale.id}">${escapeHtml(sale.code)}</a></td>
        <td>${escapeHtml(sale.customerName) || 'Não identificado'}</td>
        <td>${methodIcon} ${escapeHtml(formatPaymentMethod(payment.paymentMethod))}</td>
        <td>${formatCurrency(payment.amount)}</td>
        <td>${statusBadge}</td>
        <td>
            <button class="btn btn-sm btn-outline-primary" onclick="viewPaymentDetails('${payment.id}')">
                <i class="bi bi-eye"></i>
            </button>
            ${payment.paymentStatus === 'APPROVED' ? `
                <button class="btn btn-sm btn-outline-danger" onclick="cancelPayment('${payment.id}')">
                    <i class="bi bi-x-circle"></i>
                </button>
            ` : ''}
        </td>
    `;
    
    return tr;
}

async function loadPaymentMethods() {
    const container = document.getElementById('paymentMethodsConfig');
    
    const methods = [
        { name: 'Dinheiro', type: 'DINHEIRO', icon: 'cash-coin', active: true },
        { name: 'Cartão de Débito', type: 'CARTAO_DEBITO', icon: 'credit-card', active: true },
        { name: 'Cartão de Crédito', type: 'CARTAO_CREDITO', icon: 'credit-card-2-front', active: true },
        { name: 'PIX', type: 'PIX', icon: 'qr-code', active: true },
        { name: 'Boleto', type: 'BOLETO', icon: 'upc-scan', active: false }
    ];
    
    container.innerHTML = methods.map(method => `
        <div class="card mb-3">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center">
                        <div class="bg-primary bg-opacity-10 p-3 rounded me-3">
                            <i class="bi bi-${method.icon} text-primary fs-4"></i>
                        </div>
                        <div>
                            <h6 class="mb-0">${method.name}</h6>
                            <small class="text-muted">${method.type}</small>
                        </div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox" id="toggle_${method.type}" 
                                   ${method.active ? 'checked' : ''} onchange="togglePaymentMethod('${method.type}')">
                            <label class="form-check-label" for="toggle_${method.type}">
                                ${method.active ? 'Ativo' : 'Inativo'}
                            </label>
                        </div>
                        <button class="btn btn-sm btn-outline-secondary" onclick="configureMethod('${method.type}')">
                            <i class="bi bi-gear"></i> Configurar
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

function togglePaymentMethod(type) {
    const toggle = document.getElementById(`toggle_${type}`);
    const label = toggle.nextElementSibling;
    label.textContent = toggle.checked ? 'Ativo' : 'Inativo';
    
    showToast(`${formatPaymentMethod(type)} ${toggle.checked ? 'ativado' : 'desativado'}`, 'success');
}

function configureMethod(type) {
    // Implementar configurações específicas por método
    alert(`Configurações de ${formatPaymentMethod(type)} em desenvolvimento`);
}

async function viewPaymentDetails(paymentId) {
    try {
        const response = await fetch(`/api/Payments/${paymentId}`);
        const result = await response.json();
        
        if (result.success) {
            const payment = result.data;
            
            let detailsHtml = `
                <div class="mb-3"><strong>Método:</strong> ${escapeHtml(formatPaymentMethod(payment.paymentMethod))}</div>
                <div class="mb-3"><strong>Valor:</strong> ${formatCurrency(payment.amount)}</div>
                <div class="mb-3"><strong>Status:</strong> ${getStatusBadge(payment.paymentStatus)}</div>
                <div class="mb-3"><strong>Data:</strong> ${formatDateTime(payment.paymentDate)}</div>
            `;
            
            // Adicionar detalhes específicos
            if (payment.paymentMethod === 'DINHEIRO' && payment.cashReceived) {
                detailsHtml += `
                    <div class="mb-3"><strong>Valor Recebido:</strong> ${formatCurrency(payment.cashReceived)}</div>
                    <div class="mb-3"><strong>Troco:</strong> ${formatCurrency(payment.changeAmount)}</div>
                `;
            }
            
            if (payment.paymentMethod.includes('CARTAO')) {
                detailsHtml += `
                    <div class="mb-3"><strong>Bandeira:</strong> ${escapeHtml(payment.cardBrand)}</div>
                    <div class="mb-3"><strong>Últimos Dígitos:</strong> **** ${escapeHtml(payment.cardLastDigits)}</div>
                    <div class="mb-3"><strong>Parcelas:</strong> ${payment.installments}x</div>
                    <div class="mb-3"><strong>NSU:</strong> ${escapeHtml(payment.nsu)}</div>
                `;
            }
            
            if (payment.paymentMethod === 'PIX' && payment.pixTransactionId) {
                detailsHtml += `
                    <div class="mb-3"><strong>Chave PIX:</strong> ${escapeHtml(payment.pixKey)}</div>
                    <div class="mb-3"><strong>ID Transação:</strong> ${escapeHtml(payment.pixTransactionId)}</div>
                `;
            }
            
            showModal('Detalhes do Pagamento', detailsHtml);
        }
    } catch (error) {
        console.error('Erro ao carregar detalhes:', error);
        showToast('Erro ao carregar detalhes do pagamento', 'error');
    }
}

async function cancelPayment(paymentId) {
    if (!confirm('Deseja realmente cancelar/estornar este pagamento?')) {
        return;
    }
    
    try {
        const response = await fetch(`/api/Payments/${paymentId}/cancel`, {
            method: 'PUT'
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Pagamento cancelado com sucesso', 'success');
            loadRecentTransactions();
            loadDashboardData();
        } else {
            showToast(result.message || 'Erro ao cancelar pagamento', 'error');
        }
    } catch (error) {
        console.error('Erro ao cancelar pagamento:', error);
        showToast('Erro ao cancelar pagamento', 'error');
    }
}

async function generateReport() {
    const period = document.getElementById('reportPeriod').value;
    let startDate, endDate;
    
    switch (period) {
        case 'today':
            startDate = endDate = new Date().toISOString().split('T')[0];
            break;
        case 'week':
            endDate = new Date();
            startDate = new Date(endDate);
            startDate.setDate(startDate.getDate() - 7);
            startDate = startDate.toISOString().split('T')[0];
            endDate = endDate.toISOString().split('T')[0];
            break;
        case 'month':
            endDate = new Date();
            startDate = new Date(endDate);
            startDate.setMonth(startDate.getMonth() - 1);
            startDate = startDate.toISOString().split('T')[0];
            endDate = endDate.toISOString().split('T')[0];
            break;
        case 'custom':
            startDate = document.getElementById('reportStartDate').value;
            endDate = document.getElementById('reportEndDate').value;
            break;
    }
    
    showToast('Gerando relatório...', 'info');
    
    // Implementar geração de PDF
    setTimeout(() => {
        showToast('Relatório gerado com sucesso!', 'success');
    }, 1500);
}

function exportToExcel() {
    showToast('Exportando para Excel...', 'info');
    // Implementar exportação
}

function exportToCSV() {
    showToast('Exportando para CSV...', 'info');
    // Implementar exportação
}

function printReport() {
    window.print();
}

// Funções auxiliares
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}

function formatDateTime(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleString('pt-BR');
}

function formatPaymentMethod(method) {
    const methods = {
        'DINHEIRO': 'Dinheiro',
        'CARTAO_DEBITO': 'Cartão de Débito',
        'CARTAO_CREDITO': 'Cartão de Crédito',
        'PIX': 'PIX',
        'BOLETO': 'Boleto'
    };
    return methods[method] || method;
}

function getMethodIcon(method) {
    const icons = {
        'DINHEIRO': 'bi-cash-coin',
        'CARTAO_DEBITO': 'bi-credit-card',
        'CARTAO_CREDITO': 'bi-credit-card-2-front',
        'PIX': 'bi-qr-code',
        'BOLETO': 'bi-upc-scan'
    };
    const icon = icons[method] || 'bi-wallet2';
    return `<i class="bi ${icon}"></i>`;
}

function getStatusBadge(status) {
    const badges = {
        'PENDING': '<span class="badge bg-warning">Pendente</span>',
        'APPROVED': '<span class="badge bg-success">Aprovado</span>',
        'CANCELLED': '<span class="badge bg-secondary">Cancelado</span>',
        'REFUNDED': '<span class="badge bg-danger">Estornado</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${escapeHtml(status)}</span>`;
}

function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer') || createToastContainer();
    
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${escapeHtml(message)}</div>
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

function showModal(title, content) {
    const modalHtml = `
        <div class="modal fade" id="dynamicModal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">${title}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        ${content}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Fechar</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    const existingModal = document.getElementById('dynamicModal');
    if (existingModal) existingModal.remove();
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    const modal = new bootstrap.Modal(document.getElementById('dynamicModal'));
    modal.show();
}
