using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;
using Models.Pharmacy;
using DTOs.CustomerFormulas;

namespace Service.CustomerFormulas;

public class PharmaceuticalAnalysisService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PharmaceuticalAnalysisService> _logger;

    public PharmaceuticalAnalysisService(
        AppDbContext context,
        ILogger<PharmaceuticalAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CustomerFormula>> GetPendingFormulasAsync(Guid establishmentId)
    {
        return await _context.CustomerFormulas
            .Include(cf => cf.ProductType)
            .Include(cf => cf.ProductSubType)
            .Where(cf => cf.EstablishmentId == establishmentId
                      && (cf.Status == "AGUARDANDO_ANALISE" || cf.Status == "EM_ANALISE"))
            .OrderBy(cf => cf.PaidAt)
            .ToListAsync();
    }

    public async Task<bool> StartAnalysisAsync(Guid formulaId, Guid pharmacistId)
    {
        var formula = await _context.CustomerFormulas.FindAsync(formulaId);

        if (formula == null || formula.Status != "AGUARDANDO_ANALISE")
            return false;

        formula.Status = "EM_ANALISE";
        formula.PharmacistId = pharmacistId;
        formula.UpdatedAt = DateTime.UtcNow;

        var log = new PharmaceuticalAnalysisLog
        {
            Id = Guid.NewGuid(),
            CustomerFormulaId = formulaId,
            PharmacistId = pharmacistId,
            ActionType = "STARTED",
            Analysis = "Análise iniciada",
            CreatedAt = DateTime.UtcNow
        };

        _context.PharmaceuticalAnalysisLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Análise iniciada para fórmula {Code} pelo farmacêutico {PharmacistId}",
            formula.Code, pharmacistId);

        return true;
    }

    public async Task<bool> ApproveFormulaAsync(
        Guid formulaId,
        Guid pharmacistId,
        PharmaceuticalAnalysisDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var formula = await _context.CustomerFormulas
                .Include(cf => cf.ProductSubType)
                .FirstOrDefaultAsync(cf => cf.Id == formulaId);

            if (formula == null)
                return false;

            if (formula.Status != "EM_ANALISE" && formula.Status != "AGUARDANDO_ANALISE")
                throw new Exception($"Fórmula não pode ser aprovada. Status atual: {formula.Status}");

            // 1. Atualizar fórmula
            formula.Status = "APROVADO";
            formula.PharmacistId = pharmacistId;
            formula.PharmaceuticalAnalysis = dto.Analysis;
            formula.ApprovedAt = DateTime.UtcNow;
            formula.RequiresPrescription = dto.RequiresPrescription;
            formula.EstimatedShelfLifeDays = dto.EstimatedShelfLifeDays;
            formula.UpdatedAt = DateTime.UtcNow;

            // 2. Criar ManipulationOrder automaticamente
            var manipOrder = new Models.Pharmacy.ManipulationOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = await GenerateManipulationCodeAsync(formula.EstablishmentId),
                EstablishmentId = formula.EstablishmentId,
                // CustomerId = formula.CustomerId, // Ajustar conforme modelo real
                // CustomerFormulaId = formula.Id,
                Status = "PENDING",
                QuantityToProduce = formula.Quantity,
                Unit = formula.Unit,
                CustomerName = formula.CustomerName,
                OrderDate = DateTime.UtcNow,
                ExpectedDate = DateTime.UtcNow.AddDays(7),
                RequestedByEmployeeId = pharmacistId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationOrders.Add(manipOrder);

            formula.ManipulationOrderId = manipOrder.Id;
            formula.Status = "EM_PRODUCAO";

            // 3. TODO: Atualizar OnlineOrderItem quando sistema de pedidos estiver integrado
            // if (formula.OnlineOrderId.HasValue)
            // {
            //     // Ajustar conforme modelo real OnlineOrderItem
            // }

            // 4. Criar log de aprovação
            var log = new PharmaceuticalAnalysisLog
            {
                Id = Guid.NewGuid(),
                CustomerFormulaId = formulaId,
                PharmacistId = pharmacistId,
                ActionType = "APPROVED",
                Analysis = dto.Analysis,
                CreatedAt = DateTime.UtcNow
            };

            _context.PharmaceuticalAnalysisLogs.Add(log);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Fórmula {Code} aprovada pelo farmacêutico {PharmacistId}",
                formula.Code, pharmacistId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao aprovar fórmula {FormulaId}", formulaId);
            throw;
        }
    }

    public async Task<bool> RejectFormulaAsync(
        Guid formulaId,
        Guid pharmacistId,
        string rejectionReason)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var formula = await _context.CustomerFormulas.FindAsync(formulaId);

            if (formula == null)
                return false;

            if (formula.Status != "EM_ANALISE" && formula.Status != "AGUARDANDO_ANALISE")
                throw new Exception($"Fórmula não pode ser reprovada. Status atual: {formula.Status}");

            formula.Status = "REPROVADO";
            formula.PharmacistId = pharmacistId;
            formula.RejectedAt = DateTime.UtcNow;
            formula.RejectionReason = rejectionReason;
            formula.UpdatedAt = DateTime.UtcNow;

            // Criar reembolso automaticamente (será processado depois)
            // TODO: Implementar RefundService quando disponível
            // var refund = await _refundService.CreateRefundAsync(formula.Id, formula.PaidAmount ?? 0);

            var log = new PharmaceuticalAnalysisLog
            {
                Id = Guid.NewGuid(),
                CustomerFormulaId = formulaId,
                PharmacistId = pharmacistId,
                ActionType = "REJECTED",
                Analysis = rejectionReason,
                CreatedAt = DateTime.UtcNow
            };

            _context.PharmaceuticalAnalysisLogs.Add(log);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Fórmula {Code} reprovada pelo farmacêutico {PharmacistId}. Motivo: {Reason}",
                formula.Code, pharmacistId, rejectionReason);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao reprovar fórmula {FormulaId}", formulaId);
            throw;
        }
    }

    public async Task<bool> RequestAdjustmentAsync(
        Guid formulaId,
        Guid pharmacistId,
        string adjustmentReason)
    {
        var formula = await _context.CustomerFormulas.FindAsync(formulaId);

        if (formula == null)
            return false;

        if (formula.Status != "EM_ANALISE" && formula.Status != "AGUARDANDO_ANALISE")
            throw new Exception($"Não é possível solicitar ajuste. Status atual: {formula.Status}");

        formula.Status = "AJUSTE_NECESSARIO";
        formula.PharmacistId = pharmacistId;
        formula.PharmaceuticalAnalysis = adjustmentReason;
        formula.UpdatedAt = DateTime.UtcNow;

        var log = new PharmaceuticalAnalysisLog
        {
            Id = Guid.NewGuid(),
            CustomerFormulaId = formulaId,
            PharmacistId = pharmacistId,
            ActionType = "ADJUSTMENT_REQUESTED",
            Analysis = adjustmentReason,
            CreatedAt = DateTime.UtcNow
        };

        _context.PharmaceuticalAnalysisLogs.Add(log);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Ajuste solicitado para fórmula {Code} pelo farmacêutico {PharmacistId}",
            formula.Code, pharmacistId);

        return true;
    }

    public async Task<List<PharmaceuticalAnalysisLog>> GetAnalysisHistoryAsync(Guid formulaId)
    {
        return await _context.PharmaceuticalAnalysisLogs
            .Where(log => log.CustomerFormulaId == formulaId)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<AnalysisStatistics> GetStatisticsAsync(
        Guid establishmentId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.CustomerFormulas
            .Where(cf => cf.EstablishmentId == establishmentId);

        if (startDate.HasValue)
            query = query.Where(cf => cf.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(cf => cf.CreatedAt <= endDate.Value);

        var formulas = await query.ToListAsync();

        var total = formulas.Count;
        var approved = formulas.Count(f => f.Status == "APROVADO" || f.Status == "EM_PRODUCAO" || f.Status == "FINALIZADO");
        var rejected = formulas.Count(f => f.Status == "REPROVADO");
        var pending = formulas.Count(f => f.Status == "AGUARDANDO_ANALISE" || f.Status == "EM_ANALISE");

        var avgAnalysisTime = formulas
            .Where(f => f.ApprovedAt.HasValue || f.RejectedAt.HasValue)
            .Select(f => {
                var endTime = f.ApprovedAt ?? f.RejectedAt!.Value;
                return (endTime - f.PaidAt!.Value).TotalHours;
            })
            .DefaultIfEmpty(0)
            .Average();

        return new AnalysisStatistics
        {
            TotalFormulas = total,
            ApprovedCount = approved,
            RejectedCount = rejected,
            PendingCount = pending,
            ApprovalRate = total > 0 ? (decimal)approved / total : 0,
            RejectionRate = total > 0 ? (decimal)rejected / total : 0,
            AverageAnalysisTimeHours = avgAnalysisTime
        };
    }

    private async Task<string> GenerateManipulationCodeAsync(Guid establishmentId)
    {
        var today = DateTime.Today;
        var prefix = $"MAN-{today:yyyyMMdd}";

        // Serializa a geração do número por estabelecimento para evitar corrida
        // (dois approves simultâneos lendo o mesmo max e gerando o mesmo sufixo).
        // Lock transacional: liberado automaticamente no commit/rollback da
        // transação aberta em ApproveFormulaAsync.
        await _context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({establishmentId.ToString()}, 0))");

        var lastOrder = await _context.ManipulationOrders
            .Where(mo => mo.EstablishmentId == establishmentId
                      && mo.OrderNumber.StartsWith(prefix))
            .OrderByDescending(mo => mo.OrderNumber)
            .FirstOrDefaultAsync();

        if (lastOrder == null)
        {
            return $"{prefix}-0001";
        }

        // Sufixo pode não ser numérico em dados legados — trata com TryParse.
        var lastNumber = int.TryParse(lastOrder.OrderNumber.Split('-').Last(), out var n) ? n : 0;
        return $"{prefix}-{(lastNumber + 1):D4}";
    }
}

public class AnalysisStatistics
{
    public int TotalFormulas { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal RejectionRate { get; set; }
    public double AverageAnalysisTimeHours { get; set; }
}