using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.BatchQuality;
using Service.BatchQuality;
using Validators.BatchQuality;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchQualityController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly BatchQualityService _service;

    public BatchQualityController(AppDbContext context, BatchQualityService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet("quarantine")]
    public async Task<IActionResult> GetQuarantineBatches()
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var summary = await _service.GetQuarantineSummaryAsync(establishmentId.Value);
        return Ok(summary);
    }

    [HttpGet("{batchId}")]
    public async Task<IActionResult> GetBatchDetails(Guid batchId)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var batch = await _context.Batches
            .Include(b => b.RawMaterial)
            .Include(b => b.Supplier)
            .Include(b => b.CreatedByEmployee)
            .Include(b => b.ApprovedByEmployee)
            .Where(b => b.Id == batchId && b.RawMaterial!.EstablishmentId == establishmentId.Value)
            .Select(b => new BatchQualityResponseDto
            {
                Id = b.Id,
                RawMaterialId = b.RawMaterialId,
                RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : "",
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier != null ? b.Supplier.CompanyName : "",
                BatchNumber = b.BatchNumber,
                InvoiceNumber = b.InvoiceNumber,
                ReceivedQuantity = b.ReceivedQuantity,
                CurrentQuantity = b.CurrentQuantity,
                UnitCost = b.UnitCost,
                ReceivedDate = b.ReceivedDate,
                ExpiryDate = b.ExpiryDate,
                ManufactureDate = b.ManufactureDate,
                Status = b.Status,
                CertificateNumber = b.CertificateNumber,
                ApprovalDate = b.ApprovalDate,
                ApprovedByEmployeeName = b.ApprovedByEmployee != null ? b.ApprovedByEmployee.FullName : null,
                QualityNotes = b.QualityNotes,
                CreatedAt = b.CreatedAt,
                CreatedByEmployeeName = b.CreatedByEmployee != null ? b.CreatedByEmployee.FullName : "",
                DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days
            })
            .FirstOrDefaultAsync();

        if (batch == null)
            return NotFound(new { message = "Lote não encontrado" });

        return Ok(batch);
    }

    [HttpPost("{batchId}/approve")]
    public async Task<IActionResult> ApproveBatch(Guid batchId, [FromBody] ApproveBatchDto dto)
    {
        var validator = new ApproveBatchValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.ApproveBatchAsync(
            batchId, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("{batchId}/reject")]
    public async Task<IActionResult> RejectBatch(Guid batchId, [FromBody] RejectBatchDto dto)
    {
        var validator = new RejectBatchValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.RejectBatchAsync(
            batchId, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
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