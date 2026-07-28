function escapeHtml(str) {
    if (str == null) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
}

// Estado global da aplicação
const appState = {
    selectedPrescription: null,
    uploadedFileId: null,
    ocrResult: null,
    matchResults: null,
    selectedIngredients: {}
};

// Inicializar quando a página carregar
document.addEventListener('DOMContentLoaded', function() {
    loadPrescriptions();
    setupEventListeners();
});

// ============================================================================
// SETUP DE EVENT LISTENERS
// ============================================================================

function setupEventListeners() {
    const uploadZone = document.getElementById('uploadZone');
    const fileInput = document.getElementById('fileInput');
    const prescriptionSelect = document.getElementById('prescriptionSelect');

    // Click na zona de upload
    uploadZone.addEventListener('click', () => fileInput.click());

    // Drag & Drop
    uploadZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadZone.classList.add('dragover');
    });

    uploadZone.addEventListener('dragleave', () => {
        uploadZone.classList.remove('dragover');
    });

    uploadZone.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadZone.classList.remove('dragover');
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            handleFileSelect(files[0]);
        }
    });

    // Seleção de arquivo
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            handleFileSelect(e.target.files[0]);
        }
    });

    // Seleção de prescrição
    prescriptionSelect.addEventListener('change', function() {
        appState.selectedPrescription = this.value;
        updatePrescriptionInfo();
    });
}

// ============================================================================
// CARREGAR PRESCRIÇÕES
// ============================================================================

async function loadPrescriptions() {
    try {
        const response = await fetch('/api/Prescriptions?status=PENDENTE');
        const prescriptions = await response.json();
        
        const select = document.getElementById('prescriptionSelect');
        select.innerHTML = '<option value="">Selecione uma prescrição...</option>';
        
        prescriptions.forEach(p => {
            const option = document.createElement('option');
            option.value = p.id;
            option.textContent = `${p.code} - ${p.customerName} - ${formatDate(p.prescriptionDate)}`;
            select.appendChild(option);
        });
    } catch (error) {
        showError('Erro ao carregar prescrições: ' + error.message);
    }
}

async function updatePrescriptionInfo() {
    if (!appState.selectedPrescription) {
        document.getElementById('prescriptionStatus').value = '';
        return;
    }

    try {
        const response = await fetch(`/api/Prescriptions/${appState.selectedPrescription}`);
        const prescription = await response.json();
        document.getElementById('prescriptionStatus').value = prescription.status;
    } catch (error) {
        showError('Erro ao carregar dados da prescrição');
    }
}

// ============================================================================
// UPLOAD DE ARQUIVO
// ============================================================================

function handleFileSelect(file) {
    // Validar tipo
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
        showError('Tipo de arquivo não permitido. Use JPG, PNG ou PDF.');
        return;
    }

    // Validar tamanho (5MB)
    if (file.size > 5 * 1024 * 1024) {
        showError('Arquivo muito grande. Máximo 5MB.');
        return;
    }

    // Preview para imagens
    if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e) => {
            document.getElementById('previewImage').src = e.target.result;
            document.getElementById('previewContainer').style.display = 'block';
            document.getElementById('uploadZone').style.display = 'none';
        };
        reader.readAsDataURL(file);
    }

    // Habilitar botão de upload
    document.getElementById('uploadBtn').disabled = false;
    
    // Guardar arquivo
    appState.selectedFile = file;
}

function clearUpload() {
    document.getElementById('previewContainer').style.display = 'none';
    document.getElementById('uploadZone').style.display = 'block';
    document.getElementById('uploadBtn').disabled = true;
    document.getElementById('fileInput').value = '';
    appState.selectedFile = null;
}

async function uploadFile() {
    if (!appState.selectedPrescription) {
        showError('Selecione uma prescrição primeiro');
        return;
    }

    if (!appState.selectedFile) {
        showError('Selecione um arquivo primeiro');
        return;
    }

    const uploadBtn = document.getElementById('uploadBtn');
    uploadBtn.disabled = true;
    uploadBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Enviando...';

    try {
        // Converter para Base64
        const base64 = await fileToBase64(appState.selectedFile);
        
        const payload = {
            fileName: appState.selectedFile.name,
            fileType: appState.selectedFile.type,
            fileBase64: base64
        };

        const response = await fetch(`/api/Prescriptions/${appState.selectedPrescription}/upload`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!response.ok) throw new Error('Erro no upload');

        const result = await response.json();
        appState.uploadedFileId = result.fileId;

        showSuccess('Upload realizado com sucesso!');
        moveToStep(2);
        document.getElementById('uploadSection').style.display = 'none';
        document.getElementById('ocrSection').style.display = 'block';

    } catch (error) {
        showError('Erro no upload: ' + error.message);
        uploadBtn.disabled = false;
        uploadBtn.innerHTML = '<i class="bi bi-upload"></i> Fazer Upload';
    }
}

// ============================================================================
// PROCESSAMENTO OCR
// ============================================================================

async function processOCR() {
    document.getElementById('ocrLoading').classList.add('active');
    document.getElementById('processOcrBtn').style.display = 'none';

    try {
        const response = await fetch(
            `/api/Prescriptions/${appState.selectedPrescription}/files/${appState.uploadedFileId}/parse`,
            { method: 'POST' }
        );

        if (!response.ok) throw new Error('Erro no processamento');

        appState.ocrResult = await response.json();
        displayOCRResults();
        
        document.getElementById('ocrLoading').classList.remove('active');
        document.getElementById('ocrResults').style.display = 'block';
        document.getElementById('matchBtn').style.display = 'inline-block';

    } catch (error) {
        document.getElementById('ocrLoading').classList.remove('active');
        showError('Erro no OCR: ' + error.message);
        document.getElementById('processOcrBtn').style.display = 'inline-block';
    }
}

function displayOCRResults() {
    const result = appState.ocrResult;

    // Informações do médico
    document.getElementById('doctorName').textContent = result.doctorInfo?.name || '-';
    document.getElementById('doctorCrm').textContent = 
        (result.doctorInfo?.crm || '-') + '/' + (result.doctorInfo?.crmState || '-');

    // Informações do paciente
    document.getElementById('patientName').textContent = result.patientInfo?.name || '-';
    document.getElementById('patientAge').textContent = result.patientInfo?.age || '-';

    // Componentes
    const itemsList = document.getElementById('itemsList');
    itemsList.innerHTML = '';
    result.items.forEach((item, index) => {
        const div = document.createElement('div');
        div.className = 'ocr-result';
        div.innerHTML = `
            <strong>${index + 1}. ${escapeHtml(item.component)}</strong><br>
            <small class="text-muted">Quantidade: ${escapeHtml(item.quantity)} ${escapeHtml(item.unit)}</small><br>
            <small class="text-muted">Texto original: "${escapeHtml(item.rawText)}"</small>
        `;
        itemsList.appendChild(div);
    });

    // Instruções
    document.getElementById('instructions').textContent = result.instructions || '-';

    // Confiança
    const confidence = result.overallConfidence;
    const badge = document.getElementById('confidenceBadge');
    badge.textContent = `Confiança: ${confidence.toFixed(1)}%`;
    
    if (confidence >= 90) {
        badge.className = 'confidence-badge confidence-high';
    } else if (confidence >= 70) {
        badge.className = 'confidence-badge confidence-medium';
    } else {
        badge.className = 'confidence-badge confidence-low';
    }
}

// ============================================================================
// MATCHING DE INGREDIENTES
// ============================================================================

async function matchIngredients() {
    moveToStep(3);
    document.getElementById('matchingSection').style.display = 'block';
    document.getElementById('matchLoading').classList.add('active');

    try {
        const response = await fetch(
            `/api/Prescriptions/${appState.selectedPrescription}/match-ingredients`,
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ items: appState.ocrResult.items })
            }
        );

        if (!response.ok) throw new Error('Erro no matching');

        appState.matchResults = await response.json();
        displayMatchResults();

        document.getElementById('matchLoading').classList.remove('active');
        document.getElementById('confirmBtn').style.display = 'inline-block';

    } catch (error) {
        document.getElementById('matchLoading').classList.remove('active');
        showError('Erro no matching: ' + error.message);
    }
}

function displayMatchResults() {
    const container = document.getElementById('matchResults');
    container.innerHTML = '';

    appState.matchResults.matches.forEach((match, matchIndex) => {
        const matchDiv = document.createElement('div');
        matchDiv.className = 'ingredient-match';
        
        let html = `
            <div class="d-flex justify-content-between align-items-start mb-3">
                <div>
                    <h6 class="mb-1">${escapeHtml(match.ocrText)}</h6>
                    <small class="text-muted">
                        Quantidade: ${escapeHtml(match.quantity)} ${escapeHtml(match.unit)}<br>
                        Original: "${escapeHtml(match.rawText)}"
                    </small>
                </div>
            </div>
        `;

        if (match.suggestions.length === 0) {
            html += '<div class="alert alert-warning mb-0">Nenhuma sugestão encontrada</div>';
        } else {
            html += '<div class="suggestions-list">';
            match.suggestions.forEach((suggestion, suggIndex) => {
                const stockClass = suggestion.inStock ? 'in-stock' : 'out-stock';
                const stockText = suggestion.inStock 
                    ? `${suggestion.availableQuantity} ${suggestion.unit} disponível` 
                    : 'Sem estoque';
                
                html += `
                    <div class="suggestion"
                         data-match="${matchIndex}"
                         data-suggestion="${suggIndex}"
                         onclick="selectSuggestion(${matchIndex}, ${suggIndex})">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <strong>${escapeHtml(suggestion.name)}</strong>
                                ${suggestion.dciName ? `<br><small class="text-muted">DCI: ${escapeHtml(suggestion.dciName)}</small>` : ''}
                            </div>
                            <div class="text-end">
                                <span class="confidence-badge ${getConfidenceClass(suggestion.confidence)}">
                                    ${(suggestion.confidence * 100).toFixed(0)}%
                                </span>
                                <br>
                                <span class="stock-badge ${stockClass}">${escapeHtml(stockText)}</span>
                            </div>
                        </div>
                    </div>
                `;
            });
            html += '</div>';
        }

        matchDiv.innerHTML = html;
        container.appendChild(matchDiv);
    });
}

function selectSuggestion(matchIndex, suggestionIndex) {
    // Remover seleções anteriores do mesmo match
    document.querySelectorAll(`[data-match="${matchIndex}"]`).forEach(el => {
        el.classList.remove('selected');
    });

    // Adicionar nova seleção
    const element = document.querySelector(`[data-match="${matchIndex}"][data-suggestion="${suggestionIndex}"]`);
    element.classList.add('selected');

    // Guardar seleção
    const match = appState.matchResults.matches[matchIndex];
    const suggestion = match.suggestions[suggestionIndex];
    
    appState.selectedIngredients[matchIndex] = {
        ocrText: match.ocrText,
        quantity: match.quantity,
        unit: match.unit,
        rawMaterialId: suggestion.rawMaterialId,
        name: suggestion.name,
        confidence: suggestion.confidence,
        inStock: suggestion.inStock
    };
}

function getConfidenceClass(confidence) {
    if (confidence >= 0.9) return 'confidence-high';
    if (confidence >= 0.7) return 'confidence-medium';
    return 'confidence-low';
}

// ============================================================================
// CONFIRMAÇÃO E REVISÃO
// ============================================================================

function confirmSelection() {
    const selectedCount = Object.keys(appState.selectedIngredients).length;
    const totalMatches = appState.matchResults.matches.length;

    if (selectedCount === 0) {
        showError('Selecione pelo menos um componente');
        return;
    }

    if (selectedCount < totalMatches) {
        if (!confirm(`Você selecionou ${selectedCount} de ${totalMatches} componentes. Continuar?`)) {
            return;
        }
    }

    moveToStep(4);
    displayReview();
    document.getElementById('reviewSection').style.display = 'block';
}

function displayReview() {
    const container = document.getElementById('reviewContent');
    
    let html = '<h6 class="mb-3">Componentes Selecionados:</h6>';
    
    Object.values(appState.selectedIngredients).forEach((ing, index) => {
        const confidenceClass = getConfidenceClass(ing.confidence);
        const stockText = ing.inStock ? '✓ Em estoque' : '⚠ Sem estoque';
        
        html += `
            <div class="info-section">
                <div class="d-flex justify-content-between">
                    <div>
                        <strong>${index + 1}. ${escapeHtml(ing.name)}</strong><br>
                        <small class="text-muted">
                            OCR detectou: "${escapeHtml(ing.ocrText)}"<br>
                            Quantidade: ${escapeHtml(ing.quantity)} ${escapeHtml(ing.unit)}
                        </small>
                    </div>
                    <div class="text-end">
                        <span class="confidence-badge ${confidenceClass}">
                            ${(ing.confidence * 100).toFixed(0)}%
                        </span><br>
                        <small class="${ing.inStock ? 'text-success' : 'text-warning'}">${escapeHtml(stockText)}</small>
                    </div>
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

// ============================================================================
// CRIAR ORDEM DE MANIPULAÇÃO
// ============================================================================

async function createOrder() {
    if (confirm('Criar ordem de manipulação com os componentes selecionados?')) {
        showSuccess('Funcionalidade em desenvolvimento. Os dados foram processados com sucesso!');
    }
}

// ============================================================================
// NAVEGAÇÃO E CONTROLE DE STEPS
// ============================================================================

function moveToStep(stepNumber) {
    // Atualizar indicador visual
    for (let i = 1; i <= 4; i++) {
        const step = document.getElementById(`step${i}`);
        step.classList.remove('active', 'completed');
        
        if (i < stepNumber) {
            step.classList.add('completed');
        } else if (i === stepNumber) {
            step.classList.add('active');
        }
    }
}

function startOver() {
    if (confirm('Deseja processar uma nova receita? Os dados atuais serão perdidos.')) {
        location.reload();
    }
}

// ============================================================================
// UTILIDADES
// ============================================================================

function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            const base64 = reader.result.split(',')[1];
            resolve(base64);
        };
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR');
}

function showSuccess(message) {
    alert('✓ ' + message);
}

function showError(message) {
    alert('✗ ' + message);
}
