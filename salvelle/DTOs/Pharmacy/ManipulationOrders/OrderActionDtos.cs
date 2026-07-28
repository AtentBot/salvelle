using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para mudança de status de uma ordem de manipulação
/// </summary>
public class ChangeOrderStatusDto
{
    /// <summary>
    /// Novo status da ordem
    /// </summary>
    [Required(ErrorMessage = "O status é obrigatório")]
    [MaxLength(20)]
    public string NewStatus { get; set; } = default!;
    // PENDENTE, EM_PRODUCAO, PESAGEM, MISTURA, ENVASE, ROTULAGEM, CONFERENCIA, FINALIZADO, CANCELADO

    /// <summary>
    /// Observações sobre a mudança de status
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para atribuir responsável pela manipulação
/// </summary>
public class AssignManipulatorDto
{
    /// <summary>
    /// ID do funcionário que irá manipular
    /// </summary>
    [Required(ErrorMessage = "O ID do funcionário é obrigatório")]
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// DTO para registrar conferência da ordem
/// </summary>
public class RegisterCheckDto
{
    /// <summary>
    /// ID do funcionário que conferiu
    /// </summary>
    [Required(ErrorMessage = "O ID do funcionário é obrigatório")]
    public Guid CheckedByEmployeeId { get; set; }

    /// <summary>
    /// Observações da conferência
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para aprovação farmacêutica
/// </summary>
public class PharmaceuticalApprovalDto
{
    /// <summary>
    /// ID do farmacêutico aprovador
    /// </summary>
    [Required(ErrorMessage = "O ID do farmacêutico é obrigatório")]
    public Guid PharmacistId { get; set; }

    /// <summary>
    /// Aprovado ou reprovado
    /// </summary>
    [Required(ErrorMessage = "O status de aprovação é obrigatório")]
    public bool Approved { get; set; }

    /// <summary>
    /// Observações do controle de qualidade
    /// </summary>
    [MaxLength(1000)]
    public string? QualityNotes { get; set; }

    /// <summary>
    /// Data de validade do produto manipulado
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// DTO para cancelamento de ordem
/// </summary>
public class CancelOrderDto
{
    /// <summary>
    /// Motivo do cancelamento
    /// </summary>
    [Required(ErrorMessage = "O motivo do cancelamento é obrigatório")]
    [MaxLength(500)]
    public string Reason { get; set; } = default!;
}
