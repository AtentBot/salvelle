using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;

namespace Service;

public class CustomerService
{
    private readonly AppDbContext _context;
    private const int MAX_CODE_GENERATION_RETRIES = 3;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(
        CreateCustomerDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            // Verificar CPF duplicado
            if (!string.IsNullOrWhiteSpace(dto.Cpf))
            {
                var cpfExists = await _context.Set<Customer>()
                    .AnyAsync(c => c.EstablishmentId == establishmentId &&
                                  c.Cpf == dto.Cpf);

                if (cpfExists)
                    return (false, "Já existe um cliente cadastrado com este CPF", null);
            }

            // Loop de retry para lidar com race conditions na geração do código
            for (int attempt = 1; attempt <= MAX_CODE_GENERATION_RETRIES; attempt++)
            {
                try
                {
                    // Gerar código único
                    var code = await GenerateCustomerCodeAsync(establishmentId);

                    var customer = new Customer
                    {
                        EstablishmentId = establishmentId,
                        Code = code,
                        FullName = dto.FullName.ToUpper(),
                        Cpf = dto.Cpf?.Replace(".", "").Replace("-", ""),
                        Rg = dto.Rg,
                        BirthDate = dto.BirthDate,
                        Gender = dto.Gender?.ToUpper(),
                        Phone = dto.Phone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                        WhatsApp = dto.WhatsApp?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                        Email = dto.Email?.ToLower(),
                        ZipCode = dto.ZipCode?.Replace("-", ""),
                        Street = dto.Street,
                        Number = dto.Number,
                        Complement = dto.Complement,
                        Neighborhood = dto.Neighborhood,
                        City = dto.City,
                        State = dto.State?.ToUpper(),
                        Allergies = dto.Allergies,
                        MedicalConditions = dto.MedicalConditions,
                        Observations = dto.Observations,
                        ConsentDataProcessing = dto.ConsentDataProcessing,
                        ConsentDate = dto.ConsentDataProcessing ? DateTime.UtcNow : null,
                        Status = "ATIVO",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByEmployeeId = employeeId,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Set<Customer>().Add(customer);
                    await _context.SaveChangesAsync();

                    return (true, "Cliente cadastrado com sucesso", customer);
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    // Limpar o contexto para tentar novamente
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    if (attempt == MAX_CODE_GENERATION_RETRIES)
                    {
                        return (false, "Erro ao gerar código do cliente. Por favor, tente novamente.", null);
                    }

                    // Pequeno delay antes de tentar novamente
                    await Task.Delay(50 * attempt);
                }
            }

            return (false, "Erro ao cadastrar cliente. Tente novamente.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao cadastrar cliente: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdateCustomerAsync(
        Guid customerId,
        UpdateCustomerDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            var customer = await _context.Set<Customer>()
                .FirstOrDefaultAsync(c => c.Id == customerId &&
                                         c.EstablishmentId == establishmentId);

            if (customer == null)
                return (false, "Cliente não encontrado");

            customer.FullName = dto.FullName.ToUpper();
            customer.Cpf = dto.Cpf?.Replace(".", "").Replace("-", "");
            customer.Rg = dto.Rg;
            customer.BirthDate = dto.BirthDate;
            customer.Gender = dto.Gender?.ToUpper();
            customer.Phone = dto.Phone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
            customer.WhatsApp = dto.WhatsApp?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
            customer.Email = dto.Email?.ToLower();
            customer.ZipCode = dto.ZipCode?.Replace("-", "");
            customer.Street = dto.Street;
            customer.Number = dto.Number;
            customer.Complement = dto.Complement;
            customer.Neighborhood = dto.Neighborhood;
            customer.City = dto.City;
            customer.State = dto.State?.ToUpper();
            customer.Allergies = dto.Allergies;
            customer.MedicalConditions = dto.MedicalConditions;
            customer.Observations = dto.Observations;
            customer.Status = dto.Status.ToUpper();
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedByEmployeeId = employeeId;

            await _context.SaveChangesAsync();
            return (true, "Cliente atualizado com sucesso");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao atualizar cliente: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> BlockCustomerAsync(
        Guid customerId,
        string reason,
        Guid establishmentId,
        Guid employeeId)
    {
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == customerId &&
                                     c.EstablishmentId == establishmentId);

        if (customer == null)
            return (false, "Cliente não encontrado");

        customer.Status = "BLOQUEADO";
        customer.BlockReason = reason;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();
        return (true, "Cliente bloqueado com sucesso");
    }

    public async Task<(bool Success, string Message)> UnblockCustomerAsync(
        Guid customerId,
        Guid establishmentId,
        Guid employeeId)
    {
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == customerId &&
                                     c.EstablishmentId == establishmentId);

        if (customer == null)
            return (false, "Cliente não encontrado");

        customer.Status = "ATIVO";
        customer.BlockReason = null;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();
        return (true, "Cliente desbloqueado com sucesso");
    }

    private async Task<string> GenerateCustomerCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CLI{year}";

        // Usar MAX para evitar race conditions
        var maxCode = await _context.Set<Customer>()
            .Where(c => c.EstablishmentId == establishmentId &&
                       c.Code.StartsWith(prefix))
            .MaxAsync(c => (string?)c.Code);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(maxCode) && maxCode.Length > prefix.Length)
        {
            var numberPart = maxCode.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D5}";
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException is Npgsql.PostgresException pgEx && 
               pgEx.SqlState == "23505"; // Código PostgreSQL para unique violation
    }
}
