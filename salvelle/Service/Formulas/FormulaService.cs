using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using DTOs.Formulas;

namespace Service.Formulas;

public class FormulaService
{
    private readonly AppDbContext _context;

    public FormulaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Formula? Formula)> CreateFormulaAsync(
        CreateFormulaDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verificar duplicidade de nome
            var exists = await _context.Formulas
                .AnyAsync(f => f.EstablishmentId == establishmentId &&
                              f.Name.ToUpper() == dto.Name.ToUpper() &&
                              f.IsActive);

            if (exists)
                return (false, "Já existe uma fórmula ativa com este nome", null);

            // Validar matérias-primas
            var rawMaterialIds = dto.Components.Select(c => c.RawMaterialId).Distinct().ToList();
            var rawMaterials = await _context.RawMaterials
                .Where(r => rawMaterialIds.Contains(r.Id) && r.EstablishmentId == establishmentId)
                .ToListAsync();

            if (rawMaterials.Count != rawMaterialIds.Count)
                return (false, "Uma ou mais matérias-primas não encontradas", null);

            // Gerar código único
            var code = await GenerateFormulaCodeAsync(establishmentId);

            var formula = new Formula
            {
                EstablishmentId = establishmentId,
                Code = code,
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category.ToUpper(),
                PharmaceuticalForm = dto.PharmaceuticalForm.ToUpper(),
                StandardYield = dto.StandardYield,
                ShelfLifeDays = dto.ShelfLifeDays ?? 180,
                PreparationInstructions = dto.PreparationInstructions,
                StorageInstructions = dto.StorageInstructions,
                UsageInstructions = dto.UsageInstructions,
                RequiresSpecialControl = dto.RequiresSpecialControl,
                RequiresPrescription = dto.RequiresPrescription,
                IsActive = dto.IsActive,
                Version = 1,
                CreatedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Formulas.Add(formula);
            await _context.SaveChangesAsync();

            // Adicionar componentes
            int orderIndex = 1;
            foreach (var componentDto in dto.Components)
            {
                var component = new FormulaComponent
                {
                    FormulaId = formula.Id,
                    RawMaterialId = componentDto.RawMaterialId,
                    Quantity = componentDto.Quantity,
                    Unit = componentDto.Unit,
                    ComponentType = componentDto.ComponentType.ToUpper(),
                    OrderIndex = orderIndex++,
                    SpecialInstructions = componentDto.SpecialInstructions,
                    IsOptional = componentDto.IsOptional
                };

                _context.FormulaComponents.Add(component);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Fórmula criada com sucesso", formula);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao criar fórmula: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdateFormulaAsync(
        Guid formulaId,
        UpdateFormulaDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var formula = await _context.Formulas
                .Include(f => f.Components)
                .FirstOrDefaultAsync(f => f.Id == formulaId && f.EstablishmentId == establishmentId);

            if (formula == null)
                return (false, "Fórmula não encontrada");

            // Verificar duplicidade de nome (exceto ela mesma)
            var exists = await _context.Formulas
                .AnyAsync(f => f.EstablishmentId == establishmentId &&
                              f.Name.ToUpper() == dto.Name.ToUpper() &&
                              f.Id != formulaId &&
                              f.IsActive);

            if (exists)
                return (false, "Já existe outra fórmula ativa com este nome");

            // Validar matérias-primas
            var rawMaterialIds = dto.Components.Select(c => c.RawMaterialId).Distinct().ToList();
            var rawMaterials = await _context.RawMaterials
                .Where(r => rawMaterialIds.Contains(r.Id) && r.EstablishmentId == establishmentId)
                .ToListAsync();

            if (rawMaterials.Count != rawMaterialIds.Count)
                return (false, "Uma ou mais matérias-primas não encontradas");

            // Atualizar fórmula
            formula.Name = dto.Name;
            formula.Description = dto.Description;
            formula.Category = dto.Category.ToUpper();
            formula.PharmaceuticalForm = dto.PharmaceuticalForm.ToUpper();
            formula.StandardYield = dto.StandardYield;
            formula.ShelfLifeDays = dto.ShelfLifeDays ?? 180;
            formula.PreparationInstructions = dto.PreparationInstructions;
            formula.StorageInstructions = dto.StorageInstructions;
            formula.UsageInstructions = dto.UsageInstructions;
            formula.RequiresSpecialControl = dto.RequiresSpecialControl;
            formula.RequiresPrescription = dto.RequiresPrescription;
            formula.IsActive = dto.IsActive;
            formula.Version += 1;
            formula.UpdatedByEmployeeId = employeeId;
            formula.UpdatedAt = DateTime.UtcNow;

            // Remover componentes antigos
            _context.FormulaComponents.RemoveRange(formula.Components);

            // Adicionar novos componentes
            int orderIndex = 1;
            foreach (var componentDto in dto.Components)
            {
                var component = new FormulaComponent
                {
                    FormulaId = formula.Id,
                    RawMaterialId = componentDto.RawMaterialId,
                    Quantity = componentDto.Quantity,
                    Unit = componentDto.Unit,
                    ComponentType = componentDto.ComponentType.ToUpper(),
                    OrderIndex = orderIndex++,
                    SpecialInstructions = componentDto.SpecialInstructions,
                    IsOptional = componentDto.IsOptional
                };

                _context.FormulaComponents.Add(component);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Fórmula atualizada com sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao atualizar fórmula: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteFormulaAsync(
        Guid formulaId,
        Guid establishmentId)
    {
        var formula = await _context.Formulas
            .FirstOrDefaultAsync(f => f.Id == formulaId && f.EstablishmentId == establishmentId);

        if (formula == null)
            return (false, "Fórmula não encontrada");

        // Soft delete
        formula.IsActive = false;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Fórmula desativada com sucesso");
    }

    public async Task<(bool Success, string Message, Formula? Formula)> DuplicateFormulaAsync(
        Guid formulaId,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var original = await _context.Formulas
                .Include(f => f.Components)
                .FirstOrDefaultAsync(f => f.Id == formulaId && f.EstablishmentId == establishmentId);

            if (original == null)
                return (false, "Fórmula original não encontrada", null);

            var code = await GenerateFormulaCodeAsync(establishmentId);

            var newFormula = new Formula
            {
                EstablishmentId = establishmentId,
                Code = code,
                Name = $"{original.Name} (Cópia)",
                Description = original.Description,
                Category = original.Category,
                PharmaceuticalForm = original.PharmaceuticalForm,
                StandardYield = original.StandardYield,
                ShelfLifeDays = original.ShelfLifeDays,
                PreparationInstructions = original.PreparationInstructions,
                StorageInstructions = original.StorageInstructions,
                UsageInstructions = original.UsageInstructions,
                RequiresSpecialControl = original.RequiresSpecialControl,
                RequiresPrescription = original.RequiresPrescription,
                IsActive = true,
                Version = 1,
                CreatedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Formulas.Add(newFormula);
            await _context.SaveChangesAsync();

            int orderIndex = 1;
            foreach (var originalComponent in original.Components)
            {
                var newComponent = new FormulaComponent
                {
                    FormulaId = newFormula.Id,
                    RawMaterialId = originalComponent.RawMaterialId,
                    Quantity = originalComponent.Quantity,
                    Unit = originalComponent.Unit,
                    ComponentType = originalComponent.ComponentType,
                    OrderIndex = orderIndex++,
                    SpecialInstructions = originalComponent.SpecialInstructions,
                    IsOptional = originalComponent.IsOptional
                };

                _context.FormulaComponents.Add(newComponent);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Fórmula duplicada com sucesso", newFormula);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao duplicar fórmula: {ex.Message}", null);
        }
    }

    public async Task<FormulaCostCalculationDto> CalculateFormulaCostAsync(
        Guid formulaId,
        Guid establishmentId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Components)
                .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(f => f.Id == formulaId && f.EstablishmentId == establishmentId);

        if (formula == null)
            throw new Exception("Fórmula não encontrada");

        var componentsCost = new List<ComponentCostDto>();
        decimal totalCost = 0;

        foreach (var component in formula.Components)
        {
            var lastBatch = await _context.Batches
                .Where(b => b.RawMaterialId == component.RawMaterialId &&
                           b.Status.ToUpper() == "APROVADO" &&
                           b.CurrentQuantity > 0)
                .OrderByDescending(b => b.ReceivedDate)
                .FirstOrDefaultAsync();

            var unitCost = lastBatch?.UnitCost ?? 0;
            var componentTotalCost = component.Quantity * unitCost;
            totalCost += componentTotalCost;

            componentsCost.Add(new ComponentCostDto
            {
                RawMaterialName = component.RawMaterial?.Name ?? "",
                Quantity = component.Quantity,
                Unit = component.Unit,
                UnitCost = unitCost,
                TotalCost = componentTotalCost,
                IsAvailable = lastBatch != null
            });
        }

        var profitMargin = 2.5m; // 150% de lucro
        var suggestedPrice = totalCost * profitMargin;

        return new FormulaCostCalculationDto
        {
            FormulaId = formula.Id,
            FormulaName = formula.Name,
            TotalCost = totalCost,
            SuggestedPrice = suggestedPrice,
            ProfitMargin = profitMargin,
            ComponentsCost = componentsCost
        };
    }

    private async Task<string> GenerateFormulaCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"F{year}";

        var lastFormula = await _context.Formulas
            .Where(f => f.EstablishmentId == establishmentId && f.Code.StartsWith(prefix))
            .OrderByDescending(f => f.Code)
            .Select(f => f.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastFormula != null && lastFormula.Length > prefix.Length)
        {
            var numberPart = lastFormula.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }
}