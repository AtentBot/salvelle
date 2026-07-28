using System.Text.Json;
using Data;
using DTOs.ManipulationSteps;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;

namespace Service;

public class WeighingStepService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WeighingStepService> _logger;
    private const decimal TOLERANCE_PERCENTAGE = 5.0m;

    public WeighingStepService(AppDbContext context, ILogger<WeighingStepService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool success, string? error, WeighingStepResponseDto? result)> ProcessWeighingStepAsync(
        Guid manipulationOrderId,
        WeighingStepRequestDto request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Validar ordem de manipulação
            var order = await _context.ManipulationOrders
                .Include(o => o.Formula)
                    .ThenInclude(f => f!.Components)
                        .ThenInclude(c => c.RawMaterial)
                .FirstOrDefaultAsync(o => o.Id == manipulationOrderId);

            if (order == null)
                return (false, "Ordem de manipulação não encontrada", null);

            // Status válidos para iniciar pesagem
            var validStatuses = new[] { "PENDENTE", "EM_PRODUCAO", "PESAGEM" };
            if (!validStatuses.Contains(order.Status))
                return (false, $"Ordem não pode iniciar pesagem. Status atual: {order.Status}", null);

            if (order.Formula?.Components == null || !order.Formula.Components.Any())
                return (false, "Fórmula não possui componentes cadastrados", null);

            // 2. Validar funcionário
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
                return (false, "Funcionário não encontrado", null);

            if (employee.Status?.ToUpper() != "ATIVO")
                return (false, $"Funcionário não está ativo. Status atual: {employee.Status}", null);

            // 3. Verificar se já existe etapa de pesagem em andamento
            var existingStep = await _context.Set<ManipulationStep>()
                .FirstOrDefaultAsync(s => s.ManipulationOrderId == manipulationOrderId &&
                                        s.StepType == "PESAGEM" &&
                                        s.Status != "CONCLUIDA");

            ManipulationStep step;
            if (existingStep != null)
            {
                // Continuar etapa existente
                step = existingStep;
            }
            else
            {
                // Criar nova etapa
                step = new ManipulationStep
                {
                    Id = Guid.NewGuid(),
                    ManipulationOrderId = manipulationOrderId,
                    StepType = "PESAGEM",
                    Status = "EM_EXECUCAO",
                    PerformedByEmployeeId = request.EmployeeId,
                    StartedAt = DateTime.UtcNow,
                    Observations = request.Observations,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Set<ManipulationStep>().Add(step);
            }

            // 4. Validar e processar cada componente pesado
            var warnings = new List<string>();
            var componentsWeighed = new List<ComponentWeighingResultDto>();

            foreach (var componentDto in request.Components)
            {
                // Buscar componente na fórmula
                var formulaComponent = order.Formula.Components
                    .FirstOrDefault(c => c.Id == componentDto.ComponentId);

                if (formulaComponent == null)
                {
                    warnings.Add($"Componente {componentDto.ComponentId} não encontrado na fórmula");
                    continue;
                }

                // Validar lote
                var batch = await _context.Batches
                    .Include(b => b.RawMaterial)
                    .FirstOrDefaultAsync(b => b.Id == componentDto.BatchId &&
                                            b.RawMaterialId == formulaComponent.RawMaterialId);

                if (batch == null)
                {
                    return (false, $"Lote {componentDto.BatchId} não encontrado ou não corresponde à matéria-prima", null);
                }

                // Validar status do lote
                if (batch.Status != "APROVADO")
                {
                    return (false, $"Lote {batch.BatchNumber} não está aprovado para uso. Status: {batch.Status}", null);
                }

                // Validar validade
                if (batch.ExpiryDate < DateTime.UtcNow)
                {
                    return (false, $"Lote {batch.BatchNumber} está vencido", null);
                }

                // Alerta de vencimento próximo (30 dias)
                if (batch.ExpiryDate < DateTime.UtcNow.AddDays(30))
                {
                    warnings.Add($"ATENÇÃO: Lote {batch.BatchNumber} vence em menos de 30 dias ({batch.ExpiryDate:dd/MM/yyyy})");
                }

                // Calcular quantidade necessária considerando fator de correção
                var plannedQuantity = formulaComponent.Quantity * order.QuantityToProduce;
                var correctedQuantity = plannedQuantity * batch.RawMaterial!.PurityFactor;

                // Validar quantidade disponível
                if (batch.CurrentQuantity < componentDto.WeighedQuantity)
                {
                    return (false, $"Quantidade insuficiente no lote {batch.BatchNumber}. Disponível: {batch.CurrentQuantity}, Solicitado: {componentDto.WeighedQuantity}", null);
                }

                // Calcular desvio percentual
                var deviation = Math.Abs((componentDto.WeighedQuantity - correctedQuantity) / correctedQuantity * 100);
                var isWithinTolerance = deviation <= TOLERANCE_PERCENTAGE;

                if (!isWithinTolerance)
                {
                    warnings.Add($"DESVIO ELEVADO: {formulaComponent.RawMaterial!.Name} - {deviation:F2}% (Tolerância: {TOLERANCE_PERCENTAGE}%)");
                }

                // 5. Criar movimentação de estoque
                var stockMovement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = order.EstablishmentId,
                    RawMaterialId = formulaComponent.RawMaterialId,
                    BatchId = batch.Id,
                    MovementType = "MANIPULACAO",
                    Quantity = -componentDto.WeighedQuantity,
                    StockBefore = batch.RawMaterial.CurrentStock,
                    StockAfter = batch.RawMaterial.CurrentStock - componentDto.WeighedQuantity,
                    Reason = $"Pesagem para manipulação {order.OrderNumber}",
                    ManipulationOrderId = manipulationOrderId,
                    DocumentNumber = order.OrderNumber,
                    MovementDate = DateTime.UtcNow,
                    PerformedByEmployeeId = request.EmployeeId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(stockMovement);

                // Atualizar quantidades
                batch.CurrentQuantity -= componentDto.WeighedQuantity;
                batch.RawMaterial.CurrentStock -= componentDto.WeighedQuantity;
                batch.RawMaterial.UpdatedAt = DateTime.UtcNow;

                // 6. Registrar foto (se fornecida)
                string? photoUrl = null;
                if (!string.IsNullOrWhiteSpace(componentDto.PhotoBase64))
                {
                    var photo = new ManipulationPhoto
                    {
                        Id = Guid.NewGuid(),
                        ManipulationOrderId = manipulationOrderId,
                        ManipulationStepId = step.Id,
                        StepType = "PESAGEM",
                        PhotoUrl = componentDto.PhotoBase64,
                        Description = $"Pesagem de {formulaComponent.RawMaterial!.Name} - Lote {batch.BatchNumber}",
                        CapturedByEmployeeId = request.EmployeeId,
                        CapturedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Set<ManipulationPhoto>().Add(photo);
                    photoUrl = photo.Id.ToString();
                }

                // Adicionar ao resultado
                componentsWeighed.Add(new ComponentWeighingResultDto
                {
                    ComponentId = componentDto.ComponentId,
                    ComponentName = formulaComponent.RawMaterial!.Name,
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    PlannedQuantity = correctedQuantity,
                    WeighedQuantity = componentDto.WeighedQuantity,
                    DeviationPercentage = deviation,
                    IsWithinTolerance = isWithinTolerance,
                    PhotoUrl = photoUrl
                });
            }

            // 7. Salvar dados da etapa como JSON
            var stepData = new
            {
                ComponentsWeighed = componentsWeighed.Select(c => new
                {
                    c.ComponentId,
                    c.BatchId,
                    c.WeighedQuantity,
                    c.DeviationPercentage,
                    c.IsWithinTolerance
                }).ToList(),
                Warnings = warnings,
                ProcessedAt = DateTime.UtcNow
            };

            step.StepData = JsonSerializer.Serialize(stepData);
            step.CompletedAt = DateTime.UtcNow;
            step.Status = "CONCLUIDA";
            step.UpdatedAt = DateTime.UtcNow;

            // 8. Atualizar status da ordem
            if (order.Status == "PENDENTE")
            {
                order.Status = "EM_PRODUCAO";
                order.StartDate = DateTime.UtcNow;
            }

            order.Status = "PESAGEM";
            order.UpdatedAt = DateTime.UtcNow;

            // Salvar todas as alterações
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Etapa de pesagem concluída - Ordem: {OrderNumber}, Componentes: {Count}, Funcionário: {EmployeeId}",
                order.OrderNumber, componentsWeighed.Count, request.EmployeeId);

            var response = new WeighingStepResponseDto
            {
                StepId = step.Id,
                Status = step.Status,
                StartedAt = step.StartedAt ?? DateTime.UtcNow,
                CompletedAt = step.CompletedAt,
                ComponentsWeighed = componentsWeighed,
                Warnings = warnings,
                Observations = step.Observations
            };

            return (true, null, response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao processar etapa de pesagem - Ordem: {OrderId}", manipulationOrderId);
            return (false, $"Erro ao processar pesagem: {ex.Message}", null);
        }
    }
}