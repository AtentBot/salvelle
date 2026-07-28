using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;
using Models;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashRegisterController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CashRegisterService _cashService;

    public CashRegisterController(AppDbContext context)
    {
        _context = context;
        _cashService = new CashRegisterService(context);
    }

    private Guid? GetEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null ? Guid.Parse(claim.Value) : null;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        return employee?.EstablishmentId;
    }

    /// <summary>
    /// Busca o caixa atualmente aberto
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentCashRegister()
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId.Value);

        if (cashRegister == null)
            return Ok(new { success = true, isOpen = false, message = "Nenhum caixa aberto" });

        return Ok(new
        {
            success = true,
            isOpen = true,
            cashRegister = new
            {
                cashRegister.Id,
                cashRegister.Code,
                cashRegister.OpeningDate,
                cashRegister.OpeningBalance,
                cashRegister.TotalCash,
                cashRegister.TotalCard,
                cashRegister.TotalDebit,
                cashRegister.TotalCredit,
                cashRegister.TotalPix,
                cashRegister.TotalBoleto,
                cashRegister.TotalOther,
                cashRegister.TotalSales,
                cashRegister.SalesCount,
                cashRegister.TotalWithdrawals,
                cashRegister.TotalSupplies,
                TotalReceived = cashRegister.TotalCash + cashRegister.TotalCard + cashRegister.TotalPix + cashRegister.TotalBoleto + cashRegister.TotalOther,
                CurrentCashBalance = cashRegister.OpeningBalance + cashRegister.TotalCash - cashRegister.TotalWithdrawals + cashRegister.TotalSupplies
            }
        });
    }

    /// <summary>
    /// Abre um novo caixa
    /// </summary>
    [HttpPost("open")]
    public async Task<IActionResult> OpenCashRegister([FromBody] OpenCashRegisterDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var (success, message, cashRegister) = await _cashService.OpenCashRegisterAsync(
            establishmentId.Value,
            employeeId.Value,
            dto.OpeningBalance,
            dto.Observations);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new
        {
            success = true,
            message,
            cashRegisterId = cashRegister!.Id,
            code = cashRegister.Code
        });
    }

    /// <summary>
    /// Fecha o caixa
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseCashRegister([FromBody] CloseCashRegisterDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        // Buscar caixa aberto
        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId.Value);
        if (cashRegister == null)
            return BadRequest(new { success = false, message = "Nenhum caixa aberto" });

        var (success, message) = await _cashService.CloseCashRegisterAsync(
            cashRegister.Id,
            employeeId.Value,
            dto.ActualClosingBalance,
            dto.Observations);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Realiza sangria (retirada de dinheiro)
    /// </summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> WithdrawCash([FromBody] CashOperationDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId.Value);
        if (cashRegister == null)
            return BadRequest(new { success = false, message = "Nenhum caixa aberto" });

        var (success, message) = await _cashService.WithdrawCashAsync(
            cashRegister.Id,
            dto.Amount,
            dto.Reason ?? "Sangria",
            employeeId.Value);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Realiza suprimento (entrada de dinheiro)
    /// </summary>
    [HttpPost("supply")]
    public async Task<IActionResult> SupplyCash([FromBody] CashOperationDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId.Value);
        if (cashRegister == null)
            return BadRequest(new { success = false, message = "Nenhum caixa aberto" });

        var (success, message) = await _cashService.SupplyCashAsync(
            cashRegister.Id,
            dto.Amount,
            dto.Reason ?? "Suprimento",
            employeeId.Value);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Lista movimentos do caixa atual
    /// </summary>
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements()
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId.Value);
        if (cashRegister == null)
            return BadRequest(new { success = false, message = "Nenhum caixa aberto" });

        var movements = await _cashService.GetCashMovementsAsync(cashRegister.Id);

        return Ok(new
        {
            success = true,
            movements = movements.Select(m => new
            {
                m.Id,
                m.MovementType,
                m.PaymentMethod,
                m.Amount,
                m.Description,
                m.CreatedAt
            })
        });
    }

    /// <summary>
    /// Histórico de caixas
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int limit = 30)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { success = false, message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        var registers = await _cashService.GetCashRegisterHistoryAsync(
            establishmentId.Value, startDate, endDate, limit);

        return Ok(new
        {
            success = true,
            registers = registers.Select(r => new
            {
                r.Id,
                r.Code,
                r.Status,
                r.OpeningDate,
                r.ClosingDate,
                r.OpeningBalance,
                r.TotalSales,
                r.SalesCount,
                r.TotalCash,
                r.TotalCard,
                r.TotalDebit,
                r.TotalCredit,
                r.TotalPix,
                r.TotalBoleto,
                r.TotalOther,
                r.TotalWithdrawals,
                r.TotalSupplies,
                r.TotalCancellations,
                r.ClosingBalance,
                r.ExpectedBalance,
                r.Difference
            })
        });
    }
}

// DTOs
public class OpenCashRegisterDto
{
    public decimal OpeningBalance { get; set; }
    public string? Observations { get; set; }
}

public class CloseCashRegisterDto
{
    public decimal ActualClosingBalance { get; set; }
    public string? Observations { get; set; }
}

public class CashOperationDto
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
