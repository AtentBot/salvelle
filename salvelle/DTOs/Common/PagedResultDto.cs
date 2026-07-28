using System.Collections.Generic;

namespace DTOs;

/// <summary>
/// DTO genérico para respostas paginadas
/// </summary>
/// <typeparam name="T">Tipo do item da lista</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Lista de itens da página atual
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Número da página atual
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Tamanho da página
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total de itens em todas as páginas
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total de páginas
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indica se existe página anterior
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Indica se existe próxima página
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Construtor vazio
    /// </summary>
    public PagedResultDto() { }

    /// <summary>
    /// Construtor com parâmetros
    /// </summary>
    public PagedResultDto(List<T> items, int totalItems, int pageNumber, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }
}
