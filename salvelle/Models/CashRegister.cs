using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models;

/// <summary>
/// Registro de abertura e fechamento de caixa
/// </summary>
[Table("cash_registers")]
public class CashRegister
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("opening_date")]
    public DateTime OpeningDate { get; set; }

    [Column("closing_date")]
    public DateTime? ClosingDate { get; set; }

    [Column("opened_by_employee_id")]
    public Guid OpenedByEmployeeId { get; set; }
    public Employee? OpenedByEmployee { get; set; }

    [Column("closed_by_employee_id")]
    public Guid? ClosedByEmployeeId { get; set; }
    public Employee? ClosedByEmployee { get; set; }

    [Column("opening_balance")]
    public decimal OpeningBalance { get; set; }

    [Column("closing_balance")]
    public decimal? ClosingBalance { get; set; }

    [Column("expected_balance")]
    public decimal? ExpectedBalance { get; set; }

    [Column("difference")]
    public decimal? Difference { get; set; }

    [Column("total_sales")]
    public decimal TotalSales { get; set; } = 0;

    [Column("total_cash")]
    public decimal TotalCash { get; set; } = 0;

    [Column("total_card")]
    public decimal TotalCard { get; set; } = 0;

    [Column("total_pix")]
    public decimal TotalPix { get; set; } = 0;

    [Column("sales_count")]
    public int SalesCount { get; set; } = 0;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ABERTO";

    [Column("observations")]
    public string? Observations { get; set; }

    [Column("total_debit")]
    public decimal TotalDebit { get; set; } = 0;

    [Column("total_credit")]
    public decimal TotalCredit { get; set; } = 0;

    [Column("total_boleto")]
    public decimal TotalBoleto { get; set; } = 0;

    [Column("total_other")]
    public decimal TotalOther { get; set; } = 0;

    [Column("total_withdrawals")]
    public decimal TotalWithdrawals { get; set; } = 0;

    [Column("total_supplies")]
    public decimal TotalSupplies { get; set; } = 0;

    [Column("total_cancellations")]
    public decimal TotalCancellations { get; set; } = 0;

    [Column("closing_observations")]
    public string? ClosingObservations { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
}

/// <summary>
/// Movimentações de caixa (entradas, saídas, sangrias, suprimentos, estornos)
/// </summary>
[Table("cash_movements")]
public class CashMovement
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("cash_register_id")]
    public Guid CashRegisterId { get; set; }

    [ForeignKey("CashRegisterId")]
    public CashRegister? CashRegister { get; set; }

    [Column("movement_type")]
    [MaxLength(20)]
    public string MovementType { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("payment_method")]
    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [Column("sale_id")]
    public Guid? SaleId { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    [Column("movement_date")]
    public DateTime MovementDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
