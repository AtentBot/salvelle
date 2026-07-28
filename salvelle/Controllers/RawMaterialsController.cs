using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using DTOs;

namespace Controllers.Api;

/// <summary>
/// Controller API para gestão de Matérias-Primas
/// Autenticação via EmployeeAuthMiddleware (HttpContext.Items["Employee"])
/// Rotas protegidas - requer X-Session-Token
/// 
/// DTOs estão em: RawMaterialsDTOs.cs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RawMaterialsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<RawMaterialsController> _logger;

    public RawMaterialsController(AppDbContext db, ILogger<RawMaterialsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // LISTAGEM E BUSCA
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista todas as matérias-primas do estabelecimento com informações de preço
    /// GET /api/RawMaterials
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? controlType,
        [FromQuery] string? allowedUsage,
        [FromQuery] string? priceSource,
        [FromQuery] bool? isVirtual,
        [FromQuery] bool? lowStock,
        [FromQuery] bool? outdatedPrice,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });

        try
        {
            var query = _db.RawMaterials
                .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive);

            // Filtro de busca
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(rm =>
                    rm.Name.ToLower().Contains(searchLower) ||
                    (rm.DcbCode != null && rm.DcbCode.ToLower().Contains(searchLower)) ||
                    (rm.DciCode != null && rm.DciCode.ToLower().Contains(searchLower)) ||
                    rm.CasNumber.ToLower().Contains(searchLower) ||
                    (rm.Synonyms != null && rm.Synonyms.ToLower().Contains(searchLower)));
            }

            // Filtro por tipo de controle
            if (!string.IsNullOrEmpty(controlType))
                query = query.Where(rm => rm.ControlType == controlType);

            // Filtro por uso permitido (ORAL, TOPICAL, BOTH)
            if (!string.IsNullOrEmpty(allowedUsage))
                query = query.Where(rm => rm.AllowedUsage == "BOTH" || rm.AllowedUsage == allowedUsage);

            // Filtro por origem do preço
            if (!string.IsNullOrEmpty(priceSource))
                query = query.Where(rm => rm.PriceSource == priceSource);

            // Filtro por virtual (sem estoque físico)
            if (isVirtual.HasValue)
                query = query.Where(rm => rm.IsVirtual == isVirtual.Value);

            // Filtro por categoria
            if (!string.IsNullOrEmpty(category))
                query = query.Where(rm => rm.Category == category);

            // Filtro de estoque baixo
            if (lowStock.HasValue && lowStock.Value)
                query = query.Where(rm => rm.CurrentStock <= rm.MinimumStock);

            // Filtro por preço desatualizado (mais de 6 meses)
            if (outdatedPrice.HasValue && outdatedPrice.Value)
            {
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                query = query.Where(rm =>
                    rm.LastPriceDate == null ||
                    rm.LastPriceDate < sixMonthsAgo);
            }

            var totalRecords = await query.CountAsync();

            var materials = await query
                .OrderBy(rm => rm.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rm => new RawMaterialListDto
                {
                    Id = rm.Id,
                    Name = rm.Name,
                    DcbCode = rm.DcbCode,
                    DciCode = rm.DciCode,
                    CasNumber = rm.CasNumber,
                    ControlType = rm.ControlType,
                    Category = rm.Category,
                    AllowedUsage = rm.AllowedUsage,
                    CurrentStock = rm.CurrentStock,
                    MinimumStock = rm.MinimumStock,
                    MaximumStock = rm.MaximumStock,
                    Unit = rm.Unit,

                    // Informações de preço
                    BasePrice = rm.BasePrice,
                    LastKnownPrice = rm.LastKnownPrice,
                    LastPriceDate = rm.LastPriceDate,
                    IsVirtual = rm.IsVirtual,
                    PriceSource = rm.PriceSource,

                    // Preço atual calculado
                    CurrentPrice = rm.CurrentStock > 0
                        ? rm.LastKnownPrice ?? rm.BasePrice ?? 0
                        : rm.LastKnownPrice ?? rm.BasePrice ?? 0,

                    // Propriedades físicas
                    BulkDensity = rm.BulkDensity,
                    TappedDensity = rm.TappedDensity,
                    CorrectionFactor = rm.CorrectionFactor,
                    PurityFactor = rm.PurityFactor,
                    DilutionFactor = rm.DilutionFactor,

                    // Indicadores
                    StockStatus = rm.CurrentStock <= 0 ? "SEM_ESTOQUE" :
                                  rm.CurrentStock <= rm.MinimumStock ? "BAIXO" :
                                  rm.CurrentStock >= rm.MaximumStock ? "EXCESSO" : "NORMAL",

                    PriceStatus = rm.CurrentStock > 0 ? "ESTOQUE" :
                                  rm.LastKnownPrice.HasValue ? "HISTORICO" : "BASE",

                    IsPriceOutdated = rm.LastPriceDate == null ||
                                      rm.LastPriceDate < DateTime.UtcNow.AddMonths(-6),

                    DaysSinceLastPrice = rm.LastPriceDate.HasValue
                        ? (int)(DateTime.UtcNow - rm.LastPriceDate.Value).TotalDays
                        : (int?)null,

                    RequiresSpecialAuthorization = rm.RequiresSpecialAuthorization,
                    RequiresRefrigeration = rm.RequiresRefrigeration,
                    Popularity = rm.Popularity,
                    CreatedAt = rm.CreatedAt,
                    UpdatedAt = rm.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = materials,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                },
                filters = new { search, controlType, allowedUsage, priceSource, isVirtual, lowStock, outdatedPrice, category }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar matérias-primas");
            return StatusCode(500, new { success = false, error = "Erro ao buscar matérias-primas" });
        }
    }

    /// <summary>
    /// Busca matérias-primas por nome/código (autocomplete)
    /// GET /api/RawMaterials/search?q={query}&usage={ORAL|TOPICAL}&limit={20}
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? usage = null,
        [FromQuery] int limit = 20)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { success = true, data = new List<object>() });

        var query = _db.RawMaterials
            .Where(r => r.EstablishmentId == employee.EstablishmentId && r.IsActive)
            .Where(r => r.Name.ToLower().Contains(q.ToLower())
                     || (r.DcbCode != null && r.DcbCode.ToLower().Contains(q.ToLower()))
                     || (r.Synonyms != null && r.Synonyms.ToLower().Contains(q.ToLower())));

        // Filtro por uso (ORAL, TOPICAL, BOTH)
        if (!string.IsNullOrEmpty(usage))
        {
            query = query.Where(r => r.AllowedUsage == "BOTH" || r.AllowedUsage == usage);
        }

        var results = await query
            .OrderByDescending(r => r.Popularity)
            .ThenBy(r => r.Name)
            .Take(limit)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.DcbCode,
                r.Category,
                r.Unit,
                r.AllowedUsage,
                r.BasePrice,
                r.BulkDensity,
                r.TappedDensity,
                r.CorrectionFactor,
                r.PurityFactor,
                r.DilutionFactor,
                r.CurrentStock,
                isControlled = r.ControlType != null && r.ControlType != "COMUM" && r.ControlType != "",
                r.ControlType
            })
            .ToListAsync();

        return Ok(new { success = true, data = results });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // DETALHES
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retorna detalhes de uma matéria-prima incluindo informações de preço
    /// GET /api/RawMaterials/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var material = await _db.RawMaterials
            .Include(rm => rm.Batches!.Where(b => b.CurrentQuantity > 0))
            .FirstOrDefaultAsync(rm =>
                rm.Id == id &&
                rm.EstablishmentId == employee.EstablishmentId &&
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        // Buscar último lote com preço (lotes com quantidade > 0)
        var lastBatchWithPrice = await _db.Batches
            .Where(b => b.RawMaterialId == id && b.CurrentQuantity > 0)
            .OrderByDescending(b => b.ReceivedDate)
            .Select(b => new { b.UnitCost, b.ReceivedDate })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                material.Id,
                material.Name,
                material.DcbCode,
                material.DciCode,
                material.CasNumber,
                material.Description,
                material.ControlType,
                material.Category,
                material.Synonyms,
                material.Indications,
                material.Unit,

                // Propriedades físicas
                material.AllowedUsage,
                material.PhysicalState,
                material.BulkDensity,
                material.TappedDensity,
                material.CorrectionFactor,
                material.PurityFactor,
                material.DilutionFactor,
                material.LossFactor,
                material.EquivalenceFactor,

                // Estoque
                material.CurrentStock,
                material.MinimumStock,
                material.MaximumStock,
                material.IsVirtual,

                // Preços
                material.BasePrice,
                material.LastKnownPrice,
                material.LastPriceDate,
                material.PriceSource,

                // Preço atual calculado
                CurrentPrice = material.CurrentStock > 0
                    ? lastBatchWithPrice?.UnitCost ?? material.LastKnownPrice ?? material.BasePrice ?? 0
                    : material.LastKnownPrice ?? material.BasePrice ?? 0,

                PriceStatus = material.CurrentStock > 0 ? "ESTOQUE" :
                              material.LastKnownPrice.HasValue ? "HISTORICO" : "BASE",

                // Info do último lote
                LastBatchPrice = lastBatchWithPrice?.UnitCost,
                LastBatchDate = lastBatchWithPrice?.ReceivedDate,

                // Armazenamento
                material.StorageConditions,
                material.RequiresRefrigeration,
                material.LightSensitive,
                material.HumiditySensitive,
                material.RequiresSpecialAuthorization,
                material.Popularity,

                // Lotes ativos
                ActiveBatches = material.Batches?.Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.CurrentQuantity,
                    b.UnitCost,
                    b.ReceivedDate,
                    b.ExpiryDate
                }).OrderBy(b => b.ExpiryDate).ToList(),

                material.CreatedAt,
                material.UpdatedAt
            }
        });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CRIAR E ATUALIZAR
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cria uma nova matéria-prima
    /// POST /api/RawMaterials
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar estoque" });

        if (dto.ControlType != "COMUM" && !await HasControlledSubstancePermission(employee))
            return StatusCode(403, new { error = "Sem permissão para cadastrar substâncias controladas" });

        // Validar duplicidade
        var existing = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Where(rm => rm.Name.ToLower() == dto.Name.ToLower() ||
                        (dto.CasNumber != null && rm.CasNumber == dto.CasNumber) ||
                        (dto.DcbCode != null && rm.DcbCode == dto.DcbCode))
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest(new { error = "Já existe matéria-prima com este nome, CAS ou DCB" });

        var material = new RawMaterial
        {
            Id = Guid.NewGuid(),
            EstablishmentId = employee.EstablishmentId,
            Name = dto.Name.Trim(),
            DcbCode = dto.DcbCode?.Trim(),
            DciCode = dto.DciCode?.Trim(),
            CasNumber = dto.CasNumber?.Trim() ?? "",
            Description = dto.Description?.Trim(),
            ControlType = dto.ControlType?.ToUpper() ?? "COMUM",
            Category = dto.Category?.Trim(),
            Synonyms = dto.Synonyms?.Trim(),
            Indications = dto.Indications?.Trim(),
            Unit = dto.Unit?.ToLower() ?? "g",

            // Propriedades físicas
            AllowedUsage = dto.AllowedUsage ?? "BOTH",
            PhysicalState = dto.PhysicalState ?? "SOLID",
            BulkDensity = dto.BulkDensity,
            TappedDensity = dto.TappedDensity,
            CorrectionFactor = dto.CorrectionFactor ?? 1.0m,
            PurityFactor = dto.PurityFactor ?? 1.0m,
            DilutionFactor = dto.DilutionFactor ?? 1.0m,
            LossFactor = dto.LossFactor ?? 0m,

            // Estoque
            CurrentStock = 0,
            MinimumStock = dto.MinimumStock >= 0 ? dto.MinimumStock : 0,
            MaximumStock = dto.MaximumStock >= 0 ? dto.MaximumStock : 0,

            // Preço
            BasePrice = dto.BasePrice,
            IsVirtual = true, // Novo ingrediente começa como virtual
            PriceSource = dto.BasePrice.HasValue ? "BASE" : "",

            // Armazenamento
            StorageConditions = dto.StorageConditions?.Trim(),
            RequiresRefrigeration = dto.RequiresRefrigeration,
            LightSensitive = dto.LightSensitive,
            HumiditySensitive = dto.HumiditySensitive,
            RequiresSpecialAuthorization = dto.ControlType != "COMUM",

            Popularity = dto.Popularity > 0 ? dto.Popularity : 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employee.Id,
            UpdatedByEmployeeId = employee.Id
        };

        _db.RawMaterials.Add(material);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Matéria-prima {Name} criada por {Employee}", material.Name, employee.FullName);

        return CreatedAtAction(nameof(GetById), new { id = material.Id }, new
        {
            success = true,
            message = "Matéria-prima criada com sucesso",
            data = new { material.Id, material.Name }
        });
    }

    /// <summary>
    /// Atualiza uma matéria-prima
    /// PUT /api/RawMaterials/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm =>
                rm.Id == id &&
                rm.EstablishmentId == employee.EstablishmentId &&
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        // Verificar permissão para controlados
        if (dto.ControlType != null && dto.ControlType != "COMUM" && !await HasControlledSubstancePermission(employee))
            return StatusCode(403, new { error = "Sem permissão para alterar substâncias controladas" });

        // Atualizar campos básicos
        if (!string.IsNullOrEmpty(dto.Name)) material.Name = dto.Name.Trim();
        if (dto.Description != null) material.Description = dto.Description.Trim();
        if (!string.IsNullOrEmpty(dto.ControlType)) material.ControlType = dto.ControlType.ToUpper();
        if (!string.IsNullOrEmpty(dto.Category)) material.Category = dto.Category.Trim();
        if (dto.Synonyms != null) material.Synonyms = dto.Synonyms.Trim();
        if (dto.Indications != null) material.Indications = dto.Indications.Trim();
        if (!string.IsNullOrEmpty(dto.Unit)) material.Unit = dto.Unit.ToLower();

        // Propriedades físicas
        if (!string.IsNullOrEmpty(dto.AllowedUsage)) material.AllowedUsage = dto.AllowedUsage;
        if (!string.IsNullOrEmpty(dto.PhysicalState)) material.PhysicalState = dto.PhysicalState;
        if (dto.BulkDensity.HasValue) material.BulkDensity = dto.BulkDensity.Value;
        if (dto.TappedDensity.HasValue) material.TappedDensity = dto.TappedDensity.Value;
        if (dto.CorrectionFactor.HasValue) material.CorrectionFactor = dto.CorrectionFactor.Value;
        if (dto.PurityFactor.HasValue) material.PurityFactor = dto.PurityFactor.Value;
        if (dto.DilutionFactor.HasValue) material.DilutionFactor = dto.DilutionFactor.Value;
        if (dto.LossFactor.HasValue) material.LossFactor = dto.LossFactor.Value;

        // Estoque
        if (dto.MinimumStock.HasValue) material.MinimumStock = dto.MinimumStock.Value;
        if (dto.MaximumStock.HasValue) material.MaximumStock = dto.MaximumStock.Value;

        // Preço
        if (dto.BasePrice.HasValue) material.BasePrice = dto.BasePrice.Value;

        // Armazenamento
        if (dto.StorageConditions != null) material.StorageConditions = dto.StorageConditions.Trim();
        if (dto.RequiresRefrigeration.HasValue) material.RequiresRefrigeration = dto.RequiresRefrigeration.Value;
        if (dto.LightSensitive.HasValue) material.LightSensitive = dto.LightSensitive.Value;
        if (dto.HumiditySensitive.HasValue) material.HumiditySensitive = dto.HumiditySensitive.Value;

        if (dto.Popularity.HasValue) material.Popularity = dto.Popularity.Value;

        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;
        material.RequiresSpecialAuthorization = material.ControlType != "COMUM";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Matéria-prima {Name} atualizada por {Employee}", material.Name, employee.FullName);

        return Ok(new { success = true, message = "Matéria-prima atualizada" });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // IMPORTAR DO CATÁLOGO GLOBAL
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Importa um ou mais itens do catálogo global para o estoque do estabelecimento.
    /// Itens já cadastrados (mesmo nome) são ignorados silenciosamente.
    /// POST /api/RawMaterials/import-from-catalog
    /// </summary>
    [HttpPost("import-from-catalog")]
    public async Task<IActionResult> ImportFromCatalog([FromBody] ImportFromCatalogDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar estoque" });

        if (dto.CatalogIds == null || dto.CatalogIds.Count == 0)
            return BadRequest(new { error = "Nenhum item selecionado" });

        var catalogItems = await _db.RawMaterialsCatalog
            .Where(c => dto.CatalogIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();

        if (catalogItems.Count == 0)
            return NotFound(new { error = "Nenhum item válido encontrado no catálogo" });

        // Quais hormonios/controlados precisam de permissão extra
        var needsControlled = catalogItems.Any(c =>
            !string.IsNullOrEmpty(c.ControlType) && c.ControlType != "COMUM");
        if (needsControlled && !await HasControlledSubstancePermission(employee))
            return StatusCode(403, new { error = "Seleção inclui substâncias controladas; sem permissão" });

        // Já cadastrados (por nome, case-insensitive)
        var existingNames = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Select(rm => rm.Name.ToLower())
            .ToListAsync();
        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var imported = new List<object>();
        var skipped = new List<object>();

        foreach (var c in catalogItems)
        {
            if (existingSet.Contains(c.Name))
            {
                skipped.Add(new { c.Id, c.Name, reason = "Já cadastrado neste estabelecimento" });
                continue;
            }

            var material = new RawMaterial
            {
                Id = Guid.NewGuid(),
                EstablishmentId = employee.EstablishmentId,
                Name = c.Name,
                DcbCode = c.DcbCode,
                CasNumber = c.CasNumber ?? "",
                Category = c.Category,
                ControlType = c.ControlType,
                AllowedUsage = c.AllowedUsage,
                PhysicalState = c.PhysicalState,
                Unit = c.Unit,
                PurityFactor = c.DefaultPurityFactor,
                CorrectionFactor = c.DefaultCorrectionFactor,
                Synonyms = c.Synonyms,
                Indications = c.Indications,
                Popularity = c.Popularity,
                CurrentStock = 0,
                MinimumStock = 0,
                MaximumStock = 0,
                IsVirtual = dto.AsVirtual ?? true,
                IsActive = true,
                PriceSource = "BASE",
                RequiresSpecialAuthorization = c.ControlType != "COMUM",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.RawMaterials.Add(material);
            existingSet.Add(c.Name);
            imported.Add(new { catalogId = c.Id, id = material.Id, c.Name });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Import do catálogo: {Imported} importados, {Skipped} pulados (estabelecimento {EstId} por {EmpName})",
            imported.Count, skipped.Count, employee.EstablishmentId, employee.FullName);

        return Ok(new
        {
            success = true,
            importedCount = imported.Count,
            skippedCount = skipped.Count,
            imported,
            skipped
        });
    }

    public class ImportFromCatalogDto
    {
        public List<Guid> CatalogIds { get; set; } = new();
        public bool? AsVirtual { get; set; } = true;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // PREÇOS
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Atualiza o preço base de uma matéria-prima
    /// PATCH /api/RawMaterials/{id}/base-price
    /// </summary>
    [HttpPatch("{id:guid}/base-price")]
    public async Task<IActionResult> UpdateBasePrice(Guid id, [FromBody] UpdateBasePriceDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar preços" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm =>
                rm.Id == id &&
                rm.EstablishmentId == employee.EstablishmentId &&
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        var oldPrice = material.BasePrice;
        material.BasePrice = dto.BasePrice;
        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;

        // Se não tem LastKnownPrice, atualiza PriceSource
        if (!material.LastKnownPrice.HasValue && material.CurrentStock <= 0)
            material.PriceSource = "BASE";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Preço base de {MaterialName} alterado de {OldPrice} para {NewPrice} por {EmployeeName}",
            material.Name, oldPrice, dto.BasePrice, employee.FullName);

        return Ok(new
        {
            success = true,
            message = "Preço base atualizado com sucesso",
            data = new
            {
                material.Id,
                material.Name,
                OldBasePrice = oldPrice,
                NewBasePrice = material.BasePrice,
                material.PriceSource
            }
        });
    }

    /// <summary>
    /// Atualização em lote de preços base
    /// POST /api/RawMaterials/bulk-update-prices
    /// </summary>
    [HttpPost("bulk-update-prices")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] List<BulkPriceUpdateDto> updates)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para atualizar preços" });

        if (updates == null || !updates.Any())
            return BadRequest(new { error = "Lista de atualizações vazia" });

        var ids = updates.Select(u => u.Id).ToList();
        var materials = await _db.RawMaterials
            .Where(r => r.EstablishmentId == employee.EstablishmentId && ids.Contains(r.Id))
            .ToListAsync();

        int updated = 0;
        foreach (var material in materials)
        {
            var update = updates.FirstOrDefault(u => u.Id == material.Id);
            if (update?.NewPrice != null && update.NewPrice > 0)
            {
                material.BasePrice = update.NewPrice.Value;
                material.LastPriceDate = DateTime.UtcNow;
                material.UpdatedAt = DateTime.UtcNow;
                material.UpdatedByEmployeeId = employee.Id;
                updated++;
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Atualização em lote de {Count} preços por {Employee}", updated, employee.FullName);

        return Ok(new { success = true, updated, total = updates.Count });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ESTATÍSTICAS
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retorna estatísticas de precificação
    /// GET /api/RawMaterials/pricing-statistics
    /// </summary>
    [HttpGet("pricing-statistics")]
    public async Task<IActionResult> GetPricingStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var materials = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        var stats = new
        {
            totalMaterials = materials.Count,

            // Por estoque
            withStock = materials.Count(m => m.CurrentStock > 0),
            withoutStock = materials.Count(m => m.CurrentStock <= 0),
            virtualOnly = materials.Count(m => m.IsVirtual),

            // Por origem de preço
            priceFromStock = materials.Count(m => m.PriceSource == "ESTOQUE"),
            priceFromHistory = materials.Count(m => m.PriceSource == "HISTORICO"),
            priceFromBase = materials.Count(m => m.PriceSource == "BASE"),

            // Qualidade dos preços
            withBasePrice = materials.Count(m => m.BasePrice.HasValue && m.BasePrice > 0),
            withoutBasePrice = materials.Count(m => !m.BasePrice.HasValue || m.BasePrice <= 0),
            withLastKnownPrice = materials.Count(m => m.LastKnownPrice.HasValue),
            outdatedPrice = materials.Count(m =>
                m.LastPriceDate == null || m.LastPriceDate < sixMonthsAgo),

            // Por uso permitido
            oralUse = materials.Count(m => m.AllowedUsage == "ORAL" || m.AllowedUsage == "BOTH"),
            topicalUse = materials.Count(m => m.AllowedUsage == "TOPICAL" || m.AllowedUsage == "BOTH"),

            // Por categoria
            byCategory = materials
                .Where(m => !string.IsNullOrEmpty(m.Category))
                .GroupBy(m => m.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10),

            // Mais usados sem estoque
            popularWithoutStock = materials
                .Where(m => m.CurrentStock <= 0)
                .OrderByDescending(m => m.Popularity)
                .Take(10)
                .Select(m => new { m.Id, m.Name, m.Popularity, m.LastKnownPrice, m.BasePrice }),

            // Preços desatualizados (mais usados primeiro)
            outdatedPriceList = materials
                .Where(m => m.LastPriceDate == null || m.LastPriceDate < sixMonthsAgo)
                .OrderByDescending(m => m.Popularity)
                .Take(10)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.LastPriceDate,
                    DaysSinceUpdate = m.LastPriceDate.HasValue
                        ? (int)(DateTime.UtcNow - m.LastPriceDate.Value).TotalDays
                        : (int?)null,
                    m.BasePrice,
                    m.LastKnownPrice
                })
        };

        return Ok(new { success = true, data = stats });
    }

    /// <summary>
    /// Retorna estatísticas gerais de estoque
    /// GET /api/RawMaterials/statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var materials = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        return Ok(new
        {
            success = true,
            data = new
            {
                totalMaterials = materials.Count,
                controlledSubstances = materials.Count(m => m.ControlType != "COMUM" && !string.IsNullOrEmpty(m.ControlType)),
                byControlType = materials.GroupBy(m => m.ControlType ?? "COMUM")
                    .Select(g => new { controlType = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count),
                lowStock = materials.Count(m => m.CurrentStock > 0 && m.CurrentStock <= m.MinimumStock),
                outOfStock = materials.Count(m => m.CurrentStock <= 0 && !m.IsVirtual),
                excessStock = materials.Count(m => m.CurrentStock >= m.MaximumStock && m.MaximumStock > 0),
                normalStock = materials.Count(m => m.CurrentStock > m.MinimumStock && (m.MaximumStock == 0 || m.CurrentStock < m.MaximumStock)),
                virtualIngredients = materials.Count(m => m.IsVirtual),
                withBasePrice = materials.Count(m => m.BasePrice.HasValue && m.BasePrice > 0),
                withoutPrice = materials.Count(m => !m.BasePrice.HasValue && !m.LastKnownPrice.HasValue),
                outdatedPrice = materials.Count(m => m.LastPriceDate == null || m.LastPriceDate < sixMonthsAgo),
                oralUse = materials.Count(m => m.AllowedUsage == "ORAL" || m.AllowedUsage == "BOTH"),
                topicalUse = materials.Count(m => m.AllowedUsage == "TOPICAL" || m.AllowedUsage == "BOTH")
            }
        });
    }

    /// <summary>
    /// Retorna lista de categorias disponíveis
    /// GET /api/RawMaterials/categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var categories = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Where(rm => !string.IsNullOrEmpty(rm.Category))
            .GroupBy(rm => rm.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderBy(x => x.category)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // EXCLUIR
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Desativa uma matéria-prima (soft delete)
    /// DELETE /api/RawMaterials/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm =>
                rm.Id == id &&
                rm.EstablishmentId == employee.EstablishmentId &&
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        // Verificar se tem estoque
        if (material.CurrentStock > 0)
            return BadRequest(new { error = "Não é possível excluir matéria-prima com estoque" });

        material.IsActive = false;
        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;

        await _db.SaveChangesAsync();

        _logger.LogWarning("Matéria-prima {Name} desativada por {Employee}", material.Name, employee.FullName);

        return Ok(new { success = true, message = "Matéria-prima desativada" });
    }

    // ════════════════════════════════════════════════════════════════════════════
    // MÉTODOS AUXILIARES
    // ════════════════════════════════════════════════════════════════════════════

    private bool IsEmployeeActive(Employee employee)
    {
        if (employee.Status.ToUpper() != "ATIVO") return false;
        if (employee.TerminationDate.HasValue && employee.TerminationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)) return false;
        if (employee.LockedUntil.HasValue && employee.LockedUntil.Value > DateTime.UtcNow) return false;
        return true;
    }

    private async Task<bool> HasStockManagementPermission(Employee employee)
    {
        if (employee.JobPosition == null)
            await _db.Entry(employee).Reference(e => e.JobPosition).LoadAsync();

        if (employee.JobPosition == null) return false;

        var allowedCodes = new[] { "pharmacist", "pharmacist_rt", "manager", "admin", "stock_assistant" };
        return allowedCodes.Contains(employee.JobPosition.Code);
    }

    private async Task<bool> HasControlledSubstancePermission(Employee employee)
    {
        if (employee.JobPosition == null)
            await _db.Entry(employee).Reference(e => e.JobPosition).LoadAsync();

        if (employee.JobPosition == null) return false;

        var allowedCodes = new[] { "pharmacist", "pharmacist_rt", "admin" };
        return allowedCodes.Contains(employee.JobPosition.Code);
    }
}