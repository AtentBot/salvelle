namespace DTOs.Reports;

// ===================================================================
// FILTROS DE RELATÓRIOS
// ===================================================================

public class ReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? FormulaId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public bool IncludeCanceled { get; set; } = false;
}

// ===================================================================
// RELATÓRIO DE ORDENS POR PERÍODO
// ===================================================================

public class OrdersByPeriodReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CanceledOrders { get; set; }
    public int PendingOrders { get; set; }
    public int InProductionOrders { get; set; }
    public int OverdueOrders { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageProductionTimeMinutes { get; set; }
    public List<OrdersByStatusDto> ByStatus { get; set; } = new();
    public List<OrdersByDayDto> ByDay { get; set; } = new();
    public List<OrdersByFormulaDto> ByFormula { get; set; } = new();
    public List<OrderDetailReportDto> Orders { get; set; } = new();
}

public class OrdersByStatusDto
{
    public string Status { get; set; } = default!;
    public string StatusDisplay { get; set; } = default!;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class OrdersByDayDto
{
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = default!;
    public int Created { get; set; }
    public int Completed { get; set; }
    public int Canceled { get; set; }
}

public class OrdersByFormulaDto
{
    public Guid FormulaId { get; set; }
    public string FormulaCode { get; set; } = default!;
    public string FormulaName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal TotalRevenue { get; set; }
}

public class OrderDetailReportDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? FormulaName { get; set; }
    public string CustomerName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public int? ProductionTimeMinutes { get; set; }
    public bool IsOverdue { get; set; }
}

// ===================================================================
// RELATÓRIO DE RENDIMENTO
// ===================================================================

public class YieldReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalProductions { get; set; }
    public decimal? AverageYield { get; set; }
    public decimal? MinYield { get; set; }
    public decimal? MaxYield { get; set; }
    public int WithinAcceptableRange { get; set; }
    public int BelowAcceptable { get; set; }
    public int AboveAcceptable { get; set; }
    public decimal AcceptableRangeMin { get; set; } = 95;
    public decimal AcceptableRangeMax { get; set; } = 105;
    public List<YieldByFormulaDto> ByFormula { get; set; } = new();
    public List<YieldByEmployeeDto> ByEmployee { get; set; } = new();
    public List<YieldDetailDto> Details { get; set; } = new();
}

public class YieldByFormulaDto
{
    public Guid FormulaId { get; set; }
    public string FormulaCode { get; set; } = default!;
    public string FormulaName { get; set; } = default!;
    public int ProductionCount { get; set; }
    public decimal? AverageYield { get; set; }
    public decimal? MinYield { get; set; }
    public decimal? MaxYield { get; set; }
    public decimal StandardDeviation { get; set; }
}

public class YieldByEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = default!;
    public int ProductionCount { get; set; }
    public decimal? AverageYield { get; set; }
    public decimal? AverageTimeMinutes { get; set; }
}

public class YieldDetailDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? FormulaName { get; set; }
    public decimal? ExpectedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public decimal? YieldPercentage { get; set; }
    public string? DeviationReason { get; set; }
    public string? ProducedBy { get; set; }
    public DateTime ProductionDate { get; set; }
}

// ===================================================================
// RELATÓRIO DE PERDAS
// ===================================================================

public class LossesReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalLossRecords { get; set; }
    public decimal TotalQuantityLost { get; set; }
    public decimal TotalValueLost { get; set; }
    public List<LossByTypeDto> ByType { get; set; } = new();
    public List<LossByRawMaterialDto> ByRawMaterial { get; set; } = new();
    public List<LossByReasonDto> ByReason { get; set; } = new();
    public List<LossByMonthDto> ByMonth { get; set; } = new();
    public List<LossDetailDto> Details { get; set; } = new();
}

public class LossByTypeDto
{
    public string LossType { get; set; } = default!;
    public string LossTypeDisplay { get; set; } = default!;
    public int Count { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Percentage { get; set; }
}

public class LossByRawMaterialDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public string? DcbCode { get; set; }
    public int LossCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal TotalValue { get; set; }
}

public class LossByReasonDto
{
    public string Reason { get; set; } = default!;
    public int Count { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class LossByMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = default!;
    public int LossCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class LossDetailDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? RawMaterialName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string LossType { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public decimal? ValueLost { get; set; }
    public string? RegisteredBy { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// ===================================================================
// RELATÓRIO DE CUSTOS
// ===================================================================

public class CostReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalProductions { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalOverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalLossValue { get; set; }
    public decimal AverageCostPerOrder { get; set; }
    public decimal? AverageUnitCost { get; set; }
    public List<CostByFormulaDto> ByFormula { get; set; } = new();
    public List<CostByMonthDto> ByMonth { get; set; } = new();
    public List<CostByComponentDto> TopComponents { get; set; } = new();
    public List<CostDetailDto> Details { get; set; } = new();
}

public class CostByFormulaDto
{
    public Guid FormulaId { get; set; }
    public string FormulaCode { get; set; } = default!;
    public string FormulaName { get; set; } = default!;
    public int ProductionCount { get; set; }
    public decimal? TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? AverageUnitCost { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CostByMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = default!;
    public int ProductionCount { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal LossValue { get; set; }
}

public class CostByComponentDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public decimal TotalQuantityUsed { get; set; }
    public string Unit { get; set; } = default!;
    public decimal TotalCost { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CostDetailDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? FormulaName { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? UnitCost { get; set; }
    public DateTime ProductionDate { get; set; }
}

// ===================================================================
// RELATÓRIO DE PRODUTIVIDADE
// ===================================================================

public class ProductivityReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalWorkDays { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public decimal AverageOrdersPerDay { get; set; }
    public decimal AverageProductionTimeMinutes { get; set; }
    public List<ProductivityByEmployeeDto> ByEmployee { get; set; } = new();
    public List<ProductivityByDayOfWeekDto> ByDayOfWeek { get; set; } = new();
    public List<ProductivityByHourDto> ByHour { get; set; } = new();
}

public class ProductivityByEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = default!;
    public string? JobPosition { get; set; }
    public int OrdersCompleted { get; set; }
    public decimal TotalQuantityProduced { get; set; }
    public decimal AverageProductionTimeMinutes { get; set; }
    public decimal? AverageYield { get; set; }
    public int LossCount { get; set; }
    public decimal Efficiency { get; set; } // % de ordens no prazo
}

public class ProductivityByDayOfWeekDto
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = default!;
    public int OrdersCompleted { get; set; }
    public decimal AverageProductionTime { get; set; }
}

public class ProductivityByHourDto
{
    public int Hour { get; set; }
    public string HourRange { get; set; } = default!; // "08:00-09:00"
    public int OrdersStarted { get; set; }
    public int OrdersCompleted { get; set; }
}

// ===================================================================
// DASHBOARD EXECUTIVO
// ===================================================================

public class ExecutiveDashboardDto
{
    public DateTime GeneratedAt { get; set; }
    public string Period { get; set; } = default!;
    
    // KPIs Principais
    public int TotalOrdersMonth { get; set; }
    public int CompletedOrdersMonth { get; set; }
    public decimal CompletionRateMonth { get; set; }
    public decimal RevenueMonth { get; set; }
    public decimal CostMonth { get; set; }
    public decimal ProfitMarginMonth { get; set; }
    
    // Comparativo com mês anterior
    public decimal OrdersGrowth { get; set; }
    public decimal RevenueGrowth { get; set; }
    public decimal CostGrowth { get; set; }
    
    // Alertas
    public int OverdueOrders { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiringBatches { get; set; }
    public int PendingApprovals { get; set; }
    
    // Top 5
    public List<TopFormulaDto> TopFormulas { get; set; } = new();
    public List<TopEmployeeDto> TopEmployees { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    
    // Gráficos (dados para Chart.js)
    public List<ChartDataPointDto> OrdersLast30Days { get; set; } = new();
    public List<ChartDataPointDto> RevenueLast6Months { get; set; } = new();
    public List<ChartDataPointDto> CostBreakdown { get; set; } = new();
}

public class TopFormulaDto
{
    public string FormulaName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class TopEmployeeDto
{
    public string EmployeeName { get; set; } = default!;
    public int OrdersCompleted { get; set; }
    public decimal? AverageYield { get; set; }
}

public class TopCustomerDto
{
    public string CustomerName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ChartDataPointDto
{
    public string Label { get; set; } = default!;
    public decimal Value { get; set; }
    public string? Color { get; set; }
}
