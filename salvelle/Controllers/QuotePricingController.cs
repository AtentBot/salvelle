using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Service.Pricing;

namespace Controllers.Api;

/// <summary>
/// API Controller para Orçamentos com Precificação Integrada
/// Rota: /api/QuotePricing/*
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuotePricingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly QuotePricingService _pricingService;
    private readonly ILogger<QuotePricingController> _logger;

    public QuotePricingController(
        AppDbContext context,
        QuotePricingService pricingService,
        ILogger<QuotePricingController> logger)
    {
        _context = context;
        _pricingService = pricingService;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.Id ?? Guid.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CÁLCULO DE PREÇO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula precificação sem criar orçamento (preview)
    /// POST /api/QuotePricing/calculate
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<QuotePricingResultDto>> Calculate([FromBody] QuotePricingRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Estabelecimento não identificado" });

        var result = await _pricingService.CalculatePricingAsync(request, establishmentId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Calcula e cria orçamento
    /// POST /api/QuotePricing/create
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<CreateQuoteResponseDto>> CreateQuote([FromBody] CreateQuoteRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Estabelecimento não identificado" });

        // Calcular precificação
        var pricing = await _pricingService.CalculatePricingAsync(request.Pricing, establishmentId);

        if (!pricing.Success)
            return BadRequest(new { message = pricing.Message });

        // Criar orçamento
        var quoteId = await _pricingService.CreateQuoteAsync(
            pricing,
            request.Metadata,
            establishmentId,
            employeeId);

        // Buscar código gerado
        var quote = await _context.PrescriptionQuotes.FindAsync(quoteId);

        return Ok(new CreateQuoteResponseDto
        {
            QuoteId = quoteId,
            Code = quote?.Code ?? "",
            PublicToken = quote?.PublicToken ?? "",
            Pricing = pricing,
            Message = "Orçamento criado com sucesso"
        });
    }

    /// <summary>
    /// Recalcula um orçamento existente com novas configurações
    /// POST /api/QuotePricing/{id}/recalculate
    /// </summary>
    [HttpPost("{id}/recalculate")]
    public async Task<ActionResult<QuotePricingResultDto>> RecalculateQuote(Guid id, [FromBody] RecalculateRequestDto? request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (quote == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        // Extrair componentes do JSON
        var components = new List<QuoteComponentRequestDto>();
        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<QuoteComponentRequestDto>>(
                    quote.ComponentsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (parsed != null)
                    components = parsed;
            }
            catch { }
        }

        // Calcular com novos parâmetros
        var pricingRequest = new QuotePricingRequestDto
        {
            Components = components,
            CustomMarkup = request?.CustomMarkup,
            LaborCost = request?.LaborCost,
            DiscountPercentage = request?.DiscountPercentage
        };

        var result = await _pricingService.CalculatePricingAsync(pricingRequest, establishmentId);

        if (!result.Success)
            return BadRequest(result);

        // Atualizar orçamento se solicitado
        if (request?.SaveChanges == true)
        {
            quote.MarkupPercentage = result.MarkupPercentage;
            quote.MarkupValue = result.MarkupValue;
            quote.LaborCost = result.LaborCost;
            quote.DiscountPercentage = result.DiscountPercentage;
            quote.DiscountValue = result.DiscountValue;
            quote.Subtotal = result.Subtotal;
            quote.FinalPrice = result.FinalPrice;
            quote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ORÇAMENTO A PARTIR DE CÁLCULOS EXISTENTES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cria orçamento a partir de cálculo de cápsulas
    /// POST /api/QuotePricing/from-capsule-calculation
    /// </summary>
    [HttpPost("from-capsule-calculation")]
    public async Task<ActionResult<CreateQuoteResponseDto>> CreateFromCapsuleCalculation([FromBody] CapsuleToQuoteRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        if (establishmentId == Guid.Empty)
            return Unauthorized();

        // Converter resultado do cálculo de cápsulas para componentes
        var components = request.Components.Select(c => new QuoteComponentRequestDto
        {
            RawMaterialId = c.RawMaterialId,
            Name = c.Name,
            Quantity = c.TotalQuantityMg / 1000, // Converter mg para g
            Unit = "g",
            UnitCost = c.UnitCost,
            IsQsp = c.IsExcipient
        }).ToList();

        var pricingRequest = new QuotePricingRequestDto
        {
            PharmaceuticalFormId = request.PharmaceuticalFormId,
            SubtypeId = request.CapsuleSizeId,
            Components = components,
            CustomMarkup = request.CustomMarkup,
            DiscountPercentage = request.DiscountPercentage
        };

        var pricing = await _pricingService.CalculatePricingAsync(pricingRequest, establishmentId);

        if (!pricing.Success)
            return BadRequest(new { message = pricing.Message });

        var metadata = new QuoteMetadataDto
        {
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            PharmaceuticalForm = "Cápsula",
            TotalQuantity = request.CapsuleCount.ToString(),
            TotalQuantityUnit = "cápsulas",
            UsageType = "ORAL",
            Instructions = request.Instructions
        };

        var quoteId = await _pricingService.CreateQuoteAsync(pricing, metadata, establishmentId, employeeId);

        var quote = await _context.PrescriptionQuotes.FindAsync(quoteId);

        return Ok(new CreateQuoteResponseDto
        {
            QuoteId = quoteId,
            Code = quote?.Code ?? "",
            PublicToken = quote?.PublicToken ?? "",
            Pricing = pricing,
            Message = "Orçamento de cápsulas criado com sucesso"
        });
    }

    /// <summary>
    /// Cria orçamento a partir de cálculo de formulação tópica
    /// POST /api/QuotePricing/from-topical-calculation
    /// </summary>
    [HttpPost("from-topical-calculation")]
    public async Task<ActionResult<CreateQuoteResponseDto>> CreateFromTopicalCalculation([FromBody] TopicalToQuoteRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        if (establishmentId == Guid.Empty)
            return Unauthorized();

        // Converter resultado do cálculo tópico para componentes
        var components = new List<QuoteComponentRequestDto>();

        // Adicionar ativos
        foreach (var active in request.ActiveResults)
        {
            components.Add(new QuoteComponentRequestDto
            {
                RawMaterialId = active.RawMaterialId,
                Name = active.Name,
                Quantity = active.CorrectedQuantity,
                Unit = active.Unit,
                UnitCost = active.UnitCost,
                IsQsp = false
            });
        }

        // Adicionar base
        foreach (var baseItem in request.BaseResults)
        {
            components.Add(new QuoteComponentRequestDto
            {
                RawMaterialId = baseItem.RawMaterialId,
                Name = baseItem.Name,
                Quantity = baseItem.CorrectedQuantity,
                Unit = baseItem.Unit,
                UnitCost = baseItem.UnitCost,
                IsQsp = baseItem.IsQsp
            });
        }

        var pricingRequest = new QuotePricingRequestDto
        {
            PharmaceuticalFormId = request.PharmaceuticalFormId,
            SubtypeId = request.BaseSubtypeId,
            Components = components,
            CustomMarkup = request.CustomMarkup,
            DiscountPercentage = request.DiscountPercentage
        };

        var pricing = await _pricingService.CalculatePricingAsync(pricingRequest, establishmentId);

        if (!pricing.Success)
            return BadRequest(new { message = pricing.Message });

        var metadata = new QuoteMetadataDto
        {
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            PharmaceuticalForm = request.FormName ?? "Creme",
            TotalQuantity = request.TotalQuantity.ToString(),
            TotalQuantityUnit = request.TotalUnit,
            UsageType = "TOPICO",
            Instructions = request.Instructions
        };

        var quoteId = await _pricingService.CreateQuoteAsync(pricing, metadata, establishmentId, employeeId);

        var quote = await _context.PrescriptionQuotes.FindAsync(quoteId);

        return Ok(new CreateQuoteResponseDto
        {
            QuoteId = quoteId,
            Code = quote?.Code ?? "",
            PublicToken = quote?.PublicToken ?? "",
            Pricing = pricing,
            Message = $"Orçamento de {metadata.PharmaceuticalForm} criado com sucesso"
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LISTAGEM E BUSCA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista orçamentos com filtros
    /// GET /api/QuotePricing?status=PENDENTE&page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<QuoteListResponseDto>> ListQuotes(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var query = _context.PrescriptionQuotes
            .Where(q => q.EstablishmentId == establishmentId);

        // Filtros
        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => 
                q.Code.Contains(search) ||
                (q.CustomerName != null && q.CustomerName.Contains(search)) ||
                (q.CustomerPhone != null && q.CustomerPhone.Contains(search)));
        }

        if (startDate.HasValue)
            query = query.Where(q => q.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(q => q.CreatedAt <= endDate.Value.AddDays(1));

        var total = await query.CountAsync();

        var quotes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuoteListItemDto
            {
                Id = q.Id,
                Code = q.Code,
                CustomerName = q.CustomerName,
                CustomerPhone = q.CustomerPhone,
                PharmaceuticalForm = q.PharmaceuticalForm,
                TotalQuantity = q.TotalQuantity,
                FinalPrice = q.FinalPrice,
                Status = q.Status,
                CreatedAt = q.CreatedAt,
                ValidUntil = q.ValidUntil,
                EstimatedDays = q.EstimatedDays
            })
            .ToListAsync();

        return Ok(new QuoteListResponseDto
        {
            Items = quotes,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// Busca orçamento detalhado
    /// GET /api/QuotePricing/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuoteDetailDto>> GetQuote(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var quote = await _context.PrescriptionQuotes
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (quote == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        // Deserializar componentes
        var components = new List<QuoteComponentResultDto>();
        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<QuoteComponentResultDto>>(
                    quote.ComponentsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (parsed != null)
                    components = parsed;
            }
            catch { }
        }

        // Buscar configuração atual para mostrar nomes das taxas
        var config = await _context.EstablishmentPricingConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

        return Ok(new QuoteDetailDto
        {
            Id = quote.Id,
            Code = quote.Code,
            PublicToken = quote.PublicToken,
            
            // Cliente
            CustomerName = quote.CustomerName,
            CustomerPhone = quote.CustomerPhone,
            CustomerEmail = quote.CustomerEmail,
            
            // Médico
            DoctorName = quote.DoctorName,
            DoctorCrm = quote.DoctorCrm,
            
            // Fórmula
            PharmaceuticalForm = quote.PharmaceuticalForm,
            TotalQuantity = quote.TotalQuantity,
            TotalQuantityUnit = quote.TotalQuantityUnit,
            UsageType = quote.UsageType,
            Instructions = quote.Instructions,
            
            // Componentes
            Components = components,
            
            // Valores
            MaterialsCost = quote.MaterialsCost,
            MarkupPercentage = quote.MarkupPercentage,
            MarkupValue = quote.MarkupValue,
            LaborCost = quote.LaborCost,
            PackagingCost = quote.PackagingCost,
            Subtotal = quote.Subtotal,
            DiscountPercentage = quote.DiscountPercentage,
            DiscountValue = quote.DiscountValue,
            FinalPrice = quote.FinalPrice,
            
            // Taxas (nomes da config atual)
            Fee1Name = config?.Fee1Name ?? "Taxa 1",
            Fee2Name = config?.Fee2Name ?? "Taxa 2",
            Fee3Name = config?.Fee3Name ?? "Taxa 3",
            
            // Prazo
            EstimatedDays = quote.EstimatedDays,
            ValidUntil = quote.ValidUntil,
            
            // Status
            Status = quote.Status,
            CreatedAt = quote.CreatedAt,
            ApprovedAt = quote.ApprovedAt,
            RejectedAt = quote.RejectedAt,
            RejectionReason = quote.RejectionReason,
            
            // Vínculos
            PrescriptionId = quote.PrescriptionId,
            ManipulationOrderId = quote.ManipulationOrderId,
            SaleId = quote.SaleId
        });
    }

    /// <summary>
    /// Estatísticas de orçamentos
    /// GET /api/QuotePricing/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<QuoteStatsDto>> GetStats([FromQuery] int? days = 30)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days!.Value);

        var quotes = await _context.PrescriptionQuotes
            .Where(q => q.EstablishmentId == establishmentId && q.CreatedAt >= startDate)
            .ToListAsync();

        var stats = new QuoteStatsDto
        {
            TotalQuotes = quotes.Count,
            PendingQuotes = quotes.Count(q => q.Status == "PENDENTE"),
            ApprovedQuotes = quotes.Count(q => q.Status == "APROVADO" || q.Status == "CONVERTIDO"),
            RejectedQuotes = quotes.Count(q => q.Status == "RECUSADO"),
            ExpiredQuotes = quotes.Count(q => q.Status == "EXPIRADO"),
            
            TotalValue = quotes.Sum(q => q.FinalPrice),
            ApprovedValue = quotes.Where(q => q.Status == "APROVADO" || q.Status == "CONVERTIDO").Sum(q => q.FinalPrice),
            AverageValue = quotes.Any() ? quotes.Average(q => q.FinalPrice) : 0,
            
            ConversionRate = quotes.Any() 
                ? (decimal)quotes.Count(q => q.Status == "APROVADO" || q.Status == "CONVERTIDO") / quotes.Count * 100 
                : 0,
            
            AverageEstimatedDays = quotes.Any() ? (int)quotes.Average(q => q.EstimatedDays) : 0
        };

        return Ok(stats);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs ADICIONAIS
// ═══════════════════════════════════════════════════════════════════════════════

public class CreateQuoteRequestDto
{
    public QuotePricingRequestDto Pricing { get; set; } = new();
    public QuoteMetadataDto Metadata { get; set; } = new();
}

public class CreateQuoteResponseDto
{
    public Guid QuoteId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PublicToken { get; set; } = string.Empty;
    public QuotePricingResultDto Pricing { get; set; } = new();
    public string? Message { get; set; }
}

public class RecalculateRequestDto
{
    public decimal? CustomMarkup { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool SaveChanges { get; set; }
}

public class CapsuleToQuoteRequestDto
{
    public Guid PharmaceuticalFormId { get; set; }
    public Guid? CapsuleSizeId { get; set; }
    public int CapsuleCount { get; set; }
    public List<CapsuleComponentDto> Components { get; set; } = new();
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Instructions { get; set; }
    public decimal? CustomMarkup { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class CapsuleComponentDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalQuantityMg { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsExcipient { get; set; }
}

public class TopicalToQuoteRequestDto
{
    public Guid PharmaceuticalFormId { get; set; }
    public Guid? BaseSubtypeId { get; set; }
    public string? FormName { get; set; }
    public decimal TotalQuantity { get; set; }
    public string TotalUnit { get; set; } = "g";
    public List<TopicalComponentDto> ActiveResults { get; set; } = new();
    public List<TopicalComponentDto> BaseResults { get; set; } = new();
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Instructions { get; set; }
    public decimal? CustomMarkup { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class TopicalComponentDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CorrectedQuantity { get; set; }
    public string Unit { get; set; } = "g";
    public decimal? UnitCost { get; set; }
    public bool IsQsp { get; set; }
}

public class QuoteListResponseDto
{
    public List<QuoteListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class QuoteListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? PharmaceuticalForm { get; set; }
    public string? TotalQuantity { get; set; }
    public decimal FinalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public int EstimatedDays { get; set; }
}

public class QuoteDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? PublicToken { get; set; }
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    
    public string? DoctorName { get; set; }
    public string? DoctorCrm { get; set; }
    
    public string? PharmaceuticalForm { get; set; }
    public string? TotalQuantity { get; set; }
    public string? TotalQuantityUnit { get; set; }
    public string? UsageType { get; set; }
    public string? Instructions { get; set; }
    
    public List<QuoteComponentResultDto> Components { get; set; } = new();
    
    public decimal MaterialsCost { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal MarkupValue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal FinalPrice { get; set; }
    
    public string Fee1Name { get; set; } = string.Empty;
    public string Fee2Name { get; set; } = string.Empty;
    public string Fee3Name { get; set; } = string.Empty;
    
    public int EstimatedDays { get; set; }
    public DateTime ValidUntil { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    public Guid? PrescriptionId { get; set; }
    public Guid? ManipulationOrderId { get; set; }
    public Guid? SaleId { get; set; }
}

public class QuoteStatsDto
{
    public int TotalQuotes { get; set; }
    public int PendingQuotes { get; set; }
    public int ApprovedQuotes { get; set; }
    public int RejectedQuotes { get; set; }
    public int ExpiredQuotes { get; set; }
    
    public decimal TotalValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal AverageValue { get; set; }
    
    public decimal ConversionRate { get; set; }
    public int AverageEstimatedDays { get; set; }
}
