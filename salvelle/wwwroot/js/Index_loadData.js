    // ═══════════════════════════════════════════════════════════════════════════
    // LISTAGEM - CORRIGIDO PARA FORMATO DA API
    // ═══════════════════════════════════════════════════════════════════════════
    async function loadData() {
        const tbody = document.getElementById('tableBody');
        tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4"><div class="spinner-border spinner-border-sm"></div></td></tr>';

        const params = new URLSearchParams({
            page: currentPage,
            pageSize: pageSize
        });

        const search = document.getElementById('searchInput').value;
        const control = document.getElementById('filterControl').value;
        const usage = document.getElementById('filterUsage').value;
        const stock = document.getElementById('filterStock').value;
        const active = document.getElementById('filterActive').value;

        if (search) params.append('search', search);
        if (control) params.append('controlType', control);
        if (usage) params.append('allowedUsage', usage);
        if (stock) params.append('stockStatus', stock);
        if (active) params.append('isActive', active);
        if (currentCategory) params.append('category', currentCategory);

        try {
            const response = await fetch(`/api/RawMaterials?${params}`);
            const result = await response.json();

            // ✅ CORREÇÃO: A API retorna { success, data, pagination }
            if (result.success) {
                const items = result.data || [];
                const total = result.pagination?.totalRecords || items.length;
                const totalPages = result.pagination?.totalPages || 1;

                renderTable(items);
                renderPagination(total, totalPages);
                
                // Atualizar contador
                document.getElementById('countAll').textContent = total || '-';
            } else {
                throw new Error(result.error || 'Erro desconhecido');
            }
        } catch (e) {
            console.error('Erro ao carregar:', e);
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-4">Erro ao carregar dados</td></tr>';
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ESTATÍSTICAS - CORRIGIDO PARA FORMATO DA API
    // ═══════════════════════════════════════════════════════════════════════════
    async function loadStats() {
        try {
            const response = await fetch('/api/RawMaterials/statistics');
            const result = await response.json();

            // ✅ CORREÇÃO: A API retorna { success, data: { ... } }
            if (result.success && result.data) {
                const stats = result.data;
                document.getElementById('statTotal').textContent = stats.totalMaterials || '-';
                document.getElementById('statAtivos').textContent = stats.totalMaterials || '-';
                document.getElementById('statControlados').textContent = stats.controlledSubstances || '-';
                document.getElementById('statBaixo').textContent = stats.lowStock || '-';
                document.getElementById('statSemPreco').textContent = stats.withoutPrice || '-';
                document.getElementById('statVirtuais').textContent = stats.virtualIngredients || '-';
            }
        } catch (e) {
            console.error('Erro ao carregar estatísticas:', e);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CATEGORIAS - CORRIGIDO PARA FORMATO DA API
    // ═══════════════════════════════════════════════════════════════════════════
    async function loadCategories() {
        try {
            const response = await fetch('/api/RawMaterials/categories');
            const result = await response.json();

            // ✅ CORREÇÃO: A API retorna { success, data: [...] }
            if (result.success && result.data) {
                const container = document.getElementById('categoryTabs');
                
                // Manter a tab "Todas"
                let html = `<span class="category-tab active" data-category="" onclick="filterByCategory(this)">
                    Todas <span class="count" id="countAll">-</span>
                </span>`;

                result.data.forEach(cat => {
                    html += `<span class="category-tab" data-category="${cat.category}" onclick="filterByCategory(this)">
                        ${cat.category} <span class="count">${cat.count}</span>
                    </span>`;
                });

                container.innerHTML = html;
            }
        } catch (e) {
            console.error('Erro ao carregar categorias:', e);
        }
    }
