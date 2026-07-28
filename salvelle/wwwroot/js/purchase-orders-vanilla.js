const PurchaseOrders = {
    init() {
        this.loadSuppliers();
        this.loadOrders();
        this.bindEvents();
    },

    bindEvents() {
        document.getElementById('btnFilter')?.addEventListener('click', () => this.loadOrders());
        document.getElementById('filterStatus')?.addEventListener('change', () => this.loadOrders());
        document.getElementById('filterSupplier')?.addEventListener('change', () => this.loadOrders());
    },

    async loadSuppliers() {
        try {
            const response = await fetch('/api/Suppliers', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                },
                credentials: 'include'
            });

            if (!response.ok) {
                console.error('Erro ao carregar fornecedores:', response.status);
                return;
            }

            const suppliers = await response.json();

            const select = document.getElementById('filterSupplier');
            suppliers.forEach(supplier => {
                const option = document.createElement('option');
                option.value = supplier.id;
                option.textContent = supplier.companyName;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Erro ao carregar fornecedores:', error);
        }
    },

    async loadOrders() {
        try {
            this.showElement('loadingOrders');
            this.hideElement('ordersTable');
            this.hideElement('noOrders');

            const params = new URLSearchParams();
            
            const supplierId = document.getElementById('filterSupplier')?.value;
            if (supplierId) params.append('supplierId', supplierId);
            
            const status = document.getElementById('filterStatus')?.value;
            if (status) params.append('status', status);
            
            const startDate = document.getElementById('filterStartDate')?.value;
            if (startDate) params.append('startDate', startDate);
            
            const endDate = document.getElementById('filterEndDate')?.value;
            if (endDate) params.append('endDate', endDate);

            const response = await fetch(`/api/PurchaseOrders?${params}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                },
                credentials: 'include'
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Erro na API:', errorText);
                throw new Error(`Erro ${response.status}: ${errorText}`);
            }

            const orders = await response.json();

            this.updateStats(orders);
            this.renderOrders(orders);

        } catch (error) {
            console.error('Erro ao carregar pedidos:', error);
            this.hideElement('loadingOrders');
            this.hideElement('ordersTable');
            this.showElement('noOrders');
            document.getElementById('noOrders').innerHTML = `
                <i class="bi bi-exclamation-triangle text-danger" style="font-size: 3rem;"></i>
                <p class="text-danger mt-3">Erro ao carregar pedidos</p>
                <p class="text-muted small">${error.message}</p>
            `;
        } finally {
            this.hideElement('loadingOrders');
        }
    },

    updateStats(orders) {
        const stats = {
            pending: orders.filter(o => o.status === 'PENDENTE').length,
            approved: orders.filter(o => o.status === 'APROVADO').length,
            sent: orders.filter(o => o.status === 'ENVIADO').length,
            received: orders.filter(o => o.status === 'RECEBIDO').length
        };

        document.getElementById('statPending').textContent = stats.pending;
        document.getElementById('statApproved').textContent = stats.approved;
        document.getElementById('statSent').textContent = stats.sent;
        document.getElementById('statReceived').textContent = stats.received;
    },

    renderOrders(orders) {
        const tbody = document.getElementById('ordersTableBody');
        tbody.innerHTML = '';

        if (orders.length === 0) {
            this.showElement('noOrders');
            document.getElementById('noOrders').innerHTML = `
                <i class="bi bi-inbox" style="font-size: 3rem; color: #ccc;"></i>
                <p class="text-muted mt-3">Nenhum pedido encontrado</p>
            `;
            return;
        }

        this.showElement('ordersTable');

        orders.forEach(order => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td><strong>${order.orderNumber}</strong></td>
                <td>${order.supplierName}</td>
                <td>${this.formatDate(order.orderDate)}</td>
                <td>${order.expectedDeliveryDate ? this.formatDate(order.expectedDeliveryDate) : '-'}</td>
                <td>${this.renderStatus(order.status)}</td>
                <td>${this.formatCurrency(order.totalValue)}</td>
                <td class="fw-bold text-success">${this.formatCurrency(order.finalValue)}</td>
                <td>${order.items.length}</td>
                <td>
                    <a href="/compras/${order.id}" class="btn btn-sm btn-primary">
                        <i class="bi bi-eye"></i> Ver
                    </a>
                </td>
            `;
            tbody.appendChild(row);
        });
    },

    renderStatus(status) {
        const badges = {
            'PENDENTE': '<span class="badge bg-warning">Pendente</span>',
            'APROVADO': '<span class="badge bg-success">Aprovado</span>',
            'ENVIADO': '<span class="badge bg-primary">Enviado</span>',
            'RECEBIDO': '<span class="badge bg-info">Recebido</span>',
            'CANCELADO': '<span class="badge bg-danger">Cancelado</span>'
        };
        return badges[status] || status;
    },

    formatDate(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleDateString('pt-BR');
    },

    formatCurrency(value) {
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL'
        }).format(value);
    },

    showElement(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = 'block';
    },

    hideElement(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = 'none';
    },

    showError(message) {
        alert(message);
    }
};
