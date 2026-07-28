using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Service;
using Validators;
using Models;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class SngpcController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SngpcService _service;

    public SngpcController(AppDbContext context, SngpcService service)
    {
        _context = context;
        _service = service;
    }

    // ============================================
    // MOVIMENTAÇÕES
    // ============================================

    [HttpPost("movements")]
    public async Task<IActionResult> RegisterMovement([FromBody] RegisterControlledMovementDto dto)
    {
        var validator = new RegisterControlledMovementValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "FARMACEUTICO" });
        if (!hasPermission)
            return Forbid();

        var (success, message, movement) = await _service.RegisterMovementAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, movementId = movement!.Id });
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? controlledList = null,
        [FromQuery] Guid? rawMaterialId = null,
        [FromQuery] bool? pendingOnly = false)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<ControlledSubstanceMovement>()
            .Where(m => m.EstablishmentId == establishmentId.Value);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(controlledList))
            query = query.Where(m => m.ControlledList == controlledList.ToUpper());

        if (rawMaterialId.HasValue)
            query = query.Where(m => m.RawMaterialId == rawMaterialId.Value);

        if (pendingOnly.HasValue && pendingOnly.Value)
            query = query.Where(m => !m.SngpcSent);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new ControlledMovementResponseDto
            {
                Id = m.Id,
                MovementDate = m.MovementDate,
                MovementType = m.MovementType,
                ControlledList = m.ControlledList,
                SubstanceName = m.SubstanceName,
                SubstanceDcbCode = m.SubstanceDcbCode,
                Quantity = m.Quantity,
                Unit = m.Unit,
                BalanceBefore = m.BalanceBefore,
                BalanceAfter = m.BalanceAfter,
                PatientName = m.PatientName,
                DoctorName = m.DoctorName,
                PrescriptionNumber = m.PrescriptionNumber,
                SngpcSent = m.SngpcSent,
                SngpcSentAt = m.SngpcSentAt,
                SngpcStatus = m.SngpcStatus,
                CreatedAt = m.CreatedAt,
                CreatedByEmployeeName = ""
            })
            .ToListAsync();

        return Ok(movements);
    }

    [HttpGet("movements/{id}")]
    public async Task<IActionResult> GetMovementById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var movement = await _context.Set<ControlledSubstanceMovement>()
            .Where(m => m.Id == id && m.EstablishmentId == establishmentId.Value)
            .Select(m => new ControlledMovementResponseDto
            {
                Id = m.Id,
                MovementDate = m.MovementDate,
                MovementType = m.MovementType,
                ControlledList = m.ControlledList,
                SubstanceName = m.SubstanceName,
                SubstanceDcbCode = m.SubstanceDcbCode,
                Quantity = m.Quantity,
                Unit = m.Unit,
                BalanceBefore = m.BalanceBefore,
                BalanceAfter = m.BalanceAfter,
                PatientName = m.PatientName,
                DoctorName = m.DoctorName,
                PrescriptionNumber = m.PrescriptionNumber,
                SngpcSent = m.SngpcSent,
                SngpcSentAt = m.SngpcSentAt,
                SngpcStatus = m.SngpcStatus,
                CreatedAt = m.CreatedAt,
                CreatedByEmployeeName = ""
            })
            .FirstOrDefaultAsync();

        if (movement == null)
            return NotFound(new { message = "Movimentação não encontrada" });

        return Ok(movement);
    }

    // ============================================
    // BALANÇOS
    // ============================================

    [HttpPost("balances/generate")]
    public async Task<IActionResult> GenerateBalances([FromBody] GenerateBalanceDto dto)
    {
        var validator = new GenerateBalanceValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "FARMACEUTICO" });
        if (!hasPermission)
            return Forbid();

        var (success, message, balances) = await _service.GenerateBalancesAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, balancesCount = balances.Count });
    }

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances(
        [FromQuery] string? balanceType = null,
        [FromQuery] string? status = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<ControlledSubstanceBalance>()
            .Where(b => b.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(balanceType))
            query = query.Where(b => b.BalanceType == balanceType.ToUpper());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status.ToUpper());

        var balances = await query
            .OrderByDescending(b => b.ReferenceDate)
            .Select(b => new BalanceResponseDto
            {
                Id = b.Id,
                ReferenceDate = b.ReferenceDate,
                BalanceType = b.BalanceType,
                ControlledList = b.ControlledList,
                SubstanceName = b.SubstanceName,
                InitialBalance = b.InitialBalance,
                TotalEntries = b.TotalEntries,
                TotalExits = b.TotalExits,
                TotalLosses = b.TotalLosses,
                FinalBalance = b.FinalBalance,
                PhysicalBalance = b.PhysicalBalance,
                Difference = b.Difference,
                Unit = b.Unit,
                Status = b.Status,
                SngpcSent = b.SngpcSent
            })
            .ToListAsync();

        return Ok(balances);
    }

    [HttpPost("balances/{id}/close")]
    public async Task<IActionResult> CloseBalance(Guid id, [FromBody] CloseBalanceDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.CloseBalanceAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // ============================================
    // RECEITUÁRIOS ESPECIAIS
    // ============================================

    [HttpPost("prescriptions")]
    public async Task<IActionResult> RegisterSpecialPrescription([FromBody] RegisterSpecialPrescriptionDto dto)
    {
        var validator = new RegisterSpecialPrescriptionValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, control) = await _service.RegisterSpecialPrescriptionAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, prescriptionControlId = control!.Id });
    }

    [HttpGet("prescriptions")]
    public async Task<IActionResult> GetSpecialPrescriptions(
        [FromQuery] string? prescriptionType = null,
        [FromQuery] string? status = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<SpecialPrescriptionControl>()
            .Where(s => s.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(prescriptionType))
            query = query.Where(s => s.PrescriptionType == prescriptionType.ToUpper());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.ToUpper());

        var prescriptions = await query
            .OrderByDescending(s => s.IssueDate)
            .ToListAsync();

        return Ok(prescriptions);
    }

    // ============================================
    // XML E RELATÓRIOS
    // ============================================

    [HttpPost("xml/generate")]
    public async Task<IActionResult> GenerateXml([FromBody] SngpcXmlRequestDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT" });
        if (!hasPermission)
            return Forbid();

        var xml = await _service.GenerateSngpcXmlAsync(
            establishmentId.Value,
            dto.StartDate,
            dto.EndDate,
            dto.ControlledList);

        return Ok(new { xml, filename = $"SNGPC_{dto.StartDate:yyyyMMdd}_{dto.EndDate:yyyyMMdd}.xml" });
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;

        var report = await _service.GetReportAsync(establishmentId.Value, start, end);

        return Ok(report);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var pendingMovements = await _context.Set<ControlledSubstanceMovement>()
            .CountAsync(m => m.EstablishmentId == establishmentId.Value && !m.SngpcSent);

        var openBalances = await _context.Set<ControlledSubstanceBalance>()
            .CountAsync(b => b.EstablishmentId == establishmentId.Value && b.Status == "ABERTO");

        var activePrescriptions = await _context.Set<SpecialPrescriptionControl>()
            .CountAsync(s => s.EstablishmentId == establishmentId.Value && s.Status == "ATIVA");

        var expiringPrescriptions = await _context.Set<SpecialPrescriptionControl>()
            .CountAsync(s => s.EstablishmentId == establishmentId.Value &&
                            s.Status == "ATIVA" &&
                            s.ValidityDate <= DateTime.Today.AddDays(7));

        var dashboard = new
        {
            PendingMovements = pendingMovements,
            OpenBalances = openBalances,
            ActivePrescriptions = activePrescriptions,
            ExpiringPrescriptions = expiringPrescriptions
        };

        return Ok(dashboard);
    }

    private Guid? GetEmployeeId()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session?.EmployeeId;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        return employee?.EstablishmentId;
    }

    private async Task<bool> HasPermission(Guid employeeId, string[] allowedPositions)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.JobPosition == null)
            return false;

        return allowedPositions.Contains(employee.JobPosition.Code, StringComparer.OrdinalIgnoreCase);
    }
}
