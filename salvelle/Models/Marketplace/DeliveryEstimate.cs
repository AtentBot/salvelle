using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("delivery_estimates")]
public class DeliveryEstimate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("estimated_minutes")]
    public int EstimatedMinutes { get; set; }

    [Column("estimated_delivery_at")]
    public DateTime EstimatedDeliveryAt { get; set; }

    [Column("actual_delivery_at")]
    public DateTime? ActualDeliveryAt { get; set; }

    /// <summary>
    /// ESTIMADO, EM_PREPARO, SAIU_ENTREGA, ENTREGUE, CANCELADO
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ESTIMADO";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public virtual OnlineOrder? Order { get; set; }

    // Status helpers
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "ESTIMADO" => "Previsão Informada",
        "EM_PREPARO" => "Em Preparo",
        "SAIU_ENTREGA" => "Saiu para Entrega",
        "ENTREGUE" => "Entregue",
        "CANCELADO" => "Cancelado",
        _ => Status
    };
}
