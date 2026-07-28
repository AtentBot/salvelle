const PurchaseOrders = {
    init() {
        this.loadSuppliers();
        this.loadOrders();
        this.bindEvents();
    },

    bindEvents() {
        $('#btnFilter').click(() => this.loadOrders());
        $('#filterStatus, #filterSupplier').change(() => this.loadOrders());
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

            const select = $('#filterSupplier');
            suppliers.forEach(supplier => {
                select.append(`<option value="${supplier.id}">${supplier.companyName}</option>`);
            });
        } catch (error) {
            console.error('Erro ao carregar fornecedores:', error);
        }
    },

    async loadOrders() {
        try {
            $('#loadingOrders').show();
            $('#ordersTable').hide();
            $('#noOrders').hide();

            const params = new URLSearchParams();
            
            const supplierId = $('#filterSupplier').val();
            if (supplierId) params.append('supplierId', supplierId);
            
            const status = $('#filterStatus').val();
            if (status) params.append('status', status);
            
            const startDate = $('#filterStartDate').val();
            if (startDate) params.append('startDate', startDate);
            
            const endDate = $('#filterEndDate').val();
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
            $('#loadingOrders').hide();
            $('#ordersTable').hide();
            $('#noOrders').show();
            $('#noOrders').html(`
                <i class="bi bi-exclamation-triangle text-danger" style="font-size: 3rem;"></i>
                <p class="text-danger mt-3">Erro ao carregar pedidos</p>
                <p class="text-muted small">${error.message}</p>
            `);
        } finally {
            $('#loadingOrders').hide();
        }
    },

    updateStats(orders) {
        const stats = {
            pending: orders.filter(o => o.status === 'PENDENTE').length,
            approved: orders.filter(o => o.status === 'APROVADO').length,
            sent: orders.filter(o => o.status === 'ENVIADO').length,
            received: orders.filter(o => o.status === 'RECEBIDO').length
        };

        $('#statPending').text(stats.pending);
        $('#statApproved').text(stats.approved);
        $('#statSent').text(stats.sent);
        $('#statReceived').text(stats.received);
    },

    renderOrders(orders) {
        const tbody = $('#ordersTableBody');
        tbody.empty();

        if (orders.length === 0) {
            $('#noOrders').show();
            return;
        }

        $('#ordersTable').show();

        orders.forEach(order => {
            const row = `
                <tr>
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
                </tr>
            `;
            tbody.append(row);
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

    showError(message) {
        Swal.fire({
            icon: 'error',
            title: 'Erro',
            text: message
        });
    }
};
