/**
 * Salvelle - Admin Establishments Management JavaScript
 * Handles establishment listing, filtering, blocking, and details
 */

let currentPage = 1;
let currentFilters = {
    status: '',
    search: '',
    subscriptionStatus: ''
};

document.addEventListener('DOMContentLoaded', function() {
    initializeEstablishmentsList();
    initializeFilters();
    initializeSearch();
    initializeBulkActions();
});

// ============================================
// ESTABLISHMENTS LIST
// ============================================

async function loadEstablishments(page = 1, take = 20) {
    showLoading();
    
    try {
        const params = new URLSearchParams({
            skip: (page - 1) * take,
            take: take,
            ...currentFilters
        });
        
        // Remove empty params
        for (let [key, value] of params.entries()) {
            if (!value) params.delete(key);
        }
        
        const response = await fetch(`/api/admin/establishments?${params.toString()}`);
        
        if (!response.ok) {
            throw new Error('Erro ao carregar estabelecimentos');
        }
        
        const data = await response.json();
        updateEstablishmentsTable(data.establishments);
        updatePagination(data.total, page, take);
        
        currentPage = page;
        
    } catch (error) {
        console.error('Load establishments error:', error);
        showAlert('danger', 'Erro ao carregar estabelecimentos. Tente novamente.');
    } finally {
        hideLoading();
    }
}

function initializeEstablishmentsList() {
    // Load from URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    currentFilters.status = urlParams.get('status') || '';
    currentFilters.subscriptionStatus = urlParams.get('subscriptionStatus') || '';
    currentFilters.search = urlParams.get('search') || '';
    
    // Set filter values in UI
    if (currentFilters.status) {
        document.getElementById('filterStatus').value = currentFilters.status;
    }
    if (currentFilters.subscriptionStatus) {
        document.getElementById('filterSubscription').value = currentFilters.subscriptionStatus;
    }
    if (currentFilters.search) {
        document.getElementById('searchInput').value = currentFilters.search;
    }
    
    loadEstablishments(1);
}

function updateEstablishmentsTable(establishments) {
    const tbody = document.getElementById('establishmentsTableBody');
    if (!tbody) return;
    
    if (!establishments || establishments.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="text-center text-muted py-5">
                    <i class="bi bi-inbox fs-1 d-block mb-3"></i>
                    Nenhum estabelecimento encontrado
                </td>
            </tr>
        `;
        return;
    }
    
    let html = '';
    
    establishments.forEach(est => {
        const statusBadge = est.isActive 
            ? '<span class="badge bg-success">Ativo</span>' 
            : '<span class="badge bg-danger">Inativo</span>';
        
        const subBadge = getSubscriptionBadge(est.subscriptionStatus);
        const createdDate = new Date(est.createdAt).toLocaleDateString('pt-BR');
        
        html += `
            <tr>
                <td>
                    <input type="checkbox" class="form-check-input establishment-checkbox" 
                           value="${est.id}">
                </td>
                <td>
                    <strong>${est.nomeFantasia}</strong><br>
                    <small class="text-muted">${est.cnpj}</small>
                </td>
                <td>${est.email}</td>
                <td>${est.whatsApp || '-'}</td>
                <td>${statusBadge}</td>
                <td>${subBadge}</td>
                <td><small class="text-muted">${createdDate}</small></td>
                <td>
                    <div class="btn-group btn-group-sm">
                        <a href="/admin/establishments/${est.id}" 
                           class="btn btn-outline-primary" title="Ver detalhes">
                            <i class="bi bi-eye"></i>
                        </a>
                        <button class="btn btn-outline-${est.isActive ? 'warning' : 'success'}" 
                                onclick="toggleEstablishmentStatus('${est.id}', ${est.isActive})"
                                title="${est.isActive ? 'Bloquear' : 'Desbloquear'}">
                            <i class="bi bi-${est.isActive ? 'lock' : 'unlock'}"></i>
                        </button>
                        <button class="btn btn-outline-danger" 
                                onclick="deleteEstablishment('${est.id}')"
                                title="Excluir">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    });
    
    tbody.innerHTML = html;
}

function updatePagination(total, currentPage, take) {
    const totalPages = Math.ceil(total / take);
    const pagination = document.getElementById('pagination');
    
    if (!pagination || totalPages <= 1) {
        if (pagination) pagination.innerHTML = '';
        return;
    }
    
    let html = '<ul class="pagination justify-content-center">';
    
    // Previous button
    html += `
        <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="loadEstablishments(${currentPage - 1}); return false;">
                <i class="bi bi-chevron-left"></i>
            </a>
        </li>
    `;
    
    // Page numbers
    const maxPages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxPages / 2));
    let endPage = Math.min(totalPages, startPage + maxPages - 1);
    
    if (endPage - startPage < maxPages - 1) {
        startPage = Math.max(1, endPage - maxPages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
        html += `
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" onclick="loadEstablishments(${i}); return false;">
                    ${i}
                </a>
            </li>
        `;
    }
    
    // Next button
    html += `
        <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="loadEstablishments(${currentPage + 1}); return false;">
                <i class="bi bi-chevron-right"></i>
            </a>
        </li>
    `;
    
    html += '</ul>';
    
    // Show info
    const showing = document.getElementById('showingInfo');
    if (showing) {
        const start = (currentPage - 1) * take + 1;
        const end = Math.min(currentPage * take, total);
        showing.textContent = `Mostrando ${start} a ${end} de ${total} estabelecimentos`;
    }
    
    pagination.innerHTML = html;
}

// ============================================
// FILTERS
// ============================================

function initializeFilters() {
    const filterStatus = document.getElementById('filterStatus');
    const filterSubscription = document.getElementById('filterSubscription');
    const clearFiltersBtn = document.getElementById('clearFilters');
    
    if (filterStatus) {
        filterStatus.addEventListener('change', function() {
            currentFilters.status = this.value;
            applyFilters();
        });
    }
    
    if (filterSubscription) {
        filterSubscription.addEventListener('change', function() {
            currentFilters.subscriptionStatus = this.value;
            applyFilters();
        });
    }
    
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function(e) {
            e.preventDefault();
            clearAllFilters();
        });
    }
}

function applyFilters() {
    updateURLParams();
    loadEstablishments(1);
}

function clearAllFilters() {
    currentFilters = {
        status: '',
        search: '',
        subscriptionStatus: ''
    };
    
    document.getElementById('filterStatus').value = '';
    document.getElementById('filterSubscription').value = '';
    document.getElementById('searchInput').value = '';
    
    updateURLParams();
    loadEstablishments(1);
}

function updateURLParams() {
    const params = new URLSearchParams();
    
    if (currentFilters.status) params.set('status', currentFilters.status);
    if (currentFilters.subscriptionStatus) params.set('subscriptionStatus', currentFilters.subscriptionStatus);
    if (currentFilters.search) params.set('search', currentFilters.search);
    
    const newUrl = params.toString() 
        ? `${window.location.pathname}?${params.toString()}`
        : window.location.pathname;
    
    window.history.replaceState({}, '', newUrl);
}

// ============================================
// SEARCH
// ============================================

let searchTimeout;

function initializeSearch() {
    const searchInput = document.getElementById('searchInput');
    
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            
            searchTimeout = setTimeout(() => {
                currentFilters.search = this.value.trim();
                applyFilters();
            }, 500); // Debounce 500ms
        });
    }
}

// ============================================
// ESTABLISHMENT ACTIONS
// ============================================

async function toggleEstablishmentStatus(establishmentId, currentStatus) {
    const action = currentStatus ? 'bloquear' : 'desbloquear';
    const actionPast = currentStatus ? 'bloqueado' : 'desbloqueado';
    
    let reason = '';
    if (currentStatus) {
        reason = prompt('Motivo do bloqueio:');
        if (!reason) return;
    }
    
    if (!confirm(`Tem certeza que deseja ${action} este estabelecimento?`)) {
        return;
    }
    
    showLoading();
    
    try {
        const endpoint = currentStatus 
            ? `/api/admin/establishments/${establishmentId}/block`
            : `/api/admin/establishments/${establishmentId}/unblock`;
        
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ reason })
        });
        
        if (response.ok) {
            showAlert('success', `Estabelecimento ${actionPast} com sucesso!`);
            loadEstablishments(currentPage);
        } else {
            const error = await response.json();
            throw new Error(error.message || `Erro ao ${action} estabelecimento`);
        }
    } catch (error) {
        console.error('Toggle status error:', error);
        showAlert('danger', error.message);
    } finally {
        hideLoading();
    }
}

async function deleteEstablishment(establishmentId) {
    if (!confirm('ATENÇÃO: Esta ação irá desativar o estabelecimento e cancelar sua assinatura. Tem certeza?')) {
        return;
    }
    
    const confirmText = prompt('Digite "CONFIRMAR" para prosseguir:');
    if (confirmText !== 'CONFIRMAR') {
        return;
    }
    
    showLoading();
    
    try {
        const response = await fetch(`/api/admin/establishments/${establishmentId}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            showAlert('success', 'Estabelecimento excluído com sucesso!');
            loadEstablishments(currentPage);
        } else {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao excluir estabelecimento');
        }
    } catch (error) {
        console.error('Delete error:', error);
        showAlert('danger', error.message);
    } finally {
        hideLoading();
    }
}

// ============================================
// BULK ACTIONS
// ============================================

function initializeBulkActions() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const bulkActionBtn = document.getElementById('bulkActionBtn');
    
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            const checkboxes = document.querySelectorAll('.establishment-checkbox');
            checkboxes.forEach(cb => cb.checked = this.checked);
            updateBulkActionButton();
        });
    }
    
    // Update bulk button when individual checkboxes change
    document.addEventListener('change', function(e) {
        if (e.target.classList.contains('establishment-checkbox')) {
            updateBulkActionButton();
        }
    });
}

function updateBulkActionButton() {
    const selectedCheckboxes = document.querySelectorAll('.establishment-checkbox:checked');
    const bulkActionBtn = document.getElementById('bulkActionBtn');
    
    if (bulkActionBtn) {
        bulkActionBtn.disabled = selectedCheckboxes.length === 0;
        bulkActionBtn.textContent = `Ações em lote (${selectedCheckboxes.length})`;
    }
}

async function executeBulkAction(action) {
    const selectedCheckboxes = document.querySelectorAll('.establishment-checkbox:checked');
    const establishmentIds = Array.from(selectedCheckboxes).map(cb => cb.value);
    
    if (establishmentIds.length === 0) {
        showAlert('warning', 'Selecione pelo menos um estabelecimento');
        return;
    }
    
    if (!confirm(`Executar "${action}" em ${establishmentIds.length} estabelecimentos?`)) {
        return;
    }
    
    showLoading();
    
    try {
        const response = await fetch('/api/admin/establishments/bulk-action', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                action: action,
                establishmentIds: establishmentIds
            })
        });
        
        if (response.ok) {
            showAlert('success', 'Ação executada com sucesso!');
            loadEstablishments(currentPage);
        } else {
            throw new Error('Erro ao executar ação em lote');
        }
    } catch (error) {
        console.error('Bulk action error:', error);
        showAlert('danger', error.message);
    } finally {
        hideLoading();
    }
}

// ============================================
// EXPORT FUNCTIONS
// ============================================

async function exportEstablishments() {
    showLoading();
    
    try {
        const params = new URLSearchParams(currentFilters);
        const response = await fetch(`/api/admin/establishments/export?${params.toString()}`);
        
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `establishments_${new Date().toISOString().split('T')[0]}.csv`;
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

function getSubscriptionBadge(status) {
    const badges = {
        'TRIALING': '<span class="badge bg-info"><i class="bi bi-clock-history me-1"></i>Teste</span>',
        'ACTIVE': '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>Ativa</span>',
        'PAST_DUE': '<span class="badge bg-warning"><i class="bi bi-exclamation-triangle me-1"></i>Pendente</span>',
        'CANCELED': '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>Cancelada</span>',
        'INCOMPLETE': '<span class="badge bg-secondary">Incompleta</span>'
    };
    
    return badges[status] || '<span class="badge bg-secondary">-</span>';
}

function showLoading() {
    const loader = document.getElementById('tableLoader');
    if (loader) {
        loader.style.display = 'flex';
    }
}

function hideLoading() {
    const loader = document.getElementById('tableLoader');
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

window.EstablishmentsAdmin = {
    load: loadEstablishments,
    toggleStatus: toggleEstablishmentStatus,
    delete: deleteEstablishment,
    export: exportEstablishments,
    bulkAction: executeBulkAction
};
