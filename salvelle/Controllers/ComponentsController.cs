using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Formulas;
using Models.Pharmacy;

namespace Controllers;

[ApiController]
[Route("api/formulas/{formulaId}/[controller]")]
public class ComponentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComponentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FormulaComponentDto>>>> GetAll(Guid formulaId)
    {
        var components = await _context.FormulaComponents
            .Include(c => c.RawMaterial)
            .Where(c => c.FormulaId == formulaId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new FormulaComponentDto
            {
                Id = c.Id,
                FormulaId = c.FormulaId,
                RawMaterialId = c.RawMaterialId,
                RawMaterialCode = c.RawMaterial!.DcbCode ?? c.RawMaterial.CasNumber,
                RawMaterialName = c.RawMaterial.Name,
                RawMaterialUnit = c.RawMaterial.Unit,
                RawMaterialIsControlled = c.RawMaterial.ControlType != "COMUM",
                Quantity = c.Quantity,
                Unit = c.Unit,
                ComponentType = c.ComponentType,
                OrderIndex = c.OrderIndex,
                SpecialInstructions = c.SpecialInstructions,
                IsOptional = c.IsOptional
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FormulaComponentDto>>.SuccessResponse(components));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FormulaComponentDto>>> GetById(Guid formulaId, Guid id)
    {
        var component = await _context.FormulaComponents
            .Include(c => c.RawMaterial)
            .Where(c => c.Id == id && c.FormulaId == formulaId)
            .Select(c => new FormulaComponentDto
            {
                Id = c.Id,
                FormulaId = c.FormulaId,
                RawMaterialId = c.RawMaterialId,
                RawMaterialCode = c.RawMaterial!.DcbCode ?? c.RawMaterial.CasNumber,
                RawMaterialName = c.RawMaterial.Name,
                RawMaterialUnit = c.RawMaterial.Unit,
                RawMaterialIsControlled = c.RawMaterial.ControlType != "COMUM",
                Quantity = c.Quantity,
                Unit = c.Unit,
                ComponentType = c.ComponentType,
                OrderIndex = c.OrderIndex,
                SpecialInstructions = c.SpecialInstructions,
                IsOptional = c.IsOptional
            })
            .FirstOrDefaultAsync();

        if (component == null)
            return NotFound(ApiResponse<FormulaComponentDto>.ErrorResponse("Componente não encontrado"));

        return Ok(ApiResponse<FormulaComponentDto>.SuccessResponse(component));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> Create(Guid formulaId, [FromBody] CreateFormulaComponentDto dto)
    {
        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula == null)
            return NotFound(ApiResponse.ErrorResponse("Fórmula não encontrada"));

        var exists = await _context.FormulaComponents
            .AnyAsync(c => c.FormulaId == formulaId && c.RawMaterialId == dto.RawMaterialId);

        if (exists)
            return BadRequest(ApiResponse.ErrorResponse("Matéria-prima já adicionada nesta fórmula"));

        var component = new FormulaComponent
        {
            Id = Guid.NewGuid(),
            FormulaId = formulaId,
            RawMaterialId = dto.RawMaterialId,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            ComponentType = dto.ComponentType,
            OrderIndex = dto.OrderIndex,
            SpecialInstructions = dto.SpecialInstructions,
            IsOptional = dto.IsOptional
        };

        _context.FormulaComponents.Add(component);
        
        formula.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Componente adicionado com sucesso"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(Guid formulaId, Guid id, [FromBody] UpdateFormulaComponentDto dto)
    {
        var component = await _context.FormulaComponents
            .Where(c => c.Id == id && c.FormulaId == formulaId)
            .FirstOrDefaultAsync();

        if (component == null)
            return NotFound(ApiResponse.ErrorResponse("Componente não encontrado"));

        component.Quantity = dto.Quantity;
        component.Unit = dto.Unit;
        component.ComponentType = dto.ComponentType;
        component.OrderIndex = dto.OrderIndex;
        component.SpecialInstructions = dto.SpecialInstructions;
        component.IsOptional = dto.IsOptional;

        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula != null)
            formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Componente atualizado com sucesso"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid formulaId, Guid id)
    {
        var component = await _context.FormulaComponents
            .Where(c => c.Id == id && c.FormulaId == formulaId)
            .FirstOrDefaultAsync();

        if (component == null)
            return NotFound(ApiResponse.ErrorResponse("Componente não encontrado"));

        _context.FormulaComponents.Remove(component);

        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula != null)
            formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Componente removido com sucesso"));
    }

    [HttpPut("reorder")]
    public async Task<ActionResult<ApiResponse>> Reorder(Guid formulaId, [FromBody] Dictionary<Guid, int> newOrder)
    {
        var components = await _context.FormulaComponents
            .Where(c => c.FormulaId == formulaId)
            .ToListAsync();

        foreach (var component in components)
        {
            if (newOrder.TryGetValue(component.Id, out int newIndex))
                component.OrderIndex = newIndex;
        }

        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula != null)
            formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Ordem dos componentes atualizada"));
    }
}
