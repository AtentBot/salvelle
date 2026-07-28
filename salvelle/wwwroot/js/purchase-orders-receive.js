const PurchaseOrdersReceive = {
    orderId: null,
    order: null,

    init(orderId) {
        this.orderId = orderId;
        this.loadOrder();
        this.bindEvents();
        this.setCurrentDateTime();
    },

    setCurrentDateTime() {
        const now = new Date();
        const datetime = now.toISOString().slice(0, 16);
        $('#actualDeliveryDate').val(datetime);
    },

    bindEvents() {
        $('#formReceiveOrder').submit((e) => this.submitReceive(e));
    },

    async loadOrder() {
        try {
            $('#loadingOrder').show();
            $('#formReceiveOrder').hide();

            const response = await fetch(`/api/PurchaseOrders/${this.orderId}`);
            
            if (!response.ok) {
                throw new Error('Pedido não encontrado');
            }

            this.order = await response.json();

            if (this.order.status !== 'ENVIADO') {
                throw new Error('Este pedido não está pronto para recebimento');
            }

            this.renderOrder();

        } catch (error) {
            console.error('Erro ao carregar pedido:', error);
            this.showError(error.message);
            setTimeout(() => window.location.href = `/compras/${this.orderId}`, 2000);
        } finally {
            $('#loadingOrder').hide();
        }
    },

    renderOrder() {
        $('#orderNumber').text(this.order.orderNumber);
        $('#supplierName').text(this.order.supplierName);
        $('#orderDate').text(this.formatDate(this.order.orderDate));
        $('#totalValue').text(this.formatCurrency(this.order.finalValue));

        this.renderItems();
        $('#formReceiveOrder').show();
    },

    renderItems() {
        const container = $('#itemsContainer');
        container.empty();

        this.order.items.forEach((item, index) => {
            const card = this.createItemCard(item, index);
            container.append(card);
        });
    },

    createItemCard(item, index) {
        return `
            <div class="item-card" data-item-index="${index}">
                <div class="item-header">
                    ${item.rawMaterialName}
                    <span class="float-end text-muted">
                        ${item.quantityOrdered} ${item.unit}
                    </span>
                </div>

                <div class="batch-info">
                    <input type="hidden" name="items[${index}].purchaseOrderItemId" value="${item.id}">

                    <div class="row g-3">
                        <div class="col-md-3">
                            <label class="form-label">Quantidade Recebida *</label>
                            <input type="number" name="items[${index}].quantityReceived" 
                                   class="form-control" min="0.01" step="0.01" 
                                   max="${item.quantityOrdered}" value="${item.quantityOrdered}" required>
                            <small class="text-muted">Máx: ${item.quantityOrdered}</small>
                        </div>

                        <div class="col-md-3">
                            <label class="form-label">Nº do Lote *</label>
                            <input type="text" name="items[${index}].batchNumber" 
                                   class="form-control" maxlength="50" required>
                        </div>

                        <div class="col-md-3">
                            <label class="form-label">Data de Fabricação *</label>
                            <input type="date" name="items[${index}].manufactureDate" 
                                   class="form-control" max="${new Date().toISOString().split('T')[0]}" required>
                        </div>

                        <div class="col-md-3">
                            <label class="form-label">Data de Validade *</label>
                            <input type="date" name="items[${index}].expiryDate" 
                                   class="form-control" min="${new Date().toISOString().split('T')[0]}" required>
                        </div>

                        <div class="col-md-6">
                            <label class="form-label">Certificado de Análise</label>
                            <input type="text" name="items[${index}].certificateOfAnalysis" 
                                   class="form-control" maxlength="200">
                        </div>

                        <div class="col-md-6">
                            <label class="form-label">Observações</label>
                            <input type="text" name="items[${index}].notes" 
                                   class="form-control" maxlength="200">
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    async submitReceive(e) {
        e.preventDefault();

        if (!this.validateForm()) {
            return;
        }

        const data = this.collectFormData();

        const result = await Swal.fire({
            title: 'Confirmar Recebimento',
            html: `
                <p>Confirma o recebimento de <strong>${data.items.length} item(ns)</strong>?</p>
                <p class="text-muted small">Após a confirmação, os lotes serão criados no estoque.</p>
            `,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, confirmar',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) return;

        try {
            const response = await fetch(`/api/PurchaseOrders/${this.orderId}/receive`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao confirmar recebimento');
            }

            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: 'Recebimento confirmado. Lotes criados no estoque.',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                window.location.href = `/compras/${this.orderId}`;
            });

        } catch (error) {
            this.showError(error.message);
        }
    },

    validateForm() {
        const form = document.getElementById('formReceiveOrder');
        
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            this.showError('Preencha todos os campos obrigatórios');
            return false;
        }

        const items = this.collectFormData().items;
        
        for (const item of items) {
            if (item.expiryDate <= item.manufactureDate) {
                this.showError('A data de validade deve ser posterior à data de fabricação');
                return false;
            }

            if (item.quantityReceived > this.order.items.find(i => i.id === item.purchaseOrderItemId).quantityOrdered) {
                this.showError('Quantidade recebida não pode ser maior que a quantidade pedida');
                return false;
            }
        }

        return true;
    },

    collectFormData() {
        const actualDeliveryDate = new Date($('#actualDeliveryDate').val()).toISOString();
        const supplierInvoiceNumber = $('#supplierInvoiceNumber').val();

        const items = [];
        $('.item-card').each(function() {
            const index = $(this).data('item-index');
            items.push({
                purchaseOrderItemId: parseInt($(`input[name="items[${index}].purchaseOrderItemId"]`).val()),
                quantityReceived: parseFloat($(`input[name="items[${index}].quantityReceived"]`).val()),
                batchNumber: $(`input[name="items[${index}].batchNumber"]`).val(),
                manufactureDate: new Date($(`input[name="items[${index}].manufactureDate"]`).val()).toISOString(),
                expiryDate: new Date($(`input[name="items[${index}].expiryDate"]`).val()).toISOString(),
                certificateOfAnalysis: $(`input[name="items[${index}].certificateOfAnalysis"]`).val() || null,
                notes: $(`input[name="items[${index}].notes"]`).val() || null
            });
        });

        return {
            actualDeliveryDate,
            supplierInvoiceNumber: supplierInvoiceNumber || null,
            items
        };
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
