using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Fiscal;
using Models;
using Models.Fiscal;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Service;

/// <summary>
/// Interface para provedores de NF-e (Focus, Tecnospeed, ACBr, etc)
/// </summary>
public interface INFeProvider
{
    Task<NFeResultDto> EmitirAsync(string xml, FiscalConfig config);
    Task<CancelamentoResultDto> CancelarAsync(string chave, string protocolo, string justificativa, FiscalConfig config);
    Task<InutilizacaoResultDto> InutilizarAsync(int serie, int numeroInicial, int numeroFinal, string justificativa, FiscalConfig config);
    Task<string> ConsultarAsync(string chave, FiscalConfig config);
    Task<bool> TestarConexaoAsync(FiscalConfig config);
}

/// <summary>
/// Serviço principal para gestão fiscal
/// </summary>
public class FiscalService
{
    private readonly AppDbContext _context;
    private readonly INFeProvider? _provider;

    public FiscalService(AppDbContext context, INFeProvider? provider = null)
    {
        _context = context;
        _provider = provider ?? new InternalNFeProvider();
    }

    // ============================================================
    // CONFIGURAÇÃO
    // ============================================================

    public async Task<FiscalConfig?> GetConfigAsync(Guid establishmentId)
    {
        return await _context.Set<FiscalConfig>()
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && c.IsActive);
    }

    public async Task<FiscalConfigStatusDto> GetConfigStatusAsync(Guid establishmentId)
    {
        var config = await GetConfigAsync(establishmentId);
        var status = new FiscalConfigStatusDto
        {
            IsConfigured = config != null,
            Warnings = new List<string>()
        };

        if (config == null)
        {
            status.Warnings.Add("Configuração fiscal não encontrada");
            return status;
        }

        status.Environment = config.Environment;
        status.Provider = config.Provider;
        status.TaxRegime = config.TaxRegime;
        status.NfeLastNumber = config.NfeLastNumber;
        status.NfceLastNumber = config.NfceLastNumber;
        status.ContingencyEnabled = config.ContingencyEnabled;

        // Verificar certificado
        if (!string.IsNullOrEmpty(config.CertificatePath) && config.CertificateExpiry.HasValue)
        {
            status.HasValidCertificate = config.CertificateExpiry.Value > DateTime.UtcNow;
            status.CertificateExpiry = config.CertificateExpiry;
            status.DaysUntilExpiry = (int)(config.CertificateExpiry.Value - DateTime.UtcNow).TotalDays;

            if (status.DaysUntilExpiry <= 0)
                status.Warnings.Add("Certificado digital EXPIRADO!");
            else if (status.DaysUntilExpiry <= 30)
                status.Warnings.Add($"Certificado expira em {status.DaysUntilExpiry} dias");
        }
        else
        {
            status.HasValidCertificate = false;
            status.Warnings.Add("Certificado digital não configurado");
        }

        // Verificar CSC para NFC-e
        if (string.IsNullOrEmpty(config.CscId) || string.IsNullOrEmpty(config.CscToken))
            status.Warnings.Add("CSC não configurado (necessário para NFC-e)");

        // Verificar ambiente
        if (config.Environment == "HOMOLOGACAO")
            status.Warnings.Add("Sistema em ambiente de HOMOLOGAÇÃO");

        return status;
    }

    public async Task<(bool Success, string Message)> SaveConfigAsync(Guid establishmentId, FiscalConfigDto dto)
    {
        var config = await GetConfigAsync(establishmentId);
        
        if (config == null)
        {
            config = new FiscalConfig
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<FiscalConfig>().Add(config);
        }

        config.Environment = dto.Environment;
        config.Uf = dto.Uf;
        config.NfeSeries = dto.NfeSeries;
        config.NfceSeries = dto.NfceSeries;
        config.CscId = dto.CscId;
        config.CscToken = dto.CscToken;
        config.TaxRegime = dto.TaxRegime;
        config.DefaultCfopVenda = dto.DefaultCfopVenda;
        config.DefaultCfopManipulacao = dto.DefaultCfopManipulacao;
        config.DefaultNcmManipulacao = dto.DefaultNcmManipulacao;
        config.Provider = dto.Provider;
        config.ProviderApiKey = dto.ProviderApiKey;
        config.ProviderApiSecret = dto.ProviderApiSecret;
        config.PrintDanfeAuto = dto.PrintDanfeAuto;
        config.ContingencyEnabled = dto.ContingencyEnabled;
        config.DefaultNature = dto.DefaultNature;
        config.DefaultAdditionalInfo = dto.DefaultAdditionalInfo;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Configuração salva com sucesso");
    }

    public async Task<(bool Success, string Message)> UploadCertificateAsync(
        Guid establishmentId, 
        byte[] certificateData, 
        string password)
    {
        try
        {
            // Validar certificado
            var cert = new X509Certificate2(certificateData, password);
            
            if (cert.NotAfter < DateTime.UtcNow)
                return (false, "Certificado expirado");

            var config = await GetConfigAsync(establishmentId);
            if (config == null)
                return (false, "Configure os dados fiscais primeiro");

            // Salvar certificado (em produção, usar storage seguro)
            var certPath = Path.Combine("Storage", "Certificates", $"{establishmentId}.pfx");
            Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
            await File.WriteAllBytesAsync(certPath, certificateData);

            config.CertificatePath = certPath;
            config.CertificatePassword = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(password)); // Em produção, criptografar!
            config.CertificateExpiry = cert.NotAfter;
            config.CertificateSerial = cert.SerialNumber;
            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            return (true, $"Certificado válido até {cert.NotAfter:dd/MM/yyyy}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao processar certificado: {ex.Message}");
        }
    }

    // ============================================================
    // EMISSÃO
    // ============================================================

    public async Task<NFeResultDto> EmitirNFeAsync(Guid establishmentId, EmitirNFeDto dto, Guid userId)
    {
        var config = await GetConfigAsync(establishmentId);
        if (config == null)
            return new NFeResultDto { Success = false, ErrorMessage = "Configuração fiscal não encontrada" };

        // Buscar venda
        var sale = await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == dto.SaleId && s.EstablishmentId == establishmentId);

        if (sale == null)
            return new NFeResultDto { Success = false, ErrorMessage = "Venda não encontrada" };

        // Verificar se já existe nota para esta venda
        var existingInvoice = await _context.Set<FiscalInvoice>()
            .FirstOrDefaultAsync(f => f.SaleId == sale.Id && f.Status == "AUTORIZADO");

        if (existingInvoice != null)
            return new NFeResultDto 
            { 
                Success = true, 
                FiscalInvoiceId = existingInvoice.Id,
                InvoiceKey = existingInvoice.InvoiceKey,
                InvoiceNumber = existingInvoice.InvoiceNumber,
                ErrorMessage = "Nota já emitida para esta venda"
            };

        // Obter próximo número
        var series = dto.Series ?? (dto.InvoiceType == "NFE" ? config.NfeSeries : config.NfceSeries);
        var number = await GetNextNumberAsync(establishmentId, dto.InvoiceType, series);

        // Buscar dados do estabelecimento
        var establishment = await _context.Establishments.FindAsync(establishmentId);
        if (establishment == null)
            return new NFeResultDto { Success = false, ErrorMessage = "Estabelecimento não encontrado" };

        // Buscar dados do cliente
        Customer? customer = null;
        if (sale.CustomerId.HasValue)
            customer = await _context.Customers.FindAsync(sale.CustomerId.Value);

        try
        {
            // Criar registro da nota
            var invoice = new FiscalInvoice
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                SaleId = sale.Id,
                InvoiceNumber = number,
                Series = series,
                InvoiceType = dto.InvoiceType,
                TotalAmount = sale.TotalAmount,
                IssueDate = DateTime.UtcNow,
                Status = "PROCESSANDO",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<FiscalInvoice>().Add(invoice);

            // Gerar XML
            var xml = await GenerateXmlAsync(invoice, sale, establishment, customer, config, dto);

            // Log da tentativa
            await LogEventAsync(establishmentId, invoice.Id, "EMISSAO", 
                $"Tentativa de emissão {dto.InvoiceType} #{number}", xml, null, userId);

            // Emitir via provedor
            var result = await _provider!.EmitirAsync(xml, config);

            if (result.Success)
            {
                invoice.InvoiceKey = result.InvoiceKey;
                invoice.Protocol = result.Protocol;
                invoice.AuthorizationDate = result.AuthorizationDate ?? DateTime.UtcNow;
                invoice.Status = "AUTORIZADO";
                invoice.XmlPath = result.XmlPath;

                // Atualizar último número
                if (dto.InvoiceType == "NFE")
                    config.NfeLastNumber = number;
                else
                    config.NfceLastNumber = number;

                await LogEventAsync(establishmentId, invoice.Id, "EMISSAO", 
                    $"{dto.InvoiceType} #{number} autorizada", null, result.Protocol, userId);
            }
            else
            {
                invoice.Status = "REJEITADO";
                invoice.ErrorMessage = result.ErrorMessage;

                await LogEventAsync(establishmentId, invoice.Id, "ERRO", 
                    $"Rejeição: {result.ErrorMessage}", null, result.ErrorCode, userId);
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.FiscalInvoiceId = invoice.Id;
            result.InvoiceNumber = number;
            result.Series = series;

            return result;
        }
        catch (Exception ex)
        {
            await LogEventAsync(establishmentId, null, "ERRO", 
                $"Exceção: {ex.Message}", null, null, userId);

            // Se contingência habilitada, adicionar à fila
            if (config.ContingencyEnabled)
            {
                await AddToQueueAsync(establishmentId, sale.Id, dto.InvoiceType);
                return new NFeResultDto 
                { 
                    Success = false, 
                    InContingency = true,
                    ErrorMessage = "Erro na comunicação. Nota adicionada à fila de contingência."
                };
            }

            return new NFeResultDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CancelamentoResultDto> CancelarNFeAsync(
        Guid establishmentId, 
        CancelarNFeDto dto, 
        Guid userId)
    {
        if (dto.Justification.Length < 15)
            return new CancelamentoResultDto { Success = false, ErrorMessage = "Justificativa deve ter no mínimo 15 caracteres" };

        var config = await GetConfigAsync(establishmentId);
        if (config == null)
            return new CancelamentoResultDto { Success = false, ErrorMessage = "Configuração fiscal não encontrada" };

        var invoice = await _context.Set<FiscalInvoice>()
            .FirstOrDefaultAsync(f => f.Id == dto.FiscalInvoiceId && f.EstablishmentId == establishmentId);

        if (invoice == null)
            return new CancelamentoResultDto { Success = false, ErrorMessage = "Nota não encontrada" };

        if (invoice.Status != "AUTORIZADO")
            return new CancelamentoResultDto { Success = false, ErrorMessage = "Apenas notas autorizadas podem ser canceladas" };

        // Verificar prazo (24h para NFC-e, 24h ou 168h para NF-e dependendo da UF)
        var horasLimite = invoice.InvoiceType == "NFCE" ? 24 : 24;
        if ((DateTime.UtcNow - invoice.AuthorizationDate!.Value).TotalHours > horasLimite)
            return new CancelamentoResultDto { Success = false, ErrorMessage = $"Prazo de {horasLimite}h para cancelamento expirado" };

        try
        {
            await LogEventAsync(establishmentId, invoice.Id, "CANCELAMENTO", 
                $"Tentativa de cancelamento: {dto.Justification}", null, null, userId);

            var result = await _provider!.CancelarAsync(
                invoice.InvoiceKey!, 
                invoice.Protocol!, 
                dto.Justification, 
                config);

            if (result.Success)
            {
                invoice.Status = "CANCELADO";
                invoice.CancellationDate = DateTime.UtcNow;
                invoice.CancellationReason = dto.Justification;
                invoice.UpdatedAt = DateTime.UtcNow;

                await LogEventAsync(establishmentId, invoice.Id, "CANCELAMENTO", 
                    "Cancelamento autorizado", null, result.Protocol, userId);
            }
            else
            {
                await LogEventAsync(establishmentId, invoice.Id, "ERRO", 
                    $"Cancelamento rejeitado: {result.ErrorMessage}", null, result.ErrorCode, userId);
            }

            await _context.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            return new CancelamentoResultDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<InutilizacaoResultDto> InutilizarNumeracaoAsync(
        Guid establishmentId, 
        InutilizarNumeracaoDto dto, 
        Guid userId)
    {
        if (dto.Justification.Length < 15)
            return new InutilizacaoResultDto { Success = false, ErrorMessage = "Justificativa deve ter no mínimo 15 caracteres" };

        var config = await GetConfigAsync(establishmentId);
        if (config == null)
            return new InutilizacaoResultDto { Success = false, ErrorMessage = "Configuração fiscal não encontrada" };

        // Criar registro
        var gap = new FiscalNumberGap
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            InvoiceType = dto.InvoiceType,
            Series = dto.Series,
            StartNumber = dto.StartNumber,
            EndNumber = dto.EndNumber,
            Justification = dto.Justification,
            Status = "PROCESSANDO",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Set<FiscalNumberGap>().Add(gap);

        try
        {
            var result = await _provider!.InutilizarAsync(
                dto.Series, 
                dto.StartNumber, 
                dto.EndNumber, 
                dto.Justification, 
                config);

            gap.Status = result.Success ? "AUTORIZADO" : "REJEITADO";
            gap.Protocol = result.Protocol;
            gap.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            gap.Status = "ERRO";
            await _context.SaveChangesAsync();
            return new InutilizacaoResultDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ============================================================
    // CONSULTAS
    // ============================================================

    public async Task<List<FiscalInvoiceListDto>> GetInvoicesAsync(
        Guid establishmentId, 
        string? status = null, 
        string? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Set<FiscalInvoice>()
            .Include(f => f.Sale)
            .Where(f => f.EstablishmentId == establishmentId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(f => f.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(f => f.InvoiceType == type);

        if (startDate.HasValue)
            query = query.Where(f => f.IssueDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(f => f.IssueDate <= endDate.Value);

        var invoices = await query
            .OrderByDescending(f => f.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<FiscalInvoiceListDto>();
        
        foreach (var invoice in invoices)
        {
            string? customerName = null;
            if (invoice.Sale?.CustomerId != null)
            {
                customerName = await _context.Customers
                    .Where(c => c.Id == invoice.Sale.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefaultAsync();
            }

            result.Add(new FiscalInvoiceListDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Series = invoice.Series,
                InvoiceType = invoice.InvoiceType,
                InvoiceKey = invoice.InvoiceKey,
                SaleCode = invoice.Sale?.Code,
                CustomerName = customerName ?? "Consumidor",
                TotalAmount = invoice.TotalAmount,
                IssueDate = invoice.IssueDate,
                Status = invoice.Status,
                StatusDisplay = GetStatusDisplay(invoice.Status),
                StatusColor = GetStatusColor(invoice.Status),
                CanCancel = invoice.Status == "AUTORIZADO" && 
                           (DateTime.UtcNow - invoice.AuthorizationDate!.Value).TotalHours < 24,
                HasDanfe = !string.IsNullOrEmpty(invoice.PdfPath),
                HasXml = !string.IsNullOrEmpty(invoice.XmlPath)
            });
        }

        return result;
    }

    public async Task<FiscalInvoiceDetailDto?> GetInvoiceDetailAsync(Guid invoiceId, Guid establishmentId)
    {
        var invoice = await _context.Set<FiscalInvoice>()
            .Include(f => f.Sale)
            .FirstOrDefaultAsync(f => f.Id == invoiceId && f.EstablishmentId == establishmentId);

        if (invoice == null) return null;

        var items = await _context.Set<FiscalInvoiceItem>()
            .Where(i => i.FiscalInvoiceId == invoiceId)
            .OrderBy(i => i.ItemNumber)
            .ToListAsync();

        string? customerName = null, customerCpf = null, customerAddress = null;
        
        if (invoice.Sale?.CustomerId != null)
        {
            var customer = await _context.Customers.FindAsync(invoice.Sale.CustomerId);
            if (customer != null)
            {
                customerName = customer.FullName;
                customerCpf = customer.Cpf;
                customerAddress = $"{customer.Street}, {customer.Number} - {customer.Neighborhood}, {customer.City}/{customer.State}";
            }
        }

        return new FiscalInvoiceDetailDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Series = invoice.Series,
            InvoiceType = invoice.InvoiceType,
            InvoiceKey = invoice.InvoiceKey,
            Protocol = invoice.Protocol,
            TotalAmount = invoice.TotalAmount,
            IssueDate = invoice.IssueDate,
            AuthorizationDate = invoice.AuthorizationDate,
            Status = invoice.Status,
            SaleId = invoice.SaleId,
            SaleCode = invoice.Sale?.Code,
            SaleDate = invoice.Sale?.SaleDate,
            CustomerName = customerName,
            CustomerCpfCnpj = customerCpf,
            CustomerAddress = customerAddress,
            CancellationDate = invoice.CancellationDate,
            CancellationReason = invoice.CancellationReason,
            ErrorMessage = invoice.ErrorMessage,
            XmlUrl = !string.IsNullOrEmpty(invoice.XmlPath) ? $"/api/Fiscal/xml/{invoice.Id}" : null,
            DanfeUrl = !string.IsNullOrEmpty(invoice.PdfPath) ? $"/api/Fiscal/danfe/{invoice.Id}" : null,
            Items = items.Select(i => new FiscalInvoiceItemDto
            {
                ItemNumber = i.ItemNumber,
                Description = i.Description,
                Ncm = i.Ncm,
                Cfop = i.Cfop,
                Unit = i.Unit,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Discount = i.Discount,
                IcmsValue = i.IcmsValue,
                PisValue = i.PisValue,
                CofinsValue = i.CofinsValue
            }).ToList()
        };
    }

    public async Task<FiscalStatsDto> GetStatsAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var invoices = await _context.Set<FiscalInvoice>()
            .Where(f => f.EstablishmentId == establishmentId)
            .ToListAsync();

        return new FiscalStatsDto
        {
            TotalNFeEmitidas = invoices.Count(i => i.InvoiceType == "NFE" && i.Status == "AUTORIZADO"),
            TotalNFCeEmitidas = invoices.Count(i => i.InvoiceType == "NFCE" && i.Status == "AUTORIZADO"),
            TotalCanceladas = invoices.Count(i => i.Status == "CANCELADO"),
            TotalPendentes = invoices.Count(i => i.Status == "PENDENTE" || i.Status == "PROCESSANDO"),
            TotalErros = invoices.Count(i => i.Status == "REJEITADO"),
            TotalFaturado = invoices.Where(i => i.Status == "AUTORIZADO").Sum(i => i.TotalAmount),
            
            NFeHoje = invoices.Count(i => i.InvoiceType == "NFE" && i.IssueDate.Date == today && i.Status == "AUTORIZADO"),
            NFCeHoje = invoices.Count(i => i.InvoiceType == "NFCE" && i.IssueDate.Date == today && i.Status == "AUTORIZADO"),
            FaturamentoHoje = invoices.Where(i => i.IssueDate.Date == today && i.Status == "AUTORIZADO").Sum(i => i.TotalAmount),
            
            NFeMes = invoices.Count(i => i.InvoiceType == "NFE" && i.IssueDate >= monthStart && i.Status == "AUTORIZADO"),
            NFCeMes = invoices.Count(i => i.InvoiceType == "NFCE" && i.IssueDate >= monthStart && i.Status == "AUTORIZADO"),
            FaturamentoMes = invoices.Where(i => i.IssueDate >= monthStart && i.Status == "AUTORIZADO").Sum(i => i.TotalAmount)
        };
    }

    // ============================================================
    // FILA DE CONTINGÊNCIA
    // ============================================================

    public async Task AddToQueueAsync(Guid establishmentId, Guid saleId, string invoiceType)
    {
        var queueItem = new FiscalQueue
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            SaleId = saleId,
            InvoiceType = invoiceType,
            Status = "PENDENTE",
            NextAttempt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<FiscalQueue>().Add(queueItem);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FiscalQueueItemDto>> GetQueueAsync(Guid establishmentId)
    {
        return await _context.Set<FiscalQueue>()
            .Include(q => q.Sale)
            .Where(q => q.EstablishmentId == establishmentId && q.Status != "CONCLUIDO")
            .OrderBy(q => q.CreatedAt)
            .Select(q => new FiscalQueueItemDto
            {
                Id = q.Id,
                SaleId = q.SaleId,
                SaleCode = q.Sale != null ? q.Sale.Code : "",
                InvoiceType = q.InvoiceType,
                Status = q.Status,
                Attempts = q.Attempts,
                LastAttempt = q.LastAttempt,
                NextAttempt = q.NextAttempt,
                ErrorMessage = q.ErrorMessage,
                CreatedAt = q.CreatedAt
            })
            .ToListAsync();
    }

    public async Task ProcessQueueAsync(Guid establishmentId, Guid userId)
    {
        var pendingItems = await _context.Set<FiscalQueue>()
            .Where(q => q.EstablishmentId == establishmentId && 
                       q.Status == "PENDENTE" && 
                       q.NextAttempt <= DateTime.UtcNow &&
                       q.Attempts < q.MaxAttempts)
            .Take(10)
            .ToListAsync();

        foreach (var item in pendingItems)
        {
            item.Status = "PROCESSANDO";
            item.Attempts++;
            item.LastAttempt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = await EmitirNFeAsync(establishmentId, new EmitirNFeDto
            {
                SaleId = item.SaleId,
                InvoiceType = item.InvoiceType
            }, userId);

            if (result.Success)
            {
                item.Status = "CONCLUIDO";
                item.FiscalInvoiceId = result.FiscalInvoiceId;
            }
            else
            {
                item.Status = item.Attempts >= item.MaxAttempts ? "ERRO" : "PENDENTE";
                item.ErrorMessage = result.ErrorMessage;
                item.NextAttempt = DateTime.UtcNow.AddMinutes(5 * item.Attempts);
            }

            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<int> GetNextNumberAsync(Guid establishmentId, string invoiceType, int series)
    {
        var config = await GetConfigAsync(establishmentId);
        if (config == null) throw new Exception("Configuração não encontrada");

        var lastNumber = invoiceType == "NFE" ? config.NfeLastNumber : config.NfceLastNumber;
        return lastNumber + 1;
    }

    private async Task<string> GenerateXmlAsync(
        FiscalInvoice invoice, 
        Sale sale, 
        Establishment establishment, 
        Customer? customer,
        FiscalConfig config,
        EmitirNFeDto dto)
    {
        // Em produção, usar biblioteca adequada para gerar XML válido
        // Este é um exemplo simplificado
        
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("NFe",
                new XAttribute("xmlns", "http://www.portalfiscal.inf.br/nfe"),
                new XElement("infNFe",
                    new XAttribute("Id", $"NFe{GenerateAccessKey(invoice, establishment, config)}"),
                    new XElement("ide",
                        new XElement("cUF", GetUfCode(config.Uf)),
                        new XElement("natOp", dto.NatureOperation ?? config.DefaultNature),
                        new XElement("mod", invoice.InvoiceType == "NFE" ? "55" : "65"),
                        new XElement("serie", invoice.Series),
                        new XElement("nNF", invoice.InvoiceNumber),
                        new XElement("dhEmi", invoice.IssueDate.ToString("yyyy-MM-ddTHH:mm:sszzz")),
                        new XElement("tpNF", "1"), // 1=Saída
                        new XElement("idDest", "1"), // 1=Interna
                        new XElement("tpAmb", config.Environment == "PRODUCAO" ? "1" : "2"),
                        new XElement("tpEmis", "1"), // 1=Normal
                        new XElement("finNFe", "1") // 1=Normal
                    ),
                    new XElement("emit",
                        new XElement("CNPJ", establishment.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "")),
                        new XElement("xNome", establishment.RazaoSocial),
                        new XElement("xFant", establishment.NomeFantasia),
                        new XElement("IE", establishment.InscricaoEstadual)
                    ),
                    GenerateDestElement(customer, invoice.InvoiceType),
                    GenerateDetElements(sale, config),
                    new XElement("total",
                        new XElement("ICMSTot",
                            new XElement("vProd", sale.Subtotal),
                            new XElement("vDesc", sale.DiscountAmount),
                            new XElement("vNF", sale.TotalAmount)
                        )
                    ),
                    GeneratePagElements(sale)
                )
            )
        );

        return xml.ToString();
    }

    private XElement? GenerateDestElement(Customer? customer, string invoiceType)
    {
        if (customer == null && invoiceType == "NFCE")
            return null; // NFC-e permite consumidor não identificado

        if (customer == null)
            return new XElement("dest", new XElement("xNome", "CONSUMIDOR NAO IDENTIFICADO"));

        return new XElement("dest",
            !string.IsNullOrEmpty(customer.Cpf) ? 
                new XElement("CPF", customer.Cpf.Replace(".", "").Replace("-", "")) : null,
            new XElement("xNome", customer.FullName),
            new XElement("indIEDest", "9") // 9=Não contribuinte
        );
    }

    private XElement GenerateDetElements(Sale sale, FiscalConfig config)
    {
        var items = new XElement("det");
        int itemNum = 1;

        foreach (var item in sale.Items)
        {
            items.Add(new XElement("det",
                new XAttribute("nItem", itemNum++),
                new XElement("prod",
                    new XElement("xProd", item.Description),
                    new XElement("NCM", config.DefaultNcmManipulacao),
                    new XElement("CFOP", item.ManipulationOrderId.HasValue ? 
                        config.DefaultCfopManipulacao : config.DefaultCfopVenda),
                    new XElement("uCom", "UN"),
                    new XElement("qCom", item.Quantity),
                    new XElement("vUnCom", item.UnitPrice),
                    new XElement("vProd", item.TotalPrice)
                ),
                new XElement("imposto",
                    GenerateTaxElement(config.TaxRegime)
                )
            ));
        }

        return items;
    }

    private XElement GenerateTaxElement(string taxRegime)
    {
        if (taxRegime == "SIMPLES_NACIONAL")
        {
            return new XElement("ICMS",
                new XElement("ICMSSN102",
                    new XElement("orig", "0"),
                    new XElement("CSOSN", "102") // Sem permissão de crédito
                )
            );
        }

        return new XElement("ICMS",
            new XElement("ICMS00",
                new XElement("orig", "0"),
                new XElement("CST", "00"),
                new XElement("modBC", "3"),
                new XElement("vBC", "0.00"),
                new XElement("pICMS", "0.00"),
                new XElement("vICMS", "0.00")
            )
        );
    }

    private XElement GeneratePagElements(Sale sale)
    {
        var formaPag = sale.PaymentMethod switch
        {
            "DINHEIRO" => "01",
            "CARTAO_DEBITO" => "04",
            "CARTAO_CREDITO" => "03",
            "PIX" => "17",
            _ => "99"
        };

        return new XElement("pag",
            new XElement("detPag",
                new XElement("tPag", formaPag),
                new XElement("vPag", sale.TotalAmount)
            ),
            sale.ChangeAmount > 0 ? new XElement("vTroco", sale.ChangeAmount) : null
        );
    }

    private string GenerateAccessKey(FiscalInvoice invoice, Establishment establishment, FiscalConfig config)
    {
        // Chave de acesso com 44 dígitos
        var key = new StringBuilder();
        key.Append(GetUfCode(config.Uf)); // 2
        key.Append(invoice.IssueDate.ToString("yyMM")); // 4
        key.Append(establishment.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "").PadLeft(14, '0')); // 14
        key.Append(invoice.InvoiceType == "NFE" ? "55" : "65"); // 2
        key.Append(invoice.Series.ToString().PadLeft(3, '0')); // 3
        key.Append(invoice.InvoiceNumber.ToString().PadLeft(9, '0')); // 9
        key.Append("1"); // tpEmis: 1=Normal
        key.Append(System.Security.Cryptography.RandomNumberGenerator.GetInt32(10000000, 99999999).ToString()); // 8 cNF

        // Calcular dígito verificador (módulo 11)
        var dv = CalculateMod11(key.ToString());
        key.Append(dv);

        return key.ToString();
    }

    private int CalculateMod11(string chave)
    {
        int peso = 2;
        int soma = 0;

        for (int i = chave.Length - 1; i >= 0; i--)
        {
            soma += int.Parse(chave[i].ToString()) * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private string GetUfCode(string uf)
    {
        return uf.ToUpper() switch
        {
            "AC" => "12", "AL" => "27", "AP" => "16", "AM" => "13", "BA" => "29",
            "CE" => "23", "DF" => "53", "ES" => "32", "GO" => "52", "MA" => "21",
            "MT" => "51", "MS" => "50", "MG" => "31", "PA" => "15", "PB" => "25",
            "PR" => "41", "PE" => "26", "PI" => "22", "RJ" => "33", "RN" => "24",
            "RS" => "43", "RO" => "11", "RR" => "14", "SC" => "42", "SP" => "35",
            "SE" => "28", "TO" => "17", _ => "35"
        };
    }

    private string GetStatusDisplay(string status)
    {
        return status switch
        {
            "PENDENTE" => "Pendente",
            "PROCESSANDO" => "Processando",
            "AUTORIZADO" => "Autorizada",
            "CANCELADO" => "Cancelada",
            "REJEITADO" => "Rejeitada",
            _ => status
        };
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "PENDENTE" => "warning",
            "PROCESSANDO" => "info",
            "AUTORIZADO" => "success",
            "CANCELADO" => "secondary",
            "REJEITADO" => "danger",
            _ => "dark"
        };
    }

    private async Task LogEventAsync(
        Guid establishmentId, 
        Guid? invoiceId, 
        string eventType,
        string description,
        string? requestXml,
        string? statusCode,
        Guid? userId)
    {
        var log = new FiscalLog
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            FiscalInvoiceId = invoiceId,
            EventType = eventType,
            EventDescription = description,
            RequestXml = requestXml,
            StatusCode = statusCode,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<FiscalLog>().Add(log);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Provedor interno (simulado) para desenvolvimento/testes
/// </summary>
public class InternalNFeProvider : INFeProvider
{
    public Task<NFeResultDto> EmitirAsync(string xml, FiscalConfig config)
    {
        // Simulação para ambiente de homologação/desenvolvimento
        var success = config.Environment == "HOMOLOGACAO" ||
                      System.Security.Cryptography.RandomNumberGenerator.GetInt32(100) > 5;

        if (success)
        {
            return Task.FromResult(new NFeResultDto
            {
                Success = true,
                InvoiceKey = Guid.NewGuid().ToString("N").Substring(0, 44).ToUpper(),
                Protocol = $"135{DateTime.Now:yyyyMMddHHmmss}{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999)}",
                AuthorizationDate = DateTime.UtcNow,
                XmlPath = $"/storage/xml/{DateTime.Now:yyyyMM}/{Guid.NewGuid()}.xml"
            });
        }

        return Task.FromResult(new NFeResultDto
        {
            Success = false,
            ErrorCode = "999",
            ErrorMessage = "Erro simulado para testes"
        });
    }

    public Task<CancelamentoResultDto> CancelarAsync(string chave, string protocolo, string justificativa, FiscalConfig config)
    {
        return Task.FromResult(new CancelamentoResultDto
        {
            Success = true,
            Protocol = $"135{DateTime.Now:yyyyMMddHHmmss}",
            CancellationDate = DateTime.UtcNow
        });
    }

    public Task<InutilizacaoResultDto> InutilizarAsync(int serie, int numeroInicial, int numeroFinal, string justificativa, FiscalConfig config)
    {
        return Task.FromResult(new InutilizacaoResultDto
        {
            Success = true,
            Protocol = $"135{DateTime.Now:yyyyMMddHHmmss}",
            ProcessedAt = DateTime.UtcNow
        });
    }

    public Task<string> ConsultarAsync(string chave, FiscalConfig config)
    {
        return Task.FromResult("<nfeProc>Nota autorizada</nfeProc>");
    }

    public Task<bool> TestarConexaoAsync(FiscalConfig config)
    {
        return Task.FromResult(true);
    }
}
