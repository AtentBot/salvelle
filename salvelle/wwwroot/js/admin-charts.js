/**
 * Salvelle - Admin Charts JavaScript
 * Handles all chart visualizations for admin dashboard and reports
 */

// Chart instances
let charts = {
    revenue: null,
    subscriptions: null,
    churn: null,
    growth: null,
    planDistribution: null
};

// Chart colors
const COLORS = {
    primary: '#0d6efd',
    success: '#198754',
    danger: '#dc3545',
    warning: '#ffc107',
    info: '#0dcaf0',
    secondary: '#6c757d',
    light: '#f8f9fa',
    dark: '#212529'
};

document.addEventListener('DOMContentLoaded', function() {
    initializeCharts();
});

// ============================================
// REVENUE CHART
// ============================================

async function createRevenueChart(elementId, months = 12) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch(`/api/admin/dashboard/revenue-chart?months=${months}`);
        const data = await response.json();
        
        // Destroy existing chart
        if (charts.revenue) {
            charts.revenue.destroy();
        }
        
        const labels = data.map(item => {
            const date = new Date(item.month);
            return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
        });
        
        const revenueData = data.map(item => item.revenue / 100);
        const invoicesData = data.map(item => item.invoiceCount || 0);
        
        charts.revenue = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Receita (R$)',
                        data: revenueData,
                        borderColor: COLORS.success,
                        backgroundColor: hexToRgba(COLORS.success, 0.1),
                        tension: 0.4,
                        fill: true,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Número de Faturas',
                        data: invoicesData,
                        borderColor: COLORS.primary,
                        backgroundColor: hexToRgba(COLORS.primary, 0.1),
                        tension: 0.4,
                        fill: true,
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                if (context.parsed.y !== null) {
                                    if (context.dataset.yAxisID === 'y') {
                                        label += formatCurrency(context.parsed.y * 100);
                                    } else {
                                        label += context.parsed.y;
                                    }
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return 'R$ ' + value.toLocaleString('pt-BR');
                            }
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        beginAtZero: true,
                        grid: {
                            drawOnChartArea: false
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Revenue chart error:', error);
    }
}

// ============================================
// SUBSCRIPTIONS STATUS CHART
// ============================================

async function createSubscriptionsChart(elementId) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch('/api/admin/dashboard/metrics');
        const metrics = await response.json();
        
        // Destroy existing chart
        if (charts.subscriptions) {
            charts.subscriptions.destroy();
        }
        
        charts.subscriptions = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Ativas', 'Em Teste', 'Pendentes', 'Canceladas'],
                datasets: [{
                    data: [
                        metrics.activeSubscriptions || 0,
                        metrics.trialingEstablishments || 0,
                        metrics.pastDueEstablishments || 0,
                        metrics.canceledSubscriptions || 0
                    ],
                    backgroundColor: [
                        COLORS.success,
                        COLORS.info,
                        COLORS.warning,
                        COLORS.danger
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
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
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Subscriptions chart error:', error);
    }
}

// ============================================
// CHURN RATE CHART
// ============================================

async function createChurnChart(elementId, months = 6) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch(`/api/admin/dashboard/churn-chart?months=${months}`);
        const data = await response.json();
        
        // Destroy existing chart
        if (charts.churn) {
            charts.churn.destroy();
        }
        
        const labels = data.map(item => {
            const date = new Date(item.month);
            return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
        });
        
        const churnRates = data.map(item => item.churnRate || 0);
        
        charts.churn = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Taxa de Churn (%)',
                    data: churnRates,
                    backgroundColor: churnRates.map(rate => 
                        rate < 5 ? COLORS.success : rate < 10 ? COLORS.warning : COLORS.danger
                    ),
                    borderWidth: 0
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
                                return `Churn: ${context.parsed.y.toFixed(2)}%`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return value + '%';
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Churn chart error:', error);
    }
}

// ============================================
// GROWTH CHART (NEW SUBSCRIPTIONS)
// ============================================

async function createGrowthChart(elementId, months = 12) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch(`/api/admin/dashboard/growth-chart?months=${months}`);
        const data = await response.json();
        
        // Destroy existing chart
        if (charts.growth) {
            charts.growth.destroy();
        }
        
        const labels = data.map(item => {
            const date = new Date(item.month);
            return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
        });
        
        const newSubs = data.map(item => item.newSubscriptions || 0);
        const canceledSubs = data.map(item => item.canceledSubscriptions || 0);
        const netGrowth = data.map(item => (item.newSubscriptions || 0) - (item.canceledSubscriptions || 0));
        
        charts.growth = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Novas Assinaturas',
                        data: newSubs,
                        backgroundColor: COLORS.success,
                        stack: 'Stack 0'
                    },
                    {
                        label: 'Cancelamentos',
                        data: canceledSubs,
                        backgroundColor: COLORS.danger,
                        stack: 'Stack 1'
                    },
                    {
                        label: 'Crescimento Líquido',
                        data: netGrowth,
                        type: 'line',
                        borderColor: COLORS.primary,
                        backgroundColor: hexToRgba(COLORS.primary, 0.1),
                        tension: 0.4,
                        fill: true
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    } catch (error) {
        console.error('Growth chart error:', error);
    }
}

// ============================================
// PLAN DISTRIBUTION CHART
// ============================================

async function createPlanDistributionChart(elementId) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch('/api/admin/dashboard/metrics');
        const metrics = await response.json();
        
        // Destroy existing chart
        if (charts.planDistribution) {
            charts.planDistribution.destroy();
        }
        
        const planData = metrics.subscriptionsByPlan || [];
        const labels = planData.map(p => p.planName);
        const data = planData.map(p => p.count);
        const colors = [COLORS.primary, COLORS.success, COLORS.warning, COLORS.info];
        
        charts.planDistribution = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Plan distribution chart error:', error);
    }
}

// ============================================
// MRR/ARR TREND CHART
// ============================================

async function createMRRChart(elementId, months = 12) {
    const ctx = document.getElementById(elementId);
    if (!ctx) return;
    
    try {
        const response = await fetch(`/api/admin/dashboard/mrr-chart?months=${months}`);
        const data = await response.json();
        
        const labels = data.map(item => {
            const date = new Date(item.month);
            return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
        });
        
        const mrrData = data.map(item => item.mrr / 100);
        const arrData = data.map(item => item.arr / 100);
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'MRR',
                        data: mrrData,
                        borderColor: COLORS.success,
                        backgroundColor: hexToRgba(COLORS.success, 0.1),
                        tension: 0.4,
                        fill: true
                    },
                    {
                        label: 'ARR',
                        data: arrData,
                        borderColor: COLORS.primary,
                        backgroundColor: hexToRgba(COLORS.primary, 0.1),
                        tension: 0.4,
                        fill: true
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + formatCurrency(context.parsed.y * 100);
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
    } catch (error) {
        console.error('MRR chart error:', error);
    }
}

// ============================================
// INITIALIZE ALL CHARTS
// ============================================

function initializeCharts() {
    // Revenue Chart
    if (document.getElementById('revenueChart')) {
        createRevenueChart('revenueChart', 12);
    }
    
    // Subscriptions Status Chart
    if (document.getElementById('subscriptionsChart')) {
        createSubscriptionsChart('subscriptionsChart');
    }
    
    // Churn Chart
    if (document.getElementById('churnChart')) {
        createChurnChart('churnChart', 6);
    }
    
    // Growth Chart
    if (document.getElementById('growthChart')) {
        createGrowthChart('growthChart', 12);
    }
    
    // Plan Distribution Chart
    if (document.getElementById('planDistributionChart')) {
        createPlanDistributionChart('planDistributionChart');
    }
    
    // MRR Chart
    if (document.getElementById('mrrChart')) {
        createMRRChart('mrrChart', 12);
    }
}

// ============================================
// CHART CONTROLS
// ============================================

function updateChartPeriod(chartType, months) {
    switch(chartType) {
        case 'revenue':
            createRevenueChart('revenueChart', months);
            break;
        case 'churn':
            createChurnChart('churnChart', months);
            break;
        case 'growth':
            createGrowthChart('growthChart', months);
            break;
        case 'mrr':
            createMRRChart('mrrChart', months);
            break;
    }
}

function refreshAllCharts() {
    initializeCharts();
}

function downloadChart(chartId) {
    const canvas = document.getElementById(chartId);
    if (!canvas) return;
    
    const url = canvas.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url;
    a.download = `${chartId}_${new Date().toISOString().split('T')[0]}.png`;
    a.click();
}

// ============================================
// UTILITY FUNCTIONS
// ============================================

function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value / 100);
}

function hexToRgba(hex, alpha) {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

// ============================================
// EXPORT FOR GLOBAL ACCESS
// ============================================

window.AdminCharts = {
    initialize: initializeCharts,
    refresh: refreshAllCharts,
    updatePeriod: updateChartPeriod,
    download: downloadChart,
    create: {
        revenue: createRevenueChart,
        subscriptions: createSubscriptionsChart,
        churn: createChurnChart,
        growth: createGrowthChart,
        planDistribution: createPlanDistributionChart,
        mrr: createMRRChart
    }
};
