using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Prescriptions;
using Data;
using DTOs;
using Service;
using Validators;
using Models;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PrescriptionService _service;

    public PrescriptionsController(AppDbContext context, PrescriptionService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] bool? includeExpired = false)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status.ToUpper() == status.ToUpper());

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        if (!includeExpired.Value)
            query = query.Where(p => p.Status != "EXPIRADA");

        var prescriptions = await query
            .OrderByDescending(p => p.PrescriptionDate)
            .Select(p => new PrescriptionListDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DoctorName = p.DoctorName,
                PrescriptionType = p.PrescriptionType,
                Status = p.Status,
                IsExpired = p.ExpirationDate.Date < DateTime.UtcNow.Date,
                DaysUntilExpiration = (p.ExpirationDate.Date - DateTime.UtcNow.Date).Days
            })
            .ToListAsync();

        return Ok(prescriptions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var prescription = await _context.Set<Prescription>()
            .Where(p => p.Id == id && p.EstablishmentId == establishmentId.Value)
            .Select(p => new PrescriptionResponseDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerId = p.CustomerId,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                CustomerCpf = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.Cpf)
                    .FirstOrDefault(),
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DaysUntilExpiration = (int)(p.ExpirationDate - DateTime.Today).TotalDays,
                IsExpired = p.ExpirationDate < DateTime.Today,
                DoctorName = p.DoctorName,
                DoctorCrm = p.DoctorCrm,
                DoctorCrmState = p.DoctorCrmState,
                PrescriptionType = p.PrescriptionType,
                ControlledType = p.ControlledType,
                PrescriptionColor = p.PrescriptionColor,
                Medications = p.Medications,
                Posology = p.Posology,
                Observations = p.Observations,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                ValidatedAt = p.ValidatedAt,
                ValidatedByEmployeeName = "",
                ValidationNotes = p.ValidationNotes,
                ManipulationOrderId = p.ManipulationOrderId,
                ManipulationOrderCode = null,
                ManipulationGeneratedAt = p.ManipulationGeneratedAt,
                CancelledAt = p.CancelledAt,
                CancelledByEmployeeName = "",
                CancellationReason = p.CancellationReason,
                CreatedAt = p.CreatedAt,
                CreatedByEmployeeName = "",
                UpdatedAt = p.UpdatedAt,
                UpdatedByEmployeeName = ""
            })
            .FirstOrDefaultAsync();

        if (prescription == null)
            return NotFound(new { message = "Prescrição não encontrada" });

        return Ok(prescription);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var validator = new CreatePrescriptionValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, prescription) = await _service.CreatePrescriptionAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = prescription!.Id },
            new { message, prescriptionId = prescription.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrescriptionDto dto)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { success = false, message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId.Value);

            if (prescription == null)
                return NotFound(new { success = false, message = "Prescrição não encontrada" });

            // Só pode editar se estiver PENDENTE ou RASCUNHO
            if (prescription.Status != "PENDENTE" && prescription.Status != "RASCUNHO")
                return BadRequest(new { success = false, message = "Apenas prescrições pendentes ou rascunhos podem ser editadas" });

            // Atualizar campos
            if (!string.IsNullOrEmpty(dto.DoctorName))
                prescription.DoctorName = dto.DoctorName;
            if (!string.IsNullOrEmpty(dto.DoctorCrm))
                prescription.DoctorCrm = dto.DoctorCrm;
            if (!string.IsNullOrEmpty(dto.DoctorCrmState))
                prescription.DoctorCrmState = dto.DoctorCrmState;
            if (dto.CustomerId.HasValue)
                prescription.CustomerId = dto.CustomerId.Value;
            if (!string.IsNullOrEmpty(dto.Medications))
                prescription.Medications = dto.Medications;
            if (!string.IsNullOrEmpty(dto.Posology))
                prescription.Posology = dto.Posology;
            if (dto.Observations != null)
                prescription.Observations = dto.Observations;
            if (!string.IsNullOrEmpty(dto.PrescriptionType))
                prescription.PrescriptionType = dto.PrescriptionType;
            if (dto.ControlledType != null)
                prescription.ControlledType = dto.ControlledType;
            if (dto.PrescriptionDate.HasValue)
                prescription.PrescriptionDate = dto.PrescriptionDate.Value;
            if (dto.ExpirationDate.HasValue)
                prescription.ExpirationDate = dto.ExpirationDate.Value;

            prescription.UpdatedByEmployeeId = employeeId.Value;
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Prescrição atualizada com sucesso" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao atualizar: {ex.Message}" });
        }
    }

    [HttpPut("{id}/validate")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidatePrescriptionDto dto)
    {
        try
        {
            var validator = new ValidatePrescriptionValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { success = false, message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) });

            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { success = false, message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

            var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "FARMACEUTICO" });
            if (!hasPermission)
                return StatusCode(403, new { success = false, message = "Apenas farmacêuticos podem validar prescrições" });

            var (success, message) = await _service.ValidatePrescriptionAsync(
                id, dto, establishmentId.Value, employeeId.Value);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao validar: {ex.Message}" });
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPrescriptionDto dto)
    {
        try
        {
            var validator = new CancelPrescriptionValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { success = false, message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) });

            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { success = false, message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

            var (success, message) = await _service.CancelPrescriptionAsync(
                id, dto.Reason, establishmentId.Value, employeeId.Value);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao cancelar: {ex.Message}" });
        }
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 7)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var expiringDate = DateTime.Today.AddDays(days);

        var prescriptions = await _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId.Value &&
                       p.Status == "VALIDADA" &&
                       p.ExpirationDate <= expiringDate &&
                       p.ExpirationDate >= DateTime.Today)
            .OrderBy(p => p.ExpirationDate)
            .Select(p => new PrescriptionListDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DoctorName = p.DoctorName,
                PrescriptionType = p.PrescriptionType,
                Status = p.Status,
                IsExpired = false,
                DaysUntilExpiration = (int)(p.ExpirationDate - DateTime.Today).TotalDays
            })
            .ToListAsync();

        return Ok(prescriptions);
    }

    // ===== MÉTODOS DE OCR =====

    /// <summary>
    /// Upload de arquivo de receita
    /// </summary>
    [HttpPost("{id}/upload")]
    public async Task<IActionResult> UploadFile(Guid id, [FromBody] UploadPrescriptionFileDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        try
        {
            var prescription = await _context.Set<Prescription>()
                .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId.Value);

            if (prescription == null)
                return NotFound(new { message = "Prescrição não encontrada" });

            var fileBytes = Convert.FromBase64String(dto.FileBase64);
            if (fileBytes.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Arquivo muito grande. Máximo 5MB." });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "application/pdf" };
            if (!allowedTypes.Contains(dto.FileType.ToLower()))
                return BadRequest(new { message = "Tipo não permitido." });

            var prescriptionFile = new PrescriptionFile
            {
                Id = Guid.NewGuid(),
                PrescriptionId = id,
                FileName = dto.FileName,
                FileType = dto.FileType,
                FileBase64 = dto.FileBase64,
                FileSizeBytes = fileBytes.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedByEmployeeId = employeeId.Value,
                OcrStatus = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<PrescriptionFile>().Add(prescriptionFile);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arquivo enviado", fileId = prescriptionFile.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro", error = ex.Message });
        }
    }

    /// <summary>
    /// Processar OCR
    /// </summary>
    [HttpPost("{id}/files/{fileId}/parse")]
    public async Task<IActionResult> ParseFile(Guid id, Guid fileId)
    {
        try
        {
            var file = await _context.Set<PrescriptionFile>()
                .FirstOrDefaultAsync(f => f.Id == fileId && f.PrescriptionId == id);

            if (file == null)
                return NotFound(new { message = "Arquivo não encontrado" });

            if (string.IsNullOrEmpty(file.FileBase64))
                return BadRequest(new { message = "Arquivo sem dados" });

            file.OcrStatus = "PROCESSING";
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var ocrService = HttpContext.RequestServices.GetRequiredService<OpenAIPrescriptionParserService>();
            var ocrResult = await ocrService.ParsePrescriptionAsync(file.FileBase64, file.FileType);

            file.OcrStatus = "COMPLETED";
            file.OcrProcessedAt = DateTime.UtcNow;
            file.OcrResult = System.Text.Json.JsonSerializer.Serialize(ocrResult);
            file.OcrConfidence = (decimal)ocrResult.OverallConfidence;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(ocrResult);
        }
        catch (Exception ex)
        {
            var file = await _context.Set<PrescriptionFile>().FindAsync(fileId);
            if (file != null)
            {
                file.OcrStatus = "FAILED";
                file.OcrErrorMessage = ex.Message;
                file.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return StatusCode(500, new { message = "Erro OCR", error = ex.Message });
        }
    }

    /// <summary>
    /// Matching de ingredientes
    /// </summary>
    [HttpPost("{id}/match-ingredients")]
    public async Task<IActionResult> MatchIngredients(Guid id, [FromBody] List<OcrItemDto> items)
    {
        try
        {
            var matcherService = HttpContext.RequestServices.GetRequiredService<IngredientMatcherService>();
            var matches = await matcherService.FindMatchesAsync(items);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro", error = ex.Message });
        }
    }

    // ===== MÉTODOS AUXILIARES =====

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