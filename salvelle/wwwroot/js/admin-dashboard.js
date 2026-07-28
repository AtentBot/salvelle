/**
 * Salvelle - Admin Dashboard JavaScript
 * Handles admin dashboard metrics, charts, and real-time updates
 */

document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
    initializeRefreshButton();
    initializeMetricsCards();
});

// ============================================
// DASHBOARD INITIALIZATION
// ============================================

async function initializeDashboard() {
    showLoading();
    
    try {
        await Promise.all([
            loadDashboardMetrics(),
            loadRecentSignups(),
            loadRevenueChart()
        ]);
    } catch (error) {
        console.error('Dashboard initialization error:', error);
        showAlert('danger', 'Erro ao carregar dashboard. Por favor, recarregue a página.');
    } finally {
        hideLoading();
    }
}

// ============================================
// LOAD DASHBOARD METRICS
// ============================================

async function loadDashboardMetrics() {
    try {
        const response = await fetch('/api/admin/dashboard/metrics');
        
        if (!response.ok) {
            throw new Error('Erro ao carregar métricas');
        }

        const metrics = await response.json();
        updateMetricsDisplay(metrics);
        
    } catch (error) {
        console.error('Metrics loading error:', error);
        throw error;
    }
}

function updateMetricsDisplay(metrics) {
    // Total Establishments
    updateMetricCard('totalEstablishments', metrics.totalEstablishments, null);
    updateMetricCard('activeEstablishments', metrics.activeEstablishments, 'success');
    updateMetricCard('inactiveEstablishments', metrics.inactiveEstablishments, 'danger');
    
    // Subscriptions
    updateMetricCard('trialingEstablishments', metrics.trialingEstablishments, 'info');
    updateMetricCard('pastDueEstablishments', metrics.pastDueEstablishments, 'warning');
    
    // Revenue
    updateMetricCard('mrr', formatCurrency(metrics.monthlyRecurringRevenue), 'success');
    updateMetricCard('arr', formatCurrency(metrics.annualRecurringRevenue), 'primary');
    
    // Growth
    updateMetricCard('newSubscriptionsThisMonth', metrics.newSubscriptionsThisMonth, 'info');
    updateMetricCard('churnRate', metrics.churnRate.toFixed(2) + '%', 
        metrics.churnRate < 5 ? 'success' : 'warning');
    
    // Subscriptions by Plan
    updateSubscriptionsByPlan(metrics.subscriptionsByPlan);
}

function updateMetricCard(elementId, value, badgeClass) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    // Animate number change
    const currentValue = parseInt(element.textContent.replace(/\D/g, '')) || 0;
    const newValue = typeof value === 'string' ? parseInt(value.replace(/\D/g, '')) : value;
    
    if (currentValue !== newValue && typeof newValue === 'number') {
        animateValue(element, currentValue, newValue, 1000);
    } else {
        element.textContent = value;
    }
    
    // Update badge color
    if (badgeClass) {
        const card = element.closest('.metric-card');
        if (card) {
            card.className = `metric-card border-${badgeClass}`;
        }
    }
}

function updateSubscriptionsByPlan(planData) {
    const container = document.getElementById('subscriptionsByPlan');
    if (!container) return;
    
    if (!planData || planData.length === 0) {
        container.innerHTML = '<p class="text-muted">Nenhuma assinatura ativa</p>';
        return;
    }
    
    let html = '<div class="list-group">';
    
    planData.forEach(plan => {
        const percentage = plan.totalSubscriptions > 0 
            ? ((plan.count / plan.totalSubscriptions) * 100).toFixed(1) 
            : 0;
        
        html += `
            <div class="list-group-item">
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <strong>${plan.planName}</strong>
                        <small class="text-muted ms-2">${plan.count} assinaturas</small>
                    </div>
                    <span class="badge bg-primary">${percentage}%</span>
                </div>
                <div class="progress mt-2" style="height: 5px;">
                    <div class="progress-bar" role="progressbar" 
                         style="width: ${percentage}%"></div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

// ============================================
// LOAD RECENT SIGNUPS
// ============================================

async function loadRecentSignups() {
    try {
        const response = await fetch('/api/admin/dashboard/recent-signups?limit=10');
        
        if (!response.ok) {
            throw new Error('Erro ao carregar cadastros recentes');
        }

        const signups = await response.json();
        updateRecentSignupsTable(signups);
        
    } catch (error) {
        console.error('Recent signups loading error:', error);
        throw error;
    }
}

function updateRecentSignupsTable(signups) {
    const tbody = document.getElementById('recentSignupsTable');
    if (!tbody) return;
    
    if (!signups || signups.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Nenhum cadastro recente</td></tr>';
        return;
    }
    
    let html = '';
    
    signups.forEach(signup => {
        const statusBadge = getSubscriptionStatusBadge(signup.subscriptionStatus);
        const date = new Date(signup.createdAt).toLocaleDateString('pt-BR');
        
        html += `
            <tr>
                <td>
                    <strong>${signup.nomeFantasia}</strong><br>
                    <small class="text-muted">${signup.cnpj}</small>
                </td>
                <td>${signup.email}</td>
                <td>${signup.planName || '-'}</td>
                <td>${statusBadge}</td>
                <td>
                    <small class="text-muted">${date}</small>
                </td>
                <td>
                    <a href="/admin/establishments/${signup.id}" 
                       class="btn btn-sm btn-outline-primary">
                        <i class="bi bi-eye"></i>
                    </a>
                </td>
            </tr>
        `;
    });
    
    tbody.innerHTML = html;
}

// ============================================
// LOAD REVENUE CHART
// ============================================

let revenueChart = null;

async function loadRevenueChart(months = 6) {
    try {
        const response = await fetch(`/api/admin/dashboard/revenue-chart?months=${months}`);
        
        if (!response.ok) {
            throw new Error('Erro ao carregar gráfico de receita');
        }

        const chartData = await response.json();
        renderRevenueChart(chartData);
        
    } catch (error) {
        console.error('Revenue chart loading error:', error);
        throw error;
    }
}

function renderRevenueChart(data) {
    const ctx = document.getElementById('revenueChart');
    if (!ctx) return;
    
    // Destroy existing chart
    if (revenueChart) {
        revenueChart.destroy();
    }
    
    const labels = data.map(item => {
        const date = new Date(item.month);
        return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
    });
    
    const values = data.map(item => item.revenue / 100); // Convert cents to reais
    
    revenueChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Receita (R$)',
                data: values,
                borderColor: '#0d6efd',
                backgroundColor: 'rgba(13, 110, 253, 0.1)',
                tension: 0.4,
                fill: true
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
                            return 'Receita: ' + formatCurrency(context.parsed.y * 100);
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
}

// ============================================
// REFRESH FUNCTIONALITY
// ============================================

function initializeRefreshButton() {
    const refreshBtn = document.getElementById('refreshDashboard');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', async function(e) {
            e.preventDefault();
            
            const icon = this.querySelector('i');
            icon.classList.add('spin');
            this.disabled = true;
            
            try {
                await initializeDashboard();
                showAlert('success', 'Dashboard atualizado com sucesso!');
            } catch (error) {
                showAlert('danger', 'Erro ao atualizar dashboard.');
            } finally {
                icon.classList.remove('spin');
                this.disabled = false;
            }
        });
    }
}

// Auto-refresh every 5 minutes
setInterval(() => {
    loadDashboardMetrics().catch(console.error);
}, 300000); // 5 minutes

// ============================================
// METRICS CARDS INTERACTION
// ============================================

function initializeMetricsCards() {
    const metricCards = document.querySelectorAll('.metric-card');
    
    metricCards.forEach(card => {
        card.addEventListener('click', function() {
            const metricType = this.dataset.metric;
            if (metricType) {
                filterEstablishmentsByMetric(metricType);
            }
        });
    });
}

function filterEstablishmentsByMetric(metricType) {
    // Navigate to establishments page with filter
    const filterMap = {
        'active': 'status=ACTIVE',
        'inactive': 'status=INACTIVE',
        'trialing': 'subscriptionStatus=TRIALING',
        'pastDue': 'subscriptionStatus=PAST_DUE'
    };
    
    const filter = filterMap[metricType];
    if (filter) {
        window.location.href = `/admin/establishments?${filter}`;
    }
}

// ============================================
// QUICK ACTIONS
// ============================================

async function sendBulkNotification() {
    const message = prompt('Digite a mensagem para enviar a todos os estabelecimentos ativos:');
    
    if (!message) return;
    
    if (!confirm('Tem certeza que deseja enviar esta mensagem para todos?')) {
        return;
    }
    
    showLoading();
    
    try {
        const response = await fetch('/api/admin/notifications/bulk', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ message })
        });
        
        if (response.ok) {
            showAlert('success', 'Notificações enviadas com sucesso!');
        } else {
            throw new Error('Erro ao enviar notificações');
        }
    } catch (error) {
        console.error('Bulk notification error:', error);
        showAlert('danger', 'Erro ao enviar notificações. Tente novamente.');
    } finally {
        hideLoading();
    }
}

async function exportDashboardData() {
    showLoading();
    
    try {
        const response = await fetch('/api/admin/dashboard/export', {
            method: 'GET'
        });
        
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `dashboard_${new Date().toISOString().split('T')[0]}.csv`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
            
            showAlert('success', 'Dados exportados com sucesso!');
        } else {
            throw new Error('Erro ao exportar dados');
        }
    } catch (error) {
        console.error('Export error:', error);
        showAlert('danger', 'Erro ao exportar dados. Tente novamente.');
    } finally {
        hideLoading();
    }
}

// ============================================
// UTILITY FUNCTIONS
// ============================================

function getSubscriptionStatusBadge(status) {
    const badges = {
        'TRIALING': '<span class="badge bg-info">Teste</span>',
        'ACTIVE': '<span class="badge bg-success">Ativa</span>',
        'PAST_DUE': '<span class="badge bg-warning">Pendente</span>',
        'CANCELED': '<span class="badge bg-danger">Cancelada</span>',
        'INCOMPLETE': '<span class="badge bg-secondary">Incompleta</span>'
    };
    
    return badges[status] || '<span class="badge bg-secondary">-</span>';
}

function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value / 100);
}

function animateValue(element, start, end, duration) {
    const range = end - start;
    const increment = range / (duration / 16);
    let current = start;
    
    const timer = setInterval(() => {
        current += increment;
        if ((increment > 0 && current >= end) || (increment < 0 && current <= end)) {
            element.textContent = end.toLocaleString('pt-BR');
            clearInterval(timer);
        } else {
            element.textContent = Math.floor(current).toLocaleString('pt-BR');
        }
    }, 16);
}

function showLoading() {
    const loader = document.getElementById('dashboardLoader');
    if (loader) {
        loader.style.display = 'flex';
    }
}

function hideLoading() {
    const loader = document.getElementById('dashboardLoader');
    if (loader) {
        loader.style.display = 'none';
    }
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

// ============================================
// EXPORT FOR GLOBAL ACCESS
// ============================================

window.AdminDashboard = {
    refresh: initializeDashboard,
    sendBulkNotification,
    exportData: exportDashboardData,
    loadRevenueChart
};
