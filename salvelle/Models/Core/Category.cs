using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Models.Core;

[Index(nameof(Name), IsUnique = true)]
public class Category
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== IDENTIFICAÇÃO ====================
    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    // ==================== CONTROLE ====================
    public bool IsActive { get; set; } = true;

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    // ==================== RELACIONAMENTOS ====================
    public ICollection<Establishment>? Establishments { get; set; }
}