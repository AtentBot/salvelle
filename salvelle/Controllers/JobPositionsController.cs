using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class JobPositionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobPositionsController> _logger;

    public JobPositionsController(AppDbContext db, ILogger<JobPositionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== LISTAR CARGOS ====================
    /// <summary>
    /// GET: /api/JobPositions
    /// Lista todos os cargos ativos do estabelecimento
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Pegar funcionário logado do contexto (definido pelo middleware)
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        try
        {
            var jobPositions = await _db.JobPositions
                .Where(jp => jp.EstablishmentId == employee.EstablishmentId && jp.IsActive)
                .OrderBy(jp => jp.HierarchyLevel)
                .ThenBy(jp => jp.Name)
                .Select(jp => new
                {
                    id = jp.Id,
                    code = jp.Code,
                    name = jp.Name,
                    description = jp.Description,
                    hierarchyLevel = jp.HierarchyLevel,
                    requiresCertification = jp.RequiresCertification,
                    requiredCertification = jp.RequiredCertification,
                    suggestedSalaryMin = jp.SuggestedSalaryMin,
                    suggestedSalaryMax = jp.SuggestedSalaryMax,
                    isSystemDefault = jp.IsSystemDefault
                })
                .ToListAsync();

            return Ok(new { items = jobPositions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar cargos");
            return StatusCode(500, new { error = "Erro ao carregar cargos" });
        }
    }

    // ==================== OBTER CARGO POR ID ====================
    /// <summary>
    /// GET: /api/JobPositions/{id}
    /// Obtém detalhes de um cargo específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        var jobPosition = await _db.JobPositions
            .Where(jp => jp.Id == id && jp.EstablishmentId == employee.EstablishmentId)
            .Select(jp => new
            {
                id = jp.Id,
                code = jp.Code,
                name = jp.Name,
                description = jp.Description,
                responsibilities = jp.Responsibilities,
                hierarchyLevel = jp.HierarchyLevel,
                reportsTo = jp.ReportsTo,
                requiredEducation = jp.RequiredEducation,
                requiredCertification = jp.RequiredCertification,
                requiresCertification = jp.RequiresCertification,
                requiredExperience = jp.RequiredExperience,
                suggestedSalaryMin = jp.SuggestedSalaryMin,
                suggestedSalaryMax = jp.SuggestedSalaryMax,
                salaryType = jp.SalaryType,
                isActive = jp.IsActive,
                isSystemDefault = jp.IsSystemDefault,
                createdAt = jp.CreatedAt,
                updatedAt = jp.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (jobPosition == null)
        {
            return NotFound(new { error = "Cargo não encontrado" });
        }

        return Ok(jobPosition);
    }

    // ==================== CRIAR CARGO ====================
    /// <summary>
    /// POST: /api/JobPositions
    /// Cria um novo cargo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobPositionDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        // Verificar permissão (apenas OWNER e GENERAL_MANAGER)
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        if (!allowedCodes.Contains(employee.JobPosition?.Code ?? ""))
        {
            return Forbid();
        }

        // Verificar se código já existe
        if (await _db.JobPositions.AnyAsync(jp => 
            jp.EstablishmentId == employee.EstablishmentId && 
            jp.Code == dto.Code))
        {
            return Conflict(new { error = "Código de cargo já existe" });
        }

        var jobPosition = new JobPosition
        {
            Id = Guid.NewGuid(),
            EstablishmentId = employee.EstablishmentId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Responsibilities = dto.Responsibilities,
            HierarchyLevel = dto.HierarchyLevel,
            ReportsTo = dto.ReportsTo,
            RequiredEducation = dto.RequiredEducation,
            RequiredCertification = dto.RequiredCertification,
            RequiresCertification = dto.RequiresCertification,
            RequiredExperience = dto.RequiredExperience,
            SuggestedSalaryMin = dto.SuggestedSalaryMin,
            SuggestedSalaryMax = dto.SuggestedSalaryMax,
            SalaryType = dto.SalaryType,
            IsActive = true,
            IsSystemDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = employee.Id
        };

        _db.JobPositions.Add(jobPosition);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cargo {Name} criado por {EmployeeId}", dto.Name, employee.Id);

        return CreatedAtAction(nameof(GetById), new { id = jobPosition.Id }, new
        {
            id = jobPosition.Id,
            code = jobPosition.Code,
            name = jobPosition.Name,
            message = "Cargo criado com sucesso"
        });
    }

    // ==================== ATUALIZAR CARGO ====================
    /// <summary>
    /// PUT: /api/JobPositions/{id}
    /// Atualiza um cargo existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPositionDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        // Verificar permissão
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        if (!allowedCodes.Contains(employee.JobPosition?.Code ?? ""))
        {
            return Forbid();
        }

        var jobPosition = await _db.JobPositions
            .FirstOrDefaultAsync(jp => jp.Id == id && jp.EstablishmentId == employee.EstablishmentId);

        if (jobPosition == null)
        {
            return NotFound(new { error = "Cargo não encontrado" });
        }

        // Não permitir editar cargos padrão do sistema
        if (jobPosition.IsSystemDefault)
        {
            return BadRequest(new { error = "Cargos padrão do sistema não podem ser editados" });
        }

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Name)) jobPosition.Name = dto.Name;
        if (dto.Description != null) jobPosition.Description = dto.Description;
        if (dto.Responsibilities != null) jobPosition.Responsibilities = dto.Responsibilities;
        if (dto.HierarchyLevel.HasValue) jobPosition.HierarchyLevel = dto.HierarchyLevel.Value;
        if (dto.ReportsTo.HasValue) jobPosition.ReportsTo = dto.ReportsTo;
        if (dto.RequiredEducation != null) jobPosition.RequiredEducation = dto.RequiredEducation;
        if (dto.RequiredCertification != null) jobPosition.RequiredCertification = dto.RequiredCertification;
        if (dto.RequiresCertification.HasValue) jobPosition.RequiresCertification = dto.RequiresCertification.Value;
        if (dto.RequiredExperience != null) jobPosition.RequiredExperience = dto.RequiredExperience;
        if (dto.SuggestedSalaryMin.HasValue) jobPosition.SuggestedSalaryMin = dto.SuggestedSalaryMin;
        if (dto.SuggestedSalaryMax.HasValue) jobPosition.SuggestedSalaryMax = dto.SuggestedSalaryMax;
        if (dto.SalaryType != null) jobPosition.SalaryType = dto.SalaryType;
        if (dto.IsActive.HasValue) jobPosition.IsActive = dto.IsActive.Value;

        jobPosition.UpdatedAt = DateTime.UtcNow;
        jobPosition.UpdatedBy = employee.Id;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Cargo {Id} atualizado por {EmployeeId}", id, employee.Id);

        return Ok(new { message = "Cargo atualizado com sucesso" });
    }

    // ==================== DESATIVAR CARGO ====================
    /// <summary>
    /// DELETE: /api/JobPositions/{id}
    /// Desativa um cargo (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        // Verificar permissão
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        if (!allowedCodes.Contains(employee.JobPosition?.Code ?? ""))
        {
            return Forbid();
        }

        var jobPosition = await _db.JobPositions
            .FirstOrDefaultAsync(jp => jp.Id == id && jp.EstablishmentId == employee.EstablishmentId);

        if (jobPosition == null)
        {
            return NotFound(new { error = "Cargo não encontrado" });
        }

        // Não permitir excluir cargos padrão do sistema
        if (jobPosition.IsSystemDefault)
        {
            return BadRequest(new { error = "Cargos padrão do sistema não podem ser excluídos" });
        }

        // Verificar se há funcionários com este cargo
        var hasEmployees = await _db.Employees.AnyAsync(e => e.JobPositionId == id);
        if (hasEmployees)
        {
            return BadRequest(new { error = "Não é possível excluir cargo com funcionários vinculados. Desative-o ou transfira os funcionários primeiro." });
        }

        // Soft delete
        jobPosition.IsActive = false;
        jobPosition.UpdatedAt = DateTime.UtcNow;
        jobPosition.UpdatedBy = employee.Id;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Cargo {Id} desativado por {EmployeeId}", id, employee.Id);

        return Ok(new { message = "Cargo desativado com sucesso" });
    }
}

// ==================== DTOs ====================
public class CreateJobPositionDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Responsibilities { get; set; }
    public int HierarchyLevel { get; set; } = 1;
    public Guid? ReportsTo { get; set; }
    public string? RequiredEducation { get; set; }
    public string? RequiredCertification { get; set; }
    public bool RequiresCertification { get; set; } = false;
    public string? RequiredExperience { get; set; }
    public decimal? SuggestedSalaryMin { get; set; }
    public decimal? SuggestedSalaryMax { get; set; }
    public string? SalaryType { get; set; }
}

public class UpdateJobPositionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Responsibilities { get; set; }
    public int? HierarchyLevel { get; set; }
    public Guid? ReportsTo { get; set; }
    public string? RequiredEducation { get; set; }
    public string? RequiredCertification { get; set; }
    public bool? RequiresCertification { get; set; }
    public string? RequiredExperience { get; set; }
    public decimal? SuggestedSalaryMin { get; set; }
    public decimal? SuggestedSalaryMax { get; set; }
    public string? SalaryType { get; set; }
    public bool? IsActive { get; set; }
}
