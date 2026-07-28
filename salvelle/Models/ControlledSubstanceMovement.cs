using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("controlled_substance_movements")]
public class ControlledSubstanceMovement
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("raw_material_id")]
    public Guid RawMaterialId { get; set; }

    [Column("batch_id")]
    public Guid? BatchId { get; set; }

    [Column("movement_date")]
    public DateTime MovementDate { get; set; }

    [Column("movement_type")]
    [MaxLength(20)]
    public string MovementType { get; set; } = string.Empty;
    // ENTRADA, SAIDA, TRANSFERENCIA, PERDA, DEVOLUCAO, AJUSTE

    // Classificação Portaria 344/98
    [Column("controlled_list")]
    [MaxLength(10)]
    public string ControlledList { get; set; } = string.Empty;
    // A1, A2, A3, B1, B2, C1, C2, C3, C4, C5

    [Column("substance_dcb_code")]
    [MaxLength(20)]
    public string SubstanceDcbCode { get; set; } = string.Empty;

    [Column("substance_name")]
    [MaxLength(200)]
    public string SubstanceName { get; set; } = string.Empty;

    // Quantidades
    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    [MaxLength(10)]
    public string Unit { get; set; } = string.Empty;

    [Column("balance_before")]
    public decimal BalanceBefore { get; set; }

    [Column("balance_after")]
    public decimal BalanceAfter { get; set; }

    // Origem/Destino
    [Column("supplier_id")]
    public Guid? SupplierId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("sale_id")]
    public Guid? SaleId { get; set; }

    [Column("purchase_order_id")]
    public Guid? PurchaseOrderId { get; set; }

    // Receituário (quando aplicável)
    [Column("prescription_number")]
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    [Column("prescription_type")]
    [MaxLength(20)]
    public string? PrescriptionType { get; set; }
    // AMARELA, AZUL, BRANCA_2_VIAS

    [Column("doctor_name")]
    [MaxLength(200)]
    public string? DoctorName { get; set; }

    [Column("doctor_crm")]
    [MaxLength(20)]
    public string? DoctorCrm { get; set; }

    [Column("patient_name")]
    [MaxLength(200)]
    public string? PatientName { get; set; }

    [Column("patient_cpf")]
    [MaxLength(11)]
    public string? PatientCpf { get; set; }

    // Nota Fiscal (entrada)
    [Column("invoice_number")]
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [Column("invoice_date")]
    public DateTime? InvoiceDate { get; set; }

    // SNGPC
    [Column("sngpc_sent")]
    public bool SngpcSent { get; set; } = false;

    [Column("sngpc_sent_at")]
    public DateTime? SngpcSentAt { get; set; }

    [Column("sngpc_protocol")]
    [MaxLength(50)]
    public string? SngpcProtocol { get; set; }

    [Column("sngpc_status")]
    [MaxLength(20)]
    public string? SngpcStatus { get; set; }
    // PENDENTE, ENVIADO, ACEITO, REJEITADO

    // Observações
    [Column("observations")]
    public string? Observations { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("authorized_by_pharmacist_id")]
    public Guid? AuthorizedByPharmacistId { get; set; }

    [Column("authorized_at")]
    public DateTime? AuthorizedAt { get; set; }
}