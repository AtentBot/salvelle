using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("controlled_substance_balances")]
public class ControlledSubstanceBalance
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("raw_material_id")]
    public Guid RawMaterialId { get; set; }

    [Column("reference_date")]
    public DateTime ReferenceDate { get; set; }

    [Column("balance_type")]
    [MaxLength(20)]
    public string BalanceType { get; set; } = string.Empty;
    // MENSAL, TRIMESTRAL, ANUAL

    [Column("controlled_list")]
    [MaxLength(10)]
    public string ControlledList { get; set; } = string.Empty;

    [Column("substance_dcb_code")]
    [MaxLength(20)]
    public string SubstanceDcbCode { get; set; } = string.Empty;

    [Column("substance_name")]
    [MaxLength(200)]
    public string SubstanceName { get; set; } = string.Empty;

    // Saldos
    [Column("initial_balance")]
    public decimal InitialBalance { get; set; }

    [Column("total_entries")]
    public decimal TotalEntries { get; set; }

    [Column("total_exits")]
    public decimal TotalExits { get; set; }

    [Column("total_losses")]
    public decimal TotalLosses { get; set; }

    [Column("total_adjustments")]
    public decimal TotalAdjustments { get; set; }

    [Column("final_balance")]
    public decimal FinalBalance { get; set; }

    [Column("physical_balance")]
    public decimal? PhysicalBalance { get; set; }

    [Column("difference")]
    public decimal? Difference { get; set; }

    [Column("unit")]
    [MaxLength(10)]
    public string Unit { get; set; } = string.Empty;

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ABERTO";
    // ABERTO, FECHADO, ENVIADO

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("closed_by_employee_id")]
    public Guid? ClosedByEmployeeId { get; set; }

    // SNGPC
    [Column("sngpc_sent")]
    public bool SngpcSent { get; set; } = false;

    [Column("sngpc_sent_at")]
    public DateTime? SngpcSentAt { get; set; }

    [Column("sngpc_protocol")]
    [MaxLength(50)]
    public string? SngpcProtocol { get; set; }

    [Column("observations")]
    public string? Observations { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }
}