using Data;
using DTOs;
using DTOs.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Pharmacy;
using System.Security.Cryptography;
using System.Text.Json;

namespace Service.Prescriptions;

/// <summary>
/// Serviço para gerenciar orçamentos de prescrições magistrais
/// </summary>
public class PrescriptionQuoteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PrescriptionQuoteService> _logger;
    private readonly string _baseUrl;

    // Configurações padrão (podem vir de configuração do estabelecimento)
    private const decimal DefaultMarkupPercentage = 150m;
    private const decimal DefaultLaborCostPerItem = 5m;
    private const decimal DefaultPackagingCost = 3m;
    private const int DefaultValidityDays = 7;
    private const int DefaultEstimatedDays = 3;
    private const decimal DefaultUnitCost = 0.10m;

    public PrescriptionQuoteService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<PrescriptionQuoteService> logger)
    {
        _context = context;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://salvelle.atentbot.com";
    }

    /// <summary>
    /// Cria um orçamento a partir dos ingredientes confirmados
    /// </summary>
    public async Task<(bool Success, string Message, PrescriptionQuoteDto? Quote)> CreateQuoteAsync(
        CreateQuoteFromOcrDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            _logger.LogInformation("Criando orçamento para {Count} ingredientes", dto.Ingredients.Count);

            if (!dto.Ingredients.Any())
                return (false, "Nenhum ingrediente informado", null);

            var rawMaterialIds = dto.Ingredients.Select(i => i.RawMaterialId).ToList();
            var rawMaterials = await _context.RawMaterials
                .Include(rm => rm.Batches!.Where(b => b.Status == "APROVADO" && b.ExpiryDate > DateTime.UtcNow))
                .Where(rm => rawMaterialIds.Contains(rm.Id))
                .ToDictionaryAsync(rm => rm.Id);

            if (rawMaterials.Count != rawMaterialIds.Count)
                return (false, "Uma ou mais matérias-primas não encontradas", null);

            var establishment = await _context.Establishments
                .FirstOrDefaultAsync(e => e.Id == establishmentId);

            if (establishment == null)
                return (false, "Estabelecimento não encontrado", null);

            var components = new List<QuoteComponent>();
            decimal totalMaterialsCost = 0;

            foreach (var ing in dto.Ingredients)
            {
                var rm = rawMaterials[ing.RawMaterialId];
                var unitCost = GetUnitCost(rm);

                var quantity = ing.Quantity;
                if (ing.Unit == "%" && !string.IsNullOrEmpty(dto.TotalQuantity))
                {
                    var totalQty = ParseQuantity(dto.TotalQuantity);
                    quantity = (ing.Quantity / 100) * totalQty;
                }

                var totalCost = quantity * unitCost;
                totalMaterialsCost += totalCost;

                components.Add(new QuoteComponent
                {
                    RawMaterialId = rm.Id,
                    Name = rm.Name,
                    DcbCode = rm.DcbCode,
                    Quantity = quantity,
                    Unit = ing.Unit == "%" ? "g" : ing.Unit,
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    IsQsp = ing.IsQsp,
                    IsControlled = rm.ControlType != "COMUM",
                    ControlType = rm.ControlType
                });
            }

            var markupValue = totalMaterialsCost * (DefaultMarkupPercentage / 100);
            var laborCost = DefaultLaborCostPerItem * components.Count;
            var packagingCost = DefaultPackagingCost;
            var subtotal = totalMaterialsCost + markupValue + laborCost + packagingCost;
            var finalPrice = subtotal;

            var code = await GenerateQuoteCodeAsync(establishmentId);
            var publicToken = GeneratePublicToken();

            var quote = new PrescriptionQuote
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = code,
                PublicToken = publicToken,
                PrescriptionId = dto.PrescriptionId,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName ?? "",
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = dto.CustomerEmail,
                DoctorName = dto.Doctor.Name,
                DoctorCrm = dto.Doctor.Crm,
                DoctorCrmState = dto.Doctor.CrmState,
                DoctorSpecialty = dto.Doctor.Specialty,
                UsageType = dto.Usage.ToUpper(),
                PharmaceuticalForm = dto.PharmaceuticalForm,
                TotalQuantity = dto.TotalQuantity,
                TotalQuantityNumeric = ParseQuantity(dto.TotalQuantity),
                TotalQuantityUnit = ParseUnit(dto.TotalQuantity),
                Instructions = dto.Instructions,
                ComponentsJson = JsonSerializer.Serialize(components),
                MaterialsCost = totalMaterialsCost,
                MarkupPercentage = DefaultMarkupPercentage,
                MarkupValue = markupValue,
                LaborCost = laborCost,
                PackagingCost = packagingCost,
                Subtotal = subtotal,
                DiscountPercentage = 0,
                DiscountValue = 0,
                FinalPrice = finalPrice,
                EstimatedDays = DefaultEstimatedDays,
                ValidUntil = DateTime.UtcNow.AddDays(DefaultValidityDays),
                Status = "PENDENTE",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PrescriptionQuotes.Add(quote);
            await _context.SaveChangesAsync();

            var quoteDto = MapToDto(quote, establishment);

            _logger.LogInformation("Orçamento {Code} criado com sucesso. Valor: R$ {Price}",
                quote.Code, quote.FinalPrice);

            return (true, "Orçamento criado com sucesso", quoteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar orçamento");
            return (false, $"Erro ao criar orçamento: {ex.Message}", null);
        }
    }

    private decimal GetUnitCost(RawMaterial rm)
    {
        var lastBatch = rm.Batches?
            .OrderByDescending(b => b.ReceivedDate)
            .FirstOrDefault();

        if (lastBatch != null && lastBatch.UnitCost > 0)
            return lastBatch.UnitCost;

        return rm.ControlType switch
        {
            "COMUM" => DefaultUnitCost,
            "LISTA_A" or "LISTA_B" => DefaultUnitCost * 5,
            "ANTIMICROBIANO" => DefaultUnitCost * 2,
            _ => DefaultUnitCost
        };
    }

    /// <summary>
    /// Busca orçamento pelo token público (para página pública)
    /// </summary>
    public async Task<PrescriptionQuoteDto?> GetByPublicTokenAsync(string token)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.PublicToken == token);

        if (quote == null)
            return null;

        quote.ViewCount++;
        quote.LastViewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == quote.EstablishmentId);

        return MapToDto(quote, establishment);
    }

    /// <summary>
    /// Busca orçamento por ID
    /// </summary>
    public async Task<PrescriptionQuoteDto?> GetByIdAsync(Guid id, Guid establishmentId)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (quote == null)
            return null;

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == quote.EstablishmentId);

        return MapToDto(quote, establishment);
    }

    /// <summary>
    /// Lista todos os orçamentos do estabelecimento
    /// </summary>
    public async Task<List<PrescriptionQuoteDto>> GetAllAsync(
        Guid establishmentId,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.PrescriptionQuotes
            .Where(q => q.EstablishmentId == establishmentId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        if (startDate.HasValue)
            query = query.Where(q => q.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(q => q.CreatedAt <= endDate.Value.AddDays(1));

        var quotes = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == establishmentId);

        return quotes.Select(q => MapToDto(q, establishment)).ToList();
    }

    /// <summary>
    /// Aprova o orçamento pelo cliente (via link público) e CRIA ORDEM DE MANIPULAÇÃO
    /// </summary>
    public async Task<(bool Success, string Message, Guid? ManipulationOrderId)> ApproveByTokenAsync(
        string token,
        string? customerObservations,
        string? clientIp)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.PublicToken == token);

        if (quote == null)
            return (false, "Orçamento não encontrado", null);

        if (quote.Status != "PENDENTE")
            return (false, $"Orçamento não pode ser aprovado. Status atual: {quote.Status}", null);

        if (quote.ValidUntil < DateTime.UtcNow)
        {
            quote.Status = "EXPIRADO";
            await _context.SaveChangesAsync();
            return (false, "Orçamento expirado", null);
        }

        // Aprovar o orçamento
        quote.Status = "APROVADO";
        quote.ApprovedAt = DateTime.UtcNow;
        quote.ApprovedIp = clientIp;
        quote.CustomerObservations = customerObservations;
        quote.UpdatedAt = DateTime.UtcNow;

        // ========== CRIAR ORDEM DE MANIPULAÇÃO AUTOMATICAMENTE ==========
        ManipulationOrder? order = null;
        try
        {
            order = await CreateManipulationOrderFromQuoteAsync(quote);
            quote.ManipulationOrderId = order.Id;
            
            _logger.LogInformation(
                "Ordem de Manipulação {OrderNumber} criada automaticamente para orçamento {QuoteCode}",
                order.OrderNumber, quote.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar OM automática para orçamento {QuoteCode}. Continuando sem OM.", quote.Code);
            // Não falha a aprovação, apenas loga o erro
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Orçamento {Code} aprovado pelo cliente", quote.Code);

        var message = order != null 
            ? $"Orçamento aprovado! Ordem de Manipulação {order.OrderNumber} criada."
            : "Orçamento aprovado com sucesso";

        return (true, message, order?.Id);
    }

    /// <summary>
    /// Cria a ordem de manipulação a partir do orçamento aprovado
    /// </summary>
    // =====================================================
    // SUBSTITUIR o método CreateManipulationOrderFromQuoteAsync 
    // no PrescriptionQuoteService.cs (linhas ~314-400)
    // =====================================================

    /// <summary>
    /// Cria a ordem de manipulação a partir do orçamento aprovado
    /// </summary>
    private async Task<ManipulationOrder> CreateManipulationOrderFromQuoteAsync(PrescriptionQuote quote)
    {
        var orderCode = await GenerateManipulationOrderCodeAsync(quote.EstablishmentId);

        // Extrair quantidade
        decimal quantity = 1;
        if (!string.IsNullOrEmpty(quote.TotalQuantity))
        {
            var numericPart = new string(quote.TotalQuantity.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (!string.IsNullOrEmpty(numericPart))
            {
                decimal.TryParse(numericPart.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out quantity);
            }
            if (quantity <= 0) quantity = 1;
        }

        // Buscar código da prescrição se existir
        string? prescriptionCode = null;
        if (quote.PrescriptionId.HasValue)
        {
            var prescription = await _context.Prescriptions.FindAsync(quote.PrescriptionId.Value);
            prescriptionCode = prescription?.Code;
        }

        // Buscar um employee do estabelecimento para atribuir (se CreatedByEmployeeId for null)
        Guid requestedByEmployeeId = quote.CreatedByEmployeeId;
        if (requestedByEmployeeId == Guid.Empty)
        {
            // Buscar qualquer employee do estabelecimento
            var anyEmployee = await _context.Employees
                .Where(e => e.EstablishmentId == quote.EstablishmentId )
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            if (anyEmployee != Guid.Empty)
                requestedByEmployeeId = anyEmployee;
        }

        var order = new ManipulationOrder
        {
            Id = Guid.NewGuid(),
            EstablishmentId = quote.EstablishmentId,
            PrescriptionQuoteId = quote.Id,
            OrderNumber = orderCode,
            Code = orderCode,
            PrescriptionNumber = prescriptionCode,
            PrescriberName = quote.DoctorName,
            PrescriberRegistration = quote.DoctorCrm,
            CustomerName = quote.CustomerName ?? "Cliente",
            CustomerPhone = quote.CustomerPhone,
            QuantityToProduce = quantity,
            Unit = quote.TotalQuantityUnit ?? "un",
            SpecialInstructions = quote.Instructions,
            Status = "AGUARDANDO_PRODUCAO",
            Priority = "NORMAL",
            OrderDate = DateTime.UtcNow,
            ExpectedDate = DateTime.UtcNow.AddDays(quote.EstimatedDays),
            RequestedByEmployeeId = requestedByEmployeeId,
            PassedQualityControl = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationOrders.Add(order);

        // Criar componentes da ordem a partir dos componentes do orçamento
        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            try
            {
                var components = JsonSerializer.Deserialize<List<QuoteComponent>>(
                    quote.ComponentsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (components != null)
                {
                    int index = 0;
                    foreach (var comp in components)
                    {
                        var orderComponent = new ManipulationOrderComponent
                        {
                            Id = Guid.NewGuid(),
                            ManipulationOrderId = order.Id,
                            RawMaterialId = comp.RawMaterialId,
                            RequiredQuantity = comp.Quantity,
                            Unit = comp.Unit ?? "g",
                            UnitCost = comp.UnitCost,
                            TotalCost = comp.TotalCost,
                            OrderIndex = index++,
                            Status = "PENDENTE",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.ManipulationOrderComponents.Add(orderComponent);
                    }

                    _logger.LogInformation("Criados {Count} componentes para OM {OrderNumber}",
                        components.Count, order.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao parsear componentes do orçamento {QuoteId}", quote.Id);
            }
        }

        return order;
    }

    /// <summary>
    /// Gera código único para ordem de manipulação
    /// </summary>
    private async Task<string> GenerateManipulationOrderCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");

        var count = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.CreatedAt.Date == today.Date)
            .CountAsync();

        return $"OM{dateStr}{(count + 1):D4}";
    }

    /// <summary>
    /// Rejeita o orçamento pelo cliente
    /// </summary>
    public async Task<(bool Success, string Message)> RejectByTokenAsync(
        string token,
        string? rejectionReason,
        string? customerObservations)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.PublicToken == token);

        if (quote == null)
            return (false, "Orçamento não encontrado");

        if (quote.Status != "PENDENTE")
            return (false, $"Orçamento não pode ser recusado. Status atual: {quote.Status}");

        quote.Status = "RECUSADO";
        quote.RejectedAt = DateTime.UtcNow;
        quote.RejectionReason = rejectionReason;
        quote.CustomerObservations = customerObservations;
        quote.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Orçamento {Code} recusado pelo cliente. Motivo: {Reason}",
            quote.Code, rejectionReason);

        return (true, "Orçamento recusado");
    }

    /// <summary>
    /// Converte orçamento aprovado em venda
    /// </summary>
    public async Task<(bool Success, string Message, Guid? SaleId)> ConvertToSaleAsync(
        Guid quoteId,
        ConvertQuoteToSaleDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

        if (quote == null)
            return (false, "Orçamento não encontrado", null);

        if (quote.Status != "APROVADO")
            return (false, "Apenas orçamentos aprovados podem ser convertidos em venda", null);

        quote.Status = "CONVERTIDO";
        quote.UpdatedAt = DateTime.UtcNow;
        quote.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Orçamento {Code} convertido em venda", quote.Code);

        return (true, "Orçamento convertido em venda", null);
    }

    /// <summary>
    /// Atualiza valores do orçamento
    /// </summary>
    public async Task<(bool Success, string Message)> UpdatePricingAsync(
        Guid quoteId,
        decimal? markupPercentage,
        decimal? discountPercentage,
        decimal? laborCost,
        decimal? packagingCost,
        Guid establishmentId,
        Guid employeeId)
    {
        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

        if (quote == null)
            return (false, "Orçamento não encontrado");

        if (quote.Status != "PENDENTE")
            return (false, "Apenas orçamentos pendentes podem ser alterados");

        if (markupPercentage.HasValue)
            quote.MarkupPercentage = markupPercentage.Value;

        if (discountPercentage.HasValue)
            quote.DiscountPercentage = discountPercentage.Value;

        if (laborCost.HasValue)
            quote.LaborCost = laborCost.Value;

        if (packagingCost.HasValue)
            quote.PackagingCost = packagingCost.Value;

        quote.MarkupValue = quote.MaterialsCost * (quote.MarkupPercentage / 100);
        quote.Subtotal = quote.MaterialsCost + quote.MarkupValue + quote.LaborCost + quote.PackagingCost;
        quote.DiscountValue = quote.Subtotal * (quote.DiscountPercentage / 100);
        quote.FinalPrice = quote.Subtotal - quote.DiscountValue;
        quote.UpdatedAt = DateTime.UtcNow;
        quote.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        return (true, "Valores atualizados");
    }

    // ===== MÉTODOS AUXILIARES =====

    private async Task<string> GenerateQuoteCodeAsync(Guid establishmentId)
    {
        var today = DateTime.Today;
        var count = await _context.PrescriptionQuotes
            .CountAsync(q => q.EstablishmentId == establishmentId &&
                            q.CreatedAt.Date == today);

        return $"ORC{today:yyyyMMdd}{(count + 1):D4}";
    }

    private string GeneratePublicToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private decimal ParseQuantity(string quantityText)
    {
        if (string.IsNullOrWhiteSpace(quantityText))
            return 0;

        var numbers = new string(quantityText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        numbers = numbers.Replace(',', '.');

        if (decimal.TryParse(numbers, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        return 0;
    }

    private string ParseUnit(string quantityText)
    {
        if (string.IsNullOrWhiteSpace(quantityText))
            return "un";

        var text = quantityText.ToLower();

        if (text.Contains("capsula") || text.Contains("cápsula") || text.Contains("caps"))
            return "cápsulas";
        if (text.Contains("ml") || text.Contains("mililitro"))
            return "mL";
        if (text.Contains("litro") || text.Contains("l"))
            return "L";
        if (text.Contains("kg") || text.Contains("quilo"))
            return "kg";
        if (text.Contains("mg") || text.Contains("miligrama"))
            return "mg";
        if (text.Contains("g") || text.Contains("grama"))
            return "g";

        return "un";
    }

    private PrescriptionQuoteDto MapToDto(PrescriptionQuote quote, Models.Establishment? establishment)
    {
        var components = new List<PrescriptionQuoteItemDto>();

        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            var comps = JsonSerializer.Deserialize<List<QuoteComponent>>(quote.ComponentsJson);
            if (comps != null)
            {
                components = comps.Select(c => new PrescriptionQuoteItemDto
                {
                    RawMaterialId = c.RawMaterialId,
                    Name = c.Name,
                    DcbCode = c.DcbCode,
                    Quantity = c.Quantity,
                    Unit = c.Unit,
                    UnitCost = c.UnitCost,
                    TotalCost = c.TotalCost,
                    IsQsp = c.IsQsp,
                    IsControlled = c.IsControlled
                }).ToList();
            }
        }

        var address = "";
        if (establishment != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(establishment.Street))
                parts.Add(establishment.Street);
            if (!string.IsNullOrEmpty(establishment.Number))
                parts.Add(establishment.Number);
            if (!string.IsNullOrEmpty(establishment.Neighborhood))
                parts.Add(establishment.Neighborhood);
            if (!string.IsNullOrEmpty(establishment.City))
                parts.Add($"{establishment.City}/{establishment.State}");
            address = string.Join(", ", parts);
        }

        return new PrescriptionQuoteDto
        {
            Id = quote.Id,
            Code = quote.Code,
            PublicToken = quote.PublicToken,
            PublicUrl = $"{_baseUrl}/PrescriptionsView/Quote/{quote.PublicToken}",
            CustomerId = quote.CustomerId,
            CustomerName = quote.CustomerName,
            CustomerPhone = quote.CustomerPhone,
            CustomerEmail = quote.CustomerEmail,
            DoctorName = quote.DoctorName,
            DoctorCrm = quote.DoctorCrm ?? "",
            Usage = quote.UsageType,
            PharmaceuticalForm = quote.PharmaceuticalForm,
            TotalQuantity = quote.TotalQuantity,
            Instructions = quote.Instructions ?? "",
            Components = components,
            MaterialsCost = quote.MaterialsCost,
            MarkupPercentage = quote.MarkupPercentage,
            MarkupValue = quote.MarkupValue,
            LaborCost = quote.LaborCost,
            PackagingCost = quote.PackagingCost,
            Subtotal = quote.Subtotal,
            DiscountPercentage = quote.DiscountPercentage,
            DiscountValue = quote.DiscountValue,
            FinalPrice = quote.FinalPrice,
            EstimatedDays = quote.EstimatedDays,
            ValidUntil = quote.ValidUntil,
            Status = quote.Status,
            CreatedAt = quote.CreatedAt,
            ApprovedAt = quote.ApprovedAt,
            RejectedAt = quote.RejectedAt,
            RejectionReason = quote.RejectionReason,
            ManipulationOrderId = quote.ManipulationOrderId,
            PharmacyName = establishment?.NomeFantasia ?? establishment?.RazaoSocial ?? "",
            PharmacyPhone = establishment?.Phone ?? "",
            PharmacyAddress = address,
            PharmacyLogo = null
        };
    }
}

/// <summary>
/// Estrutura de componente do orçamento (para serialização JSON)
/// </summary>
public class QuoteComponent
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
    public bool IsQsp { get; set; }
}
