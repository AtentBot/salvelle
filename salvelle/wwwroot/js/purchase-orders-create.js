const PurchaseOrdersCreate = {
    items: [],
    rawMaterials: [],

    init() {
        this.loadSuppliers();
        this.loadRawMaterials();
        this.bindEvents();
        this.setupMinDate();
    },

    setupMinDate() {
        const today = new Date().toISOString().split('T')[0];
        $('#expectedDeliveryDate').attr('min', today);
    },

    bindEvents() {
        $('#btnAddItem').click(() => this.openAddItemModal());
        $('#btnConfirmAddItem').click(() => this.addItem());
        $('#btnCancelCreate').click(() => window.location.href = '/compras');
        $('#formCreateOrder').submit((e) => this.submitOrder(e));
        
        $('#discountValue, #shippingValue').on('input', () => this.updateTotals());
        
        $('#itemQuantity, #itemUnitPrice, #itemDiscountPercentage').on('input', () => this.updateItemTotal());
        
        $('#modalAddItem').on('hidden.bs.modal', () => this.resetItemForm());
    },

    async loadSuppliers() {
        try {
            const response = await fetch('/api/Suppliers');
            const suppliers = await response.json();
            
            const select = $('#supplierId');
            suppliers.forEach(supplier => {
                select.append(`<option value="${supplier.id}">${supplier.companyName}</option>`);
            });
        } catch (error) {
            console.error('Erro ao carregar fornecedores:', error);
        }
    },

    async loadRawMaterials() {
        try {
            const response = await fetch('/api/RawMaterials');
            const materials = await response.json();
            this.rawMaterials = materials;
            
            const select = $('#itemRawMaterialId');
            materials.forEach(material => {
                select.append(`<option value="${material.id}">${material.name}</option>`);
            });
        } catch (error) {
            console.error('Erro ao carregar matérias-primas:', error);
        }
    },

    openAddItemModal() {
        this.resetItemForm();
        const modal = new bootstrap.Modal(document.getElementById('modalAddItem'));
        modal.show();
    },

    resetItemForm() {
        $('#itemRawMaterialId').val('');
        $('#itemQuantity').val('');
        $('#itemUnit').val('g');
        $('#itemUnitPrice').val('0');
        $('#itemDiscountPercentage').val('0');
        $('#itemNotes').val('');
        $('#itemTotal').val('R$ 0,00');
    },

    updateItemTotal() {
        const quantity = parseFloat($('#itemQuantity').val()) || 0;
        const unitPrice = parseFloat($('#itemUnitPrice').val()) || 0;
        const discountPercentage = parseFloat($('#itemDiscountPercentage').val()) || 0;
        
        const subtotal = quantity * unitPrice;
        const discount = subtotal * (discountPercentage / 100);
        const total = subtotal - discount;
        
        $('#itemTotal').val(this.formatCurrency(total));
    },

    addItem() {
        const rawMaterialId = $('#itemRawMaterialId').val();
        const quantity = parseFloat($('#itemQuantity').val());
        const unit = $('#itemUnit').val();
        const unitPrice = parseFloat($('#itemUnitPrice').val());
        const discountPercentage = parseFloat($('#itemDiscountPercentage').val()) || 0;
        const notes = $('#itemNotes').val();

        if (!rawMaterialId || !quantity || !unit) {
            this.showError('Preencha todos os campos obrigatórios');
            return;
        }

        const rawMaterial = this.rawMaterials.find(m => m.id === rawMaterialId);
        if (!rawMaterial) {
            this.showError('Matéria-prima não encontrada');
            return;
        }

        const subtotal = quantity * unitPrice;
        const discount = subtotal * (discountPercentage / 100);
        const total = subtotal - discount;

        this.items.push({
            rawMaterialId,
            rawMaterialName: rawMaterial.name,
            quantityOrdered: quantity,
            unit,
            unitPrice,
            discountPercentage,
            totalPrice: total,
            notes
        });

        this.renderItems();
        this.updateTotals();
        
        bootstrap.Modal.getInstance(document.getElementById('modalAddItem')).hide();
    },

    renderItems() {
        const tbody = $('#itemsTableBody');
        tbody.empty();

        if (this.items.length === 0) {
            tbody.append(`
                <tr>
                    <td colspan="7" class="text-center text-muted">
                        Nenhum item adicionado. Clique em "Adicionar Item" para começar.
                    </td>
                </tr>
            `);
            return;
        }

        this.items.forEach((item, index) => {
            const row = `
                <tr>
                    <td>${item.rawMaterialName}</td>
                    <td>${item.quantityOrdered}</td>
                    <td>${item.unit}</td>
                    <td>${this.formatCurrency(item.unitPrice)}</td>
                    <td>${item.discountPercentage}%</td>
                    <td>${this.formatCurrency(item.totalPrice)}</td>
                    <td>
                        <button type="button" class="btn btn-sm btn-danger" onclick="PurchaseOrdersCreate.removeItem(${index})">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
            tbody.append(row);
        });
    },

    removeItem(index) {
        this.items.splice(index, 1);
        this.renderItems();
        this.updateTotals();
    },

    updateTotals() {
        const subtotal = this.items.reduce((sum, item) => sum + item.totalPrice, 0);
        const discount = parseFloat($('#discountValue').val()) || 0;
        const shipping = parseFloat($('#shippingValue').val()) || 0;
        const finalValue = subtotal - discount + shipping;

        $('#displaySubtotal').val(this.formatCurrency(subtotal));
        $('#displayFinalValue').val(this.formatCurrency(finalValue));
    },

    async submitOrder(e) {
        e.preventDefault();

        if (this.items.length === 0) {
            this.showError('Adicione pelo menos um item ao pedido');
            return;
        }

        const supplierId = $('#supplierId').val();
        if (!supplierId) {
            this.showError('Selecione um fornecedor');
            return;
        }

        const data = {
            supplierId,
            expectedDeliveryDate: $('#expectedDeliveryDate').val() || null,
            discountValue: parseFloat($('#discountValue').val()) || 0,
            shippingValue: parseFloat($('#shippingValue').val()) || 0,
            notes: $('#notes').val(),
            items: this.items.map(item => ({
                rawMaterialId: item.rawMaterialId,
                quantityOrdered: item.quantityOrdered,
                unit: item.unit,
                unitPrice: item.unitPrice,
                discountPercentage: item.discountPercentage,
                notes: item.notes
            }))
        };

        try {
            const response = await fetch('/api/PurchaseOrders', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao criar pedido');
            }

            const result = await response.json();

            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: result.message || 'Pedido criado com sucesso',
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                window.location.href = '/compras';
            });

        } catch (error) {
            this.showError(error.message);
        }
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
