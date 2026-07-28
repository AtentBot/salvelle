using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Componente/ingrediente de uma ordem de manipulação
/// </summary>
[Table("manipulation_order_components")]
public class ManipulationOrderComponent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    
    [Column("raw_material_id")]
    public Guid RawMaterialId { get; set; }
    
    [Column("batch_id")]
    public Guid? BatchId { get; set; }
    
    [Column("required_quantity")]
    public decimal RequiredQuantity { get; set; }
    
    [Column("weighed_quantity")]
    public decimal? WeighedQuantity { get; set; }
    
    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    [Column("unit_cost")]
    public decimal UnitCost { get; set; }
    
    [Column("total_cost")]
    public decimal TotalCost { get; set; }
    
    [Column("order_index")]
    public int OrderIndex { get; set; }
    
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE"; // PENDENTE, PESADO, CONFERIDO
    
    [Column("weighed_at")]
    public DateTime? WeighedAt { get; set; }
    
    [Column("weighed_by_employee_id")]
    public Guid? WeighedByEmployeeId { get; set; }
    
    [Column("checked_at")]
    public DateTime? CheckedAt { get; set; }
    
    [Column("checked_by_employee_id")]
    public Guid? CheckedByEmployeeId { get; set; }
    
    [Column("observations")]
    public string? Observations { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("ManipulationOrderId")]
    public virtual ManipulationOrder? ManipulationOrder { get; set; }
    
    [ForeignKey("RawMaterialId")]
    public virtual RawMaterial? RawMaterial { get; set; }
    
    [ForeignKey("BatchId")]
    public virtual Batch? Batch { get; set; }
}
