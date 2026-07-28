using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Registro de perdas durante a manipulação (RDC 67/2007)
/// </summary>
[Table("manipulation_losses")]
public class ManipulationLoss
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("raw_material_id")]
    public Guid? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Column("reason")]
    [MaxLength(500)]
    public string Reason { get; set; } = default!;

    [Column("loss_type")]
    [MaxLength(50)]
    public string LossType { get; set; } = "PROCESSO"; // PROCESSO, QUEBRA, CONTAMINACAO, VENCIMENTO, OUTRO

    [Column("registered_by_employee_id")]
    public Guid RegisteredByEmployeeId { get; set; }
    public Employee? RegisteredByEmployee { get; set; }

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [Column("value_lost")]
    public decimal? ValueLost { get; set; }
}

/// <summary>
/// Registro de sobras durante a manipulação
/// </summary>
[Table("manipulation_leftovers")]
public class ManipulationLeftover
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("raw_material_id")]
    public Guid? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Column("destination")]
    [MaxLength(200)]
    public string Destination { get; set; } = default!; // REINTEGRAR_ESTOQUE, DESCARTE, OUTRA_MANIPULACAO

    [Column("destination_details")]
    [MaxLength(500)]
    public string? DestinationDetails { get; set; }

    [Column("registered_by_employee_id")]
    public Guid RegisteredByEmployeeId { get; set; }
    public Employee? RegisteredByEmployee { get; set; }

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [Column("reintegrated_to_stock")]
    public bool ReintegratedToStock { get; set; } = false;

    [Column("reintegration_date")]
    public DateTime? ReintegrationDate { get; set; }
}

/// <summary>
/// Conferência dupla (requisito RDC 67/2007)
/// </summary>
[Table("dual_verifications")]
public class DualVerification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("verification_type")]
    [MaxLength(50)]
    public string VerificationType { get; set; } = default!; // PESAGEM, ROTULAGEM, CONFERENCIA_FINAL

    // Primeira verificação (geralmente o manipulador)
    [Column("first_verifier_id")]
    public Guid FirstVerifierId { get; set; }
    public Employee? FirstVerifier { get; set; }

    [Column("first_verification_at")]
    public DateTime FirstVerificationAt { get; set; }

    [Column("first_verifier_notes")]
    [MaxLength(500)]
    public string? FirstVerifierNotes { get; set; }

    [Column("first_verifier_signature")]
    [MaxLength(500)]
    public string? FirstVerifierSignature { get; set; } // Base64 ou hash

    // Segunda verificação (conferente ou farmacêutico)
    [Column("second_verifier_id")]
    public Guid SecondVerifierId { get; set; }
    public Employee? SecondVerifier { get; set; }

    [Column("second_verification_at")]
    public DateTime? SecondVerificationAt { get; set; }

    [Column("second_verifier_notes")]
    [MaxLength(500)]
    public string? SecondVerifierNotes { get; set; }

    [Column("second_verifier_signature")]
    [MaxLength(500)]
    public string? SecondVerifierSignature { get; set; }

    // Resultado
    [Column("approved")]
    public bool Approved { get; set; }

    [Column("rejection_reason")]
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Detalhes da verificação
    [Column("checklist_json")]
    public string? ChecklistJson { get; set; } // JSON com itens verificados

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Registro de produção - quantidade real produzida
/// </summary>
[Table("production_records")]
public class ProductionRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }

    [Column("produced_by_employee_id")]
    public Guid ProducedByEmployeeId { get; set; }

    [Column("verified_by_employee_id")]
    public Guid? VerifiedByEmployeeId { get; set; }

    [Column("approved_by_pharmacist_id")]
    public Guid? ApprovedByPharmacistId { get; set; }

    // Dados da Produção
    [Column("batch_number")]
    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    [Column("production_date")]
    public DateTime ProductionDate { get; set; }

    [Column("expiration_date")]
    public DateTime ExpirationDate { get; set; }

    [Column("quantity_produced")]
    public decimal QuantityProduced { get; set; }

    [Column("unit")]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    // Condições Ambientais
    [Column("temperature")]
    public decimal? Temperature { get; set; }

    [Column("humidity")]
    public decimal? Humidity { get; set; }

    [Column("environmental_conditions")]
    [StringLength(500)]
    public string? EnvironmentalConditions { get; set; }

    // Equipamentos Utilizados
    [Column("equipment_used")]
    [StringLength(1000)]
    public string? EquipmentUsed { get; set; }

    // Observações
    [Column("observations")]
    [StringLength(2000)]
    public string? Observations { get; set; }

    // Controle de Qualidade
    [Column("quality_check_passed")]
    public bool QualityCheckPassed { get; set; }

    [Column("quality_check_notes")]
    [StringLength(1000)]
    public string? QualityCheckNotes { get; set; }

    // Status
    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // ==================== NAVIGATION PROPERTIES ====================

    /// <summary>
    /// Ordem de manipulação relacionada
    /// </summary>
    [ForeignKey("ManipulationOrderId")]
    public virtual ManipulationOrder? ManipulationOrder { get; set; }

    /// <summary>
    /// Funcionário que registrou/produziu
    /// </summary>
    //[ForeignKey("ProducedByEmployeeId")]
    //public virtual Employee? RecordedByEmployee { get; set; }

    /// <summary>
    /// Funcionário que produziu (mesmo que RecordedByEmployee)
    /// </summary>
    [ForeignKey("ProducedByEmployeeId")]
    public virtual Employee? ProducedByEmployee { get; set; }

    /// <summary>
    /// Funcionário que verificou
    /// </summary>
    [ForeignKey("VerifiedByEmployeeId")]
    public virtual Employee? VerifiedByEmployee { get; set; }

    /// <summary>
    /// Farmacêutico que aprovou
    /// </summary>
    [ForeignKey("ApprovedByPharmacistId")]
    public virtual Employee? ApprovedByPharmacist { get; set; }

    // ==================== YIELD/RENDIMENTO ====================
    [Column("expected_quantity")]
    public decimal? ExpectedQuantity { get; set; }

    [Column("actual_quantity")]
    public decimal? ActualQuantity { get; set; }

    [Column("yield_percentage")]
    public decimal? YieldPercentage { get; set; }

    [Column("is_yield_acceptable")]
    public bool IsYieldAcceptable { get; set; } = true;

    [Column("yield_deviation_reason")]
    [StringLength(500)]
    public string? YieldDeviationReason { get; set; }

    // ==================== DATAS DE PRODUÇÃO ====================
    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [Column("production_start")]
    public DateTime? ProductionStart { get; set; }

    [Column("production_end")]
    public DateTime? ProductionEnd { get; set; }

    [Column("total_production_time_minutes")]
    public int? TotalProductionTimeMinutes { get; set; }

    // ==================== QUALIDADE ====================
    [Column("quality_notes")]
    [StringLength(2000)]
    public string? QualityNotes { get; set; }
}
