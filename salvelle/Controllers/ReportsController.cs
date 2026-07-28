using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Reports;
using DTOs;
using Models.Employees;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    private static string SanitizeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var trimmed = value.Replace(";", ",").Replace("\n", " ").Replace("\r", " ");
        if (trimmed.StartsWith("=") || trimmed.StartsWith("+") || trimmed.StartsWith("-") || trimmed.StartsWith("@") || trimmed.StartsWith("\t"))
            trimmed = "'" + trimmed;
        return trimmed;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            throw new UnauthorizedAccessException("Usuário não autenticado");
        return employee.EstablishmentId;
    }

    // Helper para converter DateTime para UTC (PostgreSQL exige)
    private static DateTime ToUtc(DateTime? date, bool endOfDay = false)
    {
        if (!date.HasValue)
            return DateTime.UtcNow;

        var dt = date.Value;

        // Se Kind é Unspecified, assumir que é UTC
        if (dt.Kind == DateTimeKind.Unspecified)
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        else if (dt.Kind == DateTimeKind.Local)
            dt = dt.ToUniversalTime();

        // Se endOfDay, ajustar para 23:59:59
        if (endOfDay)
            dt = dt.Date.AddDays(1).AddTicks(-1);

        return dt;
    }

    // ===================================================================
    // RELATÓRIO DE ORDENS POR PERÍODO
    // ===================================================================

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<OrdersByPeriodReportDto>>> GetOrdersReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? formulaId,
        [FromQuery] string? status,
        [FromQuery] bool includeCanceled = false)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var query = _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.OrderDate >= start &&
                       o.OrderDate <= end);

        if (formulaId.HasValue)
            query = query.Where(o => o.FormulaId == formulaId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (!includeCanceled)
            query = query.Where(o => o.Status != "CANCELADO");

        var orders = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var report = new OrdersByPeriodReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalOrders = orders.Count,
            CompletedOrders = orders.Count(o => o.Status == "FINALIZADO" || o.Status == "ENTREGUE"),
            CanceledOrders = orders.Count(o => o.Status == "CANCELADO"),
            PendingOrders = orders.Count(o => o.Status == "PENDENTE"),
            InProductionOrders = orders.Count(o => o.Status != "PENDENTE" &&
                                                   o.Status != "FINALIZADO" &&
                                                   o.Status != "ENTREGUE" &&
                                                   o.Status != "CANCELADO"),
            OverdueOrders = orders.Count(o => o.ExpectedDate < now &&
                                              o.Status != "FINALIZADO" &&
                                              o.Status != "ENTREGUE" &&
                                              o.Status != "CANCELADO")
        };

        report.CompletionRate = report.TotalOrders > 0
            ? (decimal)report.CompletedOrders / report.TotalOrders * 100
            : 0;

        var completedWithTime = orders
            .Where(o => o.CompletionDate.HasValue && o.StartDate.HasValue)
            .ToList();

        report.AverageProductionTimeMinutes = completedWithTime.Any()
            ? (decimal)completedWithTime.Average(o => (o.CompletionDate!.Value - o.StartDate!.Value).TotalMinutes)
            : 0;

        report.ByStatus = orders
            .GroupBy(o => o.Status)
            .Select(g => new OrdersByStatusDto
            {
                Status = g.Key,
                StatusDisplay = GetStatusDisplay(g.Key),
                Count = g.Count(),
                Percentage = report.TotalOrders > 0 ? (decimal)g.Count() / report.TotalOrders * 100 : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        report.ByDay = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new OrdersByDayDto
            {
                Date = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                DayOfWeek = GetDayOfWeekPt(g.Key.DayOfWeek),
                Created = g.Count(),
                Completed = g.Count(o => o.CompletionDate?.Date == g.Key),
                Canceled = g.Count(o => o.Status == "CANCELADO")
            })
            .OrderBy(x => x.Date)
            .ToList();

        report.ByFormula = orders
            .Where(o => o.Formula != null)
            .GroupBy(o => new { o.FormulaId, o.Formula!.Code, o.Formula.Name, o.Formula.Category })
            .Select(g => new OrdersByFormulaDto
            {
                FormulaId = g.Key.FormulaId ?? Guid.Empty,
                FormulaCode = g.Key.Code,
                FormulaName = g.Key.Name,
                Category = g.Key.Category,
                OrderCount = g.Count(),
                TotalQuantity = g.Sum(o => o.QuantityToProduce),
                Unit = g.First().Unit
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(10)
            .ToList();

        report.Orders = orders
            .OrderByDescending(o => o.OrderDate)
            .Take(100)
            .Select(o => new OrderDetailReportDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                FormulaName = o.Formula?.Name,
                CustomerName = o.CustomerName,
                Quantity = o.QuantityToProduce,
                Unit = o.Unit,
                Status = o.Status,
                OrderDate = o.OrderDate,
                ExpectedDate = o.ExpectedDate,
                CompletionDate = o.CompletionDate,
                ProductionTimeMinutes = o.StartDate.HasValue && o.CompletionDate.HasValue
                    ? (int)(o.CompletionDate.Value - o.StartDate.Value).TotalMinutes
                    : null,
                IsOverdue = o.ExpectedDate < now && o.Status != "FINALIZADO" && o.Status != "ENTREGUE" && o.Status != "CANCELADO"
            })
            .ToList();

        return Ok(ApiResponse<OrdersByPeriodReportDto>.SuccessResponse(report));
    }

    // ===================================================================
    // RELATÓRIO DE RENDIMENTO
    // ===================================================================

    [HttpGet("yield")]
    public async Task<ActionResult<ApiResponse<YieldReportDto>>> GetYieldReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? formulaId,
        [FromQuery] Guid? employeeId)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var query = _context.ProductionRecords
            .Include(p => p.ManipulationOrder)
                .ThenInclude(o => o!.Formula)
            .Include(p => p.ProducedByEmployee)
            .Where(p => p.ManipulationOrder!.EstablishmentId == establishmentId &&
                       p.CreatedAt >= start &&
                       p.CreatedAt <= end);

        if (formulaId.HasValue)
            query = query.Where(p => p.ManipulationOrder!.FormulaId == formulaId.Value);

        if (employeeId.HasValue)
            query = query.Where(p => p.ProducedByEmployeeId == employeeId.Value);

        var records = await query.ToListAsync();

        var report = new YieldReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalProductions = records.Count,
            AcceptableRangeMin = 95,
            AcceptableRangeMax = 105
        };

        if (records.Any())
        {
            report.AverageYield = records.Average(r => r.YieldPercentage);
            report.MinYield = records.Min(r => r.YieldPercentage);
            report.MaxYield = records.Max(r => r.YieldPercentage);
            report.WithinAcceptableRange = records.Count(r => r.YieldPercentage >= 95 && r.YieldPercentage <= 105);
            report.BelowAcceptable = records.Count(r => r.YieldPercentage < 95);
            report.AboveAcceptable = records.Count(r => r.YieldPercentage > 105);

            report.ByFormula = records
                .Where(r => r.ManipulationOrder?.Formula != null)
                .GroupBy(r => new {
                    r.ManipulationOrder!.FormulaId,
                    r.ManipulationOrder.Formula!.Code,
                    r.ManipulationOrder.Formula.Name
                })
                .Select(g => new YieldByFormulaDto
                {
                    FormulaId = g.Key.FormulaId ?? Guid.Empty,
                    FormulaCode = g.Key.Code,
                    FormulaName = g.Key.Name,
                    ProductionCount = g.Count(),
                    AverageYield = g.Average(r => r.YieldPercentage),
                    MinYield = g.Min(r => r.YieldPercentage),
                    MaxYield = g.Max(r => r.YieldPercentage),
                    StandardDeviation = CalculateStdDev(g.Where(r => r.YieldPercentage.HasValue).Select(r => r.YieldPercentage!.Value))
                })
                .OrderByDescending(x => x.ProductionCount)
                .ToList();

            report.ByEmployee = records
                .Where(r => r.ProducedByEmployee != null)
                .GroupBy(r => new { r.ProducedByEmployeeId, r.ProducedByEmployee!.FullName })
                .Select(g => new YieldByEmployeeDto
                {
                    EmployeeId = g.Key.ProducedByEmployeeId,
                    EmployeeName = g.Key.FullName,
                    ProductionCount = g.Count(),
                    AverageYield = g.Average(r => r.YieldPercentage),
                    AverageTimeMinutes = (decimal)g.Average(r => r.TotalProductionTimeMinutes)
                })
                .OrderByDescending(x => x.ProductionCount)
                .ToList();

            report.Details = records
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .Select(r => new YieldDetailDto
                {
                    OrderId = r.ManipulationOrderId,
                    OrderNumber = r.ManipulationOrder?.OrderNumber ?? "N/A",
                    FormulaName = r.ManipulationOrder?.Formula?.Name,
                    ExpectedQuantity = r.ExpectedQuantity,
                    ActualQuantity = r.ActualQuantity,
                    YieldPercentage = r.YieldPercentage,
                    DeviationReason = r.YieldDeviationReason,
                    ProducedBy = r.ProducedByEmployee?.FullName,
                    ProductionDate = r.CreatedAt
                })
                .ToList();
        }

        return Ok(ApiResponse<YieldReportDto>.SuccessResponse(report));
    }

    // ===================================================================
    // RELATÓRIO DE PERDAS
    // ===================================================================

    [HttpGet("losses")]
    public async Task<ActionResult<ApiResponse<LossesReportDto>>> GetLossesReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? rawMaterialId,
        [FromQuery] string? lossType)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var query = _context.ManipulationLosses
            .Include(l => l.ManipulationOrder)
            .Include(l => l.RawMaterial)
            .Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrder!.EstablishmentId == establishmentId &&
                       l.RegisteredAt >= start &&
                       l.RegisteredAt <= end);

        if (rawMaterialId.HasValue)
            query = query.Where(l => l.RawMaterialId == rawMaterialId.Value);

        if (!string.IsNullOrEmpty(lossType))
            query = query.Where(l => l.LossType == lossType);

        var losses = await query.ToListAsync();

        var report = new LossesReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalLossRecords = losses.Count,
            TotalQuantityLost = losses.Sum(l => l.Quantity),
            TotalValueLost = losses.Sum(l => l.ValueLost ?? 0)
        };

        report.ByType = losses
            .GroupBy(l => l.LossType)
            .Select(g => new LossByTypeDto
            {
                LossType = g.Key ?? "OUTRO",
                LossTypeDisplay = GetLossTypeDisplay(g.Key),
                Count = g.Count(),
                TotalQuantity = g.Sum(l => l.Quantity),
                TotalValue = g.Sum(l => l.ValueLost ?? 0),
                Percentage = report.TotalLossRecords > 0 ? (decimal)g.Count() / report.TotalLossRecords * 100 : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        report.ByRawMaterial = losses
            .Where(l => l.RawMaterial != null)
            .GroupBy(l => new { l.RawMaterialId, l.RawMaterial!.Name, l.RawMaterial.DcbCode, l.Unit })
            .Select(g => new LossByRawMaterialDto
            {
                RawMaterialId = g.Key.RawMaterialId ?? Guid.Empty,
                RawMaterialName = g.Key.Name,
                DcbCode = g.Key.DcbCode,
                LossCount = g.Count(),
                TotalQuantity = g.Sum(l => l.Quantity),
                Unit = g.Key.Unit,
                TotalValue = g.Sum(l => l.ValueLost ?? 0)
            })
            .OrderByDescending(x => x.TotalValue)
            .Take(10)
            .ToList();

        report.ByReason = losses
            .GroupBy(l => l.Reason)
            .Select(g => new LossByReasonDto
            {
                Reason = g.Key,
                Count = g.Count(),
                TotalQuantity = g.Sum(l => l.Quantity),
                TotalValue = g.Sum(l => l.ValueLost ?? 0)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        report.ByMonth = losses
            .GroupBy(l => new { l.RegisteredAt.Year, l.RegisteredAt.Month })
            .Select(g => new LossByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = GetMonthName(g.Key.Month),
                LossCount = g.Count(),
                TotalQuantity = g.Sum(l => l.Quantity),
                TotalValue = g.Sum(l => l.ValueLost ?? 0)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        report.Details = losses
            .OrderByDescending(l => l.RegisteredAt)
            .Take(50)
            .Select(l => new LossDetailDto
            {
                Id = l.Id,
                OrderNumber = l.ManipulationOrder?.OrderNumber ?? "N/A",
                RawMaterialName = l.RawMaterial?.Name,
                Quantity = l.Quantity,
                Unit = l.Unit,
                LossType = l.LossType ?? "OUTRO",
                Reason = l.Reason,
                ValueLost = l.ValueLost,
                RegisteredBy = l.RegisteredByEmployee?.FullName,
                RegisteredAt = l.RegisteredAt
            })
            .ToList();

        return Ok(ApiResponse<LossesReportDto>.SuccessResponse(report));
    }

    // ===================================================================
    // RELATÓRIO DE CUSTOS
    // ===================================================================

    [HttpGet("costs")]
    public async Task<ActionResult<ApiResponse<CostReportDto>>> GetCostsReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? formulaId)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var records = await _context.ProductionRecords
            .Include(p => p.ManipulationOrder)
                .ThenInclude(o => o!.Formula)
            .Where(p => p.ManipulationOrder!.EstablishmentId == establishmentId &&
                       p.CreatedAt >= start &&
                       p.CreatedAt <= end &&
                       (formulaId == null || p.ManipulationOrder.FormulaId == formulaId))
            .ToListAsync();

        var costDetails = new List<CostDetailDto>();
        decimal totalMaterial = 0, totalLabor = 0, totalOverhead = 0;

        foreach (var record in records)
        {
            var order = record.ManipulationOrder;
            if (order?.Formula == null) continue;

            var formula = await _context.Formulas
                .Include(f => f.Components)
                    .ThenInclude(c => c.RawMaterial)
                .FirstOrDefaultAsync(f => f.Id == order.FormulaId);

            if (formula?.Components == null) continue;

            decimal materialCost = 0;
            foreach (var comp in formula.Components)
            {
                var batch = await _context.Batches
                    .Where(b => b.RawMaterialId == comp.RawMaterialId && b.Status.ToUpper() == "APROVADO")
                    .OrderByDescending(b => b.ReceivedDate)
                    .FirstOrDefaultAsync();

                var qty = comp.Quantity * order.QuantityToProduce;
                materialCost += qty * (batch?.UnitCost ?? 0);
            }

            var laborCost = materialCost * 0.30m;
            var overheadCost = materialCost * 0.20m;
            var totalCost = materialCost + laborCost + overheadCost;

            totalMaterial += materialCost;
            totalLabor += laborCost;
            totalOverhead += overheadCost;

            costDetails.Add(new CostDetailDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                FormulaName = formula.Name,
                Quantity = record.ActualQuantity,
                Unit = record.Unit,
                MaterialCost = materialCost,
                LaborCost = laborCost,
                OverheadCost = overheadCost,
                TotalCost = totalCost,
                UnitCost = record.ActualQuantity > 0 ? totalCost / record.ActualQuantity : 0,
                ProductionDate = record.CreatedAt
            });
        }

        var lossValue = await _context.ManipulationLosses
            .Where(l => l.ManipulationOrder!.EstablishmentId == establishmentId &&
                       l.RegisteredAt >= start &&
                       l.RegisteredAt <= end)
            .SumAsync(l => l.ValueLost ?? 0);

        var report = new CostReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalProductions = records.Count,
            TotalMaterialCost = totalMaterial,
            TotalLaborCost = totalLabor,
            TotalOverheadCost = totalOverhead,
            TotalCost = totalMaterial + totalLabor + totalOverhead,
            TotalLossValue = lossValue,
            AverageCostPerOrder = records.Any() ? (totalMaterial + totalLabor + totalOverhead) / records.Count : 0,
            AverageUnitCost = costDetails.Any() ? costDetails.Average(c => c.UnitCost) : 0
        };

        report.ByFormula = costDetails
            .Where(c => c.FormulaName != null)
            .GroupBy(c => c.FormulaName)
            .Select(g => new CostByFormulaDto
            {
                FormulaName = g.Key!,
                ProductionCount = g.Count(),
                TotalQuantity = g.Sum(c => c.Quantity),
                TotalCost = g.Sum(c => c.TotalCost),
                AverageUnitCost = g.Average(c => c.UnitCost),
                PercentageOfTotal = report.TotalCost > 0 ? g.Sum(c => c.TotalCost) / report.TotalCost * 100 : 0
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();

        report.ByMonth = costDetails
            .GroupBy(c => new { c.ProductionDate.Year, c.ProductionDate.Month })
            .Select(g => new CostByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = GetMonthName(g.Key.Month),
                ProductionCount = g.Count(),
                MaterialCost = g.Sum(c => c.MaterialCost),
                LaborCost = g.Sum(c => c.LaborCost),
                OverheadCost = g.Sum(c => c.OverheadCost),
                TotalCost = g.Sum(c => c.TotalCost)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        report.Details = costDetails.OrderByDescending(c => c.ProductionDate).Take(50).ToList();

        return Ok(ApiResponse<CostReportDto>.SuccessResponse(report));
    }

    // ===================================================================
    // RELATÓRIO DE PRODUTIVIDADE
    // ===================================================================

    [HttpGet("productivity")]
    public async Task<ActionResult<ApiResponse<ProductivityReportDto>>> GetProductivityReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? employeeId)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var query = _context.ManipulationOrders
            .Include(o => o.ManipulatedByEmployee)
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.CompletionDate >= start &&
                       o.CompletionDate <= end &&
                       (o.Status == "FINALIZADO" || o.Status == "ENTREGUE"));

        if (employeeId.HasValue)
            query = query.Where(o => o.ManipulatedByEmployeeId == employeeId.Value);

        var completedOrders = await query.ToListAsync();

        var workDays = (int)(end - start).TotalDays;
        if (workDays <= 0) workDays = 1;

        var report = new ProductivityReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalWorkDays = workDays,
            TotalOrdersCompleted = completedOrders.Count,
            AverageOrdersPerDay = (decimal)completedOrders.Count / workDays
        };

        var ordersWithTime = completedOrders
            .Where(o => o.StartDate.HasValue && o.CompletionDate.HasValue)
            .ToList();

        report.AverageProductionTimeMinutes = ordersWithTime.Any()
            ? (decimal)ordersWithTime.Average(o => (o.CompletionDate!.Value - o.StartDate!.Value).TotalMinutes)
            : 0;

        var productionRecords = await _context.ProductionRecords
            .Include(p => p.ManipulationOrder)
            .Include(p => p.ProducedByEmployee)
            .Where(p => p.ManipulationOrder!.EstablishmentId == establishmentId &&
                       p.CreatedAt >= start &&
                       p.CreatedAt <= end)
            .ToListAsync();

        var lossesByEmployee = await _context.ManipulationLosses
            .Include(l => l.ManipulationOrder)
            .Where(l => l.ManipulationOrder!.EstablishmentId == establishmentId &&
                       l.RegisteredAt >= start &&
                       l.RegisteredAt <= end)
            .GroupBy(l => l.RegisteredByEmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync();

        report.ByEmployee = completedOrders
            .Where(o => o.ManipulatedByEmployee != null)
            .GroupBy(o => new { o.ManipulatedByEmployeeId, o.ManipulatedByEmployee!.FullName })
            .Select(g =>
            {
                var empRecords = productionRecords.Where(r => r.ProducedByEmployeeId == g.Key.ManipulatedByEmployeeId);
                var empLosses = lossesByEmployee.FirstOrDefault(l => l.EmployeeId == g.Key.ManipulatedByEmployeeId);
                var onTime = g.Count(o => o.CompletionDate <= o.ExpectedDate);

                return new ProductivityByEmployeeDto
                {
                    EmployeeId = g.Key.ManipulatedByEmployeeId ?? Guid.Empty,
                    EmployeeName = g.Key.FullName,
                    OrdersCompleted = g.Count(),
                    TotalQuantityProduced = g.Sum(o => o.QuantityToProduce),
                    AverageProductionTimeMinutes = g.Where(o => o.StartDate.HasValue && o.CompletionDate.HasValue)
                        .Average(o => (decimal?)(o.CompletionDate!.Value - o.StartDate!.Value).TotalMinutes) ?? 0,
                    AverageYield = empRecords.Any() ? empRecords.Average(r => r.YieldPercentage) : 0,
                    LossCount = empLosses?.Count ?? 0,
                    Efficiency = g.Count() > 0 ? (decimal)onTime / g.Count() * 100 : 0
                };
            })
            .OrderByDescending(x => x.OrdersCompleted)
            .ToList();

        report.ByDayOfWeek = completedOrders
            .Where(o => o.CompletionDate.HasValue)
            .GroupBy(o => o.CompletionDate!.Value.DayOfWeek)
            .Select(g => new ProductivityByDayOfWeekDto
            {
                DayOfWeek = (int)g.Key,
                DayName = GetDayOfWeekPt(g.Key),
                OrdersCompleted = g.Count(),
                AverageProductionTime = g.Where(o => o.StartDate.HasValue)
                    .Average(o => (decimal?)(o.CompletionDate!.Value - o.StartDate!.Value).TotalMinutes) ?? 0
            })
            .OrderBy(x => x.DayOfWeek)
            .ToList();

        report.ByHour = completedOrders
            .Where(o => o.CompletionDate.HasValue)
            .GroupBy(o => o.CompletionDate!.Value.Hour)
            .Select(g => new ProductivityByHourDto
            {
                Hour = g.Key,
                HourRange = $"{g.Key:D2}:00-{(g.Key + 1):D2}:00",
                OrdersCompleted = g.Count()
            })
            .OrderBy(x => x.Hour)
            .ToList();

        return Ok(ApiResponse<ProductivityReportDto>.SuccessResponse(report));
    }

    // ===================================================================
    // DASHBOARD EXECUTIVO
    // ===================================================================

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<ExecutiveDashboardDto>>> GetExecutiveDashboard()
    {
        var establishmentId = GetEstablishmentId();
        var now = DateTime.UtcNow;
        var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        var ordersThisMonth = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.OrderDate >= startOfMonth)
            .ToListAsync();

        var ordersLastMonth = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth)
            .ToListAsync();

        var completedThisMonth = ordersThisMonth.Count(o => o.Status == "FINALIZADO" || o.Status == "ENTREGUE");

        var dashboard = new ExecutiveDashboardDto
        {
            GeneratedAt = now,
            Period = $"{GetMonthName(now.Month)}/{now.Year}",
            TotalOrdersMonth = ordersThisMonth.Count,
            CompletedOrdersMonth = completedThisMonth,
            CompletionRateMonth = ordersThisMonth.Any()
                ? (decimal)completedThisMonth / ordersThisMonth.Count * 100 : 0,
            OrdersGrowth = ordersLastMonth.Any()
                ? ((decimal)ordersThisMonth.Count - ordersLastMonth.Count) / ordersLastMonth.Count * 100 : 0
        };

        dashboard.OverdueOrders = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId &&
                            o.ExpectedDate < now &&
                            o.Status != "FINALIZADO" && o.Status != "ENTREGUE" && o.Status != "CANCELADO");

        dashboard.LowStockItems = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .CountAsync(r => _context.Batches
                .Where(b => b.RawMaterialId == r.Id && b.Status.ToUpper() == "APROVADO")
                .Sum(b => b.CurrentQuantity) < r.MinimumStock);

        var expiryLimit = DateTime.SpecifyKind(now.AddDays(30), DateTimeKind.Utc);
        dashboard.ExpiringBatches = await _context.Batches
            .CountAsync(b => b.RawMaterial!.EstablishmentId == establishmentId &&
                            b.ExpiryDate <= expiryLimit && b.ExpiryDate > now && b.CurrentQuantity > 0);

        dashboard.PendingApprovals = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "CONFERENCIA");

        dashboard.TopFormulas = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId && o.OrderDate >= startOfMonth && o.Formula != null)
            .GroupBy(o => o.Formula!.Name)
            .Select(g => new DTOs.Reports.TopFormulaDto { FormulaName = g.Key, OrderCount = g.Count() })
            .OrderByDescending(x => x.OrderCount)
            .Take(5)
            .ToListAsync();

        dashboard.TopEmployees = await _context.ProductionRecords
            .Include(p => p.ProducedByEmployee)
            .Include(p => p.ManipulationOrder)
            .Where(p => p.ManipulationOrder!.EstablishmentId == establishmentId &&
                       p.CreatedAt >= startOfMonth && p.ProducedByEmployee != null)
            .GroupBy(p => p.ProducedByEmployee!.FullName)
            .Select(g => new TopEmployeeDto
            {
                EmployeeName = g.Key,
                OrdersCompleted = g.Count(),
                AverageYield = g.Average(p => p.YieldPercentage)
            })
            .OrderByDescending(x => x.OrdersCompleted)
            .Take(5)
            .ToListAsync();

        dashboard.TopCustomers = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.OrderDate >= startOfMonth)
            .GroupBy(o => o.CustomerName)
            .Select(g => new TopCustomerDto { CustomerName = g.Key, OrderCount = g.Count() })
            .OrderByDescending(x => x.OrderCount)
            .Take(5)
            .ToListAsync();

        var thirtyDaysAgo = DateTime.SpecifyKind(now.AddDays(-30).Date, DateTimeKind.Utc);
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => DateTime.SpecifyKind(now.AddDays(-29 + i).Date, DateTimeKind.Utc))
            .ToList();

        var ordersByDay = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.OrderDate >= thirtyDaysAgo)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        dashboard.OrdersLast30Days = last30Days
            .Select(d => new ChartDataPointDto
            {
                Label = d.ToString("dd/MM"),
                Value = ordersByDay.FirstOrDefault(x => x.Date == d.Date)?.Count ?? 0
            })
            .ToList();

        return Ok(ApiResponse<ExecutiveDashboardDto>.SuccessResponse(dashboard));
    }

    // ===================================================================
    // EXPORTAÇÃO
    // ===================================================================

    [HttpGet("export/orders")]
    public async Task<ActionResult> ExportOrdersCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var orders = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId && o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var csv = "Numero;Formula;Cliente;Quantidade;Unidade;Status;Data Pedido;Data Prevista;Data Conclusao\n";
        foreach (var o in orders)
            csv += $"{SanitizeCsv(o.OrderNumber)};{SanitizeCsv(o.Formula?.Name)};{SanitizeCsv(o.CustomerName)};{o.QuantityToProduce};{SanitizeCsv(o.Unit)};{SanitizeCsv(o.Status)};{o.OrderDate:dd/MM/yyyy};{o.ExpectedDate:dd/MM/yyyy};{o.CompletionDate:dd/MM/yyyy}\n";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"ordens_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
    }

    [HttpGet("export/losses")]
    public async Task<ActionResult> ExportLossesCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var establishmentId = GetEstablishmentId();
        var start = ToUtc(startDate ?? DateTime.UtcNow.AddDays(-30));
        var end = ToUtc(endDate, endOfDay: true);

        var losses = await _context.ManipulationLosses
            .Include(l => l.ManipulationOrder).Include(l => l.RawMaterial).Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrder!.EstablishmentId == establishmentId && l.RegisteredAt >= start && l.RegisteredAt <= end)
            .OrderByDescending(l => l.RegisteredAt)
            .ToListAsync();

        var csv = "Ordem;Materia Prima;Quantidade;Unidade;Tipo;Motivo;Valor;Registrado Por;Data\n";
        foreach (var l in losses)
            csv += $"{SanitizeCsv(l.ManipulationOrder?.OrderNumber)};{SanitizeCsv(l.RawMaterial?.Name)};{l.Quantity};{SanitizeCsv(l.Unit)};{SanitizeCsv(l.LossType)};{SanitizeCsv(l.Reason)};{l.ValueLost};{SanitizeCsv(l.RegisteredByEmployee?.FullName)};{l.RegisteredAt:dd/MM/yyyy HH:mm}\n";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"perdas_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
    }

    // ===================================================================
    // HELPERS
    // ===================================================================

    private static string GetStatusDisplay(string status) => status switch
    {
        "PENDENTE" => "Pendente",
        "EM_PRODUCAO" => "Em Produção",
        "SEPARACAO" => "Separação",
        "PESAGEM" => "Pesagem",
        "MISTURA" => "Mistura",
        "ENVASE" => "Envase",
        "ROTULAGEM" => "Rotulagem",
        "CONFERENCIA" => "Conferência",
        "APROVADO" => "Aprovado",
        "FINALIZADO" => "Finalizado",
        "ENTREGUE" => "Entregue",
        "CANCELADO" => "Cancelado",
        _ => status
    };

    private static string GetLossTypeDisplay(string? lossType) => lossType switch
    {
        "PROCESSO" => "Processo",
        "QUEBRA" => "Quebra",
        "CONTAMINACAO" => "Contaminação",
        "VENCIMENTO" => "Vencimento",
        "OUTRO" => "Outro",
        _ => "Outro"
    };

    private static string GetDayOfWeekPt(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "Domingo",
        DayOfWeek.Monday => "Segunda",
        DayOfWeek.Tuesday => "Terça",
        DayOfWeek.Wednesday => "Quarta",
        DayOfWeek.Thursday => "Quinta",
        DayOfWeek.Friday => "Sexta",
        DayOfWeek.Saturday => "Sábado",
        _ => day.ToString()
    };

    private static string GetMonthName(int month) => month switch
    {
        1 => "Janeiro",
        2 => "Fevereiro",
        3 => "Março",
        4 => "Abril",
        5 => "Maio",
        6 => "Junho",
        7 => "Julho",
        8 => "Agosto",
        9 => "Setembro",
        10 => "Outubro",
        11 => "Novembro",
        12 => "Dezembro",
        _ => month.ToString()
    };

    private static decimal CalculateStdDev(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        var avg = list.Average();
        var sumOfSquares = list.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / (list.Count - 1)));
    }
}