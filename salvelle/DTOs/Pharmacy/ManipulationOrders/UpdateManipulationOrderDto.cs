using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para atualização de uma ordem de manipulação
/// </summary>
public class UpdateManipulationOrderDto
{
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
    /// Registro profissional do prescritor
    /// </summary>
    [MaxLength(50)]
    public string? PrescriberRegistration { get; set; }

    /// <summary>
    /// Nome do cliente
    /// </summary>
    [Required(ErrorMessage = "O nome do cliente é obrigatório")]
    [MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    /// <summary>
    /// Telefone do cliente
    /// </summary>
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Quantidade a produzir
    /// </summary>
    [Required(ErrorMessage = "A quantidade a produzir é obrigatória")]
    [Range(0.01, 999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
    public decimal QuantityToProduce { get; set; }

    /// <summary>
    /// Unidade de medida
    /// </summary>
    [Required(ErrorMessage = "A unidade é obrigatória")]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Instruções especiais
    /// </summary>
    [MaxLength(2000)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Data prevista de entrega
    /// </summary>
    [Required(ErrorMessage = "A data prevista é obrigatória")]
    public DateTime ExpectedDate { get; set; }
}
