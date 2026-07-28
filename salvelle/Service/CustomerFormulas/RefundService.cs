using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;
using Models.Pharmacy;

namespace Service.CustomerFormulas;

public class RefundService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RefundService> _logger;
    // private readonly IPaymentGatewayService _paymentGateway; // TODO: Implementar

    public RefundService(
        AppDbContext context,
        ILogger<RefundService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Criar solicitação de reembolso
    /// </summary>
    public async Task<Refund> CreateRefundAsync(
        Guid customerFormulaId,
        Guid onlineOrderId,
        decimal amount,
        string reason,
        Guid? createdByEmployeeId = null)
    {
        try
        {
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                CustomerFormulaId = customerFormulaId,
                OnlineOrderId = onlineOrderId,
                Amount = amount,
                Reason = reason,
                Status = "PENDENTE",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = createdByEmployeeId
            };

            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Reembolso criado: R$ {Amount} para fórmula {FormulaId}",
                amount, customerFormulaId);

            return refund;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar reembolso");
            throw;
        }
    }

    /// <summary>
    /// Processar reembolso pendente
    /// </summary>
    public async Task<bool> ProcessRefundAsync(Guid refundId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var refund = await _context.Refunds
                .Include(r => r.CustomerFormula)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null)
                return false;

            if (refund.Status != "PENDENTE")
                throw new Exception($"Reembolso não está pendente. Status atual: {refund.Status}");

            // 1. Atualizar status para processando
            refund.Status = "PROCESSADO";
            refund.ProcessedAt = DateTime.UtcNow;

            // 2. Processar reembolso no gateway de pagamento
            // TODO: Implementar integração com gateway real
            var paymentSuccess = await ProcessPaymentGatewayRefundAsync(
                refund.OnlineOrderId,
                refund.Amount
            );

            if (paymentSuccess)
            {
                // 3. Atualizar status para concluído
                refund.Status = "CONCLUIDO";
                refund.CompletedAt = DateTime.UtcNow;

                // 4. Atualizar CustomerFormula
                if (refund.CustomerFormula != null)
                {
                    refund.CustomerFormula.RefundedAt = DateTime.UtcNow;
                    refund.CustomerFormula.RefundAmount = refund.Amount;
                    refund.CustomerFormula.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Reembolso {RefundId} processado com sucesso. Valor: R$ {Amount}",
                    refundId, refund.Amount);

                // 5. Notificar cliente (TODO: Implementar WhatsApp)
                // await _whatsappService.SendRefundCompletedAsync(refund);

                return true;
            }
            else
            {
                // Falha no gateway
                refund.Status = "FALHOU";
                refund.FailureReason = "Falha ao processar reembolso no gateway de pagamento";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogError(
                    "Falha ao processar reembolso {RefundId} no gateway",
                    refundId);

                return false;
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao processar reembolso {RefundId}", refundId);

            // Marcar como falho
            var refund = await _context.Refunds.FindAsync(refundId);
            if (refund != null)
            {
                refund.Status = "FALHOU";
                refund.FailureReason = ex.Message;
                await _context.SaveChangesAsync();
            }

            throw;
        }
    }

    /// <summary>
    /// Processar reembolso via gateway de pagamento
    /// </summary>
    private Task<bool> ProcessPaymentGatewayRefundAsync(
        Guid onlineOrderId,
        decimal amount)
    {
        // Reembolso automatico desabilitado ate integracao com gateway real (Stripe Refunds API)
        // Reembolsos devem ser processados manualmente pelo admin ate a integracao estar pronta
        _logger.LogWarning(
            "Reembolso de R$ {Amount} para pedido {OrderId} requer processamento manual. Integracao com gateway pendente.",
            amount, onlineOrderId);

        throw new NotSupportedException(
            "Reembolso automatico nao disponivel. Processe manualmente via painel do Stripe ou entre em contato com o suporte.");
    }

    /// <summary>
    /// Retentar reembolso que falhou
    /// </summary>
    public async Task<bool> RetryRefundAsync(Guid refundId)
    {
        var refund = await _context.Refunds.FindAsync(refundId);

        if (refund == null)
            return false;

        if (refund.Status != "FALHOU")
            throw new Exception($"Reembolso não está em estado de falha. Status: {refund.Status}");

        // Resetar para pendente
        refund.Status = "PENDENTE";
        refund.FailureReason = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reembolso {RefundId} resetado para nova tentativa", refundId);

        // Processar novamente
        return await ProcessRefundAsync(refundId);
    }

    /// <summary>
    /// Listar reembolsos pendentes
    /// </summary>
    public async Task<List<Refund>> GetPendingRefundsAsync(Guid? establishmentId = null)
    {
        var query = _context.Refunds
            .Include(r => r.CustomerFormula)
            .Where(r => r.Status == "PENDENTE");

        if (establishmentId.HasValue)
        {
            query = query.Where(r => r.CustomerFormula!.EstablishmentId == establishmentId.Value);
        }

        return await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Listar reembolsos falhados
    /// </summary>
    public async Task<List<Refund>> GetFailedRefundsAsync(Guid? establishmentId = null)
    {
        var query = _context.Refunds
            .Include(r => r.CustomerFormula)
            .Where(r => r.Status == "FALHOU");

        if (establishmentId.HasValue)
        {
            query = query.Where(r => r.CustomerFormula!.EstablishmentId == establishmentId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Buscar reembolso por ID
    /// </summary>
    public async Task<Refund?> GetRefundByIdAsync(Guid refundId)
    {
        return await _context.Refunds
            .Include(r => r.CustomerFormula)
                .ThenInclude(cf => cf!.ProductType)
            .Include(r => r.CustomerFormula)
                .ThenInclude(cf => cf!.ProductSubType)
            .FirstOrDefaultAsync(r => r.Id == refundId);
    }

    /// <summary>
    /// Buscar reembolsos de uma fórmula
    /// </summary>
    public async Task<List<Refund>> GetRefundsByFormulaAsync(Guid customerFormulaId)
    {
        return await _context.Refunds
            .Where(r => r.CustomerFormulaId == customerFormulaId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Cancelar reembolso pendente
    /// </summary>
    public async Task<bool> CancelRefundAsync(Guid refundId, string reason)
    {
        var refund = await _context.Refunds.FindAsync(refundId);

        if (refund == null)
            return false;

        if (refund.Status != "PENDENTE")
            throw new Exception($"Apenas reembolsos pendentes podem ser cancelados. Status: {refund.Status}");

        refund.Status = "CANCELADO";
        refund.FailureReason = $"Cancelado: {reason}";
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Reembolso {RefundId} cancelado. Motivo: {Reason}",
            refundId, reason);

        return true;
    }

    /// <summary>
    /// Estatísticas de reembolsos
    /// </summary>
    public async Task<RefundStatistics> GetStatisticsAsync(
        Guid establishmentId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var refunds = await _context.Refunds
            .Include(r => r.CustomerFormula)
            .Where(r => r.CustomerFormula!.EstablishmentId == establishmentId
                     && r.CreatedAt >= startDate
                     && r.CreatedAt <= endDate)
            .ToListAsync();

        var total = refunds.Count;
        var pending = refunds.Count(r => r.Status == "PENDENTE");
        var processed = refunds.Count(r => r.Status == "PROCESSADO");
        var completed = refunds.Count(r => r.Status == "CONCLUIDO");
        var failed = refunds.Count(r => r.Status == "FALHOU");

        var totalAmount = refunds.Sum(r => r.Amount);
        var completedAmount = refunds
            .Where(r => r.Status == "CONCLUIDO")
            .Sum(r => r.Amount);

        var avgProcessingTime = refunds
            .Where(r => r.CompletedAt.HasValue && r.CreatedAt != null)
            .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
            .DefaultIfEmpty(0)
            .Average();

        return new RefundStatistics
        {
            TotalRefunds = total,
            PendingCount = pending,
            ProcessedCount = processed,
            CompletedCount = completed,
            FailedCount = failed,
            TotalAmount = totalAmount,
            CompletedAmount = completedAmount,
            AverageProcessingTimeHours = avgProcessingTime,
            SuccessRate = total > 0 ? (decimal)completed / total : 0
        };
    }

    /// <summary>
    /// Processar todos os reembolsos pendentes em lote
    /// </summary>
    public async Task<BatchRefundResult> ProcessPendingRefundsBatchAsync(Guid establishmentId)
    {
        var pendingRefunds = await GetPendingRefundsAsync(establishmentId);

        var result = new BatchRefundResult
        {
            TotalProcessed = pendingRefunds.Count
        };

        foreach (var refund in pendingRefunds)
        {
            try
            {
                var success = await ProcessRefundAsync(refund.Id);

                if (success)
                    result.SuccessCount++;
                else
                    result.FailedCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                _logger.LogError(ex, "Erro ao processar reembolso {RefundId} em lote", refund.Id);
            }
        }

        _logger.LogInformation(
            "Processamento em lote concluído: {Success} sucessos, {Failed} falhas de {Total} total",
            result.SuccessCount, result.FailedCount, result.TotalProcessed);

        return result;
    }
}

/// <summary>
/// Estatísticas de reembolsos
/// </summary>
public class RefundStatistics
{
    public int TotalRefunds { get; set; }
    public int PendingCount { get; set; }
    public int ProcessedCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CompletedAmount { get; set; }
    public double AverageProcessingTimeHours { get; set; }
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Resultado do processamento em lote
/// </summary>
public class BatchRefundResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}