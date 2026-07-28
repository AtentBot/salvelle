using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;

namespace Service;

public class SngpcService
{
    private readonly AppDbContext _context;

    public SngpcService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, ControlledSubstanceMovement? Movement)> RegisterMovementAsync(
        RegisterControlledMovementDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Buscar matéria-prima
            var rawMaterial = await _context.RawMaterials
                .FirstOrDefaultAsync(r => r.Id == dto.RawMaterialId &&
                                         r.EstablishmentId == establishmentId);

            if (rawMaterial == null)
                return (false, "Matéria-prima não encontrada", null);

            if (rawMaterial.ControlType == "COMUM")
                return (false, "Matéria-prima não é controlada", null);

            // Calcular saldo atual
            var currentBalance = await GetCurrentBalanceAsync(
                establishmentId,
                dto.RawMaterialId);

            // Calcular novo saldo
            decimal newBalance = dto.MovementType.ToUpper() switch
            {
                "ENTRADA" => currentBalance + dto.Quantity,
                "SAIDA" => currentBalance - dto.Quantity,
                "PERDA" => currentBalance - dto.Quantity,
                "AJUSTE" => dto.Quantity, // O valor é o saldo final
                _ => currentBalance
            };

            if (newBalance < 0 && dto.MovementType != "AJUSTE")
                return (false, "Saldo insuficiente", null);

            // Criar movimentação
            var movement = new ControlledSubstanceMovement
            {
                EstablishmentId = establishmentId,
                RawMaterialId = dto.RawMaterialId,
                BatchId = dto.BatchId,
                MovementDate = dto.MovementDate,
                MovementType = dto.MovementType.ToUpper(),
                ControlledList = rawMaterial.ControlType,
                SubstanceDcbCode = rawMaterial.DcbCode ?? "",
                SubstanceName = rawMaterial.Name,
                Quantity = dto.Quantity,
                Unit = rawMaterial.Unit,
                BalanceBefore = currentBalance,
                BalanceAfter = newBalance,
                SupplierId = dto.SupplierId,
                CustomerId = dto.CustomerId,
                PrescriptionId = dto.PrescriptionId,
                ManipulationOrderId = dto.ManipulationOrderId,
                SaleId = dto.SaleId,
                PurchaseOrderId = null,
                PrescriptionNumber = dto.PrescriptionNumber,
                PrescriptionType = dto.PrescriptionType,
                DoctorName = dto.DoctorName?.ToUpper(),
                DoctorCrm = dto.DoctorCrm,
                PatientName = dto.PatientName?.ToUpper(),
                PatientCpf = dto.PatientCpf?.Replace(".", "").Replace("-", ""),
                InvoiceNumber = dto.InvoiceNumber,
                InvoiceDate = dto.InvoiceDate,
                Observations = dto.Observations,
                Reason = dto.Reason,
                SngpcSent = false,
                SngpcStatus = "PENDENTE",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId
            };

            _context.Set<ControlledSubstanceMovement>().Add(movement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Movimentação registrada com sucesso", movement);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao registrar movimentação: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ControlledSubstanceBalance> Balances)> GenerateBalancesAsync(
        GenerateBalanceDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            var balances = new List<ControlledSubstanceBalance>();

            // Buscar todas as matérias-primas controladas
            var query = _context.RawMaterials
                .Where(r => r.EstablishmentId == establishmentId &&
                           r.ControlType != "COMUM");

            if (dto.RawMaterialIds != null && dto.RawMaterialIds.Any())
                query = query.Where(r => dto.RawMaterialIds.Contains(r.Id));

            var controlledMaterials = await query.ToListAsync();

            foreach (var material in controlledMaterials)
            {
                // Calcular movimentações do período
                var movements = await _context.Set<ControlledSubstanceMovement>()
                    .Where(m => m.EstablishmentId == establishmentId &&
                               m.RawMaterialId == material.Id &&
                               m.MovementDate >= dto.StartDate &&
                               m.MovementDate <= dto.EndDate)
                    .ToListAsync();

                if (!movements.Any())
                    continue;

                var initialBalance = movements.First().BalanceBefore;
                var totalEntries = movements.Where(m => m.MovementType == "ENTRADA").Sum(m => m.Quantity);
                var totalExits = movements.Where(m => m.MovementType == "SAIDA").Sum(m => m.Quantity);
                var totalLosses = movements.Where(m => m.MovementType == "PERDA").Sum(m => m.Quantity);
                var totalAdjustments = movements.Where(m => m.MovementType == "AJUSTE").Sum(m => m.Quantity);
                var finalBalance = movements.Last().BalanceAfter;

                var balance = new ControlledSubstanceBalance
                {
                    EstablishmentId = establishmentId,
                    RawMaterialId = material.Id,
                    ReferenceDate = dto.EndDate,
                    BalanceType = dto.BalanceType.ToUpper(),
                    ControlledList = material.ControlType,
                    SubstanceDcbCode = material.DcbCode ?? "",
                    SubstanceName = material.Name,
                    InitialBalance = initialBalance,
                    TotalEntries = totalEntries,
                    TotalExits = totalExits,
                    TotalLosses = totalLosses,
                    TotalAdjustments = totalAdjustments,
                    FinalBalance = finalBalance,
                    Unit = material.Unit,
                    Status = "ABERTO",
                    CreatedAt = DateTime.UtcNow,
                    CreatedByEmployeeId = employeeId
                };

                _context.Set<ControlledSubstanceBalance>().Add(balance);
                balances.Add(balance);
            }

            await _context.SaveChangesAsync();
            return (true, $"{balances.Count} balanço(s) gerado(s) com sucesso", balances);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao gerar balanços: {ex.Message}", new List<ControlledSubstanceBalance>());
        }
    }

    public async Task<(bool Success, string Message)> CloseBalanceAsync(
        Guid balanceId,
        CloseBalanceDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var balance = await _context.Set<ControlledSubstanceBalance>()
            .FirstOrDefaultAsync(b => b.Id == balanceId &&
                                     b.EstablishmentId == establishmentId);

        if (balance == null)
            return (false, "Balanço não encontrado");

        if (balance.Status == "FECHADO")
            return (false, "Balanço já está fechado");

        balance.PhysicalBalance = dto.PhysicalBalance;
        balance.Difference = dto.PhysicalBalance - balance.FinalBalance;
        balance.Status = "FECHADO";
        balance.ClosedAt = DateTime.UtcNow;
        balance.ClosedByEmployeeId = employeeId;

        if (!string.IsNullOrWhiteSpace(dto.Observations))
            balance.Observations = dto.Observations;

        await _context.SaveChangesAsync();
        return (true, "Balanço fechado com sucesso");
    }

    public async Task<(bool Success, string Message, SpecialPrescriptionControl? Control)> RegisterSpecialPrescriptionAsync(
        RegisterSpecialPrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            // Verificar se número já existe
            var exists = await _context.Set<SpecialPrescriptionControl>()
                .AnyAsync(s => s.EstablishmentId == establishmentId &&
                              s.PrescriptionNumber == dto.PrescriptionNumber &&
                              s.PrescriptionType == dto.PrescriptionType.ToUpper());

            if (exists)
                return (false, "Receita já cadastrada", null);

            // Calcular validade
            var validityDate = dto.PrescriptionType.ToUpper() switch
            {
                "AMARELA" => dto.IssueDate.AddDays(30),
                "AZUL" => dto.IssueDate.AddDays(30),
                "BRANCA_2_VIAS" => dto.IssueDate.AddDays(10),
                _ => dto.IssueDate.AddDays(30)
            };

            var control = new SpecialPrescriptionControl
            {
                EstablishmentId = establishmentId,
                PrescriptionId = dto.PrescriptionId,
                PrescriptionType = dto.PrescriptionType.ToUpper(),
                PrescriptionNumber = dto.PrescriptionNumber,
                PrescriptionSeries = dto.PrescriptionSeries,
                IssueDate = dto.IssueDate,
                ValidityDate = validityDate,
                DoctorName = dto.DoctorName.ToUpper(),
                DoctorCrm = dto.DoctorCrm,
                DoctorCrmState = dto.DoctorCrmState.ToUpper(),
                PatientName = dto.PatientName.ToUpper(),
                PatientDocument = dto.PatientDocument.Replace(".", "").Replace("-", ""),
                PatientAddress = dto.PatientAddress,
                PatientCity = dto.PatientCity,
                PatientState = dto.PatientState?.ToUpper(),
                Medication = dto.Medication,
                Quantity = dto.Quantity,
                Posology = dto.Posology,
                Status = "ATIVA",
                Retained = dto.Retained,
                RetentionReason = dto.RetentionReason,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<SpecialPrescriptionControl>().Add(control);
            await _context.SaveChangesAsync();

            return (true, "Receita especial registrada com sucesso", control);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao registrar receita: {ex.Message}", null);
        }
    }

    public async Task<string> GenerateSngpcXmlAsync(
        Guid establishmentId,
        DateTime startDate,
        DateTime endDate,
        string? controlledList = null)
    {
        var movements = await _context.Set<ControlledSubstanceMovement>()
            .Where(m => m.EstablishmentId == establishmentId &&
                       m.MovementDate >= startDate &&
                       m.MovementDate <= endDate &&
                       !m.SngpcSent)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(controlledList))
            movements = movements.Where(m => m.ControlledList == controlledList).ToList();

        // TODO: Implementar geração XML conforme padrão ANVISA
        // Por enquanto, retornar XML básico
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SNGPC>
    <Periodo>
        <DataInicio>{startDate:yyyy-MM-dd}</DataInicio>
        <DataFim>{endDate:yyyy-MM-dd}</DataFim>
    </Periodo>
    <TotalMovimentacoes>{movements.Count}</TotalMovimentacoes>
    <Movimentacoes>
";

        foreach (var movement in movements)
        {
            xml += $@"
        <Movimentacao>
            <Data>{movement.MovementDate:yyyy-MM-dd}</Data>
            <Tipo>{movement.MovementType}</Tipo>
            <Substancia>{movement.SubstanceName}</Substancia>
            <CodigoDCB>{movement.SubstanceDcbCode}</CodigoDCB>
            <Lista>{movement.ControlledList}</Lista>
            <Quantidade>{movement.Quantity}</Quantidade>
            <Unidade>{movement.Unit}</Unidade>
            <SaldoAnterior>{movement.BalanceBefore}</SaldoAnterior>
            <SaldoPosterior>{movement.BalanceAfter}</SaldoPosterior>
";

            if (!string.IsNullOrWhiteSpace(movement.PatientName))
            {
                xml += $@"
            <Paciente>
                <Nome>{movement.PatientName}</Nome>
                <CPF>{movement.PatientCpf}</CPF>
            </Paciente>
            <Prescritor>
                <Nome>{movement.DoctorName}</Nome>
                <CRM>{movement.DoctorCrm}</CRM>
            </Prescritor>
            <Receita>
                <Numero>{movement.PrescriptionNumber}</Numero>
                <Tipo>{movement.PrescriptionType}</Tipo>
            </Receita>
";
            }

            xml += @"
        </Movimentacao>";
        }

        xml += @"
    </Movimentacoes>
</SNGPC>";

        return xml;
    }

    public async Task<SngpcReportDto> GetReportAsync(
        Guid establishmentId,
        DateTime startDate,
        DateTime endDate)
    {
        var movements = await _context.Set<ControlledSubstanceMovement>()
            .Where(m => m.EstablishmentId == establishmentId &&
                       m.MovementDate >= startDate &&
                       m.MovementDate <= endDate)
            .ToListAsync();

        var pendingMovements = movements.Where(m => !m.SngpcSent).ToList();

        var openBalances = await _context.Set<ControlledSubstanceBalance>()
            .Where(b => b.EstablishmentId == establishmentId &&
                       b.Status == "ABERTO")
            .ToListAsync();

        var report = new SngpcReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalMovements = movements.Count,
            MovementsByList = movements.GroupBy(m => m.ControlledList)
                .ToDictionary(g => g.Key, g => g.Count()),
            MovementsByType = movements.GroupBy(m => m.MovementType)
                .ToDictionary(g => g.Key, g => g.Count()),
            PendingMovements = pendingMovements.Select(m => new ControlledMovementResponseDto
            {
                Id = m.Id,
                MovementDate = m.MovementDate,
                MovementType = m.MovementType,
                ControlledList = m.ControlledList,
                SubstanceName = m.SubstanceName,
                SubstanceDcbCode = m.SubstanceDcbCode,
                Quantity = m.Quantity,
                Unit = m.Unit,
                BalanceBefore = m.BalanceBefore,
                BalanceAfter = m.BalanceAfter,
                PatientName = m.PatientName,
                DoctorName = m.DoctorName,
                PrescriptionNumber = m.PrescriptionNumber,
                SngpcSent = m.SngpcSent,
                SngpcSentAt = m.SngpcSentAt,
                SngpcStatus = m.SngpcStatus,
                CreatedAt = m.CreatedAt,
                CreatedByEmployeeName = ""
            }).ToList(),
            OpenBalances = openBalances.Select(b => new BalanceResponseDto
            {
                Id = b.Id,
                ReferenceDate = b.ReferenceDate,
                BalanceType = b.BalanceType,
                ControlledList = b.ControlledList,
                SubstanceName = b.SubstanceName,
                InitialBalance = b.InitialBalance,
                TotalEntries = b.TotalEntries,
                TotalExits = b.TotalExits,
                TotalLosses = b.TotalLosses,
                FinalBalance = b.FinalBalance,
                PhysicalBalance = b.PhysicalBalance,
                Difference = b.Difference,
                Unit = b.Unit,
                Status = b.Status,
                SngpcSent = b.SngpcSent
            }).ToList()
        };

        return report;
    }

    private async Task<decimal> GetCurrentBalanceAsync(Guid establishmentId, Guid rawMaterialId)
    {
        var lastMovement = await _context.Set<ControlledSubstanceMovement>()
            .Where(m => m.EstablishmentId == establishmentId &&
                       m.RawMaterialId == rawMaterialId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        return lastMovement?.BalanceAfter ?? 0;
    }
}