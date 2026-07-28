using Data;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;

namespace Service;

/// <summary>
/// Serviço para lógica de negócio de manipulação farmacêutica
/// </summary>
public class ManipulationService
{
    private readonly AppDbContext _context;

    public ManipulationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gera número de lote para manipulação no formato: LM-YYYYMMDD-NNNN
    /// </summary>
    public string GenerateBatchNumber(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var prefix = $"LM-{today:yyyyMMdd}-";
        
        // Conta quantos lotes foram gerados hoje
        var countToday = _context.ManipulationOrders
            .Count(o => o.EstablishmentId == establishmentId && 
                       o.StartDate.HasValue &&
                       o.StartDate.Value.Date == today.Date);
        
        return $"{prefix}{(countToday + 1):D4}";
    }

    /// <summary>
    /// Calcula data de validade baseada na fórmula e componentes
    /// </summary>
    public async Task<DateTime> CalculateExpiryDate(Guid orderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Ordem não encontrada");

        // Se a fórmula tem prazo definido, usa ele
        if (order.Formula?.ShelfLifeDays > 0)
        {
            return DateTime.UtcNow.AddDays(order.Formula.ShelfLifeDays);
        }

        // Senão, busca a menor validade entre os componentes usados
        // Validade padrão: 30 dias para manipulados
        return DateTime.UtcNow.AddDays(30);
    }

    /// <summary>
    /// Verifica se todas as etapas do workflow estão completas
    /// </summary>
    public async Task<bool> AllStepsCompleted(Guid orderId)
    {
        var requiredSteps = new[] { "SEPARACAO", "PESAGEM", "MISTURA", "ENVASE", "ROTULAGEM", "CONFERENCIA", "APROVACAO" };
        
        var completedSteps = await _context.ManipulationSteps
            .Where(s => s.ManipulationOrderId == orderId && s.Status == "CONCLUIDA")
            .Select(s => s.StepType)
            .Distinct()
            .ToListAsync();

        return requiredSteps.All(r => completedSteps.Contains(r));
    }

    /// <summary>
    /// Calcula rendimento da manipulação
    /// </summary>
    public decimal CalculateYield(decimal expectedQuantity, decimal actualQuantity)
    {
        if (expectedQuantity <= 0) return 0;
        return (actualQuantity / expectedQuantity) * 100;
    }

    /// <summary>
    /// Valida se rendimento está dentro do aceitável (geralmente 95-105%)
    /// </summary>
    public bool IsYieldAcceptable(decimal yield, decimal minYield = 95, decimal maxYield = 105)
    {
        return yield >= minYield && yield <= maxYield;
    }

    /// <summary>
    /// Registra perda durante manipulação
    /// </summary>
    public async Task<ManipulationLoss> RegisterLoss(
        Guid orderId, 
        Guid? rawMaterialId,
        decimal quantity, 
        string unit,
        string reason,
        Guid employeeId)
    {
        var loss = new ManipulationLoss
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = orderId,
            RawMaterialId = rawMaterialId,
            Quantity = quantity,
            Unit = unit,
            Reason = reason,
            RegisteredByEmployeeId = employeeId,
            RegisteredAt = DateTime.UtcNow
        };

        _context.ManipulationLosses.Add(loss);
        await _context.SaveChangesAsync();

        return loss;
    }

    /// <summary>
    /// Registra sobra durante manipulação
    /// </summary>
    public async Task<ManipulationLeftover> RegisterLeftover(
        Guid orderId,
        Guid? rawMaterialId,
        decimal quantity,
        string unit,
        string destination,
        Guid employeeId)
    {
        var leftover = new ManipulationLeftover
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = orderId,
            RawMaterialId = rawMaterialId,
            Quantity = quantity,
            Unit = unit,
            Destination = destination,
            RegisteredByEmployeeId = employeeId,
            RegisteredAt = DateTime.UtcNow
        };

        _context.ManipulationLeftovers.Add(leftover);
        await _context.SaveChangesAsync();

        return leftover;
    }

    /// <summary>
    /// Registra conferência dupla
    /// </summary>
    public async Task<DualVerification> RegisterDualVerification(
        Guid orderId,
        string verificationType,
        Guid firstVerifierId,
        string firstVerifierNotes,
        Guid secondVerifierId,
        string secondVerifierNotes,
        bool approved)
    {
        var verification = new DualVerification
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = orderId,
            VerificationType = verificationType,
            FirstVerifierId = firstVerifierId,
            FirstVerificationAt = DateTime.UtcNow,
            FirstVerifierNotes = firstVerifierNotes,
            SecondVerifierId = secondVerifierId,
            SecondVerificationAt = DateTime.UtcNow,
            SecondVerifierNotes = secondVerifierNotes,
            Approved = approved,
            CreatedAt = DateTime.UtcNow
        };

        _context.DualVerifications.Add(verification);
        await _context.SaveChangesAsync();

        return verification;
    }

    /// <summary>
    /// Verifica se conferência dupla foi realizada para uma etapa
    /// </summary>
    public async Task<bool> HasDualVerification(Guid orderId, string verificationType)
    {
        return await _context.DualVerifications
            .AnyAsync(v => v.ManipulationOrderId == orderId && 
                          v.VerificationType == verificationType &&
                          v.Approved);
    }

    /// <summary>
    /// Calcula custo total da manipulação
    /// </summary>
    public async Task<ManipulationCostResult> CalculateManipulationCost(Guid orderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Ordem não encontrada");

        var result = new ManipulationCostResult
        {
            OrderId = orderId,
            OrderNumber = order.OrderNumber,
            QuantityProduced = order.QuantityToProduce
        };

        if (order.Formula?.Components != null)
        {
            foreach (var component in order.Formula.Components)
            {
                var quantityNeeded = component.Quantity * order.QuantityToProduce;
                
                // Busca custo do lote mais recente aprovado
                var latestBatch = await _context.Batches
                    .Where(b => b.RawMaterialId == component.RawMaterialId &&
                               b.Status.ToUpper() == "APROVADO")
                    .OrderByDescending(b => b.ReceivedDate)
                    .FirstOrDefaultAsync();

                var unitCost = latestBatch?.UnitCost ?? 0;
                var totalCost = quantityNeeded * unitCost;

                result.ComponentCosts.Add(new ComponentCostItem
                {
                    RawMaterialId = component.RawMaterialId,
                    RawMaterialName = component.RawMaterial?.Name ?? "N/A",
                    Quantity = quantityNeeded,
                    Unit = component.Unit,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                });

                result.TotalMaterialCost += totalCost;
            }
        }

        // Custos adicionais (podem ser configuráveis por estabelecimento)
        result.LaborCost = result.TotalMaterialCost * 0.30m; // 30% do custo de materiais
        result.OverheadCost = result.TotalMaterialCost * 0.20m; // 20% overhead
        result.TotalCost = result.TotalMaterialCost + result.LaborCost + result.OverheadCost;
        result.UnitCost = order.QuantityToProduce > 0 
            ? result.TotalCost / order.QuantityToProduce 
            : 0;

        return result;
    }

    /// <summary>
    /// Verifica disponibilidade de estoque para manipulação
    /// </summary>
    public async Task<StockAvailabilityResult> CheckStockAvailability(Guid orderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Ordem não encontrada");

        var result = new StockAvailabilityResult
        {
            OrderId = orderId,
            OrderNumber = order.OrderNumber,
            AllAvailable = true
        };

        if (order.Formula?.Components != null)
        {
            foreach (var component in order.Formula.Components)
            {
                var quantityNeeded = component.Quantity * order.QuantityToProduce;
                
                // Soma estoque disponível de lotes aprovados
                var availableStock = await _context.Batches
                    .Where(b => b.RawMaterialId == component.RawMaterialId &&
                               b.Status.ToUpper() == "APROVADO" &&
                               b.CurrentQuantity > 0 &&
                               b.ExpiryDate > DateTime.UtcNow)
                    .SumAsync(b => b.CurrentQuantity);

                var isAvailable = availableStock >= quantityNeeded;
                
                result.Items.Add(new StockAvailabilityItem
                {
                    RawMaterialId = component.RawMaterialId,
                    RawMaterialName = component.RawMaterial?.Name ?? "N/A",
                    QuantityNeeded = quantityNeeded,
                    QuantityAvailable = availableStock,
                    Unit = component.Unit,
                    IsAvailable = isAvailable,
                    Shortage = isAvailable ? 0 : quantityNeeded - availableStock
                });

                if (!isAvailable)
                    result.AllAvailable = false;
            }
        }

        return result;
    }
}

// ===================================================================
// MODELS AUXILIARES
// ===================================================================

public class ManipulationCostResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public decimal QuantityProduced { get; set; }
    public List<ComponentCostItem> ComponentCosts { get; set; } = new();
    public decimal TotalMaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnitCost { get; set; }
}

public class ComponentCostItem
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class StockAvailabilityResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public bool AllAvailable { get; set; }
    public List<StockAvailabilityItem> Items { get; set; } = new();
}

public class StockAvailabilityItem
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityAvailable { get; set; }
    public string Unit { get; set; } = default!;
    public bool IsAvailable { get; set; }
    public decimal Shortage { get; set; }
}
