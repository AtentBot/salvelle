using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.CustomerFormulas;

public class CreateCustomFormulaDto
{
    [Required]
    public Guid ProductTypeId { get; set; }

    [Required]
    public Guid ProductSubTypeId { get; set; }

    [Required]
    [Range(0.1, 10000)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    [MaxLength(200)]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? CustomerEmail { get; set; }

    [MaxLength(1000)]
    public string? CustomerNotes { get; set; }

    public string? AdditionalIngredients { get; set; } // JSON
}

public class PharmaceuticalAnalysisDto
{
    [Required]
    public Guid CustomerFormulaId { get; set; }

    [Required]
    public string Analysis { get; set; } = default!;

    public bool RequiresPrescription { get; set; }
    
    public int EstimatedShelfLifeDays { get; set; } = 90;

    public string? RejectionReason { get; set; }
    
    public string? AdjustmentRequest { get; set; }

    public bool SafetyCheck { get; set; }
    public bool DosageCheck { get; set; }
    public bool InteractionCheck { get; set; }
    public bool StabilityCheck { get; set; }
}

public class AddToCartDto
{
    [Required]
    public Guid CustomerFormulaId { get; set; }
}

public class CustomerFormulaDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Status { get; set; } = default!;
    
    public string ProductTypeName { get; set; } = default!;
    public string ProductSubTypeName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    
    public bool RequiresPrescription { get; set; }
    public int? EstimatedShelfLifeDays { get; set; }
    
    public string? PharmaceuticalAnalysis { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal? PaidAmount { get; set; }
}
