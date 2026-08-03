using Data;
using DTOs.Payments;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PaymentService _paymentService;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
        _paymentService = new PaymentService(context);
    }

    private Guid GetEstablishmentId()
    {
        var claim = User.FindFirst("EstablishmentId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    // ================================================================
    // PAGAMENTOS PENDENTES
    // ================================================================

    /// <summary>
    /// Lista pagamentos pendentes (PIX e Boleto aguardando confirmação)
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<PendingPaymentDto>>>> GetPendingPayments(
        [FromQuery] string? method,
        [FromQuery] string? status,
        [FromQuery] string? sortBy)
    {
        var establishmentId = GetEstablishmentId();
        var today = DateTime.UtcNow.Date;

        var query = _context.SalePayments
            .Include(p => p.Sale)
                .ThenInclude(s => s!.Customer)
            .Include(p => p.ProcessedByEmployee)
            .Where(p => p.Sale!.EstablishmentId == establishmentId)
            .Where(p => p.PaymentStatus == "PENDING")
            .Where(p => p.PaymentMethod == "PIX" || p.PaymentMethod == "BOLETO")
            .AsQueryable();

        // Filtro por método
        if (!string.IsNullOrEmpty(method))
        {
            query = query.Where(p => p.PaymentMethod == method);
        }

        // Filtro por status (vencido ou não)
        if (status == "OVERDUE")
        {
            query = query.Where(p => p.PaymentMethod == "BOLETO" && p.BoletoDueDate < today);
        }
        else if (status == "PENDING")
        {
            query = query.Where(p => p.PaymentMethod == "PIX" || 
                (p.PaymentMethod == "BOLETO" && (p.BoletoDueDate == null || p.BoletoDueDate >= today)));
        }

        // Ordenação
        query = sortBy switch
        {
            "amount" => query.OrderByDescending(p => p.Amount),
            "dueDate" => query.OrderBy(p => p.BoletoDueDate ?? DateTime.MaxValue),
            _ => query.OrderByDescending(p => p.PaymentDate)
        };

        var payments = await query.Select(p => new PendingPaymentDto
        {
            Id = p.Id,
            SaleId = p.SaleId,
            SaleCode = p.Sale!.Code,
            CustomerName = p.Sale.Customer != null ? p.Sale.Customer.FullName : "Cliente não identificado",
            CustomerId = p.Sale.CustomerId,
            PaymentMethod = p.PaymentMethod,
            Amount = p.Amount,
            PaymentDate = p.PaymentDate,
            PaymentStatus = p.PaymentMethod == "BOLETO" && p.BoletoDueDate < today ? "OVERDUE" : "PENDING",
            PixKey = p.PixKey,
            PixTransactionId = p.PixTransactionId,
            BoletoBarcode = p.BoletoBarcode,
            BoletoNumber = p.BoletoNumber,
            BoletoDueDate = p.BoletoDueDate,
            Observations = p.Observations,
            ProcessedByName = p.ProcessedByEmployee != null ? p.ProcessedByEmployee.FullName : null
        }).ToListAsync();

        return Ok(ApiResponse<List<PendingPaymentDto>>.SuccessResponse(payments));
    }

    /// <summary>
    /// Resumo dos pagamentos pendentes
    /// </summary>
    [HttpGet("pending/summary")]
    public async Task<ActionResult<ApiResponse<PendingPaymentsSummaryDto>>> GetPendingSummary()
    {
        var establishmentId = GetEstablishmentId();
        var today = DateTime.UtcNow.Date;

        var pendingPayments = await _context.SalePayments
            .Include(p => p.Sale)
            .Where(p => p.Sale!.EstablishmentId == establishmentId)
            .Where(p => p.PaymentStatus == "PENDING")
            .Where(p => p.PaymentMethod == "PIX" || p.PaymentMethod == "BOLETO")
            .ToListAsync();

        var pixPayments = pendingPayments.Where(p => p.PaymentMethod == "PIX").ToList();
        var boletoPayments = pendingPayments.Where(p => p.PaymentMethod == "BOLETO").ToList();
        var overduePayments = boletoPayments.Where(p => p.BoletoDueDate < today).ToList();

        var summary = new PendingPaymentsSummaryDto
        {
            TotalPending = pendingPayments.Sum(p => p.Amount),
            TotalCount = pendingPayments.Count,
            PixCount = pixPayments.Count,
            PixAmount = pixPayments.Sum(p => p.Amount),
            BoletoCount = boletoPayments.Count,
            BoletoAmount = boletoPayments.Sum(p => p.Amount),
            OverdueCount = overduePayments.Count,
            OverdueAmount = overduePayments.Sum(p => p.Amount)
        };

        return Ok(ApiResponse<PendingPaymentsSummaryDto>.SuccessResponse(summary));
    }

    /// <summary>
    /// Confirma um pagamento PIX ou Boleto
    /// </summary>
    [HttpPut("{paymentId}/confirm")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> ConfirmPayment(
        Guid paymentId,
        [FromBody] ConfirmPaymentDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        // Escopo de tenant: SalePayment não tem EstablishmentId; escopo via Sale.
        // Sem isto, tenant A confirmava/cancelava pagamento de tenant B (IDOR de escrita).
        var payment = await _context.SalePayments
            .Include(p => p.Sale)
            .FirstOrDefaultAsync(p => p.Id == paymentId
                && p.Sale!.EstablishmentId == establishmentId);

        if (payment == null)
            return NotFound(ApiResponse<PaymentDto>.ErrorResponse("Pagamento não encontrado"));

        if (payment.PaymentStatus != "PENDING")
            return BadRequest(ApiResponse<PaymentDto>.ErrorResponse("Pagamento não está pendente"));

        // Atualizar dados
        payment.PaymentStatus = "APPROVED";
        payment.PaymentDate = dto.ConfirmationDate ?? DateTime.UtcNow;
        payment.ProcessedByEmployeeId = employeeId;

        if (payment.PaymentMethod == "PIX" && !string.IsNullOrEmpty(dto.TransactionId))
        {
            payment.PixTransactionId = dto.TransactionId;
        }

        if (!string.IsNullOrEmpty(dto.Observations))
        {
            payment.Observations = (payment.Observations ?? "") + " | Confirmado: " + dto.Observations;
        }

        // Verificar se a venda está totalmente paga
        var sale = payment.Sale!;
        var totalPaid = await _context.SalePayments
            .Where(p => p.SaleId == sale.Id && (p.PaymentStatus == "APPROVED" || p.Id == paymentId))
            .SumAsync(p => p.Amount);

        if (totalPaid >= sale.TotalAmount)
        {
            sale.Status = "FINALIZADA";
        }

        sale.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var paymentDto = new PaymentDto
        {
            Id = payment.Id,
            PaymentMethod = payment.PaymentMethod,
            Amount = payment.Amount,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate,
            PixTransactionId = payment.PixTransactionId,
            BoletoBarcode = payment.BoletoBarcode,
            Observations = payment.Observations
        };

        return Ok(ApiResponse<PaymentDto>.SuccessResponse(paymentDto, "Pagamento confirmado com sucesso!"));
    }

    /// <summary>
    /// Confirma múltiplos pagamentos de uma vez
    /// </summary>
    [HttpPut("confirm-batch")]
    public async Task<ActionResult<ApiResponse<BatchConfirmResultDto>>> ConfirmBatch(
        [FromBody] BatchConfirmDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();
        var confirmed = 0;
        var errors = new List<string>();

        foreach (var paymentId in dto.PaymentIds)
        {
            var payment = await _context.SalePayments
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.Id == paymentId
                    && p.Sale!.EstablishmentId == establishmentId);

            if (payment == null)
            {
                errors.Add($"Pagamento {paymentId} não encontrado");
                continue;
            }

            if (payment.PaymentStatus != "PENDING")
            {
                errors.Add($"Pagamento {paymentId} não está pendente");
                continue;
            }

            payment.PaymentStatus = "APPROVED";
            payment.PaymentDate = DateTime.UtcNow;
            payment.ProcessedByEmployeeId = employeeId;

            // Verificar se a venda está totalmente paga
            var sale = payment.Sale!;
            var totalPaid = await _context.SalePayments
                .Where(p => p.SaleId == sale.Id && (p.PaymentStatus == "APPROVED" || p.Id == paymentId))
                .SumAsync(p => p.Amount);

            if (totalPaid >= sale.TotalAmount)
            {
                sale.Status = "FINALIZADA";
            }

            sale.UpdatedAt = DateTime.UtcNow;
            confirmed++;
        }

        await _context.SaveChangesAsync();

        var result = new BatchConfirmResultDto
        {
            ConfirmedCount = confirmed,
            ErrorCount = errors.Count,
            Errors = errors
        };

        return Ok(ApiResponse<BatchConfirmResultDto>.SuccessResponse(result, 
            $"{confirmed} pagamento(s) confirmado(s)"));
    }

    /// <summary>
    /// Cancela um pagamento pendente com motivo
    /// </summary>
    [HttpPut("{paymentId}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> CancelPayment(
        Guid paymentId,
        [FromBody] CancelPaymentDto? dto = null)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        var payment = await _context.SalePayments
            .Include(p => p.Sale)
            .FirstOrDefaultAsync(p => p.Id == paymentId
                && p.Sale!.EstablishmentId == establishmentId);

        if (payment == null)
            return NotFound(ApiResponse<string>.ErrorResponse("Pagamento não encontrado"));

        if (payment.PaymentStatus == "CANCELLED")
            return BadRequest(ApiResponse<string>.ErrorResponse("Pagamento já está cancelado"));

        payment.PaymentStatus = "CANCELLED";
        payment.ProcessedByEmployeeId = employeeId;

        var cancelReason = dto?.Reason ?? "Não informado";
        var cancelObs = dto?.Observations ?? "";
        payment.Observations = (payment.Observations ?? "") + 
            $" | CANCELADO em {Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm} - Motivo: {cancelReason}. {cancelObs}";

        // Atualizar status da venda se necessário
        var sale = payment.Sale!;
        var remainingPaid = await _context.SalePayments
            .Where(p => p.SaleId == sale.Id && p.PaymentStatus == "APPROVED")
            .SumAsync(p => p.Amount);

        if (remainingPaid < sale.TotalAmount)
        {
            sale.Status = remainingPaid > 0 ? "PAGAMENTO_PARCIAL" : "PENDENTE";
        }

        sale.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse(null, "Pagamento cancelado com sucesso"));
    }

    /// <summary>
    /// Cancela múltiplos pagamentos de uma vez
    /// </summary>
    [HttpPut("cancel-batch")]
    public async Task<ActionResult<ApiResponse<BatchConfirmResultDto>>> CancelBatch(
        [FromBody] BatchCancelDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();
        var cancelled = 0;
        var errors = new List<string>();

        foreach (var paymentId in dto.PaymentIds)
        {
            var payment = await _context.SalePayments
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.Id == paymentId
                    && p.Sale!.EstablishmentId == establishmentId);

            if (payment == null)
            {
                errors.Add($"Pagamento {paymentId} não encontrado");
                continue;
            }

            if (payment.PaymentStatus == "CANCELLED")
            {
                errors.Add($"Pagamento {paymentId} já está cancelado");
                continue;
            }

            payment.PaymentStatus = "CANCELLED";
            payment.ProcessedByEmployeeId = employeeId;
            payment.Observations = (payment.Observations ?? "") + 
                $" | CANCELADO em lote em {Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm} - Motivo: {dto.Reason}";

            var sale = payment.Sale!;
            sale.UpdatedAt = DateTime.UtcNow;
            cancelled++;
        }

        await _context.SaveChangesAsync();

        var result = new BatchConfirmResultDto
        {
            ConfirmedCount = cancelled,
            ErrorCount = errors.Count,
            Errors = errors
        };

        return Ok(ApiResponse<BatchConfirmResultDto>.SuccessResponse(result, 
            $"{cancelled} pagamento(s) cancelado(s)"));
    }

    // ================================================================
    // ENDPOINTS EXISTENTES
    // ================================================================

    /// <summary>
    /// Adiciona um pagamento a uma venda
    /// </summary>
    [HttpPost("sales/{saleId}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> AddPayment(
        Guid saleId,
        [FromBody] CreatePaymentDto dto)
    {
        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        var result = await _paymentService.AddPaymentToSaleAsync(saleId, dto, employeeId, establishmentId);

        if (!result.Success || result.Payment == null)
            return BadRequest(ApiResponse<PaymentDto>.ErrorResponse(result.Message));

        var paymentDto = new PaymentDto
        {
            Id = result.Payment.Id,
            PaymentMethod = result.Payment.PaymentMethod,
            Amount = result.Payment.Amount,
            PaymentStatus = result.Payment.PaymentStatus,
            PaymentDate = result.Payment.PaymentDate,
            CashReceived = result.Payment.CashReceived,
            ChangeAmount = result.Payment.ChangeAmount,
            CardBrand = result.Payment.CardBrand,
            CardLastDigits = result.Payment.CardLastDigits,
            Installments = result.Payment.Installments,
            Nsu = result.Payment.Nsu,
            AuthorizationCode = result.Payment.AuthorizationCode,
            PixKey = result.Payment.PixKey,
            PixTransactionId = result.Payment.PixTransactionId,
            BoletoBarcode = result.Payment.BoletoBarcode,
            BoletoDueDate = result.Payment.BoletoDueDate,
            Observations = result.Payment.Observations
        };

        return Ok(ApiResponse<PaymentDto>.SuccessResponse(paymentDto, result.Message));
    }

    /// <summary>
    /// Lista todos os pagamentos de uma venda
    /// </summary>
    [HttpGet("sales/{saleId}")]
    public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetSalePayments(Guid saleId)
    {
        var payments = await _paymentService.GetSalePaymentsAsync(saleId);
        return Ok(ApiResponse<List<PaymentDto>>.SuccessResponse(payments));
    }

    /// <summary>
    /// Busca venda com todos os pagamentos
    /// </summary>
    [HttpGet("sales/{saleId}/details")]
    public async Task<ActionResult<ApiResponse<SaleWithPaymentsDto>>> GetSaleWithPayments(Guid saleId)
    {
        var sale = await _paymentService.GetSaleWithPaymentsAsync(saleId);

        if (sale == null)
            return NotFound(ApiResponse<SaleWithPaymentsDto>.ErrorResponse("Venda não encontrada"));

        return Ok(ApiResponse<SaleWithPaymentsDto>.SuccessResponse(sale));
    }

    /// <summary>
    /// Fluxo de caixa diário
    /// </summary>
    [HttpGet("daily-cash-flow")]
    public async Task<ActionResult<ApiResponse<DailyCashFlowDto>>> GetDailyCashFlow(
        [FromQuery] DateTime? date)
    {
        var establishmentId = GetEstablishmentId();
        var targetDate = date ?? DateTime.Today;

        var cashFlow = await _paymentService.GetDailyCashFlowAsync(establishmentId, targetDate);

        return Ok(ApiResponse<DailyCashFlowDto>.SuccessResponse(cashFlow));
    }
}

// ================================================================
// DTOs para Pagamentos Pendentes
// ================================================================

public class PendingPaymentDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string SaleCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PixKey { get; set; }
    public string? PixTransactionId { get; set; }
    public string? BoletoBarcode { get; set; }
    public string? BoletoNumber { get; set; }
    public DateTime? BoletoDueDate { get; set; }
    public string? Observations { get; set; }
    public string? ProcessedByName { get; set; }
}

public class PendingPaymentsSummaryDto
{
    public decimal TotalPending { get; set; }
    public int TotalCount { get; set; }
    public int PixCount { get; set; }
    public decimal PixAmount { get; set; }
    public int BoletoCount { get; set; }
    public decimal BoletoAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
}

public class ConfirmPaymentDto
{
    public string? TransactionId { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public string? Observations { get; set; }
}

public class CancelPaymentDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

public class BatchConfirmDto
{
    public List<Guid> PaymentIds { get; set; } = new();
}

public class BatchCancelDto
{
    public List<Guid> PaymentIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

public class BatchConfirmResultDto
{
    public int ConfirmedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
