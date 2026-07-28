// ===== PDV - Ponto de Venda JavaScript =====
(function() {
'use strict';

// Helper para escapar HTML e prevenir XSS
function escapeHtml(str) {
    if (str == null) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
}

// Estado do PDV
let cart = [];
let selectedCustomer = null;
let currentCashRegister = null;
let payments = [];
let selectedPaymentMethod = null;

// ===== INICIALIZAÇÃO =====
document.addEventListener('DOMContentLoaded', function() {
    updateClock();
    setInterval(updateClock, 1000);
    loadCashRegisterStatus();
    
    // Enter na busca de produtos
    document.getElementById('productSearch')?.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            searchProduct(this.value);
        }
    });
    
    // Calcular troco ao digitar valor recebido
    document.getElementById('cashReceived')?.addEventListener('input', calculateChange);
});

function updateClock() {
    const now = new Date();
    const options = { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' };
    document.getElementById('currentTime').textContent = now.toLocaleString('pt-BR', options);
}

// ===== CAIXA =====
async function loadCashRegisterStatus() {
    try {
        const response = await fetch('/api/CashRegister/current');
        const data = await response.json();
        
        if (data.success && data.isOpen) {
            currentCashRegister = data.cashRegister;
            updateCashRegisterUI(true);
        } else {
            currentCashRegister = null;
            updateCashRegisterUI(false);
        }
    } catch (error) {
        console.error('Erro ao carregar status do caixa:', error);
        showToast('Erro ao verificar caixa', 'danger');
    }
}

function updateCashRegisterUI(isOpen) {
    const infoEl = document.getElementById('cashRegisterInfo');
    const openBtn = document.querySelector('button[onclick="openCashRegister()"], button[onclick="closeCashRegister()"]');
    
    if (isOpen && currentCashRegister) {
        // Propriedades vêm em camelCase do .NET
        const code = currentCashRegister.code || 'N/A';
        const balance = currentCashRegister.currentCashBalance || 0;
        
        infoEl.innerHTML = `<i class="bi bi-check-circle text-success"></i> Caixa: ${code} | Saldo: R$ ${formatMoney(balance)}`;
        if (openBtn) {
            openBtn.innerHTML = '<i class="bi bi-cash-stack"></i> Fechar Caixa';
            openBtn.setAttribute('onclick', 'closeCashRegister()');
            openBtn.classList.remove('btn-light');
            openBtn.classList.add('btn-warning');
        }
    } else {
        infoEl.innerHTML = '<i class="bi bi-exclamation-triangle text-warning"></i> Nenhum caixa aberto';
        if (openBtn) {
            openBtn.innerHTML = '<i class="bi bi-cash-stack"></i> Abrir Caixa';
            openBtn.setAttribute('onclick', 'openCashRegister()');
            openBtn.classList.remove('btn-warning');
            openBtn.classList.add('btn-light');
        }
    }
}

async function openCashRegister() {
    // Verificar se já tem caixa aberto
    if (currentCashRegister) {
        showToast('Já existe um caixa aberto!', 'warning');
        return;
    }
    
    // Mostrar modal para informar saldo inicial
    const result = await Swal.fire({
        title: 'Abrir Caixa',
        html: `
            <div class="text-start">
                <label class="form-label">Saldo Inicial (Fundo de Troco)</label>
                <div class="input-group">
                    <span class="input-group-text">R$</span>
                    <input type="number" id="openingBalance" class="form-control" 
                           value="200.00" step="0.01" min="0">
                </div>
                <small class="text-muted">Valor em dinheiro disponível para troco</small>
                
                <label class="form-label mt-3">Observações (opcional)</label>
                <textarea id="openingObs" class="form-control" rows="2" 
                          placeholder="Ex: Conferido com João"></textarea>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-check-lg"></i> Abrir Caixa',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#198754',
        preConfirm: () => {
            const balance = parseFloat(document.getElementById('openingBalance').value) || 0;
            const obs = document.getElementById('openingObs').value;
            return { openingBalance: balance, observations: obs };
        }
    });
    
    if (!result.isConfirmed) return;
    
    try {
        const response = await fetch('/api/CashRegister/open', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(result.value)
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(`Caixa ${data.code} aberto com sucesso!`, 'success');
            await loadCashRegisterStatus();
        } else {
            showToast(data.message || 'Erro ao abrir caixa', 'danger');
        }
    } catch (error) {
        console.error('Erro ao abrir caixa:', error);
        showToast('Erro ao abrir caixa', 'danger');
    }
}

async function closeCashRegister() {
    if (!currentCashRegister) {
        showToast('Nenhum caixa aberto!', 'warning');
        return;
    }
    
    // Propriedades vêm em camelCase
    const code = currentCashRegister.code || 'N/A';
    const expectedBalance = currentCashRegister.currentCashBalance || 0;
    
    const result = await Swal.fire({
        title: 'Fechar Caixa',
        html: `
            <div class="text-start">
                <div class="alert alert-info">
                    <strong>Caixa:</strong> ${code}<br>
                    <strong>Saldo Esperado:</strong> R$ ${formatMoney(expectedBalance)}
                </div>
                
                <label class="form-label">Saldo Contado (Dinheiro em Caixa)</label>
                <div class="input-group">
                    <span class="input-group-text">R$</span>
                    <input type="number" id="actualBalance" class="form-control" 
                           value="${expectedBalance.toFixed(2)}" step="0.01" min="0">
                </div>
                
                <label class="form-label mt-3">Observações de Fechamento</label>
                <textarea id="closingObs" class="form-control" rows="2" 
                          placeholder="Ex: Conferido e fechado"></textarea>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-lock"></i> Fechar Caixa',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#dc3545',
        preConfirm: () => {
            const actual = parseFloat(document.getElementById('actualBalance').value) || 0;
            const obs = document.getElementById('closingObs').value;
            return { actualClosingBalance: actual, observations: obs };
        }
    });
    
    if (!result.isConfirmed) return;
    
    try {
        const response = await fetch('/api/CashRegister/close', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(result.value)
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast('Caixa fechado com sucesso!', 'success');
            currentCashRegister = null;
            updateCashRegisterUI(false);
        } else {
            showToast(data.message || 'Erro ao fechar caixa', 'danger');
        }
    } catch (error) {
        console.error('Erro ao fechar caixa:', error);
        showToast('Erro ao fechar caixa', 'danger');
    }
}

// ===== CARRINHO =====
function addToCart(item) {
    if (!currentCashRegister) {
        showToast('Abra o caixa antes de adicionar itens!', 'warning');
        return;
    }
    
    // Verificar se já existe no carrinho
    const existingIndex = cart.findIndex(i => i.id === item.id);
    
    if (existingIndex >= 0) {
        cart[existingIndex].quantity += 1;
    } else {
        cart.push({
            id: item.id,
            description: item.description || item.name,
            quantity: 1,
            unitPrice: item.unitPrice || item.price,
            manipulationOrderId: item.manipulationOrderId || null
        });
    }
    
    renderCart();
    calculateTotal();
}

function removeFromCart(index) {
    cart.splice(index, 1);
    renderCart();
    calculateTotal();
}

function updateQuantity(index, delta) {
    cart[index].quantity += delta;
    if (cart[index].quantity <= 0) {
        removeFromCart(index);
    } else {
        renderCart();
        calculateTotal();
    }
}

function clearCart() {
    if (cart.length === 0) return;
    
    Swal.fire({
        title: 'Limpar Carrinho?',
        text: 'Todos os itens serão removidos',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        confirmButtonText: 'Sim, Limpar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            cart = [];
            payments = [];
            renderCart();
            renderPayments();
            calculateTotal();
        }
    });
}

function renderCart() {
    const container = document.getElementById('cartItems');
    
    if (cart.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-5">
                <i class="bi bi-cart-x fs-1 d-block mb-3"></i>
                <p>Carrinho vazio</p>
                <p class="small">Busque produtos ou escaneie código de barras</p>
            </div>
        `;
        return;
    }
    
    container.innerHTML = cart.map((item, index) => `
        <div class="d-flex justify-content-between align-items-center border-bottom py-2">
            <div class="flex-grow-1">
                <strong>${escapeHtml(item.description)}</strong>
                <div class="small text-muted">
                    R$ ${formatMoney(item.unitPrice)} x ${item.quantity} = 
                    <strong>R$ ${formatMoney(item.unitPrice * item.quantity)}</strong>
                </div>
            </div>
            <div class="btn-group btn-group-sm">
                <button class="btn btn-outline-secondary" onclick="updateQuantity(${index}, -1)">
                    <i class="bi bi-dash"></i>
                </button>
                <span class="btn btn-outline-secondary disabled">${item.quantity}</span>
                <button class="btn btn-outline-secondary" onclick="updateQuantity(${index}, 1)">
                    <i class="bi bi-plus"></i>
                </button>
                <button class="btn btn-outline-danger" onclick="removeFromCart(${index})">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>
    `).join('');
}

function calculateTotal() {
    const subtotal = cart.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
    const discountPercent = parseFloat(document.getElementById('discountInput')?.value) || 0;
    const discountAmount = subtotal * (discountPercent / 100);
    const total = subtotal - discountAmount;
    const totalPaid = payments.reduce((sum, p) => sum + p.amount, 0);
    const remaining = total - totalPaid;
    
    document.getElementById('subtotal').textContent = `R$ ${formatMoney(subtotal)}`;
    document.getElementById('discountAmount').textContent = `- R$ ${formatMoney(discountAmount)}`;
    document.getElementById('totalAmount').textContent = `R$ ${formatMoney(total)}`;
    document.getElementById('totalPaid').textContent = `R$ ${formatMoney(totalPaid)}`;
    document.getElementById('remaining').textContent = `R$ ${formatMoney(remaining)}`;
    
    // Mudar cor do restante
    const remainingEl = document.getElementById('remaining');
    if (remaining <= 0) {
        remainingEl.classList.remove('text-danger');
        remainingEl.classList.add('text-success');
    } else {
        remainingEl.classList.remove('text-success');
        remainingEl.classList.add('text-danger');
    }
    
    // Habilitar/desabilitar botão de finalizar
    const finishBtn = document.getElementById('finishSaleBtn');
    if (finishBtn) {
        finishBtn.disabled = cart.length === 0 || remaining > 0.01;
    }
    
    return { subtotal, discountAmount, total, totalPaid, remaining };
}

// Função chamada pelo botão Finalizar Venda
async function finishSale() {
    await finalizeSale();
}

// ===== BUSCA DE PRODUTOS =====
async function searchProduct(query) {
    if (!query || query.length < 2) return;
    
    if (!currentCashRegister) {
        showToast('Abra o caixa antes de adicionar itens!', 'warning');
        return;
    }
    
    try {
        // Usar endpoint unificado de busca do PDV
        const response = await fetch(`/api/PrescriptionQuotes/pdv-search?query=${encodeURIComponent(query)}`);
        const data = await response.json();
        
        if (data.success && data.data && data.data.length > 0) {
            showSearchResults(data.data);
        } else {
            showToast('Nenhum orçamento ou pedido encontrado', 'info');
        }
    } catch (error) {
        console.error('Erro na busca:', error);
        showToast('Erro ao buscar', 'danger');
    }
}

function showSearchResults(results) {
    const typeLabels = {
        'quote': { label: 'Orçamento', color: 'success' },
        'order': { label: 'OM Pronta', color: 'primary' }
    };
    
    Swal.fire({
        title: 'Selecione para Vender',
        html: `
            <div class="list-group text-start" style="max-height: 400px; overflow-y: auto;">
                ${results.map((item, i) => `
                    <button type="button" class="list-group-item list-group-item-action" 
                            onclick="selectSearchResult(${i})">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <span class="badge bg-${typeLabels[item.type].color} me-2">${typeLabels[item.type].label}</span>
                                <strong>${item.code}</strong>
                            </div>
                            <span class="badge bg-dark fs-6">R$ ${formatMoney(item.price)}</span>
                        </div>
                        <div class="mt-1">
                            <i class="bi bi-person"></i> ${item.customerName || 'Cliente não identificado'}
                        </div>
                        <small class="text-muted">
                            ${item.description} - ${item.totalQuantity}
                        </small>
                    </button>
                `).join('')}
            </div>
        `,
        showConfirmButton: false,
        showCloseButton: true,
        width: '500px'
    });
    
    window._searchResults = results;
}

async function selectSearchResult(index) {
    const item = window._searchResults[index];
    Swal.close();
    
    if (item.type === 'quote') {
        // Orçamento aprovado - mostrar modal de pagamento para converter em venda
        await showQuotePaymentModal(item);
    } else {
        // OM finalizada - adicionar ao carrinho normal
        addToCart({
            id: item.id,
            description: `${item.description} - ${item.code}`,
            unitPrice: item.price,
            manipulationOrderId: item.id
        });
        document.getElementById('productSearch').value = '';
    }
}

async function showQuotePaymentModal(quote) {
    const result = await Swal.fire({
        title: 'Finalizar Venda do Orçamento',
        html: `
            <div class="text-start">
                <div class="alert alert-success">
                    <strong>Orçamento:</strong> ${quote.code}<br>
                    <strong>Cliente:</strong> ${quote.customerName || 'Não identificado'}<br>
                    <strong>Produto:</strong> ${quote.description} - ${quote.totalQuantity}
                </div>
                
                <h5 class="text-center mb-3">
                    Total: <strong class="text-success fs-4">R$ ${formatMoney(quote.price)}</strong>
                </h5>
                
                <hr>
                
                <label class="form-label fw-bold">Forma de Pagamento</label>
                <select class="form-select mb-3" id="quotePaymentMethod">
                    <option value="DINHEIRO">💵 Dinheiro</option>
                    <option value="CARTAO_DEBITO">💳 Cartão de Débito</option>
                    <option value="CARTAO_CREDITO">💳 Cartão de Crédito</option>
                    <option value="PIX">📱 PIX</option>
                </select>
                
                <label class="form-label">Valor Pago</label>
                <div class="input-group mb-3">
                    <span class="input-group-text">R$</span>
                    <input type="number" class="form-control" id="quoteAmountPaid" 
                           value="${quote.price.toFixed(2)}" step="0.01" min="0">
                </div>
                
                <div class="row">
                    <div class="col-6">
                        <label class="form-label">Desconto</label>
                        <div class="input-group">
                            <span class="input-group-text">R$</span>
                            <input type="number" class="form-control" id="quoteDiscount" 
                                   value="0" step="0.01" min="0">
                        </div>
                    </div>
                    <div class="col-6">
                        <label class="form-label">Parcelas</label>
                        <select class="form-select" id="quoteInstallments">
                            <option value="1">1x</option>
                            <option value="2">2x</option>
                            <option value="3">3x</option>
                            <option value="4">4x</option>
                            <option value="5">5x</option>
                            <option value="6">6x</option>
                        </select>
                    </div>
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-check-circle"></i> Finalizar Venda',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#198754',
        width: '450px',
        preConfirm: () => {
            return {
                paymentMethod: document.getElementById('quotePaymentMethod').value,
                amountPaid: parseFloat(document.getElementById('quoteAmountPaid').value) || 0,
                discountAmount: parseFloat(document.getElementById('quoteDiscount').value) || 0,
                installments: parseInt(document.getElementById('quoteInstallments').value) || 1
            };
        }
    });
    
    if (!result.isConfirmed) return;
    
    // Converter orçamento em venda
    await convertQuoteToSale(quote.id, result.value);
}

async function convertQuoteToSale(quoteId, paymentData) {
    try {
        Swal.fire({ title: 'Processando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
        
        const response = await fetch(`/api/PrescriptionQuotes/${quoteId}/convert-to-sale`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                paymentMethod: paymentData.paymentMethod,
                amountPaid: paymentData.amountPaid,
                installments: paymentData.installments,
                discountAmount: paymentData.discountAmount,
                discountReason: null
            })
        });
        
        const data = await response.json();
        
        Swal.close();
        
        if (data.message && !data.message.includes('Erro')) {
            await Swal.fire({
                icon: 'success',
                title: 'Venda Realizada!',
                html: `
                    <p>${data.message}</p>
                    <hr>
                    <small class="text-muted">
                        Venda ID: ${data.saleId}<br>
                        Ordem de Manipulação: ${data.manipulationOrderId}
                    </small>
                `,
                confirmButtonText: 'OK'
            });
            
            // Atualizar caixa
            await loadCashRegisterStatus();
            
            // Limpar busca
            document.getElementById('productSearch').value = '';
        } else {
            showToast(data.message || 'Erro ao processar venda', 'danger');
        }
    } catch (error) {
        Swal.close();
        console.error('Erro ao converter orçamento:', error);
        showToast('Erro ao processar venda', 'danger');
    }
}

// ===== PAGAMENTOS =====
function showPaymentModal(method) {
    selectedPaymentMethod = method;
    document.getElementById('paymentMethod').value = method;
    
    // Atualizar título do modal
    const methodNames = {
        'DINHEIRO': 'Dinheiro',
        'CARTAO_DEBITO': 'Cartão de Débito',
        'CARTAO_CREDITO': 'Cartão de Crédito',
        'PIX': 'PIX'
    };
    document.getElementById('paymentModalTitle').textContent = `Pagamento: ${methodNames[method] || method}`;
    
    // Mostrar campos específicos
    document.getElementById('cashFields').classList.add('d-none');
    document.getElementById('cardFields').classList.add('d-none');
    document.getElementById('pixFields').classList.add('d-none');
    
    const { remaining } = calculateTotal();
    document.getElementById('paymentAmount').value = remaining.toFixed(2);
    
    switch(method) {
        case 'DINHEIRO':
            document.getElementById('cashFields').classList.remove('d-none');
            document.getElementById('cashReceived').value = remaining.toFixed(2);
            calculateChange();
            break;
        case 'CARTAO_DEBITO':
        case 'CARTAO_CREDITO':
            document.getElementById('cardFields').classList.remove('d-none');
            document.getElementById('installmentsField').style.display = 
                method === 'CARTAO_CREDITO' ? 'block' : 'none';
            break;
        case 'PIX':
            document.getElementById('pixFields').classList.remove('d-none');
            break;
    }
    
    // Abrir modal
    const modal = new bootstrap.Modal(document.getElementById('paymentModal'));
    modal.show();
}

function setRemainingAmount() {
    const { remaining } = calculateTotal();
    document.getElementById('paymentAmount').value = remaining.toFixed(2);
    if (selectedPaymentMethod === 'DINHEIRO') {
        document.getElementById('cashReceived').value = remaining.toFixed(2);
        calculateChange();
    }
}

function selectPaymentMethod(method) {
    selectedPaymentMethod = method;
    
    // Atualizar botões
    document.querySelectorAll('.payment-method-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.closest('.btn').classList.add('active');
    
    // Mostrar campos específicos
    document.getElementById('cashFields').classList.add('d-none');
    document.getElementById('cardFields').classList.add('d-none');
    document.getElementById('pixFields').classList.add('d-none');
    
    switch(method) {
        case 'DINHEIRO':
            document.getElementById('cashFields').classList.remove('d-none');
            const { remaining } = calculateTotal();
            document.getElementById('cashReceived').value = remaining.toFixed(2);
            calculateChange();
            break;
        case 'CARTAO_DEBITO':
        case 'CARTAO_CREDITO':
            document.getElementById('cardFields').classList.remove('d-none');
            document.getElementById('installmentsField').style.display = 
                method === 'CARTAO_CREDITO' ? 'block' : 'none';
            break;
        case 'PIX':
            document.getElementById('pixFields').classList.remove('d-none');
            break;
    }
    
    // Abrir modal
    const modal = new bootstrap.Modal(document.getElementById('paymentModal'));
    modal.show();
}

function calculateChange() {
    const { total, totalPaid } = calculateTotal();
    const remaining = total - totalPaid;
    const received = parseFloat(document.getElementById('cashReceived')?.value) || 0;
    const change = received - remaining;
    
    const changeDisplay = document.getElementById('changeDisplay');
    const changeAmount = document.getElementById('changeAmount');
    
    if (change > 0) {
        changeDisplay.style.display = 'block';
        changeAmount.textContent = `R$ ${formatMoney(change)}`;
    } else {
        changeDisplay.style.display = 'none';
    }
}

function addPayment() {
    if (!selectedPaymentMethod) {
        showToast('Selecione uma forma de pagamento', 'warning');
        return;
    }
    
    const { remaining } = calculateTotal();
    let amount = parseFloat(document.getElementById('paymentAmount').value) || 0;
    
    if (amount <= 0) {
        showToast('Informe um valor válido', 'warning');
        return;
    }
    
    // Limitar ao valor restante
    amount = Math.min(amount, remaining);
    
    let details = {};
    
    switch(selectedPaymentMethod) {
        case 'DINHEIRO':
            const received = parseFloat(document.getElementById('cashReceived').value) || 0;
            // Para dinheiro, o valor do pagamento é o restante, não o recebido
            // O troco é calculado separadamente
            details = {
                cashReceived: received,
                changeAmount: Math.max(0, received - remaining)
            };
            break;
        case 'CARTAO_DEBITO':
        case 'CARTAO_CREDITO':
            details = {
                cardBrand: document.getElementById('cardBrand').value,
                cardLastDigits: document.getElementById('cardLastDigits').value,
                installments: parseInt(document.getElementById('installments').value) || 1,
                nsu: document.getElementById('nsu').value,
                authorizationCode: document.getElementById('authCode').value
            };
            break;
        case 'PIX':
            details = {
                pixKey: document.getElementById('pixKey').value,
                pixTransactionId: document.getElementById('pixTransactionId').value
            };
            break;
    }
    
    payments.push({
        method: selectedPaymentMethod,
        amount: amount,
        ...details
    });
    
    renderPayments();
    calculateTotal();
    
    // Fechar modal
    bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
    
    // Verificar se pode finalizar
    const totals = calculateTotal();
    if (totals.remaining <= 0) {
        promptFinalizeSale();
    }
}

function renderPayments() {
    const container = document.getElementById('paymentsContainer');
    
    if (payments.length === 0) {
        container.innerHTML = '<p class="text-muted small">Nenhum pagamento adicionado</p>';
        return;
    }
    
    const methodNames = {
        'DINHEIRO': 'Dinheiro',
        'CARTAO_DEBITO': 'Débito',
        'CARTAO_CREDITO': 'Crédito',
        'PIX': 'PIX'
    };
    
    container.innerHTML = payments.map((p, i) => `
        <div class="d-flex justify-content-between align-items-center border-bottom py-1">
            <span>${methodNames[p.method] || p.method}</span>
            <div>
                <strong>R$ ${formatMoney(p.amount)}</strong>
                <button class="btn btn-sm btn-link text-danger p-0 ms-2" onclick="removePayment(${i})">
                    <i class="bi bi-x-circle"></i>
                </button>
            </div>
        </div>
    `).join('');
}

function removePayment(index) {
    payments.splice(index, 1);
    renderPayments();
    calculateTotal();
}

// ===== FINALIZAÇÃO =====
async function promptFinalizeSale() {
    const result = await Swal.fire({
        title: 'Finalizar Venda?',
        text: 'Confirma a finalização desta venda?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#198754',
        confirmButtonText: '<i class="bi bi-check-lg"></i> Finalizar',
        cancelButtonText: 'Continuar Editando'
    });
    
    if (result.isConfirmed) {
        await finalizeSale();
    }
}

async function finalizeSale() {
    if (!currentCashRegister) {
        showToast('Abra o caixa antes de finalizar!', 'warning');
        return;
    }
    
    if (cart.length === 0) {
        showToast('Adicione itens ao carrinho!', 'warning');
        return;
    }
    
    const totals = calculateTotal();
    if (totals.remaining > 0.01) {
        showToast('Adicione mais pagamentos para cobrir o total!', 'warning');
        return;
    }
    
    const discountPercent = parseFloat(document.getElementById('discountInput')?.value) || 0;
    
    // Montar DTO
    const saleData = {
        customerId: selectedCustomer?.id || null,
        items: cart.map(item => ({
            manipulationOrderId: item.manipulationOrderId,
            description: item.description,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            discountPercentage: 0
        })),
        paymentMethod: payments[0]?.method || 'DINHEIRO',
        paidAmount: totals.totalPaid,
        discountPercentage: discountPercent,
        observations: document.getElementById('paymentObs')?.value || ''
    };
    
    try {
        Swal.fire({ title: 'Processando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
        
        const response = await fetch('/api/PDV/quick-sale', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(saleData)
        });
        
        const data = await response.json();
        
        Swal.close();
        
        if (data.success) {
            showToast('Venda realizada com sucesso!', 'success');
            
            // Mostrar recibo
            if (data.data) {
                showReceipt(data.data);
            }
            
            // Limpar carrinho
            cart = [];
            payments = [];
            selectedCustomer = null;
            renderCart();
            renderPayments();
            calculateTotal();
            document.getElementById('customerSearch').value = '';
            document.getElementById('selectedCustomerId').value = '';
            
            // Atualizar caixa
            await loadCashRegisterStatus();
        } else {
            showToast(data.message || 'Erro ao finalizar venda', 'danger');
        }
    } catch (error) {
        Swal.close();
        console.error('Erro ao finalizar:', error);
        showToast('Erro ao finalizar venda', 'danger');
    }
}

function showReceipt(receipt) {
    Swal.fire({
        title: 'Venda Finalizada!',
        html: `
            <div class="text-start">
                <div class="text-center mb-3">
                    <strong>${receipt.establishmentName}</strong><br>
                    <small>${receipt.establishmentAddress}</small><br>
                    <small>CNPJ: ${receipt.establishmentCnpj}</small>
                </div>
                <hr>
                <strong>Código:</strong> ${receipt.code}<br>
                <strong>Data:</strong> ${new Date(receipt.saleDate).toLocaleString('pt-BR')}<br>
                ${receipt.customerName ? `<strong>Cliente:</strong> ${receipt.customerName}<br>` : ''}
                <hr>
                <table class="table table-sm">
                    <thead><tr><th>Item</th><th class="text-end">Valor</th></tr></thead>
                    <tbody>
                        ${receipt.items.map(i => `
                            <tr>
                                <td>${i.description} (${i.quantity}x)</td>
                                <td class="text-end">R$ ${formatMoney(i.totalPrice)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
                <hr>
                <div class="d-flex justify-content-between">
                    <span>Subtotal:</span>
                    <span>R$ ${formatMoney(receipt.subtotal)}</span>
                </div>
                <div class="d-flex justify-content-between">
                    <span>Desconto:</span>
                    <span>- R$ ${formatMoney(receipt.discountAmount)}</span>
                </div>
                <div class="d-flex justify-content-between fw-bold fs-5">
                    <span>TOTAL:</span>
                    <span>R$ ${formatMoney(receipt.totalAmount)}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between">
                    <span>Pago:</span>
                    <span>R$ ${formatMoney(receipt.paidAmount)}</span>
                </div>
                ${receipt.changeAmount > 0 ? `
                    <div class="d-flex justify-content-between text-success">
                        <span>Troco:</span>
                        <span>R$ ${formatMoney(receipt.changeAmount)}</span>
                    </div>
                ` : ''}
            </div>
        `,
        confirmButtonText: '<i class="bi bi-printer"></i> Imprimir',
        showCancelButton: true,
        cancelButtonText: 'Fechar'
    }).then((result) => {
        if (result.isConfirmed) {
            window.print();
        }
    });
}

// ===== CLIENTE =====
function searchCustomer() {
    const modal = new bootstrap.Modal(document.getElementById('customerModal'));
    modal.show();
    
    document.getElementById('customerModalSearch').value = '';
    document.getElementById('customersList').innerHTML = '';
    
    // Carregar todos os clientes
    loadCustomers('');
}

async function loadCustomers(query) {
    try {
        const response = await fetch(`/api/Customers?search=${encodeURIComponent(query)}`);
        const data = await response.json();
        
        const container = document.getElementById('customersList');
        
        if (!data.data || data.data.length === 0) {
            container.innerHTML = '<p class="text-muted">Nenhum cliente encontrado</p>';
            return;
        }
        
        container.innerHTML = data.data.map(c => `
            <div class="list-group-item list-group-item-action"
                 onclick="selectCustomer('${escapeHtml(c.id)}', '${escapeHtml(c.fullName)}', '${escapeHtml(c.cpf || '')}')">
                <div class="d-flex justify-content-between">
                    <strong>${escapeHtml(c.fullName)}</strong>
                    <span class="text-muted">${escapeHtml(c.cpf || 'Sem CPF')}</span>
                </div>
                <small class="text-muted">${escapeHtml(c.phone || '')} ${escapeHtml(c.email || '')}</small>
            </div>
        `).join('');
    } catch (error) {
        console.error('Erro ao buscar clientes:', error);
    }
}

function selectCustomer(id, name, cpf) {
    selectedCustomer = { id, name, cpf };
    document.getElementById('selectedCustomerId').value = id;
    document.getElementById('customerSearch').value = `${name} ${cpf ? '(' + cpf + ')' : ''}`;
    
    bootstrap.Modal.getInstance(document.getElementById('customerModal')).hide();
}

function clearCustomer() {
    selectedCustomer = null;
    document.getElementById('selectedCustomerId').value = '';
    document.getElementById('customerSearch').value = '';
}

// ===== PDV CONTROL =====
function closePDV() {
    Swal.fire({
        title: 'Fechar PDV?',
        text: 'Deseja sair do Ponto de Venda?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sim, Sair',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = '/';
        }
    });
}

function showProductsModal() {
    // TODO: Implementar modal com catálogo de produtos
    showToast('Catálogo em desenvolvimento', 'info');
}

// ===== UTILITÁRIOS =====
function formatMoney(value) {
    return (value || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function showToast(message, type = 'info') {
    // Usar Toastr se disponível, senão alert
    if (typeof toastr !== 'undefined') {
        toastr[type](message);
    } else if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });
        
        const icons = { success: 'success', danger: 'error', warning: 'warning', info: 'info' };
        Toast.fire({ icon: icons[type] || 'info', title: message });
    } else {
        alert(message);
    }
}

// Busca de clientes com debounce
document.getElementById('customerModalSearch')?.addEventListener('input', debounce(function() {
    loadCustomers(this.value);
}, 300));

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func.apply(this, args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Expor funções necessárias para onclick handlers no HTML
window.showOnlineOrdersModal = showOnlineOrdersModal;
window.openCashRegister = openCashRegister;
window.closePDV = closePDV;
window.showProductsModal = showProductsModal;
window.clearOnlineOrder = clearOnlineOrder;
window.clearCart = clearCart;
window.searchCustomer = searchCustomer;
window.clearCustomer = clearCustomer;
window.calculateTotal = calculateTotal;
window.showPaymentModal = showPaymentModal;
window.finishSale = finishSale;
window.loadOnlineOrders = loadOnlineOrders;
window.setRemainingAmount = setRemainingAmount;
window.addPayment = addPayment;
window.printReceipt = printReceipt;
window.selectOnlineOrder = selectOnlineOrder;
window.removeFromCart = removeFromCart;
window.removePayment = removePayment;
window.searchProduct = searchProduct;
})();
