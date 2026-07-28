// ═══════════════════════════════════════════════════════════════════════════
// INSTRUÇÕES DE ATUALIZAÇÃO PARA CriarFormula.cshtml
// Substitua as funções indicadas no arquivo original
// ═══════════════════════════════════════════════════════════════════════════

// ============================================================================
// 1. SUBSTITUIR A FUNÇÃO searchIngredients (linha ~397-413)
// ============================================================================

async function searchIngredients(query) {
    try {
        // USAR API DE PRICING (inclui preço e confiança)
        const results = await FormulaClearPricing.searchIngredients(query, { limit: 10 });
        autocompleteLoading.style.display = 'none';
        
        if (results && results.length > 0) {
            showAutocompleteResults(results);
        } else {
            // Fallback para API antiga se pricing não retornar
            const response = await fetch(`/api/ingredients/search?q=${encodeURIComponent(query)}&limit=10`);
            const data = await response.json();
            
            if (data.success && data.data.length > 0) {
                showAutocompleteResults(data.data);
            } else {
                showNoResults(query);
            }
        }
    } catch (error) {
        console.error('Erro na busca:', error);
        autocompleteLoading.style.display = 'none';
        hideAutocomplete();
    }
}

// ============================================================================
// 2. SUBSTITUIR A FUNÇÃO showAutocompleteResults (linha ~415-437)
// ============================================================================

function showAutocompleteResults(results) {
    autocompleteDropdown.innerHTML = results.map(item => {
        // Determinar indicador de preço
        const source = item.priceSource || item.source;
        const confidence = item.confidence || 0;
        const price = item.price;
        
        let priceIndicator = '';
        let priceDisplay = '';
        
        if (source) {
            const sourceInfo = FormulaClearPricing.getPriceSourceInfo(source);
            priceIndicator = `<span class="price-dot ${sourceInfo.class}" 
                                    title="${sourceInfo.text} - ${confidence}%"></span>`;
            priceDisplay = price ? `<small class="text-success">${FormulaClearPricing.formatPrice(price)}/${item.unit || 'un'}</small>` : '';
        }
        
        return `
            <div class="autocomplete-item" 
                 data-id="${item.id}" 
                 data-name="${escapeHtml(item.name)}"
                 data-category="${escapeHtml(item.category || '')}"
                 data-unit="${item.unit || item.defaultUnit || 'mg'}"
                 data-min="${item.minDosage || ''}"
                 data-max="${item.maxDosage || ''}"
                 data-indications="${escapeHtml(item.indications || '')}"
                 data-price="${price || ''}"
                 data-source="${source || ''}"
                 data-confidence="${confidence}"
                 onclick="selectIngredient(this)">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        ${priceIndicator}
                        <strong>${highlightMatch(item.name, ingredientInput.value)}</strong>
                    </div>
                    <div class="text-end">
                        <small class="text-muted">${item.unit || item.defaultUnit || 'mg'}</small>
                        ${priceDisplay}
                    </div>
                </div>
                <div class="d-flex justify-content-between align-items-center">
                    <small class="text-muted">
                        ${item.category ? `<span class="badge bg-light text-dark me-1">${item.category}</span>` : ''}
                        ${item.indications ? item.indications.substring(0, 35) + '...' : ''}
                    </small>
                    ${confidence > 0 ? `
                        <small class="text-muted">
                            <span class="confidence-bar-mini" style="width: 30px;">
                                <span class="confidence-bar-mini-fill ${FormulaClearPricing.getConfidenceLevel(confidence)}" 
                                      style="width: ${confidence}%"></span>
                            </span>
                        </small>
                    ` : ''}
                </div>
            </div>
        `;
    }).join('');
    autocompleteDropdown.style.display = 'block';
}

// ============================================================================
// 3. SUBSTITUIR A FUNÇÃO selectIngredient (linha ~456-489)
// ============================================================================

function selectIngredient(element) {
    const data = {
        id: element.dataset.id,
        name: element.dataset.name,
        category: element.dataset.category,
        defaultUnit: element.dataset.unit,
        minDosage: element.dataset.min ? parseFloat(element.dataset.min) : null,
        maxDosage: element.dataset.max ? parseFloat(element.dataset.max) : null,
        indications: element.dataset.indications,
        // NOVOS CAMPOS DE PRICING
        price: element.dataset.price ? parseFloat(element.dataset.price) : null,
        priceSource: element.dataset.source,
        confidence: element.dataset.confidence ? parseInt(element.dataset.confidence) : 0
    };
    
    selectedIngredientData = data;
    ingredientInput.value = data.name;
    document.getElementById('ingredientId').value = data.id;
    
    const unitSelect = document.getElementById('ingredientUnit');
    const unitOption = Array.from(unitSelect.options).find(opt => 
        opt.value.toLowerCase() === data.defaultUnit.toLowerCase()
    );
    if (unitOption) unitSelect.value = unitOption.value;
    
    // Info com indicador de preço
    let categoryInfo = data.category ? `📁 ${data.category}` : '';
    let priceInfo = '';
    
    if (data.price && data.priceSource) {
        const sourceInfo = FormulaClearPricing.getPriceSourceInfo(data.priceSource);
        priceInfo = ` • ${sourceInfo.icon} ${FormulaClearPricing.formatPrice(data.price)}/${data.defaultUnit}`;
    }
    
    document.getElementById('ingredientCategory').textContent = categoryInfo + priceInfo;
    document.getElementById('ingredientIndications').textContent = data.indications ? `• ${data.indications}` : '';
    document.getElementById('ingredientInfo').style.display = 'block';
    
    if (data.minDosage || data.maxDosage) {
        document.getElementById('dosageRange').textContent = 
            `Dosagem típica: ${data.minDosage || '?'} - ${data.maxDosage || '?'} ${data.defaultUnit}`;
        document.getElementById('dosageRange').style.display = 'block';
    }
    
    hideAutocomplete();
    document.getElementById('ingredientQuantity').focus();
}

// ============================================================================
// 4. SUBSTITUIR A FUNÇÃO updateCurrentFormulaPrice (linha ~802-844)
// ============================================================================

async function updateCurrentFormulaPrice() {
    const priceEl = document.getElementById('currentFormulaPrice');
    const confidenceEl = document.getElementById('priceConfidenceIndicator');
    
    if (currentIngredients.length === 0) {
        priceEl.textContent = 'R$ 0,00';
        confidenceEl.innerHTML = '';
        return;
    }
    
    // Primeiro: cálculo local rápido
    const localPrice = calculateFormulaPrice(currentIngredients, selectedProductQuantity || 1);
    priceEl.textContent = formatCurrency(localPrice);
    
    // Depois: API de pricing (mais preciso)
    if (typeof FormulaClearPricing !== 'undefined' && selectedProductType) {
        try {
            priceEl.innerHTML = `${formatCurrency(localPrice)} <small class="text-muted"><i class="bi bi-hourglass-split"></i></small>`;
            
            const request = {
                productType: selectedProductType.name || 'Cápsula',
                productQuantity: selectedProductQuantity || 60,
                ingredients: currentIngredients.map(ing => ({
                    rawMaterialId: ing.id || null,
                    name: ing.name,
                    quantity: ing.quantity,
                    unit: ing.unit
                }))
            };
            
            const result = await FormulaClearPricing.calculateFormulaPrice(request);
            
            if (result) {
                lastPricingResult = result;
                priceEl.textContent = formatCurrency(result.suggestedPrice);
                
                // Indicador de confiança
                const confidence = result.averageConfidence || 0;
                const sourceInfo = FormulaClearPricing.getPriceSourceInfo(
                    confidence >= 80 ? 1 : confidence >= 50 ? 2 : 3
                );
                
                confidenceEl.innerHTML = `
                    <span title="Confiança: ${confidence}%">${sourceInfo.icon} ${confidence}%</span>
                    <span class="confidence-bar-mini">
                        <span class="confidence-bar-mini-fill ${FormulaClearPricing.getConfidenceLevel(confidence)}" 
                              style="width: ${confidence}%"></span>
                    </span>
                    ${result.inStockCount > 0 ? `<small class="text-success ms-2">${result.inStockCount} em estoque</small>` : ''}
                `;
                
                // Mostrar warnings se houver
                if (result.warnings && result.warnings.length > 0) {
                    confidenceEl.innerHTML += `
                        <small class="text-warning d-block mt-1">
                            <i class="bi bi-exclamation-triangle me-1"></i>${result.warnings[0]}
                        </small>
                    `;
                }
            }
        } catch (error) {
            console.error('Erro ao calcular preço via API:', error);
            // Mantém preço local
        }
    }
}

// ============================================================================
// 5. ADICIONAR ESTILOS CSS (adicionar na seção <style>)
// ============================================================================

/*
.autocomplete-item .price-dot {
    display: inline-block;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    margin-right: 6px;
    vertical-align: middle;
}

.autocomplete-item .price-dot.stock { background-color: #28a745; }
.autocomplete-item .price-dot.historical { background-color: #ffc107; }
.autocomplete-item .price-dot.base { background-color: #dc3545; }

.autocomplete-item .confidence-bar-mini {
    display: inline-block;
    width: 30px;
    height: 3px;
    background: #e9ecef;
    border-radius: 2px;
    overflow: hidden;
    vertical-align: middle;
}

.autocomplete-item .confidence-bar-mini-fill {
    display: block;
    height: 100%;
    border-radius: 2px;
}

.autocomplete-item .confidence-bar-mini-fill.high { background: #28a745; }
.autocomplete-item .confidence-bar-mini-fill.medium { background: #ffc107; }
.autocomplete-item .confidence-bar-mini-fill.low { background: #dc3545; }
*/
