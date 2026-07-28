using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Fórmula personalizada criada pelo cliente no portal
/// Pode vir de uma receita processada por OCR ou criada manualmente
/// 
/// ATENÇÃO: Esta é a ÚNICA classe CustomerFormula do projeto.
/// Se existir outra em Models/, ela deve ser REMOVIDA.
/// </summary>
[Table("CustomerFormulas")]
public class CustomerFormula
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Code")]
    public string Code { get; set; } = default!;

    // ══════════════════════════════════════════════════════════════════════════
    // VÍNCULOS
    // ══════════════════════════════════════════════════════════════════════════

    [Required]
    [Column("EstablishmentId")]
    public Guid EstablishmentId { get; set; }

    [Column("CustomerId")]
    public Guid? CustomerId { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // DADOS DO CLIENTE (mesmo sem cadastro)
    // ══════════════════════════════════════════════════════════════════════════

    [MaxLength(200)]
    [Column("CustomerName")]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    [Column("CustomerPhone")]
    public string? CustomerPhone { get; set; }

    [MaxLength(200)]
    [Column("CustomerEmail")]
    public string? CustomerEmail { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO DO PRODUTO
    // ══════════════════════════════════════════════════════════════════════════

    [Required]
    [Column("ProductTypeId")]
    public Guid ProductTypeId { get; set; }
    
    [ForeignKey("ProductTypeId")]
    public ProductType? ProductType { get; set; }

    [Required]
    [Column("ProductSubTypeId")]
    public Guid ProductSubTypeId { get; set; }
    
    [ForeignKey("ProductSubTypeId")]
    public ProductSubType? ProductSubType { get; set; }

    [Required]
    [Column("Quantity")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("Unit")]
    public string Unit { get; set; } = default!;

    // ══════════════════════════════════════════════════════════════════════════
    // COMPONENTES E NOTAS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ingredientes adicionais em formato JSON
    /// </summary>
    [Column("AdditionalIngredients", TypeName = "jsonb")]
    public string? AdditionalIngredients { get; set; }

    [Column("CustomerNotes")]
    public string? CustomerNotes { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // FLUXO DE APROVAÇÃO
    // Status válidos: DRAFT, PENDING, ANALYZING, APPROVED, REJECTED, 
    //                 PROCESSING, READY, DELIVERED, CANCELLED
    // Também aceito para compatibilidade: AGUARDANDO_COMPRA, IN_CART, ORDERED, COMPLETED
    // ══════════════════════════════════════════════════════════════════════════

    [Required]
    [MaxLength(30)]
    [Column("Status")]
    public string Status { get; set; } = "AGUARDANDO_COMPRA";

    // ══════════════════════════════════════════════════════════════════════════
    // ANÁLISE FARMACÊUTICA
    // ══════════════════════════════════════════════════════════════════════════

    [Column("PharmacistId")]
    public Guid? PharmacistId { get; set; }

    [Column("PharmaceuticalAnalysis")]
    public string? PharmaceuticalAnalysis { get; set; }

    [Column("AnalyzedAt")]
    public DateTime? AnalyzedAt { get; set; }

    [Column("ApprovedAt")]
    public DateTime? ApprovedAt { get; set; }

    [Column("RejectedAt")]
    public DateTime? RejectedAt { get; set; }

    [Column("RejectionReason")]
    public string? RejectionReason { get; set; }

    [Column("AdjustmentRequest")]
    public string? AdjustmentRequest { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // VALIDAÇÕES TÉCNICAS
    // ══════════════════════════════════════════════════════════════════════════

    [Column("RequiresPrescription")]
    public bool RequiresPrescription { get; set; } = false;

    [Column("IsControlledSubstance")]
    public bool IsControlledSubstance { get; set; } = false;

    [Column("HasIncompatibilities")]
    public bool HasIncompatibilities { get; set; } = false;

    [Column("IncompatibilityDetails")]
    public string? IncompatibilityDetails { get; set; }

    [Column("EstimatedShelfLifeDays")]
    public int? EstimatedShelfLifeDays { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // PRECIFICAÇÃO
    // ══════════════════════════════════════════════════════════════════════════

    [Column("EstimatedPrice")]
    public decimal? EstimatedPrice { get; set; }

    [Column("FinalPrice")]
    public decimal? FinalPrice { get; set; }

    [Column("DiscountApplied")]
    public decimal DiscountApplied { get; set; } = 0;

    // ══════════════════════════════════════════════════════════════════════════
    // RELACIONAMENTOS
    // ══════════════════════════════════════════════════════════════════════════

    [Column("PrescriptionQuoteId")]
    public Guid? PrescriptionQuoteId { get; set; }

    [Column("ManipulationOrderId")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("OnlineOrderId")]
    public Guid? OnlineOrderId { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // PAGAMENTO
    // ══════════════════════════════════════════════════════════════════════════

    [Column("PaidAt")]
    public DateTime? PaidAt { get; set; }

    [Column("PaidAmount")]
    public decimal? PaidAmount { get; set; }

    [Column("RequiresRefund")]
    public bool RequiresRefund { get; set; } = false;

    [Column("RefundedAt")]
    public DateTime? RefundedAt { get; set; }

    [Column("RefundAmount")]
    public decimal? RefundAmount { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // TOKEN PARA SESSÃO
    // ══════════════════════════════════════════════════════════════════════════

    [MaxLength(100)]
    [Column("SessionToken")]
    public string? SessionToken { get; set; }

    [Column("SessionExpiresAt")]
    public DateTime? SessionExpiresAt { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // AUDITORIA
    // ══════════════════════════════════════════════════════════════════════════

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // NAVIGATION PROPERTIES
    // ══════════════════════════════════════════════════════════════════════════

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("PrescriptionQuoteId")]
    public virtual PrescriptionQuote? PrescriptionQuote { get; set; }

    public ICollection<PharmaceuticalAnalysisLog>? AnalysisLogs { get; set; }
    public ICollection<Refund>? Refunds { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES (para compatibilidade e conveniência)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Preço atual (final se disponível, senão estimado)
    /// </summary>
    [NotMapped]
    public decimal CurrentPrice => FinalPrice ?? EstimatedPrice ?? 0;

    /// <summary>
    /// Indica se foi pago
    /// </summary>
    [NotMapped]
    public bool IsPaid => PaidAt.HasValue;

    /// <summary>
    /// Nome para exibição
    /// </summary>
    [NotMapped]
    public string DisplayName => 
        $"Fórmula {ProductType?.Name ?? "Personalizada"} - {Quantity} {Unit}";

    /// <summary>
    /// Forma farmacêutica (via ProductType)
    /// Mantido para compatibilidade com código legado
    /// </summary>
    [NotMapped]
    public string PharmaceuticalForm => ProductType?.Name ?? "Manipulado";

    /// <summary>
    /// Quantidade total formatada
    /// Mantido para compatibilidade com código legado
    /// </summary>
    [NotMapped]
    public string TotalQuantity => $"{Quantity} {Unit}";

    /// <summary>
    /// Preço total (alias para CurrentPrice)
    /// Mantido para compatibilidade com código legado
    /// </summary>
    [NotMapped]
    public decimal TotalPrice => CurrentPrice;

    /// <summary>
    /// Composição em JSON (alias para AdditionalIngredients)
    /// Mantido para compatibilidade com código legado
    /// </summary>
    [NotMapped]
    public string? Composition => AdditionalIngredients;

    /// <summary>
    /// Instruções (alias para CustomerNotes)
    /// Mantido para compatibilidade com código legado
    /// </summary>
    [NotMapped]
    public string? Instructions => CustomerNotes;

    /// <summary>
    /// Status formatado para exibição
    /// </summary>
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "DRAFT" or "AGUARDANDO_COMPRA" => "Aguardando Compra",
        "PENDING" or "IN_CART" => "No Carrinho",
        "ANALYZING" => "Em Análise",
        "APPROVED" => "Aprovado",
        "REJECTED" => "Rejeitado",
        "PROCESSING" or "ORDERED" => "Em Produção",
        "READY" => "Pronto para Retirada",
        "DELIVERED" or "COMPLETED" => "Entregue",
        "CANCELLED" => "Cancelado",
        _ => Status
    };
}
