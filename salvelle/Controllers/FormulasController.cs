using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Formulas;
using Service.Formulas;
using Validators.Formulas;  
using Models.Pharmacy;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormulasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FormulaService _service;

    public FormulasController(AppDbContext context, FormulaService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] string? pharmaceuticalForm = null,
        [FromQuery] bool? isActive = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Formulas
            .Include(f => f.Components)
                .ThenInclude(c => c.RawMaterial)
            .Where(f => f.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(f => f.Category.ToUpper() == category.ToUpper());

        if (!string.IsNullOrWhiteSpace(pharmaceuticalForm))
            query = query.Where(f => f.PharmaceuticalForm.ToUpper() == pharmaceuticalForm.ToUpper());

        if (isActive.HasValue)
            query = query.Where(f => f.IsActive == isActive.Value);

        var formulas = await query
            .OrderBy(f => f.Name)
            .Select(f => new FormulaResponseDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                Category = f.Category,
                PharmaceuticalForm = f.PharmaceuticalForm,
                StandardYield = f.StandardYield,
                ShelfLifeDays = f.ShelfLifeDays,
                PreparationInstructions = f.PreparationInstructions,
                StorageInstructions = f.StorageInstructions,
                UsageInstructions = f.UsageInstructions,
                RequiresSpecialControl = f.RequiresSpecialControl,
                RequiresPrescription = f.RequiresPrescription,
                IsActive = f.IsActive,
                TotalCost = f.Components.Sum(c => c.Quantity *
                    (_context.Batches
                        .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                   b.Status.ToUpper() == "APROVADO")
                        .OrderByDescending(b => b.ReceivedDate)
                        .Select(b => b.UnitCost)
                        .FirstOrDefault())),
                Version = f.Version,
                CreatedAt = f.CreatedAt,
                CreatedByEmployeeName = "",
                UpdatedAt = f.UpdatedAt,
                UpdatedByEmployeeName = null,
                Components = f.Components.Select(c => new FormulaComponentResponseDto
                {
                    Id = c.Id,
                    RawMaterialId = c.RawMaterialId,
                    RawMaterialName = c.RawMaterial != null ? c.RawMaterial.Name : "",
                    RawMaterialDcbCode = c.RawMaterial != null ? c.RawMaterial.DcbCode : null,
                    Quantity = c.Quantity,
                    Unit = c.Unit,
                    ComponentType = c.ComponentType,
                    OrderIndex = c.OrderIndex,
                    SpecialInstructions = c.SpecialInstructions,
                    IsOptional = c.IsOptional,
                    UnitCost = _context.Batches
                        .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                   b.Status.ToUpper() == "APROVADO")
                        .OrderByDescending(b => b.ReceivedDate)
                        .Select(b => b.UnitCost)
                        .FirstOrDefault(),
                    TotalCost = c.Quantity *
                        _context.Batches
                            .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                       b.Status.ToUpper() == "APROVADO")
                            .OrderByDescending(b => b.ReceivedDate)
                            .Select(b => b.UnitCost)
                            .FirstOrDefault(),
                    IsAvailable = _context.Batches
                        .Any(b => b.RawMaterialId == c.RawMaterialId &&
                                 b.Status.ToUpper() == "APROVADO" &&
                                 b.CurrentQuantity > 0),
                    AvailableStock = _context.Batches
                        .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                   b.Status.ToUpper() == "APROVADO")
                        .Sum(b => b.CurrentQuantity)
                }).ToList()
            })
            .ToListAsync();

        return Ok(formulas);
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

        var formula = await _context.Formulas
                .Include(f => f.Components)
                    .ThenInclude(c => c.RawMaterial)
                .Where(f => f.Id == id && f.EstablishmentId == establishmentId.Value)
                .Select(f => new FormulaResponseDto
                {
                    Id = f.Id,
                    Code = f.Code,
                    Name = f.Name,
                    Description = f.Description,
                    Category = f.Category,
                    PharmaceuticalForm = f.PharmaceuticalForm,
                    StandardYield = f.StandardYield,
                    ShelfLifeDays = f.ShelfLifeDays,
                    PreparationInstructions = f.PreparationInstructions,
                    StorageInstructions = f.StorageInstructions,
                    UsageInstructions = f.UsageInstructions,
                    RequiresSpecialControl = f.RequiresSpecialControl,
                    RequiresPrescription = f.RequiresPrescription,
                    IsActive = f.IsActive,
                    TotalCost = f.Components.Sum(c => c.Quantity *
                        (_context.Batches
                            .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                       b.Status.ToUpper() == "APROVADO")
                            .OrderByDescending(b => b.ReceivedDate)
                            .Select(b => b.UnitCost)
                            .FirstOrDefault())),
                    Version = f.Version,
                    CreatedAt = f.CreatedAt,
                    CreatedByEmployeeName = "",
                    UpdatedAt = f.UpdatedAt,
                    UpdatedByEmployeeName = null,
                    Components = f.Components.Select(c => new FormulaComponentResponseDto
                    {
                        Id = c.Id,
                        RawMaterialId = c.RawMaterialId,
                        RawMaterialName = c.RawMaterial != null ? c.RawMaterial.Name : "",
                        RawMaterialDcbCode = c.RawMaterial != null ? c.RawMaterial.DcbCode : null,
                        Quantity = c.Quantity,
                        Unit = c.Unit,
                        ComponentType = c.ComponentType,
                        OrderIndex = c.OrderIndex,
                        SpecialInstructions = c.SpecialInstructions,
                        IsOptional = c.IsOptional,
                        UnitCost = _context.Batches
                            .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                       b.Status.ToUpper() == "APROVADO")
                            .OrderByDescending(b => b.ReceivedDate)
                            .Select(b => b.UnitCost)
                            .FirstOrDefault(),
                        TotalCost = c.Quantity *
                            _context.Batches
                                .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                           b.Status.ToUpper() == "APROVADO")
                                .OrderByDescending(b => b.ReceivedDate)
                                .Select(b => b.UnitCost)
                                .FirstOrDefault(),
                        IsAvailable = _context.Batches
                            .Any(b => b.RawMaterialId == c.RawMaterialId &&
                                     b.Status.ToUpper() == "APROVADO" &&
                                     b.CurrentQuantity > 0),
                        AvailableStock = _context.Batches
                            .Where(b => b.RawMaterialId == c.RawMaterialId &&
                                       b.Status.ToUpper() == "APROVADO")
                            .Sum(b => b.CurrentQuantity)
                    }).ToList()
                })
                .FirstOrDefaultAsync();

        if (formula == null)
            return NotFound(new { message = "Fórmula não encontrada" });

        return Ok(formula);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormulaDto dto)
    {
        var validator = new CreateFormulaValidator();
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

        var (success, message, formula) = await _service.CreateFormulaAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = formula!.Id },
            new { message, formulaId = formula.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormulaDto dto)
    {
        var validator = new UpdateFormulaValidator();
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

        var (success, message) = await _service.UpdateFormulaAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.DeleteFormulaAsync(id, establishmentId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
        if (!hasPermission)
            return Forbid();

        var (success, message, formula) = await _service.DuplicateFormulaAsync(
            id, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = formula!.Id },
            new { message, formulaId = formula.Id });
    }

    [HttpGet("{id}/cost")]
    public async Task<IActionResult> CalculateCost(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        try
        {
            var cost = await _service.CalculateFormulaCostAsync(id, establishmentId.Value);
            return Ok(cost);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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