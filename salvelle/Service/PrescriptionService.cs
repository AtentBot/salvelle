using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;

namespace Service;

public class PrescriptionService
{
    private readonly AppDbContext _context;

    public PrescriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Prescription? Prescription)> CreatePrescriptionAsync(
        CreatePrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            // Verificar se cliente existe
            var customerExists = await _context.Set<Customer>()
                .AnyAsync(c => c.Id == dto.CustomerId &&
                              c.EstablishmentId == establishmentId);

            if (!customerExists)
                return (false, "Cliente não encontrado", null);

            // Calcular validade
            var expirationDate = CalculateExpirationDate(
                dto.PrescriptionDate,
                dto.PrescriptionType);

            // Gerar código
            var code = await GeneratePrescriptionCodeAsync(establishmentId);

            var prescription = new Prescription
            {
                EstablishmentId = establishmentId,
                CustomerId = dto.CustomerId,
                Code = code,
                PrescriptionDate = dto.PrescriptionDate,
                ExpirationDate = expirationDate,
                DoctorName = dto.DoctorName.ToUpper(),
                DoctorCrm = dto.DoctorCrm,
                DoctorCrmState = dto.DoctorCrmState.ToUpper(),
                PrescriptionType = dto.PrescriptionType.ToUpper(),
                ControlledType = dto.ControlledType?.ToUpper(),
                PrescriptionColor = dto.PrescriptionColor?.ToUpper(),
                Medications = dto.Medications,
                Posology = dto.Posology,
                Observations = dto.Observations,
                ImageUrl = dto.ImageUrl,
                Status = "PENDENTE",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Prescription>().Add(prescription);
            await _context.SaveChangesAsync();

            return (true, "Prescrição registrada com sucesso", prescription);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao registrar prescrição: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> ValidatePrescriptionAsync(
        Guid prescriptionId,
        ValidatePrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var prescription = await _context.Set<Prescription>()
            .FirstOrDefaultAsync(p => p.Id == prescriptionId &&
                                     p.EstablishmentId == establishmentId);

        if (prescription == null)
            return (false, "Prescrição não encontrada");

        if (prescription.Status != "PENDENTE")
            return (false, "Prescrição já foi validada");

        // Verificar expiração
        if (prescription.ExpirationDate < DateTime.Today)
        {
            prescription.Status = "EXPIRADA";
            await _context.SaveChangesAsync();
            return (false, "Prescrição vencida");
        }

        prescription.Status = dto.IsValid ? "VALIDADA" : "CANCELADA";
        prescription.ValidatedAt = DateTime.UtcNow;
        prescription.ValidatedByEmployeeId = employeeId;
        prescription.ValidationNotes = dto.ValidationNotes;
        prescription.UpdatedAt = DateTime.UtcNow;
        prescription.UpdatedByEmployeeId = employeeId;

        if (!dto.IsValid)
        {
            prescription.CancelledAt = DateTime.UtcNow;
            prescription.CancelledByEmployeeId = employeeId;
            prescription.CancellationReason = dto.ValidationNotes;
        }

        await _context.SaveChangesAsync();

        return (true, dto.IsValid ?
            "Prescrição validada com sucesso" :
            "Prescrição rejeitada");
    }

    public async Task<(bool Success, string Message)> CancelPrescriptionAsync(
        Guid prescriptionId,
        string reason,
        Guid establishmentId,
        Guid employeeId)
    {
        var prescription = await _context.Set<Prescription>()
            .FirstOrDefaultAsync(p => p.Id == prescriptionId &&
                                     p.EstablishmentId == establishmentId);

        if (prescription == null)
            return (false, "Prescrição não encontrada");

        if (prescription.Status == "MANIPULADA")
            return (false, "Não é possível cancelar prescrição já manipulada");

        prescription.Status = "CANCELADA";
        prescription.CancelledAt = DateTime.UtcNow;
        prescription.CancelledByEmployeeId = employeeId;
        prescription.CancellationReason = reason;
        prescription.UpdatedAt = DateTime.UtcNow;
        prescription.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();
        return (true, "Prescrição cancelada com sucesso");
    }

    public async Task MarkExpiredPrescriptionsAsync(Guid establishmentId)
    {
        var expiredPrescriptions = await _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId &&
                       p.Status == "VALIDADA" &&
                       p.ExpirationDate < DateTime.Today)
            .ToListAsync();

        foreach (var prescription in expiredPrescriptions)
        {
            prescription.Status = "EXPIRADA";
            prescription.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private DateTime CalculateExpirationDate(DateTime prescriptionDate, string type)
    {
        return type.ToUpper() switch
        {
            "ANTIBIOTICO" => prescriptionDate.AddDays(10),
            "CONTROLE_ESPECIAL" => prescriptionDate.AddDays(30),
            _ => prescriptionDate.AddDays(180) // COMUM
        };
    }

    private async Task<string> GeneratePrescriptionCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RX{year}";

        var lastPrescription = await _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId &&
                       p.Code.StartsWith(prefix))
            .OrderByDescending(p => p.Code)
            .Select(p => p.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastPrescription != null && lastPrescription.Length > prefix.Length)
        {
            var numberPart = lastPrescription.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D5}";
    }
}
