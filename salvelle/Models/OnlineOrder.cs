using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("OnlineOrders")]
public class OnlineOrder
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string OrderNumber { get; set; } = string.Empty;
    
    public Guid CustomerId { get; set; }
    
    public Guid EstablishmentId { get; set; }
    
    // ========== VÍNCULO COM PDV ==========
    public Guid? SaleId { get; set; }
    
    [ForeignKey("SaleId")]
    public virtual Sale? Sale { get; set; }
    // =====================================
    
    [StringLength(30)]
    public string Status { get; set; } = "PENDING";
    // PENDING, CONFIRMED, PREPARING, READY, DELIVERED, CANCELLED
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal DeliveryFee { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }
    
    [StringLength(30)]
    public string? PaymentMethod { get; set; }
    // CREDIT_CARD, DEBIT_CARD, PIX, CASH, ON_DELIVERY
    
    [StringLength(30)]
    public string PaymentStatus { get; set; } = "PENDING";
    // PENDING, PAID, REFUNDED
    
    [StringLength(20)]
    public string DeliveryType { get; set; } = "PICKUP";
    // PICKUP, DELIVERY
    
    public string? DeliveryAddress { get; set; }
    
    public string? CustomerNotes { get; set; }
    
    public DateTime? EstimatedReadyAt { get; set; }
    
    public DateTime? ReadyAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // ========== MARKETPLACE COMMISSION ==========
    [Column("platform_commission_rate", TypeName = "decimal(5,4)")]
    public decimal? PlatformCommissionRate { get; set; }

    [Column("platform_commission_amount", TypeName = "decimal(10,2)")]
    public decimal? PlatformCommissionAmount { get; set; }

    [Column("net_amount_to_pharmacy", TypeName = "decimal(10,2)")]
    public decimal? NetAmountToPharmacy { get; set; }

    [Column("stripe_payment_intent_id")]
    [StringLength(200)]
    public string? StripePaymentIntentId { get; set; }

    [Column("delivery_latitude")]
    public double? DeliveryLatitude { get; set; }

    [Column("delivery_longitude")]
    public double? DeliveryLongitude { get; set; }
    
    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
    
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
    
    public virtual ICollection<OnlineOrderItem> Items { get; set; } = new List<OnlineOrderItem>();
    
    // Status helpers
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "PENDING" => "Aguardando Confirmação",
        "CONFIRMED" => "Confirmado",
        "PREPARING" => "Em Preparação",
        "READY" => "Pronto para Retirada",
        "DELIVERED" => "Entregue",
        "CANCELLED" => "Cancelado",
        _ => Status
    };
    
    [NotMapped]
    public string StatusColor => Status switch
    {
        "PENDING" => "warning",
        "CONFIRMED" => "info",
        "PREPARING" => "primary",
        "READY" => "success",
        "DELIVERED" => "secondary",
        "CANCELLED" => "danger",
        _ => "secondary"
    };
    
    [NotMapped]
    public string PaymentStatusDisplay => PaymentStatus switch
    {
        "PENDING" => "Pendente",
        "PAID" => "Pago",
        "REFUNDED" => "Estornado",
        _ => PaymentStatus
    };
}

[Table("OnlineOrderItems")]
public class OnlineOrderItem
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid OrderId { get; set; }
    
    public Guid? ProductId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation
    [ForeignKey("OrderId")]
    public virtual OnlineOrder? Order { get; set; }
    
    [ForeignKey("ProductId")]
    public virtual CatalogProduct? Product { get; set; }
}
