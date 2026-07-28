using System.ComponentModel.DataAnnotations;

namespace DTOs.Cash;

public class OpenCashRegisterDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [Range(0, 999999)]
    public decimal OpeningBalance { get; set; }

    [MaxLength(500)]
    public string? Observations { get; set; }
}

public class CloseCashRegisterDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [Range(0, 999999)]
    public decimal ClosingBalance { get; set; }

    [MaxLength(500)]
    public string? Observations { get; set; }
}

public class AddCashMovementDto
{
    [Required]
    [MaxLength(20)]
    public string MovementType { get; set; } = default!;

    [Required]
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = default!;
}

public class CashRegisterDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public DateTime OpeningDate { get; set; }
    public DateTime? ClosingDate { get; set; }
    public string OpenedByEmployeeName { get; set; } = default!;
    public string? ClosedByEmployeeName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalPix { get; set; }
    public int SalesCount { get; set; }
    public string Status { get; set; } = default!;
    public string? Observations { get; set; }
}

public class CashMovementDto
{
    public Guid Id { get; set; }
    public string MovementType { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? SaleId { get; set; }
    public string Description { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public DateTime MovementDate { get; set; }
}

public class CashRegisterReportDto
{
    public CashRegisterDto CashRegister { get; set; } = default!;
    public List<CashMovementDto> Movements { get; set; } = new();
    public Dictionary<string, decimal> ByPaymentMethod { get; set; } = new();
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
}

