using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart;

// ====================================================================
// DTOs PARA API DO CARRINHO DE COMPRAS
// ====================================================================

/// <summary>
/// DTO para adicionar produto do catálogo ao carrinho
/// Usado em: POST /api/cliente/cart/add
/// </summary>
public class AddProductToCartDto
{
    public Guid ProductId { get; set; }

    // Antes sem limite: quantidade 0/negativa passava pela checagem de estoque
    // (StockQuantity < negativo é falso) e gerava subtotal negativo no carrinho.
    [Range(1, 99, ErrorMessage = "Quantidade deve estar entre 1 e 99.")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// DTO para adicionar fórmula personalizada ao carrinho
/// Usado em: POST /api/cliente/cart/add-formula
/// </summary>
public class AddFormulaToCartDto
{
    // Identificação do tipo de produto
    public Guid ProductTypeId { get; set; }
    public Guid ProductSubTypeId { get; set; }
    
    // Nomes para exibição (usados em BuildFormulaDescription)
    public string? ProductTypeName { get; set; }
    public string? ProductSubTypeName { get; set; }
    
    // Quantidade e unidade da fórmula
    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Quantidade da fórmula inválida.")]
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; } = "g";
    
    // Dados do cliente/paciente
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerNotes { get; set; }
    
    // Ingredientes da fórmula
    public List<FormulaIngredientDto> Ingredients { get; set; } = new();
    
    // Preço estimado calculado
    public decimal EstimatedPrice { get; set; }
    
    // Foto da receita (opcional)
    public string? PrescriptionPhotoUrl { get; set; }
}

/// <summary>
/// DTO para ingrediente da fórmula
/// </summary>
public class FormulaIngredientDto
{
    public Guid? RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "mg";
}

/// <summary>
/// DTO para atualizar quantidade de item no carrinho
/// Usado em: POST /api/cliente/cart/update
/// </summary>
public class UpdateCartItemDto
{
    /// <summary>
    /// ID do item no carrinho
    /// </summary>
    public Guid ItemId { get; set; }
    
    /// <summary>
    /// Delta de quantidade: -1 para diminuir, +1 para aumentar
    /// </summary>
    [Range(-99, 99, ErrorMessage = "Ajuste de quantidade fora do intervalo permitido.")]
    public int Delta { get; set; }
}

/// <summary>
/// DTO para remover item do carrinho
/// Usado em: POST /api/cliente/cart/remove
/// </summary>
public class RemoveCartItemDto
{
    /// <summary>
    /// ID do item a ser removido
    /// </summary>
    public Guid ItemId { get; set; }
}

/// <summary>
/// DTO para criar pedido a partir do carrinho
/// Usado em: POST /api/cliente/orders/create
/// </summary>
public class CreateOrderFromCartDto
{
    /// <summary>
    /// Observações do cliente para o pedido
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Tipo de entrega: PICKUP (retirada) ou DELIVERY (entrega)
    /// </summary>
    public string DeliveryType { get; set; } = "PICKUP";
    
    /// <summary>
    /// Endereço de entrega (obrigatório se DeliveryType = DELIVERY)
    /// </summary>
    public string? DeliveryAddress { get; set; }
    
    /// <summary>
    /// Método de pagamento: PIX, CREDIT_CARD, DEBIT_CARD, CASH
    /// </summary>
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Código do cupom de desconto (opcional)
    /// </summary>
    public string? CouponCode { get; set; }
}
