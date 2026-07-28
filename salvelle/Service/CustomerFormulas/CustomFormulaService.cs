using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;
using Models.Pharmacy;
using DTOs.CustomerFormulas;

namespace Service.CustomerFormulas;

public class CustomFormulaService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomFormulaService> _logger;

    public CustomFormulaService(
        AppDbContext context,
        ILogger<CustomFormulaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Criar nova fórmula personalizada
    /// </summary>
    public async Task<CustomerFormula> CreateFormulaAsync(
        CreateCustomFormulaDto dto,
        Guid establishmentId,
        Guid? customerId = null)
    {
        try
        {
            // 1. Validar ProductType e ProductSubType
            var subType = await _context.ProductSubTypes
                .Include(pst => pst.ProductType)
                .FirstOrDefaultAsync(pst => pst.Id == dto.ProductSubTypeId);

            if (subType == null)
                throw new Exception("Subtipo de produto não encontrado");

            if (subType.ProductTypeId != dto.ProductTypeId)
                throw new Exception("Subtipo não pertence ao tipo selecionado");

            // 2. Gerar código único
            var code = await GenerateCodeAsync(establishmentId);

            // 3. Criar CustomerFormula
            var formula = new CustomerFormula
            {
                Id = Guid.NewGuid(),
                Code = code,
                EstablishmentId = establishmentId,
                CustomerId = customerId,

                // Dados do cliente
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = dto.CustomerEmail,

                // Configuração do produto
                ProductTypeId = dto.ProductTypeId,
                ProductSubTypeId = dto.ProductSubTypeId,
                Quantity = dto.Quantity,
                Unit = dto.Unit,

                // Componentes adicionais
                AdditionalIngredients = dto.AdditionalIngredients,
                CustomerNotes = dto.CustomerNotes,

                // Status inicial
                Status = "AGUARDANDO_COMPRA",

                // Token de sessão (para carrinho anônimo)
                SessionToken = Guid.NewGuid().ToString(),
                SessionExpiresAt = DateTime.UtcNow.AddDays(7),

                // Auditoria
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomerFormulas.Add(formula);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Fórmula {Code} criada com sucesso para estabelecimento {EstablishmentId}",
                formula.Code, establishmentId);

            return formula;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fórmula personalizada");
            throw;
        }
    }

    /// <summary>
    /// Gerar código único para a fórmula
    /// </summary>
    private async Task<string> GenerateCodeAsync(Guid establishmentId)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        // Buscar próximo número sequencial do dia
        var todayFormulas = await _context.CustomerFormulas
            .Where(cf => cf.EstablishmentId == establishmentId
                      && cf.Code.StartsWith($"CF-{date}-"))
            .CountAsync();

        var sequence = todayFormulas + 1;

        return $"CF-{date}-{sequence:D4}";
    }

    /// <summary>
    /// Buscar fórmula por ID
    /// </summary>
    public async Task<CustomerFormula?> GetFormulaByIdAsync(Guid id)
    {
        return await _context.CustomerFormulas
            .Include(cf => cf.ProductType)
            .Include(cf => cf.ProductSubType)
            .Include(cf => cf.AnalysisLogs)
            .FirstOrDefaultAsync(cf => cf.Id == id);
    }

    /// <summary>
    /// Buscar fórmula por código
    /// </summary>
    public async Task<CustomerFormula?> GetFormulaByCodeAsync(string code)
    {
        return await _context.CustomerFormulas
            .Include(cf => cf.ProductType)
            .Include(cf => cf.ProductSubType)
            .FirstOrDefaultAsync(cf => cf.Code == code);
    }

    /// <summary>
    /// Atualizar status da fórmula
    /// </summary>
    public async Task<bool> UpdateStatusAsync(Guid formulaId, string newStatus)
    {
        var formula = await _context.CustomerFormulas.FindAsync(formulaId);

        if (formula == null)
            return false;

        formula.Status = newStatus;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Status da fórmula {Code} atualizado para {Status}",
            formula.Code, newStatus);

        return true;
    }

    /// <summary>
    /// Listar fórmulas de um estabelecimento
    /// </summary>
    public async Task<List<CustomerFormula>> GetFormulasByEstablishmentAsync(
        Guid establishmentId,
        string? status = null)
    {
        var query = _context.CustomerFormulas
            .Include(cf => cf.ProductType)
            .Include(cf => cf.ProductSubType)
            .Where(cf => cf.EstablishmentId == establishmentId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(cf => cf.Status == status);

        return await query
            .OrderByDescending(cf => cf.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Listar fórmulas de um cliente
    /// </summary>
    public async Task<List<CustomerFormula>> GetFormulasByCustomerAsync(Guid customerId)
    {
        return await _context.CustomerFormulas
            .Include(cf => cf.ProductType)
            .Include(cf => cf.ProductSubType)
            .Where(cf => cf.CustomerId == customerId)
            .OrderByDescending(cf => cf.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Cancelar fórmula
    /// </summary>
    public async Task<bool> CancelFormulaAsync(Guid formulaId, string reason)
    {
        var formula = await _context.CustomerFormulas.FindAsync(formulaId);

        if (formula == null)
            return false;

        // Só pode cancelar se não estiver em produção
        if (formula.Status == "EM_PRODUCAO" || formula.Status == "FINALIZADO")
            throw new Exception("Não é possível cancelar fórmula em produção ou finalizada");

        formula.Status = "CANCELADO";
        formula.RejectionReason = reason;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Fórmula {Code} cancelada. Motivo: {Reason}",
            formula.Code, reason);

        return true;
    }
}