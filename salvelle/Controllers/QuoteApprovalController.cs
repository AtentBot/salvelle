using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Pharmacy;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class QuoteApprovalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<QuoteApprovalController> _logger;

    public QuoteApprovalController(AppDbContext context, ILogger<QuoteApprovalController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetEmployeeId() => 
        HttpContext.Items["EmployeeId"] is Guid id ? id : 
        (HttpContext.Items["Employee"] is Models.Employees.Employee emp ? emp.Id : Guid.Empty);
    
    private Guid GetEstablishmentId() => 
        HttpContext.Items["EstablishmentId"] is Guid id ? id : 
        (HttpContext.Items["Employee"] is Models.Employees.Employee emp ? emp.EstablishmentId : Guid.Empty);

    [HttpGet("pending")]
    public async Task<ActionResult<QuoteApiResponse<List<PendingOrderDto>>>> GetPending()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<List<PendingOrderDto>>.Error("Sessão inválida"));

        var validStatuses = new[] { "PENDENTE", "ORCAMENTO", "AGUARDANDO_APROVACAO", "AGUARDANDO_PRODUCAO" };

        var orders = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId && validStatuses.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PendingOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName ?? "Não identificado",
                FormulaName = o.Formula != null ? o.Formula.Name : "Fórmula Livre",
                Quantity = o.QuantityToProduce,
                Unit = o.Unit,
                Status = o.Status,
                StatusDisplay = GetStatusDisplay(o.Status),
                CreatedAt = o.CreatedAt,
                Priority = o.Priority ?? "NORMAL"
            })
            .ToListAsync();

        return Ok(QuoteApiResponse<List<PendingOrderDto>>.Success(orders, $"{orders.Count} ordem(ns) pendente(s)"));
    }

    [HttpGet("in-production")]
    public async Task<ActionResult<QuoteApiResponse<List<PendingOrderDto>>>> GetInProduction()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<List<PendingOrderDto>>.Error("Sessão inválida"));

        var orders = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId && o.Status == "EM_PRODUCAO")
            .OrderByDescending(o => o.StartDate ?? o.CreatedAt)
            .Select(o => new PendingOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName ?? "Não identificado",
                FormulaName = o.Formula != null ? o.Formula.Name : "Fórmula Livre",
                Quantity = o.QuantityToProduce,
                Unit = o.Unit,
                Status = o.Status,
                StatusDisplay = "Em Produção",
                CreatedAt = o.CreatedAt,
                Priority = o.Priority ?? "NORMAL"
            })
            .ToListAsync();

        return Ok(QuoteApiResponse<List<PendingOrderDto>>.Success(orders));
    }

    [HttpPost("manipulation-orders/{id}/calculate")]
    public async Task<ActionResult<QuoteApiResponse<QuotePreviewDto>>> Calculate(
        Guid id, 
        [FromBody] CalculateQuoteDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<QuotePreviewDto>.Error("Sessão inválida"));

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(QuoteApiResponse<QuotePreviewDto>.Error("Ordem não encontrada"));

        decimal totalMaterialsCost = 0;
        var components = new List<ManipulationComponentDto>();

        if (order.Formula?.Components != null)
        {
            foreach (var comp in order.Formula.Components)
            {
                decimal unitCost = 0;
                string priceSource = "BASE";
                int confidence = 30;

                var stockBatch = await _context.Batches
                    .Where(b => b.RawMaterialId == comp.RawMaterialId && 
                               b.Status == "APROVADO" &&
                               b.CurrentQuantity > 0 &&
                               b.ExpiryDate > DateTime.UtcNow)
                    .OrderBy(b => b.ExpiryDate)
                    .FirstOrDefaultAsync();

                if (stockBatch != null && stockBatch.UnitCost > 0)
                {
                    unitCost = stockBatch.UnitCost;
                    priceSource = "ESTOQUE";
                    confidence = 100;
                }
                else if (comp.RawMaterial != null && comp.RawMaterial.LastKnownPrice > 0)
                {
                    unitCost = comp.RawMaterial.LastKnownPrice ?? 0;
                    priceSource = "HISTORICO";
                    confidence = 70;
                }
                else if (comp.RawMaterial != null && comp.RawMaterial.BasePrice > 0)
                {
                    unitCost = comp.RawMaterial.BasePrice ?? 0;
                    priceSource = "BASE";
                    confidence = 30;
                }
                else
                {
                    unitCost = 0.50m;
                    priceSource = "PADRAO";
                    confidence = 10;
                }

                var quantity = comp.Quantity * (order.QuantityToProduce / 100m);
                var totalCost = quantity * unitCost;
                totalMaterialsCost += totalCost;

                components.Add(new ManipulationComponentDto
                {
                    RawMaterialId = comp.RawMaterialId,
                    Name = comp.RawMaterial?.Name ?? "Componente",
                    Quantity = quantity,
                    Unit = comp.Unit,
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    PriceSource = priceSource,
                    Confidence = confidence
                });
            }
        }

        decimal markup = dto.MarkupPercentage ?? 100m;
        decimal markupValue = totalMaterialsCost * (markup / 100m);
        decimal laborCost = dto.LaborCost ?? 25m;
        decimal packagingCost = dto.PackagingCost ?? 5m;
        decimal subtotal = totalMaterialsCost + markupValue + laborCost + packagingCost;
        decimal discount = dto.Discount ?? 0m;
        decimal total = subtotal - discount;

        int avgConfidence = components.Count > 0 
            ? (int)components.Average(c => c.Confidence) 
            : 50;

        return Ok(QuoteApiResponse<QuotePreviewDto>.Success(new QuotePreviewDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            FormulaName = order.Formula?.Name ?? "Fórmula Livre",
            Quantity = order.QuantityToProduce,
            Unit = order.Unit,
            Components = components,
            MaterialsCost = totalMaterialsCost,
            MarkupPercentage = markup,
            MarkupValue = markupValue,
            LaborCost = laborCost,
            PackagingCost = packagingCost,
            Subtotal = subtotal,
            Discount = discount,
            Total = total,
            AverageConfidence = avgConfidence,
            ValidUntil = DateTime.UtcNow.AddDays(7),
            EstimatedDelivery = "3 dias úteis"
        }));
    }

    [HttpPost("manipulation-orders/{id}/approve-and-sell")]
    public async Task<ActionResult<QuoteApiResponse<ApproveAndSellResultDto>>> ApproveAndSell(
        Guid id, 
        [FromBody] ApproveAndSellDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (employeeId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<ApproveAndSellResultDto>.Error("Sessão inválida"));

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.ManipulationOrders
                .Include(o => o.Formula)
                    .ThenInclude(f => f!.Components)
                        .ThenInclude(c => c.RawMaterial)
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(QuoteApiResponse<ApproveAndSellResultDto>.Error("Ordem não encontrada"));

            var validStatuses = new[] { "PENDENTE", "ORCAMENTO", "AGUARDANDO_APROVACAO", "AGUARDANDO_PRODUCAO" };
            if (!validStatuses.Contains(order.Status))
                return BadRequest(QuoteApiResponse<ApproveAndSellResultDto>.Error(
                    $"Status inválido: {order.Status}"));

            decimal materialsCost = CalculateMaterialsCost(order);
            decimal markup = dto.MarkupPercentage ?? 100m;
            decimal markupValue = materialsCost * (markup / 100m);
            decimal laborCost = dto.LaborCost ?? 25m;
            decimal subtotal = materialsCost + markupValue + laborCost;
            decimal discount = dto.Discount ?? 0m;
            decimal total = subtotal - discount;

            if (total <= 0)
                return BadRequest(QuoteApiResponse<ApproveAndSellResultDto>.Error("Valor total inválido"));

            if (dto.AmountPaid < total && dto.PaymentMethod != "BOLETO" && dto.Installments <= 1)
                return BadRequest(QuoteApiResponse<ApproveAndSellResultDto>.Error(
                    $"Valor insuficiente: R$ {dto.AmountPaid:N2} < R$ {total:N2}"));

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && 
                                         c.FullName == order.CustomerName);

            var saleCode = await GenerateSaleCode(establishmentId);
            decimal cashReceived = dto.CashReceived ?? dto.AmountPaid;
            decimal changeAmount = dto.PaymentMethod == "DINHEIRO" ? Math.Max(0, cashReceived - total) : 0;
            
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = saleCode,
                CustomerId = customer?.Id,
                SaleDate = DateTime.UtcNow,
                Status = "PENDENTE",
                Subtotal = subtotal,
                TotalAmount = total,
                DiscountAmount = discount,
                DiscountPercentage = subtotal > 0 ? (discount / subtotal) * 100 : 0,
                PaymentMethod = dto.PaymentMethod,
                PaidAmount = dto.AmountPaid,
                ChangeAmount = changeAmount,
                Observations = dto.Notes,
                CreatedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);

            bool hasControlled = false;
            if (order.Formula?.Components != null)
            {
                hasControlled = order.Formula.Components.Any(c => 
                    c.RawMaterial != null && 
                    c.RawMaterial.ControlType != null && 
                    c.RawMaterial.ControlType != "COMUM");
            }
            
            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                ManipulationOrderId = order.Id,
                FormulaId = order.FormulaId,
                Description = $"Manipulação: {order.Formula?.Name ?? "Fórmula Livre"} - {order.QuantityToProduce} {order.Unit}",
                Quantity = 1,
                UnitPrice = total,
                TotalPrice = total,
                CostPrice = materialsCost + laborCost,
                ProfitMargin = markupValue - discount,
                ControlType = hasControlled ? "CONTROLADO" : null,
                PrescriptionNumber = order.PrescriptionNumber
            };

            _context.SaleItems.Add(saleItem);

            string paymentStatus = dto.PaymentMethod == "BOLETO" ? "PENDING" : "APPROVED";
            
            var payment = new SalePayment
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                PaymentMethod = dto.PaymentMethod,
                Amount = dto.AmountPaid,
                PaymentStatus = paymentStatus,
                PaymentDate = DateTime.UtcNow,
                ProcessedByEmployeeId = employeeId,
                CashReceived = dto.PaymentMethod == "DINHEIRO" ? cashReceived : (decimal?)null,
                ChangeAmount = dto.PaymentMethod == "DINHEIRO" ? changeAmount : (decimal?)null,
                CardBrand = dto.CardBrand,
                CardLastDigits = dto.CardLastDigits,
                Installments = dto.Installments > 0 ? dto.Installments : 1,
                Nsu = dto.Nsu,
                AuthorizationCode = dto.AuthorizationCode,
                PixKey = dto.PixKey,
                PixTransactionId = dto.PixTransactionId,
                BoletoBarcode = dto.BoletoBarcode,
                BoletoDueDate = dto.BoletoDueDate ?? DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SalePayments.Add(payment);

            if (paymentStatus == "APPROVED")
            {
                sale.Status = "FINALIZADA";
                sale.PaymentStatus = "PAGO";
            }

            order.Status = "EM_PRODUCAO";
            order.ApprovedByPharmacistId = employeeId;
            order.UpdatedAt = DateTime.UtcNow;

            if (!order.StartDate.HasValue)
                order.StartDate = DateTime.UtcNow;

            var openCashRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(cr => cr.EstablishmentId == establishmentId && cr.Status == "ABERTO");

            if (openCashRegister != null && paymentStatus == "APPROVED")
            {
                var cashMovement = new CashMovement
                {
                    Id = Guid.NewGuid(),
                    CashRegisterId = openCashRegister.Id,
                    MovementType = "ENTRADA",
                    Amount = dto.AmountPaid,
                    PaymentMethod = dto.PaymentMethod,
                    Description = $"Venda {saleCode} - Manipulação {order.OrderNumber}",
                    SaleId = sale.Id,
                    EmployeeId = employeeId,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CashMovements.Add(cashMovement);

                openCashRegister.SalesCount++;
                openCashRegister.TotalSales += dto.AmountPaid;

                if (dto.PaymentMethod == "DINHEIRO")
                    openCashRegister.TotalCash += dto.AmountPaid;
                else if (dto.PaymentMethod == "CARTAO_CREDITO" || dto.PaymentMethod == "CARTAO_DEBITO")
                    openCashRegister.TotalCard += dto.AmountPaid;
                else if (dto.PaymentMethod == "PIX")
                    openCashRegister.TotalPix += dto.AmountPaid;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(QuoteApiResponse<ApproveAndSellResultDto>.Success(new ApproveAndSellResultDto
            {
                SaleId = sale.Id,
                SaleCode = saleCode,
                ManipulationOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Total = total,
                AmountPaid = dto.AmountPaid,
                ChangeAmount = changeAmount,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = paymentStatus,
                OrderStatus = order.Status,
                Message = "Venda realizada com sucesso!"
            }));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao aprovar ordem {OrderId}", id);
            return StatusCode(500, QuoteApiResponse<ApproveAndSellResultDto>.Error("Erro interno"));
        }
    }

    [HttpPost("manipulation-orders/{id}/approve-for-billing")]
    public async Task<ActionResult<QuoteApiResponse<ApproveResultDto>>> ApproveForBilling(
        Guid id,
        [FromBody] ApproveForBillingDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (employeeId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<ApproveResultDto>.Error("Sessão inválida"));

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(QuoteApiResponse<ApproveResultDto>.Error("Ordem não encontrada"));

        decimal materialsCost = CalculateMaterialsCost(order);
        decimal markup = dto.MarkupPercentage ?? 100m;
        decimal markupValue = materialsCost * (markup / 100m);
        decimal laborCost = dto.LaborCost ?? 25m;
        decimal subtotal = materialsCost + markupValue + laborCost;
        decimal discount = dto.Discount ?? 0m;
        decimal total = subtotal - discount;

        order.Status = "AGUARDANDO_PRODUCAO";
        order.ApprovedByPharmacistId = employeeId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(QuoteApiResponse<ApproveResultDto>.Success(new ApproveResultDto
        {
            ManipulationOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            FinalPrice = total,
            Status = order.Status,
            Message = "Orçamento aprovado. Aguardando pagamento."
        }));
    }

    [HttpPost("manipulation-orders/{id}/reject")]
    public async Task<ActionResult<QuoteApiResponse<ApproveResultDto>>> Reject(
        Guid id,
        [FromBody] RejectManipulationOrderDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (employeeId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<ApproveResultDto>.Error("Sessão inválida"));

        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(QuoteApiResponse<ApproveResultDto>.Error("Ordem não encontrada"));

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(QuoteApiResponse<ApproveResultDto>.Error("Motivo obrigatório"));

        order.Status = "CANCELADO";
        string existingNotes = order.SpecialInstructions ?? "";
        order.SpecialInstructions = "[REJEITADO] " + dto.Reason + 
            (string.IsNullOrEmpty(existingNotes) ? "" : "\n" + existingNotes);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(QuoteApiResponse<ApproveResultDto>.Success(new ApproveResultDto
        {
            ManipulationOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            FinalPrice = 0,
            Status = order.Status,
            Message = "Orçamento rejeitado."
        }));
    }

    /// <summary>
    /// Estatísticas de ordens de manipulação
    /// GET /api/QuoteApproval/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<QuoteApiResponse<ManipulationOrderStatsDto>>> GetStats([FromQuery] int days = 30)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(QuoteApiResponse<ManipulationOrderStatsDto>.Error("Sessão inválida"));

        var startDate = DateTime.UtcNow.AddDays(-days);

        var orders = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= startDate)
            .ToListAsync();

        int totalOrders = orders.Count;
        int converted = orders.Count(o => o.Status == "EM_PRODUCAO" || o.Status == "FINALIZADO");
        int pending = orders.Count(o => new[] { "PENDENTE", "ORCAMENTO", "AGUARDANDO_APROVACAO", "AGUARDANDO_PRODUCAO" }.Contains(o.Status));
        int rejected = orders.Count(o => o.Status == "CANCELADO");

        return Ok(QuoteApiResponse<ManipulationOrderStatsDto>.Success(new ManipulationOrderStatsDto
        {
            Period = "Últimos " + days + " dias",
            TotalOrders = totalOrders,
            ConvertedOrders = converted,
            PendingOrders = pending,
            RejectedOrders = rejected,
            ConversionRate = totalOrders > 0 ? (decimal)converted / totalOrders * 100 : 0
        }));
    }

    private static string GetStatusDisplay(string status)
    {
        return status switch
        {
            "PENDENTE" => "Pendente",
            "ORCAMENTO" => "Orçamento",
            "AGUARDANDO_APROVACAO" => "Aguardando Aprovação",
            "AGUARDANDO_PRODUCAO" => "Aguardando Produção",
            "EM_PRODUCAO" => "Em Produção",
            "FINALIZADO" => "Finalizado",
            "CANCELADO" => "Cancelado",
            _ => status
        };
    }

    private decimal CalculateMaterialsCost(ManipulationOrder order)
    {
        decimal total = 0;
        
        if (order.Formula?.Components != null)
        {
            foreach (var comp in order.Formula.Components)
            {
                decimal unitCost = comp.RawMaterial?.LastKnownPrice ?? 
                                   comp.RawMaterial?.BasePrice ?? 0.50m;
                decimal quantity = comp.Quantity * (order.QuantityToProduce / 100m);
                total += quantity * unitCost;
            }
        }
        
        return total > 0 ? total : 10m;
    }

    private async Task<string> GenerateSaleCode(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var prefix = "V" + today.ToString("yyMMdd");
        
        var lastSale = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.Code.StartsWith(prefix))
            .OrderByDescending(s => s.Code)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastSale != null && lastSale.Code.Length > prefix.Length)
        {
            if (int.TryParse(lastSale.Code.Substring(prefix.Length), out int lastSeq))
                sequence = lastSeq + 1;
        }

        return prefix + sequence.ToString("D4");
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs - QuoteApprovalController
// ═══════════════════════════════════════════════════════════════════════════════

public class PendingOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string FormulaName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Priority { get; set; } = "NORMAL";
}

public class RejectManipulationOrderDto
{
    public string Reason { get; set; } = "";
}

/// <summary>
/// Estatísticas de ordens de manipulação (renomeado de QuoteStatsDto para evitar conflito)
/// </summary>
public class ManipulationOrderStatsDto
{
    public string Period { get; set; } = "";
    public int TotalOrders { get; set; }
    public int ConvertedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal ConversionRate { get; set; }
}

public class QuoteApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public static QuoteApiResponse<T> Success(T data, string? message = null) => new()
    { IsSuccess = true, Data = data, Message = message };

    public static QuoteApiResponse<T> Error(string message) => new()
    { IsSuccess = false, ErrorMessage = message };
}

public class ApproveAndSellDto
{
    public string PaymentMethod { get; set; } = "DINHEIRO";
    public decimal AmountPaid { get; set; }
    public decimal? CashReceived { get; set; }
    public int Installments { get; set; } = 1;
    public decimal? Discount { get; set; }
    public string? DiscountReason { get; set; }
    public decimal? MarkupPercentage { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Notes { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastDigits { get; set; }
    public string? Nsu { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? PixKey { get; set; }
    public string? PixTransactionId { get; set; }
    public string? BoletoBarcode { get; set; }
    public DateTime? BoletoDueDate { get; set; }
}

public class ApproveAndSellResultDto
{
    public Guid SaleId { get; set; }
    public string SaleCode { get; set; } = "";
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public string Message { get; set; } = "";
}

public class ApproveForBillingDto
{
    public decimal? MarkupPercentage { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
}

public class ApproveResultDto
{
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal FinalPrice { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public class CalculateQuoteDto
{
    public decimal? MarkupPercentage { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PackagingCost { get; set; }
    public decimal? Discount { get; set; }
}

public class QuotePreviewDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string FormulaName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public List<ManipulationComponentDto> Components { get; set; } = new();
    public decimal MaterialsCost { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal MarkupValue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int AverageConfidence { get; set; }
    public DateTime ValidUntil { get; set; }
    public string EstimatedDelivery { get; set; } = "";
}

/// <summary>
/// Componente de manipulação (renomeado de QuoteComponentDto para evitar conflito)
/// </summary>
public class ManipulationComponentDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string PriceSource { get; set; } = "";
    public int Confidence { get; set; }
}
