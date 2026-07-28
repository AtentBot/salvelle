using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("Refunds")]
public class Refund
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("CustomerFormulaId")]
    public Guid CustomerFormulaId { get; set; }
    
    [ForeignKey("CustomerFormulaId")]
    public CustomerFormula? CustomerFormula { get; set; }

    [Required]
    [Column("OnlineOrderId")]
    public Guid OnlineOrderId { get; set; }

    [Required]
    [Column("Amount")]
    public decimal Amount { get; set; }

    [Required]
    [Column("Reason")]
    public string Reason { get; set; } = default!;

    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "PENDENTE";

    [Column("ProcessedAt")]
    public DateTime? ProcessedAt { get; set; }

    [Column("CompletedAt")]
    public DateTime? CompletedAt { get; set; }

    [Column("FailureReason")]
    public string? FailureReason { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }
}
