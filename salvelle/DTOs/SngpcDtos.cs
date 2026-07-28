namespace DTOs;

public class RegisterControlledMovementDto
{
    public Guid RawMaterialId { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? ManipulationOrderId { get; set; }
    public Guid? SaleId { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? PrescriptionType { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorCrm { get; set; }
    public string? PatientName { get; set; }
    public string? PatientCpf { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Observations { get; set; }
    public string? Reason { get; set; }
}

public class ControlledMovementResponseDto
{
    public Guid Id { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string ControlledList { get; set; } = string.Empty;
    public string SubstanceName { get; set; } = string.Empty;
    public string SubstanceDcbCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public string? PrescriptionNumber { get; set; }
    public bool SngpcSent { get; set; }
    public DateTime? SngpcSentAt { get; set; }
    public string? SngpcStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
}

public class BalanceResponseDto
{
    public Guid Id { get; set; }
    public DateTime ReferenceDate { get; set; }
    public string BalanceType { get; set; } = string.Empty;
    public string ControlledList { get; set; } = string.Empty;
    public string SubstanceName { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal TotalEntries { get; set; }
    public decimal TotalExits { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal FinalBalance { get; set; }
    public decimal? PhysicalBalance { get; set; }
    public decimal? Difference { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool SngpcSent { get; set; }
}

public class GenerateBalanceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BalanceType { get; set; } = "MENSAL";
    public List<Guid>? RawMaterialIds { get; set; }
}

public class CloseBalanceDto
{
    public decimal PhysicalBalance { get; set; }
    public string? Observations { get; set; }
}

public class RegisterSpecialPrescriptionDto
{
    public Guid? PrescriptionId { get; set; }
    public string PrescriptionType { get; set; } = string.Empty;
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string? PrescriptionSeries { get; set; }
    public DateTime IssueDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientDocument { get; set; } = string.Empty;
    public string? PatientAddress { get; set; }
    public string? PatientCity { get; set; }
    public string? PatientState { get; set; }
    public string Medication { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public bool Retained { get; set; } = false;
    public string? RetentionReason { get; set; }
}

public class SngpcXmlRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ControlledList { get; set; }
}

public class SngpcReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalMovements { get; set; }
    public Dictionary<string, int> MovementsByList { get; set; } = new();
    public Dictionary<string, int> MovementsByType { get; set; } = new();
    public List<ControlledMovementResponseDto> PendingMovements { get; set; } = new();
    public List<BalanceResponseDto> OpenBalances { get; set; } = new();
}