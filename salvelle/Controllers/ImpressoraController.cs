using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Integration;
using Data;
using Models.Employees;

namespace Controllers.Api;

/// <summary>
/// API de Impressão Térmica - Cupons e Etiquetas
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImpressoraController : ControllerBase
{
    private readonly ThermalPrinterService _printerService;
    private readonly AppDbContext _db;
    private readonly ILogger<ImpressoraController> _logger;

    public ImpressoraController(
        ThermalPrinterService printerService,
        AppDbContext db,
        ILogger<ImpressoraController> logger)
    {
        _printerService = printerService;
        _db = db;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    /// <summary>
    /// Lista impressoras instaladas no sistema
    /// GET /api/impressora/listar
    /// </summary>
    [HttpGet("listar")]
    public IActionResult ListarImpressoras()
    {
        try
        {
            // Usa método estático do serviço (simulado para desenvolvimento)
            var impressoras = ThermalPrinterService.GetInstalledPrinters();

            return Ok(new
            {
                success = true,
                impressoras,
                padrao = "DefaultPrinter",
                total = impressoras.Length,
                nota = "Lista simulada - configure impressoras reais no ambiente de produção"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar impressoras");
            return StatusCode(500, new { error = "Erro ao listar impressoras" });
        }
    }

    /// <summary>
    /// Imprime cupom de uma venda
    /// POST /api/impressora/cupom/venda/{saleId}
    /// </summary>
    [HttpPost("cupom/venda/{saleId:guid}")]
    public async Task<IActionResult> ImprimirCupomVenda(Guid saleId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var venda = await _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.EstablishmentId == establishmentId);

            if (venda == null)
                return NotFound(new { error = "Venda não encontrada" });

            var estabelecimento = await _db.Establishments.FindAsync(establishmentId);

            var cupom = new CupomPrintData
            {
                NomeFarmacia = estabelecimento?.NomeFantasia ?? estabelecimento?.RazaoSocial,
                CNPJ = estabelecimento?.Cnpj,
                Endereco = estabelecimento != null 
                    ? $"{estabelecimento.Street}, {estabelecimento.Number} - {estabelecimento.City}/{estabelecimento.State}"
                    : null,
                Numero = venda.Code ?? venda.Id.ToString()[..8].ToUpper(),
                Data = venda.CreatedAt,
                Cliente = venda.Customer?.FullName,
                Itens = venda.Items?.Select(i => new CupomItemPrint
                {
                    Descricao = i.Description ?? "Item",
                    Quantidade = i.Quantity,
                    ValorUnitario = i.UnitPrice,
                    ValorTotal = i.TotalPrice
                }).ToList() ?? new List<CupomItemPrint>(),
                Subtotal = venda.Subtotal,
                Desconto = venda.DiscountAmount,
                Total = venda.TotalAmount,
                Pagamentos = venda.Payments?.Select(p => new CupomPagamentoPrint
                {
                    Forma = FormatarFormaPagamento(p.PaymentMethod),
                    Valor = p.Amount
                }).ToList(),
                Troco = venda.ChangeAmount ?? 0
            };

            var result = await _printerService.PrintCupomAsync(cupom);

            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir cupom da venda {SaleId}", saleId);
            return StatusCode(500, new { error = "Erro ao imprimir cupom" });
        }
    }

    /// <summary>
    /// Imprime etiqueta de produto manipulado
    /// POST /api/impressora/etiqueta/ordem/{orderId}
    /// </summary>
    [HttpPost("etiqueta/ordem/{orderId:guid}")]
    public async Task<IActionResult> ImprimirEtiquetaOrdem(Guid orderId, [FromQuery] int copias = 1)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var ordem = await _db.ManipulationOrders
                .Include(o => o.Formula)
                .Include(o => o.ApprovedByPharmacist)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

            if (ordem == null)
                return NotFound(new { error = "Ordem não encontrada" });

            var estabelecimento = await _db.Establishments.FindAsync(establishmentId);

            // Buscar composição da fórmula
            var composicao = "";
            if (ordem.Formula != null)
            {
                var componentes = await _db.FormulaComponents
                    .Include(c => c.RawMaterial)
                    .Where(c => c.FormulaId == ordem.FormulaId)
                    .ToListAsync();

                composicao = string.Join(", ", componentes.Select(c => 
                    $"{c.RawMaterial?.Name} {c.Quantity}{c.Unit}"));
            }

            var etiqueta = new EtiquetaPrintData
            {
                NomeFarmacia = estabelecimento?.NomeFantasia,
                NomeProduto = ordem.Formula?.Name ?? "Fórmula Manipulada",
                Composicao = composicao,
                Posologia = ordem.SpecialInstructions,
                ViaAdministracao = ordem.Formula?.PharmaceuticalForm,
                Quantidade = ordem.QuantityToProduce.ToString("N0"),
                Unidade = ordem.Unit ?? "UN",
                NomePaciente = ordem.CustomerName,
                Lote = ordem.OrderNumber,
                Validade = ordem.ExpiryDate,
                DataManipulacao = ordem.CompletionDate ?? DateTime.UtcNow,
                FarmaceuticoRT = ordem.ApprovedByPharmacist?.FullName,
                CodigoBarras = ordem.OrderNumber
            };

            for (int i = 0; i < copias; i++)
            {
                var result = await _printerService.PrintEtiquetaAsync(etiqueta);
                if (!result.Success)
                    return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Etiqueta impressa: Ordem {OrderNumber}, {Copias} cópia(s)", 
                ordem.OrderNumber, copias);

            return Ok(new { success = true, message = $"{copias} etiqueta(s) impressa(s)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir etiqueta da ordem {OrderId}", orderId);
            return StatusCode(500, new { error = "Erro ao imprimir etiqueta" });
        }
    }

    /// <summary>
    /// Imprime etiqueta de lote de matéria-prima
    /// POST /api/impressora/etiqueta/lote/{batchId}
    /// </summary>
    [HttpPost("etiqueta/lote/{batchId:guid}")]
    public async Task<IActionResult> ImprimirEtiquetaLote(Guid batchId, [FromQuery] int copias = 1)
    {
        try
        {
            var lote = await _db.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (lote == null)
                return NotFound(new { error = "Lote não encontrado" });

            var etiqueta = new EtiquetaLotePrintData
            {
                NomeMateriaPrima = lote.RawMaterial?.Name ?? "Matéria-Prima",
                CodigoDCB = lote.RawMaterial?.DcbCode,
                NumeroLote = lote.BatchNumber,
                Validade = lote.ExpiryDate,
                Fornecedor = lote.Supplier?.CompanyName ?? lote.Supplier?.TradeName,
                Quantidade = lote.CurrentQuantity,
                Unidade = lote.RawMaterial?.Unit ?? "g",
                CodigoBarras = lote.BatchNumber,
                EmQuarentena = lote.Status == "QUARENTENA"
            };

            for (int i = 0; i < copias; i++)
            {
                var result = await _printerService.PrintEtiquetaLoteAsync(etiqueta);
                if (!result.Success)
                    return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Etiqueta de lote impressa: {BatchNumber}, {Copias} cópia(s)", 
                lote.BatchNumber, copias);

            return Ok(new { success = true, message = $"{copias} etiqueta(s) impressa(s)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir etiqueta do lote {BatchId}", batchId);
            return StatusCode(500, new { error = "Erro ao imprimir etiqueta" });
        }
    }

    /// <summary>
    /// Teste de impressão
    /// POST /api/impressora/teste
    /// </summary>
    [HttpPost("teste")]
    public async Task<IActionResult> TesteImpressao([FromBody] TesteImpressaoDto dto)
    {
        try
        {
            var cupom = new CupomPrintData
            {
                NomeFarmacia = "TESTE - SALVELLE",
                CNPJ = "00.000.000/0001-00",
                Endereco = "Rua Teste, 123 - Centro",
                Numero = "TESTE-001",
                Data = Helpers.BrazilTime.Now,
                Cliente = "CLIENTE TESTE",
                Itens = new List<CupomItemPrint>
                {
                    new() { Descricao = "Item de Teste 1", Quantidade = 1, ValorUnitario = 10, ValorTotal = 10 },
                    new() { Descricao = "Item de Teste 2", Quantidade = 2, ValorUnitario = 5, ValorTotal = 10 }
                },
                Subtotal = 20,
                Desconto = 0,
                Total = 20,
                Pagamentos = new List<CupomPagamentoPrint>
                {
                    new() { Forma = "Dinheiro", Valor = 20 }
                }
            };

            var result = await _printerService.PrintCupomAsync(cupom);

            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no teste de impressão");
            return StatusCode(500, new { error = "Erro no teste" });
        }
    }

    #region Helpers

    private string FormatarFormaPagamento(string? metodo)
    {
        return metodo?.ToUpper() switch
        {
            "DINHEIRO" => "Dinheiro",
            "CARTAO_CREDITO" => "Cartão Crédito",
            "CARTAO_DEBITO" => "Cartão Débito",
            "PIX" => "PIX",
            "BOLETO" => "Boleto",
            _ => metodo ?? "Outros"
        };
    }

    #endregion
}

public class TesteImpressaoDto
{
    public string? ImpressoraCupom { get; set; }
    public string? ImpressoraEtiqueta { get; set; }
}
