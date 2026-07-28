using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("platform_commissions")]
public class PlatformCommission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("week_start_date")]
    public DateTime WeekStartDate { get; set; }

    [Column("week_end_date")]
    public DateTime WeekEndDate { get; set; }

    [Column("total_sales_count")]
    public int TotalSalesCount { get; set; }

    /// <summary>
    /// Taxa de comissão: 0.07 (7%), 0.05 (5%) ou 0.03 (3%)
    /// </summary>
    [Column("commission_rate", TypeName = "decimal(5,4)")]
    public decimal CommissionRate { get; set; }

    [Column("total_sales_amount", TypeName = "decimal(18,2)")]
    public decimal TotalSalesAmount { get; set; }

    [Column("total_commission_amount", TypeName = "decimal(18,2)")]
    public decimal TotalCommissionAmount { get; set; }

    /// <summary>
    /// CALCULADO, COBRADO, PAGO
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "CALCULADO";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
}
