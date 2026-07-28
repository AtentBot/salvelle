using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Cupom de desconto
/// </summary>
[Table("Coupons")]
public class Coupon
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Estabelecimento (null = válido para todos)
    /// </summary>
    [Column("EstablishmentId")]
    public Guid? EstablishmentId { get; set; }

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    /// <summary>
    /// Código do cupom (ex: PRIMEIRA15)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("Code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do cupom
    /// </summary>
    [MaxLength(200)]
    [Column("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// Tipo de desconto: PERCENTAGE, FIXED_VALUE
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("DiscountType")]
    public string DiscountType { get; set; } = "PERCENTAGE";

    /// <summary>
    /// Percentual de desconto (se tipo = PERCENTAGE)
    /// </summary>
    [Column("DiscountPercentage", TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Valor fixo de desconto (se tipo = FIXED_VALUE)
    /// </summary>
    [Column("DiscountValue", TypeName = "decimal(10,2)")]
    public decimal? DiscountValue { get; set; }

    /// <summary>
    /// Valor mínimo do pedido para usar o cupom
    /// </summary>
    [Column("MinOrderValue", TypeName = "decimal(10,2)")]
    public decimal? MinOrderValue { get; set; }

    /// <summary>
    /// Valor máximo de desconto (se tipo = PERCENTAGE)
    /// </summary>
    [Column("MaxDiscountValue", TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountValue { get; set; }

    /// <summary>
    /// Data início da validade
    /// </summary>
    [Column("ValidFrom")]
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Data fim da validade
    /// </summary>
    [Column("ValidUntil")]
    public DateTime ValidUntil { get; set; }

    /// <summary>
    /// Máximo de usos totais (null = ilimitado)
    /// </summary>
    [Column("MaxUses")]
    public int? MaxUses { get; set; }

    /// <summary>
    /// Máximo de usos por cliente (null = ilimitado)
    /// </summary>
    [Column("MaxUsesPerCustomer")]
    public int? MaxUsesPerCustomer { get; set; }

    /// <summary>
    /// Quantidade de vezes usado
    /// </summary>
    [Column("UsedCount")]
    public int UsedCount { get; set; } = 0;

    /// <summary>
    /// Apenas para primeira compra
    /// </summary>
    [Column("FirstPurchaseOnly")]
    public bool FirstPurchaseOnly { get; set; } = false;

    /// <summary>
    /// Categorias aplicáveis (JSON array ou null para todas)
    /// </summary>
    [Column("ApplicableCategories")]
    public string? ApplicableCategories { get; set; }

    /// <summary>
    /// Cupom ativo
    /// </summary>
    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Auditoria
    /// </summary>
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedByEmployeeId")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navigation Properties
    [ForeignKey("CreatedByEmployeeId")]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey("UpdatedByEmployeeId")]
    public virtual Employee? UpdatedByEmployee { get; set; }

    public virtual ICollection<CouponUsage>? Usages { get; set; }

    // Computed Properties
    [NotMapped]
    public bool IsValid => IsActive &&
                          ValidFrom <= DateTime.UtcNow &&
                          ValidUntil >= DateTime.UtcNow &&
                          (MaxUses == null || UsedCount < MaxUses);

    [NotMapped]
    public string DiscountDisplay => DiscountType == "PERCENTAGE"
        ? $"{DiscountPercentage}% OFF"
        : $"R$ {DiscountValue:N2} OFF";

    [NotMapped]
    public int DaysRemaining => (ValidUntil - DateTime.UtcNow).Days;
}

/// <summary>
/// Registro de uso de cupom
/// </summary>
[Table("CouponUsages")]
public class CouponUsage
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("CouponId")]
    public Guid CouponId { get; set; }

    [ForeignKey("CouponId")]
    public virtual Coupon? Coupon { get; set; }

    [Required]
    [Column("CustomerId")]
    public Guid CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Column("OrderId")]
    public Guid? OrderId { get; set; }

    [Column("DiscountApplied", TypeName = "decimal(10,2)")]
    public decimal DiscountApplied { get; set; }

    [Column("UsedAt")]
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}