const PurchaseOrdersDetails = {
    orderId: null,
    order: null,

    init(orderId) {
        this.orderId = orderId;
        this.loadOrder();
        this.bindEvents();
    },

    bindEvents() {
        $('#btnApprove').click(() => this.approveOrder());
        $('#btnSend').click(() => this.sendOrder());
        $('#btnReceive').click(() => this.goToReceive());
        $('#btnDelete').click(() => this.deleteOrder());
    },

    async loadOrder() {
        try {
            $('#loadingOrder').show();
            $('#orderDetails').hide();

            const response = await fetch(`/api/PurchaseOrders/${this.orderId}`);
            
            if (!response.ok) {
                throw new Error('Pedido não encontrado');
            }

            this.order = await response.json();
            this.renderOrder();

        } catch (error) {
            console.error('Erro ao carregar pedido:', error);
            this.showError(error.message);
            setTimeout(() => window.location.href = '/compras', 2000);
        } finally {
            $('#loadingOrder').hide();
        }
    },

    renderOrder() {
        $('#orderNumber').text(this.order.orderNumber);
        $('#orderStatus').html(this.renderStatus(this.order.status));
        $('#supplierName').text(this.order.supplierName);
        $('#orderDate').text(this.formatDate(this.order.orderDate));
        $('#expectedDeliveryDate').text(this.order.expectedDeliveryDate ? this.formatDate(this.order.expectedDeliveryDate) : '-');
        $('#actualDeliveryDate').text(this.order.actualDeliveryDate ? this.formatDate(this.order.actualDeliveryDate) : '-');
        $('#createdByEmployee').text(this.order.createdByEmployeeName);
        $('#approvedByEmployee').text(this.order.approvedByEmployeeName || '-');
        
        if (this.order.notes) {
            $('#orderNotes').text(this.order.notes);
            $('#notesRow').show();
        }

        if (this.order.supplierInvoiceNumber) {
            $('#supplierInvoice').text(this.order.supplierInvoiceNumber);
            $('#invoiceRow').show();
        }

        $('#totalValue').text(this.formatCurrency(this.order.totalValue));
        $('#discountValue').text(this.formatCurrency(this.order.discountValue));
        $('#shippingValue').text(this.formatCurrency(this.order.shippingValue));
        $('#finalValue').text(this.formatCurrency(this.order.finalValue));

        this.renderItems();
        this.renderTimeline();
        this.renderActionButtons();

        $('#orderDetails').show();
    },

    renderItems() {
        const tbody = $('#itemsTableBody');
        tbody.empty();

        this.order.items.forEach(item => {
            const row = `
                <tr>
                    <td>${item.rawMaterialName}</td>
                    <td>${item.quantityOrdered}</td>
                    <td>${item.quantityReceived}</td>
                    <td>${item.unit}</td>
                    <td>${this.formatCurrency(item.unitPrice)}</td>
                    <td>${item.discountPercentage}%</td>
                    <td>${this.formatCurrency(item.totalPrice)}</td>
                    <td>${this.renderItemStatus(item.status)}</td>
                </tr>
            `;
            tbody.append(row);
        });
    },

    renderTimeline() {
        $('#createdAt').text(this.formatDateTime(this.order.createdAt));

        if (this.order.approvedAt) {
            $('#approvedAt').text(this.formatDateTime(this.order.approvedAt));
            $('#approvedTimeline').show();
        }

        if (this.order.status === 'ENVIADO' || this.order.status === 'RECEBIDO') {
            $('#sentTimeline').show();
        }

        if (this.order.actualDeliveryDate) {
            $('#receivedAt').text(this.formatDateTime(this.order.actualDeliveryDate));
            $('#receivedTimeline').show();
        }
    },

    renderActionButtons() {
        $('#btnApprove, #btnSend, #btnReceive, #btnDelete').hide();

        switch (this.order.status) {
            case 'PENDENTE':
                $('#btnApprove').show();
                $('#btnDelete').show();
                break;
            case 'APROVADO':
                $('#btnSend').show();
                $('#btnDelete').show();
                break;
            case 'ENVIADO':
                $('#btnReceive').show();
                break;
        }
    },

    async approveOrder() {
        const result = await Swal.fire({
            title: 'Aprovar Pedido',
            text: 'Deseja aprovar este pedido de compra?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, aprovar',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/api/PurchaseOrders/${this.orderId}/approve`, {
                method: 'PUT'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao aprovar pedido');
            }

            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: 'Pedido aprovado com sucesso',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                this.loadOrder();
            });

        } catch (error) {
            this.showError(error.message);
        }
    },

    async sendOrder() {
        const result = await Swal.fire({
            title: 'Enviar Pedido',
            text: 'Deseja enviar este pedido ao fornecedor?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, enviar',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/api/PurchaseOrders/${this.orderId}/send`, {
                method: 'PUT'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao enviar pedido');
            }

            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: 'Pedido enviado ao fornecedor',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                this.loadOrder();
            });

        } catch (error) {
            this.showError(error.message);
        }
    },

    goToReceive() {
        window.location.href = `/compras/${this.orderId}/receber`;
    },

    async deleteOrder() {
        const result = await Swal.fire({
            title: 'Excluir Pedido',
            text: 'Tem certeza que deseja excluir este pedido? Esta ação não pode ser desfeita.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sim, excluir',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#d33'
        });

        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/api/PurchaseOrders/${this.orderId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao excluir pedido');
            }

            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: 'Pedido excluído com sucesso',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                window.location.href = '/compras';
            });

        } catch (error) {
            this.showError(error.message);
        }
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

    renderItemStatus(status) {
        const badges = {
            'PENDENTE': '<span class="badge bg-warning">Pendente</span>',
            'RECEBIDO_PARCIAL': '<span class="badge bg-info">Parcial</span>',
            'RECEBIDO': '<span class="badge bg-success">Recebido</span>'
        };
        return badges[status] || status;
    },

    formatDate(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleDateString('pt-BR');
    },

    formatDateTime(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleString('pt-BR');
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
