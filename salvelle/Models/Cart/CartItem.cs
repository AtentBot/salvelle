using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Cart;

[Table("CartItems")]
public class CartItem
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("SessionToken")]
    public string SessionToken { get; set; } = default!;

    [Column("CustomerId")]
    public Guid? CustomerId { get; set; }

    [Required]
    [Column("EstablishmentId")]
    public Guid EstablishmentId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("ItemType")]
    public string ItemType { get; set; } = default!;

    [Column("ReferenceId")]
    public Guid? ReferenceId { get; set; }

    [Required]
    [MaxLength(300)]
    [Column("Name")]
    public string Name { get; set; } = default!;

    [Column("Description")]
    public string? Description { get; set; }

    [Column("Quantity")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column("UnitPrice")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("TotalPrice")]
    public decimal TotalPrice { get; set; }

    [Column("RequiresPrescription")]
    public bool RequiresPrescription { get; set; } = false;

    [Column("IsControlled")]
    public bool IsControlled { get; set; } = false;

    [Column("IsCustomFormula")]
    public bool IsCustomFormula { get; set; } = false;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("ExpiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
