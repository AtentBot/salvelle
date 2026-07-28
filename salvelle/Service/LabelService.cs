using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs.Labels;
using System.Text;
using Models.Employees;
using Models.Pharmacy;

namespace Service;

public class LabelService
{
    private readonly AppDbContext _context;

    public LabelService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, LabelTemplate? Template)> CreateTemplateAsync(
        CreateLabelTemplateDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            // Se for template padrão, desativar outros padrões do mesmo tipo
            if (dto.IsDefault)
            {
                var existingDefaults = await _context.Set<LabelTemplate>()
                    .Where(t => t.EstablishmentId == establishmentId &&
                               t.TemplateType == dto.TemplateType.ToUpper() &&
                               t.IsDefault)
                    .ToListAsync();

                foreach (var template in existingDefaults)
                {
                    template.IsDefault = false;
                }
            }

            var newTemplate = new LabelTemplate
            {
                EstablishmentId = establishmentId,
                Name = dto.Name,
                Description = dto.Description,
                TemplateType = dto.TemplateType.ToUpper(),
                PharmaceuticalForm = dto.PharmaceuticalForm?.ToUpper(),
                Width = dto.Width,
                Height = dto.Height,
                HtmlTemplate = dto.HtmlTemplate,
                CssStyles = dto.CssStyles,
                IncludeEstablishmentName = dto.IncludeEstablishmentName,
                IncludePharmacistName = dto.IncludePharmacistName,
                IncludeFormulaName = dto.IncludeFormulaName,
                IncludeComposition = dto.IncludeComposition,
                IncludePosology = dto.IncludePosology,
                IncludeValidity = dto.IncludeValidity,
                IncludeBatchNumber = dto.IncludeBatchNumber,
                IncludeManipulationDate = dto.IncludeManipulationDate,
                IncludePatientName = dto.IncludePatientName,
                IncludeQrCode = dto.IncludeQrCode,
                IncludeWarnings = dto.IncludeWarnings,
                IsActive = true,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<LabelTemplate>().Add(newTemplate);
            await _context.SaveChangesAsync();

            return (true, "Template criado com sucesso", newTemplate);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao criar template: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, GeneratedLabel? Label)> GenerateLabelAsync(
        GenerateLabelDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            // Buscar ordem de manipulação
            var order = await _context.ManipulationOrders
                .Include(o => o.Formula)
                .FirstOrDefaultAsync(o => o.Id == dto.ManipulationOrderId &&
                                         o.EstablishmentId == establishmentId);

            if (order == null)
                return (false, "Ordem de manipulação não encontrada", null);

            if (order.Status != "FINALIZADO")
                return (false, "Ordem ainda não foi finalizada", null);

            // Buscar template
            LabelTemplate? template;
            if (dto.TemplateId.HasValue)
            {
                template = await _context.Set<LabelTemplate>()
                    .FirstOrDefaultAsync(t => t.Id == dto.TemplateId.Value &&
                                             t.EstablishmentId == establishmentId);
            }
            else
            {
                // Buscar template padrão
                template = await _context.Set<LabelTemplate>()
                    .FirstOrDefaultAsync(t => t.EstablishmentId == establishmentId &&
                                             t.IsDefault &&
                                             t.IsActive);
            }

            if (template == null)
                return (false, "Template não encontrado", null);

            // Buscar farmacêutico aprovador
            var pharmacist = await _context.Employees
                .Include(e => e.JobPosition)
                .FirstOrDefaultAsync(e => e.Id == order.ApprovedByPharmacistId);

            // Buscar estabelecimento
            var establishment = await _context.Set<Establishment>()
                .FirstOrDefaultAsync(e => e.Id == establishmentId);

            // Gerar código do rótulo
            var labelCode = await GenerateLabelCodeAsync(establishmentId);

            // Gerar dados do QR Code
            var qrCodeData = GenerateQrCodeData(order, labelCode);

            // Buscar componentes da fórmula
            var components = await _context.Set<FormulaComponent>()
                .Where(c => c.FormulaId == order.FormulaId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            var composition = BuildComposition(components);

            // Gerar HTML do rótulo
            var html = GenerateLabelHtml(
                template,
                establishment,
                order,
                pharmacist,
                composition,
                qrCodeData,
                dto.CustomWarnings,
                dto.CustomStorageInstructions);

            var label = new GeneratedLabel
            {
                EstablishmentId = establishmentId,
                ManipulationOrderId = order.Id,
                TemplateId = template.Id,
                LabelCode = labelCode,
                PatientName = order.CustomerName ?? "PACIENTE NÃO IDENTIFICADO",
                FormulaName = order.Formula?.Name ?? "",
                Composition = composition,
                Posology = order.SpecialInstructions ?? "",
                ManipulationDate = order.CompletionDate ?? DateTime.UtcNow,
                ExpirationDate = order.ExpiryDate ?? DateTime.UtcNow.AddMonths(6),
                BatchNumber = order.OrderNumber ?? "",
                PharmacistName = pharmacist?.FullName ?? "",
                PharmacistCrm = "",
                Warnings = dto.CustomWarnings ?? order.Formula?.UsageInstructions,
                StorageInstructions = dto.CustomStorageInstructions ?? order.Formula?.StorageInstructions,
                QrCodeData = qrCodeData,
                GeneratedHtml = html,
                Status = "GERADO",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId
            };

            _context.Set<GeneratedLabel>().Add(label);
            await _context.SaveChangesAsync();

            return (true, "Rótulo gerado com sucesso", label);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao gerar rótulo: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> PrintLabelAsync(
        Guid labelId,
        PrintLabelDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var label = await _context.Set<GeneratedLabel>()
            .FirstOrDefaultAsync(l => l.Id == labelId &&
                                     l.EstablishmentId == establishmentId);

        if (label == null)
            return (false, "Rótulo não encontrado");

        // TODO: Implementar integração com impressora
        // Por enquanto, apenas registrar a impressão

        label.PrintCount += dto.Copies;
        label.LastPrintedAt = DateTime.UtcNow;
        label.LastPrintedById = employeeId;
        label.Status = "IMPRESSO";

        await _context.SaveChangesAsync();

        return (true, $"Rótulo impresso com sucesso ({dto.Copies} cópia(s))");
    }

    private string GenerateLabelHtml(
        LabelTemplate template,
        Establishment? establishment,
        ManipulationOrder order,
        Employee? pharmacist,
        string composition,
        string qrCodeData,
        string? customWarnings,
        string? customStorage)
    {
        var html = new StringBuilder(template.HtmlTemplate);

        // Substituir placeholders com dados do estabelecimento
        html.Replace("{{ESTABLISHMENT_NAME}}", establishment?.NomeFantasia ?? "");

        // Montar endereço completo
        var address = establishment != null
            ? $"{establishment.Street}, {establishment.Number}{(string.IsNullOrWhiteSpace(establishment.Complement) ? "" : " - " + establishment.Complement)} - {establishment.Neighborhood}"
            : "";
        html.Replace("{{ESTABLISHMENT_ADDRESS}}", address);

        html.Replace("{{ESTABLISHMENT_PHONE}}", establishment?.Phone ?? "");
        html.Replace("{{ESTABLISHMENT_CNPJ}}", establishment?.Cnpj ?? "");

        // Dados do produto e manipulação
        html.Replace("{{PATIENT_NAME}}", order.CustomerName ?? "");
        html.Replace("{{FORMULA_NAME}}", order.Formula?.Name ?? "");
        html.Replace("{{COMPOSITION}}", composition);
        html.Replace("{{POSOLOGY}}", order.SpecialInstructions ?? "");
        html.Replace("{{MANIPULATION_DATE}}", (order.CompletionDate ?? DateTime.UtcNow).ToString("dd/MM/yyyy"));
        html.Replace("{{EXPIRATION_DATE}}", (order.ExpiryDate ?? DateTime.UtcNow.AddMonths(6)).ToString("dd/MM/yyyy"));
        html.Replace("{{BATCH_NUMBER}}", order.OrderNumber ?? "");
        html.Replace("{{PHARMACIST_NAME}}", pharmacist?.FullName ?? "");
        html.Replace("{{PHARMACIST_CRM}}", "");
        html.Replace("{{QR_CODE_DATA}}", qrCodeData);
        html.Replace("{{WARNINGS}}", customWarnings ?? order.Formula?.UsageInstructions ?? "");
        html.Replace("{{STORAGE}}", customStorage ?? order.Formula?.StorageInstructions ?? "");

        return html.ToString();
    }

    private string BuildComposition(List<FormulaComponent> components)
    {
        var sb = new StringBuilder();
        foreach (var component in components)
        {
            var rawMaterial = _context.RawMaterials
                .FirstOrDefault(r => r.Id == component.RawMaterialId);

            if (rawMaterial != null)
            {
                sb.AppendLine($"{rawMaterial.Name} - {component.Quantity}{component.Unit}");
            }
        }
        return sb.ToString();
    }

    private string GenerateQrCodeData(ManipulationOrder order, string labelCode)
    {
        return $"OM:{order.Code}|LABEL:{labelCode}|DATE:{DateTime.UtcNow:yyyy-MM-dd}|BATCH:{order.OrderNumber}";
    }

    private async Task<string> GenerateLabelCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"LB{year}";

        var lastLabel = await _context.Set<GeneratedLabel>()
            .Where(l => l.EstablishmentId == establishmentId &&
                       l.LabelCode.StartsWith(prefix))
            .OrderByDescending(l => l.LabelCode)
            .Select(l => l.LabelCode)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastLabel != null && lastLabel.Length > prefix.Length)
        {
            var numberPart = lastLabel.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D6}";
    }
}