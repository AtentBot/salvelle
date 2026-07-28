using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para criação de uma ordem de manipulação
/// </summary>
public class CreateManipulationOrderDto
{
    /// <summary>
    /// Número da ordem (se não fornecido, será gerado automaticamente)
    /// </summary>
    [MaxLength(50)]
    public string? OrderNumber { get; set; }

    /// <summary>
    /// ID da fórmula base (opcional para manipulação magistral)
    /// </summary>
    public Guid? FormulaId { get; set; }

    /// <summary>
    /// Número da receita médica
    /// </summary>
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    /// <summary>
    /// Nome do prescritor
    /// </summary>
    [MaxLength(200)]
    public string? PrescriberName { get; set; }

    /// <summary>
    /// Número de registro profissional do prescritor (CRM, CRO, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? PrescriberRegistration { get; set; }

    /// <summary>
    /// Nome do cliente/paciente
    /// </summary>
    [Required(ErrorMessage = "O nome do cliente é obrigatório")]
    [MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    /// <summary>
    /// Telefone do cliente
    /// </summary>
    [MaxLength(20)]
    [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Formato de telefone inválido")]
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Quantidade a produzir
    /// </summary>
    [Required(ErrorMessage = "A quantidade a produzir é obrigatória")]
    [Range(0.01, 999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
    public decimal QuantityToProduce { get; set; }

    /// <summary>
    /// Unidade de medida da quantidade
    /// </summary>
    [Required(ErrorMessage = "A unidade é obrigatória")]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Instruções especiais para a manipulação
    /// </summary>
    [MaxLength(2000)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Data prevista para entrega
    /// </summary>
    [Required(ErrorMessage = "A data prevista é obrigatória")]
    public DateTime ExpectedDate { get; set; }

    /// <summary>
    /// Componentes customizados (para fórmulas magistrais sem fórmula base)
    /// </summary>
    public List<ManipulationOrderComponentDto>? CustomComponents { get; set; }
}

/// <summary>
/// DTO para componente customizado de ordem de manipulação
/// </summary>
public class ManipulationOrderComponentDto
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    [Range(0.000001, 999999.999999)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }
}
