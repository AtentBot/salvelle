using Microsoft.AspNetCore.Mvc;
using Services.Integration;
using Data;
using Models.Employees;

namespace Controllers.Api;

/// <summary>
/// API de Integração com Balanças Analíticas
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BalancaController : ControllerBase
{
    private readonly BalanceIntegrationService _balanceService;
    private readonly AppDbContext _db;
    private readonly ILogger<BalancaController> _logger;

    public BalancaController(
        BalanceIntegrationService balanceService,
        AppDbContext db,
        ILogger<BalancaController> logger)
    {
        _balanceService = balanceService;
        _db = db;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;

    /// <summary>
    /// Lista portas seriais disponíveis
    /// GET /api/balanca/portas
    /// </summary>
    [HttpGet("portas")]
    public IActionResult GetPortasDisponiveis()
    {
        try
        {
            var portas = BalanceIntegrationService.GetAvailablePorts();
            return Ok(new
            {
                success = true,
                portas,
                total = portas.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar portas");
            return StatusCode(500, new { error = "Erro ao listar portas seriais" });
        }
    }

    /// <summary>
    /// Conecta à balança
    /// POST /api/balanca/conectar
    /// </summary>
    [HttpPost("conectar")]
    public async Task<IActionResult> Conectar()
    {
        var result = await _balanceService.ConnectAsync();
        return Ok(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Desconecta da balança
    /// POST /api/balanca/desconectar
    /// </summary>
    [HttpPost("desconectar")]
    public IActionResult Desconectar()
    {
        _balanceService.Disconnect();
        return Ok(new { success = true, message = "Desconectado" });
    }

    /// <summary>
    /// Lê peso atual da balança
    /// GET /api/balanca/peso
    /// </summary>
    [HttpGet("peso")]
    public async Task<IActionResult> LerPeso()
    {
        var result = await _balanceService.ReadWeightAsync();
        
        if (!result.Success)
            return BadRequest(new { success = false, error = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            peso = result.Weight,
            unidade = result.Unit,
            estavel = result.IsStable,
            leituraEm = result.ReadAt
        });
    }

    /// <summary>
    /// Realiza tara na balança
    /// POST /api/balanca/tarar
    /// </summary>
    [HttpPost("tarar")]
    public async Task<IActionResult> Tarar()
    {
        var result = await _balanceService.TareAsync();
        return Ok(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Testa conexão com a balança
    /// POST /api/balanca/testar
    /// </summary>
    [HttpPost("testar")]
    public async Task<IActionResult> TestarConexao()
    {
        var result = await _balanceService.TestConnectionAsync();
        
        return Ok(new
        {
            success = result.Success,
            message = result.Message,
            leitura = result.Reading != null ? new
            {
                peso = result.Reading.Weight,
                unidade = result.Reading.Unit,
                estavel = result.Reading.IsStable
            } : null
        });
    }

    /// <summary>
    /// Registra pesagem para um componente da ordem de manipulação
    /// POST /api/balanca/registrar-pesagem
    /// </summary>
    [HttpPost("registrar-pesagem")]
    public async Task<IActionResult> RegistrarPesagem([FromBody] RegistrarPesagemDto dto)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return Unauthorized(new { error = "Não autenticado" });

        // Ler peso da balança
        var leitura = await _balanceService.ReadWeightAsync();
        if (!leitura.Success)
            return BadRequest(new { error = leitura.ErrorMessage ?? "Erro na leitura" });

        if (!leitura.IsStable)
            return BadRequest(new { error = "Aguarde estabilização da balança" });

        // Buscar componente
        var componente = await _db.ManipulationOrderComponents
            .FindAsync(dto.ComponenteId);

        if (componente == null)
            return NotFound(new { error = "Componente não encontrado" });

        // Converter unidade se necessário
        var pesoConvertido = ConverterUnidade(leitura.Weight, leitura.Unit, componente.Unit);

        // Registrar pesagem
        componente.WeighedQuantity = pesoConvertido;
        componente.WeighedAt = DateTime.UtcNow;
        componente.WeighedByEmployeeId = employee.Id;
        componente.Status = "PESADO";
        componente.UpdatedAt = DateTime.UtcNow;

        // Verificar tolerância
        var tolerancia = dto.ToleranciaPercentual > 0 ? dto.ToleranciaPercentual : 5m;
        var diferenca = Math.Abs(pesoConvertido - componente.RequiredQuantity);
        var percentualDiferenca = componente.RequiredQuantity > 0 
            ? (diferenca / componente.RequiredQuantity) * 100 
            : 0;

        var dentroTolarancia = percentualDiferenca <= tolerancia;

        if (!dentroTolarancia && !dto.ForcarRegistro)
        {
            return BadRequest(new
            {
                success = false,
                error = "Peso fora da tolerância",
                pesoLido = pesoConvertido,
                pesoEsperado = componente.RequiredQuantity,
                diferenca,
                percentualDiferenca = Math.Round(percentualDiferenca, 2),
                toleranciaMaxima = tolerancia,
                requerConfirmacao = true
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Pesagem registrada: Componente {ComponenteId}, Peso {Peso}{Unidade}, Por {Employee}",
            componente.Id, pesoConvertido, componente.Unit, employee.FullName);

        return Ok(new
        {
            success = true,
            message = "Pesagem registrada",
            componenteId = componente.Id,
            pesoRegistrado = pesoConvertido,
            unidade = componente.Unit,
            pesoEsperado = componente.RequiredQuantity,
            dentroTolarancia,
            percentualDiferenca = Math.Round(percentualDiferenca, 2)
        });
    }

    /// <summary>
    /// Obtém status da balança
    /// GET /api/balanca/status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            conectada = _balanceService != null,
            protocolo = "TOLEDO", // Viria das configurações
            ultimaLeitura = DateTime.UtcNow
        });
    }

    #region Helpers

    private decimal ConverterUnidade(decimal valor, string unidadeOrigem, string unidadeDestino)
    {
        if (unidadeOrigem.Equals(unidadeDestino, StringComparison.OrdinalIgnoreCase))
            return valor;

        // Converter para gramas primeiro
        decimal emGramas = unidadeOrigem.ToLower() switch
        {
            "kg" => valor * 1000,
            "mg" => valor / 1000,
            "g" => valor,
            _ => valor
        };

        // Converter de gramas para unidade destino
        return unidadeDestino.ToLower() switch
        {
            "kg" => emGramas / 1000,
            "mg" => emGramas * 1000,
            "g" => emGramas,
            _ => emGramas
        };
    }

    #endregion
}

public class RegistrarPesagemDto
{
    public Guid ComponenteId { get; set; }
    public decimal ToleranciaPercentual { get; set; } = 5;
    public bool ForcarRegistro { get; set; } = false;
}
