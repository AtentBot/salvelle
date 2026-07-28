using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;
using System.ComponentModel.DataAnnotations;

namespace Controllers.Api;

/// <summary>
/// API de Gestão de Caixa - Controle de abertura, fechamento, movimentações
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CaixaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CaixaController> _logger;

    public CaixaController(AppDbContext db, ILogger<CaixaController> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Helpers

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    
    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid establishmentId)
            return establishmentId;
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    private bool CanOperateCaixa()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return false;
        
        var code = employee.JobPosition?.Code?.ToUpper();
        return code == "MANAGER" || code == "GERENTE" || code == "PROPRIETARIO" || 
               code == "ADMIN" || code == "CAIXA" || code == "ATENDENTE" ||
               code == "PHARMACIST_RT" || code == "FARMACEUTICO_RT";
    }

    private async Task<string> GerarCodigoCaixa(Guid establishmentId)
    {
        var hoje = DateTime.UtcNow;
        var prefixo = $"CX{hoje:yyMMdd}";
        
        var ultimoCaixa = await _db.CashRegisters
            .Where(c => c.EstablishmentId == establishmentId && c.Code.StartsWith(prefixo))
            .OrderByDescending(c => c.Code)
            .FirstOrDefaultAsync();

        if (ultimoCaixa == null)
            return $"{prefixo}-001";

        var partes = ultimoCaixa.Code.Split('-');
        if (partes.Length == 2 && int.TryParse(partes[1], out var numero))
            return $"{prefixo}-{(numero + 1):D3}";

        return $"{prefixo}-001";
    }

    #endregion

    #region Caixa Status

    /// <summary>
    /// Obtém status atual do caixa (aberto/fechado)
    /// GET /api/caixa/status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            var establishmentId = GetEstablishmentId();

            // Buscar caixa aberto do dia
            var hoje = DateTime.UtcNow.Date;
            var caixaAberto = await _db.CashRegisters
                .Where(c => c.EstablishmentId == establishmentId && 
                           c.OpeningDate.Date == hoje &&
                           c.Status == "ABERTO")
                .OrderByDescending(c => c.OpeningDate)
                .FirstOrDefaultAsync();

            if (caixaAberto == null)
            {
                return Ok(new
                {
                    isOpen = false,
                    message = "Nenhum caixa aberto hoje",
                    canOpen = CanOperateCaixa()
                });
            }

            // Calcular saldo atual
            var saldoAtual = caixaAberto.OpeningBalance + 
                            caixaAberto.TotalCash + 
                            caixaAberto.TotalSupplies - 
                            caixaAberto.TotalWithdrawals;

            return Ok(new
            {
                isOpen = true,
                cashRegisterId = caixaAberto.Id,
                code = caixaAberto.Code,
                openingDate = caixaAberto.OpeningDate,
                openedBy = caixaAberto.OpenedByEmployeeId,
                openingBalance = caixaAberto.OpeningBalance,
                totalSales = caixaAberto.TotalSales,
                totalCash = caixaAberto.TotalCash,
                totalDebit = caixaAberto.TotalDebit,
                totalCredit = caixaAberto.TotalCredit,
                totalPix = caixaAberto.TotalPix,
                totalBoleto = caixaAberto.TotalBoleto,
                totalWithdrawals = caixaAberto.TotalWithdrawals,
                totalSupplies = caixaAberto.TotalSupplies,
                salesCount = caixaAberto.SalesCount,
                currentBalance = saldoAtual,
                canClose = CanOperateCaixa()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status do caixa");
            return StatusCode(500, new { error = "Erro interno ao verificar status do caixa" });
        }
    }

    #endregion

    #region Abertura/Fechamento

    /// <summary>
    /// Abre o caixa do dia
    /// POST /api/caixa/abrir
    /// </summary>
    [HttpPost("abrir")]
    public async Task<IActionResult> AbrirCaixa([FromBody] AbrirCaixaRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            if (!CanOperateCaixa())
                return Forbid();

            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;

            // Verificar se já existe caixa aberto
            var caixaExistente = await _db.CashRegisters
                .AnyAsync(c => c.EstablishmentId == establishmentId && 
                              c.OpeningDate.Date == hoje &&
                              c.Status == "ABERTO");

            if (caixaExistente)
                return BadRequest(new { error = "Já existe um caixa aberto hoje" });

            var codigo = await GerarCodigoCaixa(establishmentId);

            var caixa = new CashRegister
            {
                EstablishmentId = establishmentId,
                Code = codigo,
                OpeningDate = DateTime.UtcNow,
                OpenedByEmployeeId = employee.Id,
                OpeningBalance = request.OpeningBalance,
                Observations = request.Observations,
                Status = "ABERTO",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CashRegisters.Add(caixa);

            // Registrar movimentação de abertura
            var movimentacao = new CashMovement
            {
                CashRegisterId = caixa.Id,
                MovementType = "ABERTURA",
                Amount = request.OpeningBalance,
                PaymentMethod = "DINHEIRO",
                Description = $"Abertura de caixa - Saldo inicial: R$ {request.OpeningBalance:N2}",
                EmployeeId = employee.Id,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movimentacao);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Caixa {Code} aberto por {EmployeeId} com saldo inicial {Balance}", 
                codigo, employee.Id, request.OpeningBalance);

            return Ok(new
            {
                success = true,
                cashRegisterId = caixa.Id,
                code = caixa.Code,
                message = "Caixa aberto com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir caixa");
            return StatusCode(500, new { error = "Erro interno ao abrir caixa" });
        }
    }

    /// <summary>
    /// Fecha o caixa do dia
    /// POST /api/caixa/fechar
    /// </summary>
    [HttpPost("fechar")]
    public async Task<IActionResult> FecharCaixa([FromBody] FecharCaixaRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            if (!CanOperateCaixa())
                return Forbid();

            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId && 
                                         c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO");

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado ou já fechado" });

            // Calcular saldo esperado (dinheiro em caixa)
            var saldoEsperado = caixa.OpeningBalance + 
                               caixa.TotalCash + 
                               caixa.TotalSupplies - 
                               caixa.TotalWithdrawals;

            var diferenca = request.ClosingBalance - saldoEsperado;

            caixa.ClosingDate = DateTime.UtcNow;
            caixa.ClosedByEmployeeId = employee.Id;
            caixa.ClosingBalance = request.ClosingBalance;
            caixa.ExpectedBalance = saldoEsperado;
            caixa.Difference = diferenca;
            caixa.ClosingObservations = request.ClosingObservations;
            caixa.Status = "FECHADO";
            caixa.UpdatedAt = DateTime.UtcNow;

            // Registrar movimentação de fechamento
            var movimentacao = new CashMovement
            {
                CashRegisterId = caixa.Id,
                MovementType = "FECHAMENTO",
                Amount = request.ClosingBalance,
                PaymentMethod = "DINHEIRO",
                Description = $"Fechamento de caixa - Saldo informado: R$ {request.ClosingBalance:N2}, " +
                             $"Esperado: R$ {saldoEsperado:N2}, Diferença: R$ {diferenca:N2}",
                EmployeeId = employee.Id,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movimentacao);
            await _db.SaveChangesAsync();

            var statusDiferenca = diferenca == 0 ? "OK" : (diferenca > 0 ? "SOBRA" : "FALTA");

            _logger.LogInformation("Caixa {Code} fechado por {EmployeeId}. Diferença: {Difference} ({Status})", 
                caixa.Code, employee.Id, diferenca, statusDiferenca);

            return Ok(new
            {
                success = true,
                closingBalance = request.ClosingBalance,
                expectedBalance = saldoEsperado,
                difference = diferenca,
                differenceStatus = statusDiferenca,
                message = $"Caixa fechado com {statusDiferenca}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar caixa");
            return StatusCode(500, new { error = "Erro interno ao fechar caixa" });
        }
    }

    #endregion

    #region Sangria e Suprimento

    /// <summary>
    /// Realiza sangria (retirada de dinheiro)
    /// POST /api/caixa/sangria
    /// </summary>
    [HttpPost("sangria")]
    public async Task<IActionResult> RealizarSangria([FromBody] MovimentacaoCaixaRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            if (!CanOperateCaixa())
                return Forbid();

            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId && 
                                         c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO");

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado ou fechado" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Valor deve ser maior que zero" });

            // Verificar saldo disponível
            var saldoDisponivel = caixa.OpeningBalance + caixa.TotalCash + caixa.TotalSupplies - caixa.TotalWithdrawals;
            if (request.Amount > saldoDisponivel)
                return BadRequest(new { error = $"Saldo insuficiente. Disponível: R$ {saldoDisponivel:N2}" });

            caixa.TotalWithdrawals += request.Amount;
            caixa.UpdatedAt = DateTime.UtcNow;

            var movimentacao = new CashMovement
            {
                CashRegisterId = caixa.Id,
                MovementType = "SANGRIA",
                Amount = -request.Amount, // Negativo pois é saída
                PaymentMethod = "DINHEIRO",
                Description = request.Description ?? $"Sangria: R$ {request.Amount:N2}",
                EmployeeId = employee.Id,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movimentacao);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Sangria de R$ {Amount} realizada no caixa {Code}", request.Amount, caixa.Code);

            return Ok(new
            {
                success = true,
                movementId = movimentacao.Id,
                newBalance = saldoDisponivel - request.Amount,
                message = $"Sangria de R$ {request.Amount:N2} realizada com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar sangria");
            return StatusCode(500, new { error = "Erro interno ao realizar sangria" });
        }
    }

    /// <summary>
    /// Realiza suprimento (entrada de dinheiro)
    /// POST /api/caixa/suprimento
    /// </summary>
    [HttpPost("suprimento")]
    public async Task<IActionResult> RealizarSuprimento([FromBody] MovimentacaoCaixaRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            if (!CanOperateCaixa())
                return Forbid();

            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId && 
                                         c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO");

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado ou fechado" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Valor deve ser maior que zero" });

            caixa.TotalSupplies += request.Amount;
            caixa.UpdatedAt = DateTime.UtcNow;

            var movimentacao = new CashMovement
            {
                CashRegisterId = caixa.Id,
                MovementType = "SUPRIMENTO",
                Amount = request.Amount,
                PaymentMethod = "DINHEIRO",
                Description = request.Description ?? $"Suprimento: R$ {request.Amount:N2}",
                EmployeeId = employee.Id,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movimentacao);
            await _db.SaveChangesAsync();

            var saldoAtual = caixa.OpeningBalance + caixa.TotalCash + caixa.TotalSupplies - caixa.TotalWithdrawals;

            _logger.LogInformation("Suprimento de R$ {Amount} realizado no caixa {Code}", request.Amount, caixa.Code);

            return Ok(new
            {
                success = true,
                movementId = movimentacao.Id,
                newBalance = saldoAtual,
                message = $"Suprimento de R$ {request.Amount:N2} realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar suprimento");
            return StatusCode(500, new { error = "Erro interno ao realizar suprimento" });
        }
    }

    #endregion

    #region Movimentações

    /// <summary>
    /// Lista movimentações de um caixa
    /// GET /api/caixa/movimentacoes/{cashRegisterId}
    /// </summary>
    [HttpGet("movimentacoes/{cashRegisterId:guid}")]
    public async Task<IActionResult> ListarMovimentacoes(Guid cashRegisterId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.EstablishmentId == establishmentId);

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado" });

            var movimentacoes = await _db.CashMovements
                .Where(m => m.CashRegisterId == cashRegisterId)
                .OrderByDescending(m => m.MovementDate)
                .Select(m => new
                {
                    m.Id,
                    m.MovementType,
                    m.Amount,
                    m.PaymentMethod,
                    m.Description,
                    m.EmployeeId,
                    m.SaleId,
                    m.MovementDate,
                    m.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                cashRegisterId,
                code = caixa.Code,
                total = movimentacoes.Count,
                movimentacoes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar movimentações");
            return StatusCode(500, new { error = "Erro interno ao listar movimentações" });
        }
    }

    /// <summary>
    /// Resumo por forma de pagamento
    /// GET /api/caixa/resumo-pagamentos/{cashRegisterId}
    /// </summary>
    [HttpGet("resumo-pagamentos/{cashRegisterId:guid}")]
    public async Task<IActionResult> ResumoPagamentos(Guid cashRegisterId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.EstablishmentId == establishmentId);

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado" });

            return Ok(new
            {
                cashRegisterId,
                code = caixa.Code,
                resumo = new
                {
                    dinheiro = caixa.TotalCash,
                    cartaoDebito = caixa.TotalDebit,
                    cartaoCredito = caixa.TotalCredit,
                    pix = caixa.TotalPix,
                    boleto = caixa.TotalBoleto,
                    outros = caixa.TotalOther,
                    totalVendas = caixa.TotalSales,
                    quantidadeVendas = caixa.SalesCount,
                    sangrias = caixa.TotalWithdrawals,
                    suprimentos = caixa.TotalSupplies,
                    cancelamentos = caixa.TotalCancellations
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo de pagamentos");
            return StatusCode(500, new { error = "Erro interno ao obter resumo" });
        }
    }

    #endregion

    #region Histórico

    /// <summary>
    /// Histórico de caixas
    /// GET /api/caixa/historico
    /// </summary>
    [HttpGet("historico")]
    public async Task<IActionResult> Historico(
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var query = _db.CashRegisters
                .Where(c => c.EstablishmentId == establishmentId);

            if (dataInicio.HasValue)
                query = query.Where(c => c.OpeningDate.Date >= dataInicio.Value.Date);

            if (dataFim.HasValue)
                query = query.Where(c => c.OpeningDate.Date <= dataFim.Value.Date);

            var total = await query.CountAsync();

            var caixas = await query
                .OrderByDescending(c => c.OpeningDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.OpeningDate,
                    c.ClosingDate,
                    c.OpeningBalance,
                    c.ClosingBalance,
                    c.ExpectedBalance,
                    c.Difference,
                    c.TotalSales,
                    c.SalesCount,
                    c.Status,
                    c.OpenedByEmployeeId,
                    c.ClosedByEmployeeId
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                caixas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter histórico de caixas");
            return StatusCode(500, new { error = "Erro interno ao obter histórico" });
        }
    }

    #endregion

    #region Registro de Venda

    /// <summary>
    /// Registra venda no caixa (chamado pelo PDV)
    /// POST /api/caixa/registrar-venda
    /// </summary>
    [HttpPost("registrar-venda")]
    public async Task<IActionResult> RegistrarVenda([FromBody] RegistrarVendaCaixaRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            var establishmentId = GetEstablishmentId();

            var caixa = await _db.CashRegisters
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId && 
                                         c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO");

            if (caixa == null)
                return NotFound(new { error = "Caixa não encontrado ou fechado" });

            // Atualizar totais do caixa
            caixa.TotalSales += request.TotalAmount;
            caixa.SalesCount++;

            // Atualizar por forma de pagamento
            switch (request.PaymentMethod?.ToUpper())
            {
                case "DINHEIRO":
                    caixa.TotalCash += request.TotalAmount;
                    break;
                case "CARTAO_DEBITO":
                case "DEBITO":
                    caixa.TotalDebit += request.TotalAmount;
                    break;
                case "CARTAO_CREDITO":
                case "CREDITO":
                    caixa.TotalCredit += request.TotalAmount;
                    break;
                case "PIX":
                    caixa.TotalPix += request.TotalAmount;
                    break;
                case "BOLETO":
                    caixa.TotalBoleto += request.TotalAmount;
                    break;
                default:
                    caixa.TotalOther += request.TotalAmount;
                    break;
            }

            caixa.UpdatedAt = DateTime.UtcNow;

            // Registrar movimentação
            var movimentacao = new CashMovement
            {
                CashRegisterId = caixa.Id,
                MovementType = "VENDA",
                Amount = request.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                SaleId = request.SaleId,
                Description = $"Venda #{request.SaleId.ToString()[..8]} - {request.PaymentMethod}",
                EmployeeId = employee?.Id ?? request.EmployeeId ?? Guid.Empty,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movimentacao);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                movementId = movimentacao.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar venda no caixa");
            return StatusCode(500, new { error = "Erro interno ao registrar venda" });
        }
    }

    #endregion
}

#region DTOs

public class AbrirCaixaRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Valor inicial não pode ser negativo")]
    public decimal OpeningBalance { get; set; } = 0;
    public string? Observations { get; set; }
}

public class FecharCaixaRequest
{
    [Required]
    public Guid CashRegisterId { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Valor final não pode ser negativo")]
    public decimal ClosingBalance { get; set; }
    
    public string? ClosingObservations { get; set; }
}

public class MovimentacaoCaixaRequest
{
    [Required]
    public Guid CashRegisterId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Amount { get; set; }
    
    public string? Description { get; set; }
}

public class RegistrarVendaCaixaRequest
{
    [Required]
    public Guid CashRegisterId { get; set; }
    
    [Required]
    public Guid SaleId { get; set; }
    
    [Required]
    public decimal TotalAmount { get; set; }
    
    [Required]
    public string PaymentMethod { get; set; } = "DINHEIRO";
    
    public Guid? EmployeeId { get; set; }
}

#endregion
