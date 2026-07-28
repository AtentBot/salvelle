using System;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para filtros de consulta de ordens de manipulação
/// </summary>
public class ManipulationOrderFilterDto
{
    /// <summary>
    /// Filtrar por número da ordem (busca parcial)
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Filtrar por status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filtrar por ID da fórmula
    /// </summary>
    public Guid? FormulaId { get; set; }

    /// <summary>
    /// Filtrar por nome do cliente (busca parcial)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Filtrar por funcionário que solicitou
    /// </summary>
    public Guid? RequestedByEmployeeId { get; set; }

    /// <summary>
    /// Filtrar por funcionário que manipulou
    /// </summary>
    public Guid? ManipulatedByEmployeeId { get; set; }

    /// <summary>
    /// Filtrar por data de pedido inicial
    /// </summary>
    public DateTime? OrderDateFrom { get; set; }

    /// <summary>
    /// Filtrar por data de pedido final
    /// </summary>
    public DateTime? OrderDateTo { get; set; }

    /// <summary>
    /// Filtrar por data prevista inicial
    /// </summary>
    public DateTime? ExpectedDateFrom { get; set; }

    /// <summary>
    /// Filtrar por data prevista final
    /// </summary>
    public DateTime? ExpectedDateTo { get; set; }

    /// <summary>
    /// Incluir apenas ordens atrasadas
    /// </summary>
    public bool? OnlyOverdue { get; set; }

    /// <summary>
    /// Incluir apenas ordens que passaram no controle de qualidade
    /// </summary>
    public bool? PassedQualityControl { get; set; }

    /// <summary>
    /// Incluir apenas ordens com controle especial
    /// </summary>
    public bool? OnlySpecialControl { get; set; }

    /// <summary>
    /// Número da página (para paginação)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Tamanho da página (para paginação)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string? SortBy { get; set; } = "OrderDate";

    /// <summary>
    /// Ordem crescente ou decrescente
    /// </summary>
    public bool Ascending { get; set; } = false;
}
