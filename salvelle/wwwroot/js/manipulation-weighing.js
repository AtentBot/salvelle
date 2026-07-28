// ========================================
// MANIPULATION WEIGHING - Frontend Logic
// ========================================

const TOLERANCE_PERCENTAGE = 5.0;
let componentsData = [];
let photosData = {};

// ========================================
// INICIALIZAÇÃO
// ========================================
document.addEventListener('DOMContentLoaded', function() {
    initializeForm();
    attachEventListeners();
});

function initializeForm() {
    // Coletar dados dos componentes
    const rows = document.querySelectorAll('#componentsTable tr[data-component-index]');
    rows.forEach(row => {
        const index = row.dataset.componentIndex;
        componentsData[index] = {
            componentId: row.dataset.componentId,
            batchId: null,
            weighedQuantity: null,
            photoBase64: null,
            theoretical: parseFloat(row.querySelector('.weighed-quantity').dataset.theoretical)
        };
    });
}

// ========================================
// EVENT LISTENERS
// ========================================
function attachEventListeners() {
    // Seleção de lote
    document.querySelectorAll('.batch-select').forEach(select => {
        select.addEventListener('change', handleBatchSelection);
    });

    // Input de quantidade pesada
    document.querySelectorAll('.weighed-quantity').forEach(input => {
        input.addEventListener('input', handleWeightInput);
        input.addEventListener('blur', validateWeight);
    });

    // Botão de foto
    document.querySelectorAll('.photo-btn').forEach(btn => {
        btn.addEventListener('click', handlePhotoButton);
    });

    // Input de foto (file)
    document.querySelectorAll('.photo-input').forEach(input => {
        input.addEventListener('change', handlePhotoSelection);
    });

    // Submit do formulário
    document.getElementById('weighingForm').addEventListener('submit', handleSubmit);
}

// ========================================
// HANDLERS - SELEÇÃO DE LOTE
// ========================================
function handleBatchSelection(e) {
    const select = e.target;
    const index = select.dataset.componentIndex;
    const selectedOption = select.options[select.selectedIndex];
    
    if (selectedOption.value) {
        componentsData[index].batchId = selectedOption.value;
        
        // Mostrar informações de validade
        const expiryInfo = select.parentElement.querySelector('.expiry-info');
        const expiryDate = select.parentElement.querySelector('.expiry-date');
        expiryDate.textContent = selectedOption.dataset.expiry;
        expiryInfo.style.display = 'block';
        
        // Atualizar status visual
        updateComponentStatus(index, 'batch-selected');
    } else {
        componentsData[index].batchId = null;
        const expiryInfo = select.parentElement.querySelector('.expiry-info');
        expiryInfo.style.display = 'none';
        updateComponentStatus(index, 'pending');
    }
    
    checkFormCompletion();
}

// ========================================
// HANDLERS - PESO
// ========================================
function handleWeightInput(e) {
    const input = e.target;
    const index = input.dataset.componentIndex;
    const value = parseFloat(input.value);
    
    if (!isNaN(value) && value > 0) {
        componentsData[index].weighedQuantity = value;
        calculateDeviation(index);
        updateComponentStatus(index, 'weighed');
    } else {
        componentsData[index].weighedQuantity = null;
        clearDeviation(index);
    }
    
    checkFormCompletion();
}

function validateWeight(e) {
    const input = e.target;
    const index = input.dataset.componentIndex;
    const value = parseFloat(input.value);
    const theoretical = componentsData[index].theoretical;
    
    if (!isNaN(value) && theoretical > 0) {
        const deviation = Math.abs((value - theoretical) / theoretical * 100);
        
        if (deviation > TOLERANCE_PERCENTAGE) {
            showWarning(`Componente ${parseInt(index) + 1}: Desvio de ${deviation.toFixed(2)}% excede a tolerância de ${TOLERANCE_PERCENTAGE}%`);
        }
    }
}

function calculateDeviation(index) {
    const weighed = componentsData[index].weighedQuantity;
    const theoretical = componentsData[index].theoretical;
    
    if (!weighed || !theoretical) return;
    
    const deviation = ((weighed - theoretical) / theoretical * 100);
    const deviationAbs = Math.abs(deviation);
    const badge = document.querySelector(`.deviation-badge[data-component-index="${index}"]`);
    
    badge.textContent = `${deviation >= 0 ? '+' : ''}${deviation.toFixed(2)}%`;
    
    // Cores baseadas no desvio
    badge.classList.remove('bg-success', 'bg-warning', 'bg-danger', 'bg-secondary');
    if (deviationAbs <= 2) {
        badge.classList.add('bg-success');
    } else if (deviationAbs <= TOLERANCE_PERCENTAGE) {
        badge.classList.add('bg-warning');
    } else {
        badge.classList.add('bg-danger');
    }
}

function clearDeviation(index) {
    const badge = document.querySelector(`.deviation-badge[data-component-index="${index}"]`);
    badge.textContent = '-';
    badge.classList.remove('bg-success', 'bg-warning', 'bg-danger');
    badge.classList.add('bg-secondary');
}

// ========================================
// HANDLERS - FOTO
// ========================================
function handlePhotoButton(e) {
    const btn = e.currentTarget;
    const index = btn.dataset.componentIndex;
    const fileInput = document.querySelector(`.photo-input[data-component-index="${index}"]`);
    fileInput.click();
}

function handlePhotoSelection(e) {
    const input = e.target;
    const index = input.dataset.componentIndex;
    const file = input.files[0];
    
    if (file) {
        // Validar tamanho (2MB max)
        if (file.size > 2 * 1024 * 1024) {
            alert('Foto muito grande! Tamanho máximo: 2MB');
            input.value = '';
            return;
        }
        
        // Converter para Base64
        const reader = new FileReader();
        reader.onload = function(e) {
            const base64 = e.target.result;
            componentsData[index].photoBase64 = base64;
            
            // Mostrar preview
            const preview = document.querySelector(`.photo-preview[data-component-index="${index}"]`);
            const img = preview.querySelector('img');
            img.src = base64;
            preview.style.display = 'block';
            
            // Atualizar botão
            const btn = document.querySelector(`.photo-btn[data-component-index="${index}"]`);
            btn.classList.remove('btn-outline-primary');
            btn.classList.add('btn-success');
            btn.innerHTML = '<i class="bi bi-check-circle"></i>';
            
            updateComponentStatus(index, 'photo-added');
        };
        reader.readAsDataURL(file);
    }
}

// ========================================
// STATUS VISUAL
// ========================================
function updateComponentStatus(index, status) {
    const statusIcon = document.querySelector(`.component-status[data-component-index="${index}"]`);
    const data = componentsData[index];
    
    // Verificar se todos os campos estão preenchidos
    const isComplete = data.batchId && data.weighedQuantity && data.photoBase64;
    
    if (isComplete) {
        statusIcon.className = 'bi bi-check-circle-fill component-status text-success';
    } else if (data.batchId || data.weighedQuantity || data.photoBase64) {
        statusIcon.className = 'bi bi-circle-half component-status text-warning';
    } else {
        statusIcon.className = 'bi bi-circle component-status';
        statusIcon.style.color = '#ddd';
    }
}

// ========================================
// VALIDAÇÃO E SUBMIT
// ========================================
function checkFormCompletion() {
    const submitBtn = document.getElementById('submitBtn');
    let allComplete = true;
    
    componentsData.forEach((data, index) => {
        if (!data.batchId || !data.weighedQuantity) {
            allComplete = false;
        }
    });
    
    submitBtn.disabled = !allComplete;
}

function showWarning(message) {
    const container = document.getElementById('warningsContainer');
    const list = document.getElementById('warningsList');
    
    const li = document.createElement('li');
    li.textContent = message;
    list.appendChild(li);
    
    container.style.display = 'block';
}

function clearWarnings() {
    const container = document.getElementById('warningsContainer');
    const list = document.getElementById('warningsList');
    list.innerHTML = '';
    container.style.display = 'none';
}

async function handleSubmit(e) {
    e.preventDefault();
    
    clearWarnings();
    
    const submitBtn = document.getElementById('submitBtn');
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processando...';
    
    try {
        // Preparar dados para API
        const manipulationOrderId = document.getElementById('manipulationOrderId').value;
        const observations = document.getElementById('observations').value;
        
        const components = componentsData.map(data => ({
            componentId: data.componentId,
            batchId: data.batchId,
            weighedQuantity: data.weighedQuantity,
            photoBase64: data.photoBase64,
            notes: null
        }));
        
        const payload = {
            employeeId: "00000000-0000-0000-0000-000000000000", // Será preenchido pelo backend via Claims
            components: components,
            observations: observations
        };
        
        // Chamar API
        const response = await fetch(`/api/manipulationorders/${manipulationOrderId}/steps/pesagem`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            // Sucesso!
            if (result.data.warnings && result.data.warnings.length > 0) {
                result.data.warnings.forEach(warning => showWarning(warning));
            }
            
            // Redirecionar após 2 segundos
            setTimeout(() => {
                window.location.href = `/manipulationorders/${manipulationOrderId}`;
            }, 2000);
            
            submitBtn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Pesagem Concluída!';
            submitBtn.classList.remove('btn-primary');
            submitBtn.classList.add('btn-success');
        } else {
            // Erro
            alert('Erro ao processar pesagem: ' + (result.message || 'Erro desconhecido'));
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Confirmar Pesagem';
        }
    } catch (error) {
        console.error('Erro ao enviar pesagem:', error);
        alert('Erro ao comunicar com o servidor. Tente novamente.');
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Confirmar Pesagem';
    }
}
