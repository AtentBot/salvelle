/**
 * Salvelle Pricing Client v2.0
 * Integração frontend com PricingController API
 * Hierarquia: ESTOQUE (🟢 100%) > HISTÓRICO (🟡 50-95%) > BASE (🔴 30%)
 * 
 * Endpoints:
 * - GET  /api/pricing/ingredient/{id}           - Preço por ID
 * - GET  /api/pricing/ingredient/search?name=   - Preço por nome
 * - POST /api/pricing/ingredients/batch         - Preços em lote
 * - GET  /api/pricing/ingredients/search?q=     - Autocomplete
 * - POST /api/pricing/formula/calculate         - Calcular fórmula
 * - GET  /api/pricing/categories                - Listar categorias
 * - GET  /api/pricing/settings                  - Configurações
 * - GET  /api/pricing/statistics                - Estatísticas
 */

const FormulaClearPricing = (function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO
    // ═══════════════════════════════════════════════════════════════
    
    const API_BASE = '/api/pricing';
    const CACHE_TTL = 5 * 60 * 1000; // 5 minutos
    const priceCache = new Map();
    
    // Source enum matching backend
    const PriceSource = {
        ESTOQUE: 1,
        HISTORICO: 2,
        BASE: 3
    };

    // ═══════════════════════════════════════════════════════════════
    // CACHE HELPERS
    // ═══════════════════════════════════════════════════════════════
    
    function getCacheKey(type, id) {
        return `${type}:${id}`;
    }
    
    function setCache(key, data) {
        priceCache.set(key, {
            data,
            timestamp: Date.now()
        });
    }
    
    function getCache(key) {
        const cached = priceCache.get(key);
        if (!cached) return null;
        if (Date.now() - cached.timestamp > CACHE_TTL) {
            priceCache.delete(key);
            return null;
        }
        return cached.data;
    }
    
    function clearCache() {
        priceCache.clear();
    }

    // ═══════════════════════════════════════════════════════════════
    // API REQUEST HELPER
    // ═══════════════════════════════════════════════════════════════
    
    async function apiRequest(endpoint, options = {}) {
        try {
            const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`;
            
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });
            
            if (!response.ok) {
                const error = await response.json().catch(() => ({}));
                throw new Error(error.error || `HTTP ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error(`[FormulaClearPricing] API Error:`, error);
            throw error;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // API PÚBLICA - PREÇOS INDIVIDUAIS
    // ═══════════════════════════════════════════════════════════════
    
    /**
     * Busca preço de um ingrediente por ID
     * @param {string} rawMaterialId - UUID do ingrediente
     * @returns {Promise<IngredientPriceResult|null>}
     */
    async function getIngredientPrice(rawMaterialId) {
        if (!rawMaterialId) return null;
        
        const cacheKey = getCacheKey('price', rawMaterialId);
        const cached = getCache(cacheKey);
        if (cached) return cached;
        
        try {
            const result = await apiRequest(`/ingredient/${rawMaterialId}`);
            setCache(cacheKey, result);
            return result;
        } catch (error) {
            return null;
        }
    }
    
    /**
     * Busca preço de ingrediente por nome
     * @param {string} name - Nome do ingrediente
     * @returns {Promise<IngredientPriceResult|null>}
     */
    async function searchIngredientPrice(name) {
        if (!name || name.length < 2) return null;
        
        const cacheKey = getCacheKey('search', name.toLowerCase());
        const cached = getCache(cacheKey);
        if (cached) return cached;
        
        try {
            const result = await apiRequest(`/ingredient/search?name=${encodeURIComponent(name)}`);
            setCache(cacheKey, result);
            return result;
        } catch (error) {
            return null;
        }
    }
    
    /**
     * Busca preços de múltiplos ingredientes em batch
     * @param {string[]} rawMaterialIds - Array de UUIDs
     * @returns {Promise<IngredientPriceResult[]>}
     */
    async function getIngredientPricesBatch(rawMaterialIds) {
        if (!rawMaterialIds || !rawMaterialIds.length) return [];
        
        try {
            const response = await apiRequest('/ingredients/batch', {
                method: 'POST',
                body: JSON.stringify(rawMaterialIds)
            });
            
            // Cache individual results
            if (Array.isArray(response)) {
                response.forEach(item => {
                    if (item.rawMaterialId) {
                        setCache(getCacheKey('price', item.rawMaterialId), item);
                    }
                });
            }
            
            return response || [];
        } catch (error) {
            return [];
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // API PÚBLICA - AUTOCOMPLETE
    // ═══════════════════════════════════════════════════════════════
    
    /**
     * Busca ingredientes com autocomplete (inclui preço)
     * @param {string} query - Termo de busca
     * @param {object} options - {category, limit}
     * @returns {Promise<SearchResult[]>}
     */
    async function searchIngredients(query, options = {}) {
        if (!query || query.length < 2) return [];
        
        const { category = '', limit = 20 } = options;
        
        let url = `/ingredients/search?q=${encodeURIComponent(query)}&limit=${limit}`;
        if (category) url += `&category=${encodeURIComponent(category)}`;
        
        try {
            const response = await apiRequest(url);
            return response.data || [];
        } catch (error) {
            return [];
        }
    }
    
    /**
     * Obtém lista de categorias disponíveis
     * @returns {Promise<Category[]>}
     */
    async function getCategories() {
        try {
            const response = await apiRequest('/categories');
            return response.data || [];
        } catch (error) {
            return [];
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // API PÚBLICA - CÁLCULO DE FÓRMULA
    // ═══════════════════════════════════════════════════════════════
    
    /**
     * Calcula preço completo de uma fórmula
     * @param {FormulaRequest} request - Dados da fórmula
     * @returns {Promise<FormulaCalculationResult|null>}
     * 
     * @example
     * const result = await FormulaClearPricing.calculateFormulaPrice({
     *     productType: 'Cápsula',
     *     productQuantity: 60,
     *     ingredients: [
     *         { rawMaterialId: 'guid', name: 'Vitamina D3', quantity: 1000, unit: 'UI' },
     *         { name: 'Vitamina K2', quantity: 100, unit: 'mcg' }
     *     ]
     * });
     */
    async function calculateFormulaPrice(request) {
        if (!request || !request.ingredients || !request.ingredients.length) {
            return null;
        }
        
        try {
            const response = await apiRequest('/formula/calculate', {
                method: 'POST',
                body: JSON.stringify({
                    productType: request.productType || 'Cápsula',
                    productQuantity: request.productQuantity || 60,
                    ingredients: request.ingredients.map(ing => ({
                        rawMaterialId: ing.rawMaterialId || ing.id || null,
                        name: ing.name,
                        quantity: parseFloat(ing.quantity) || 0,
                        unit: ing.unit || 'mg'
                    }))
                })
            });
            
            return response;
        } catch (error) {
            console.error('Erro ao calcular fórmula:', error);
            return null;
        }
    }
    
    /**
     * Recalcula preço de uma CustomerFormula existente
     * @param {string} formulaId - UUID da fórmula
     * @returns {Promise<boolean>}
     */
    async function recalculateFormulaPrice(formulaId) {
        if (!formulaId) return false;
        
        try {
            await apiRequest(`/formula/${formulaId}/recalculate`, {
                method: 'POST'
            });
            return true;
        } catch (error) {
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // API PÚBLICA - CONFIGURAÇÕES E ESTATÍSTICAS
    // ═══════════════════════════════════════════════════════════════
    
    /**
     * Obtém configurações de precificação do estabelecimento
     * @returns {Promise<EstablishmentPricingSettings>}
     */
    async function getSettings() {
        try {
            return await apiRequest('/settings');
        } catch (error) {
            // Retornar defaults
            return {
                inflationRateMonthly: 0.50,
                safetyMarginPercent: 10.00,
                defaultProfitMargin: 100.00,
                manipulationFee: 25.00,
                defaultPackagingCost: 5.00,
                alertOnEstimated: true,
                blockWithoutStock: false,
                priceValidityDays: 180
            };
        }
    }
    
    /**
     * Obtém estatísticas de precificação
     * @returns {Promise<PricingStatistics>}
     */
    async function getStatistics() {
        try {
            return await apiRequest('/statistics');
        } catch (error) {
            return null;
        }
    }
    
    /**
     * Aplica desconto a um preço
     * @param {number} price - Preço original
     * @param {number} discountPercentage - Desconto (0 a 1)
     * @returns {Promise<DiscountResult>}
     */
    async function applyDiscount(price, discountPercentage) {
        try {
            return await apiRequest('/apply-discount', {
                method: 'POST',
                body: JSON.stringify({ price, discountPercentage })
            });
        } catch (error) {
            // Cálculo local fallback
            const finalPrice = price * (1 - discountPercentage);
            return {
                originalPrice: price,
                discountPercentage,
                discountAmount: price - finalPrice,
                finalPrice: Math.round(finalPrice * 100) / 100
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS UI
    // ═══════════════════════════════════════════════════════════════
    
    /**
     * Retorna ícone/cor baseado na fonte do preço
     * @param {number|string} source - Fonte do preço
     */
    function getPriceSourceInfo(source) {
        // Normalizar fonte
        const normalizedSource = typeof source === 'string' ? source.toUpperCase() : source;
        
        switch (normalizedSource) {
            case PriceSource.ESTOQUE:
            case 'ESTOQUE':
            case 1:
                return { icon: '🟢', class: 'stock', text: 'Em Estoque', color: '#28a745' };
            case PriceSource.HISTORICO:
            case 'HISTORICO':
            case 2:
                return { icon: '🟡', class: 'historical', text: 'Preço Estimado', color: '#ffc107' };
            case PriceSource.BASE:
            case 'BASE':
            case 3:
            default:
                return { icon: '🔴', class: 'base', text: 'Tabela Base', color: '#dc3545' };
        }
    }
    
    /**
     * Retorna classe CSS baseada no nível de confiança
     * @param {number} confidence - Confiança (0-100)
     */
    function getConfidenceLevel(confidence) {
        if (confidence >= 80) return 'high';
        if (confidence >= 50) return 'medium';
        return 'low';
    }
    
    /**
     * Mapeia fonte do preço para código numérico
     * @param {string} source - Fonte textual
     * @returns {number}
     */
    function mapPriceSource(source) {
        const sourceMap = {
            'ESTOQUE': 1,
            'HISTORICO': 2,
            'BASE': 3,
            'PADRAO': 3
        };
        return sourceMap[source?.toUpperCase()] || 3;
    }
    
    /**
     * Formata preço em Real brasileiro
     * @param {number} value - Valor
     */
    function formatPrice(value) {
        if (value == null || isNaN(value)) return 'R$ --';
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL'
        }).format(value);
    }
    
    /**
     * Gera HTML do indicador de preço (dot)
     * @param {string|number} source - Fonte do preço
     * @param {number} confidence - Confiança (0-100)
     */
    function renderPriceDot(source, confidence) {
        const info = getPriceSourceInfo(source);
        return `<span class="price-dot ${info.class}" 
                      title="${info.text} - ${confidence || 0}% confiança"
                      style="background-color: ${info.color}"></span>`;
    }
    
    /**
     * Gera HTML do indicador de confiança
     * @param {number} confidence - Confiança (0-100)
     */
    function renderConfidenceBar(confidence) {
        const level = getConfidenceLevel(confidence);
        const info = level === 'high' 
            ? { icon: '🟢', color: '#28a745' }
            : level === 'medium' 
                ? { icon: '🟡', color: '#ffc107' }
                : { icon: '🔴', color: '#dc3545' };
        
        return `
            <div class="confidence-indicator" title="${confidence}% confiança">
                <span class="confidence-icon">${info.icon}</span>
                <span class="confidence-text">${confidence}%</span>
                <span class="confidence-bar-mini">
                    <span class="confidence-bar-mini-fill ${level}" 
                          style="width: ${confidence}%; background-color: ${info.color}"></span>
                </span>
            </div>
        `;
    }
    
    /**
     * Gera HTML do resumo de confiança (para formulário)
     * @param {object} result - Resultado do cálculo de fórmula
     */
    function renderConfidenceSummary(result) {
        if (!result) return '';
        
        const level = getConfidenceLevel(result.averageConfidence);
        const info = getPriceSourceInfo(level === 'high' ? 1 : level === 'medium' ? 2 : 3);
        
        let html = `
            <div class="pricing-summary">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <span>Confiança do orçamento</span>
                    <span class="badge bg-${level === 'high' ? 'success' : level === 'medium' ? 'warning' : 'danger'}">
                        ${info.icon} ${result.averageConfidence}%
                    </span>
                </div>
                <div class="confidence-bar">
                    <div class="confidence-bar-fill ${level}" style="width: ${result.averageConfidence}%"></div>
                </div>
                <small class="text-muted d-block mt-1">${result.confidenceMessage}</small>
        `;
        
        // Adicionar contadores
        if (result.inStockCount > 0 || result.estimatedCount > 0 || result.baseCount > 0) {
            html += `
                <div class="mt-2 small">
                    ${result.inStockCount > 0 ? `<span class="me-2">🟢 ${result.inStockCount} em estoque</span>` : ''}
                    ${result.estimatedCount > 0 ? `<span class="me-2">🟡 ${result.estimatedCount} estimados</span>` : ''}
                    ${result.baseCount > 0 ? `<span>🔴 ${result.baseCount} base</span>` : ''}
                </div>
            `;
        }
        
        // Adicionar avisos
        if (result.warnings && result.warnings.length > 0) {
            html += `
                <div class="pricing-warnings mt-2 p-2 bg-warning bg-opacity-10 rounded">
                    ${result.warnings.map(w => `<small class="d-block">${w}</small>`).join('')}
                </div>
            `;
        }
        
        html += '</div>';
        return html;
    }

    // ═══════════════════════════════════════════════════════════════
    // INJETAR ESTILOS
    // ═══════════════════════════════════════════════════════════════
    
    function injectStyles() {
        if (document.getElementById('salvelle-pricing-styles')) return;
        
        const styles = document.createElement('style');
        styles.id = 'salvelle-pricing-styles';
        styles.textContent = `
            /* Indicador de preço (dot) */
            .price-dot {
                display: inline-block;
                width: 10px;
                height: 10px;
                border-radius: 50%;
                margin-right: 6px;
                vertical-align: middle;
            }
            .price-dot.stock { background-color: #28a745; }
            .price-dot.historical { background-color: #ffc107; }
            .price-dot.base { background-color: #dc3545; }
            
            /* Barra de confiança */
            .confidence-bar {
                height: 6px;
                background-color: #e9ecef;
                border-radius: 3px;
                overflow: hidden;
            }
            .confidence-bar-fill {
                height: 100%;
                border-radius: 3px;
                transition: width 0.3s ease;
            }
            .confidence-bar-fill.high { background-color: #28a745; }
            .confidence-bar-fill.medium { background-color: #ffc107; }
            .confidence-bar-fill.low { background-color: #dc3545; }
            
            /* Barra de confiança mini */
            .confidence-bar-mini {
                display: inline-block;
                width: 50px;
                height: 4px;
                background-color: #e9ecef;
                border-radius: 2px;
                overflow: hidden;
                vertical-align: middle;
                margin-left: 4px;
            }
            .confidence-bar-mini-fill {
                height: 100%;
                border-radius: 2px;
            }
            
            /* Indicador de confiança inline */
            .confidence-indicator {
                display: inline-flex;
                align-items: center;
                gap: 4px;
                font-size: 0.85rem;
            }
            
            /* Avisos de pricing */
            .pricing-warnings {
                border-left: 3px solid #ffc107;
            }
            
            /* Resumo de pricing */
            .pricing-summary {
                padding: 12px;
                background-color: #f8f9fa;
                border-radius: 8px;
            }
        `;
        document.head.appendChild(styles);
    }
    
    // Injetar estilos ao carregar
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectStyles);
    } else {
        injectStyles();
    }

    // ═══════════════════════════════════════════════════════════════
    // EXPORT
    // ═══════════════════════════════════════════════════════════════
    
    return {
        // API - Preços individuais
        getIngredientPrice,
        searchIngredientPrice,
        getIngredientPricesBatch,
        
        // API - Autocomplete
        searchIngredients,
        getCategories,
        
        // API - Cálculo de fórmula
        calculateFormulaPrice,
        recalculateFormulaPrice,
        
        // API - Configurações
        getSettings,
        getStatistics,
        applyDiscount,
        
        // Helpers UI
        getPriceSourceInfo,
        getConfidenceLevel,
        mapPriceSource,
        formatPrice,
        renderPriceDot,
        renderConfidenceBar,
        renderConfidenceSummary,
        clearCache,
        
        // Constants
        PriceSource
    };
})();

// Alias para compatibilidade
window.FormulaClearPricing = FormulaClearPricing;
