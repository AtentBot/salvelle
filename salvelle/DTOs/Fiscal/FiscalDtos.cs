namespace DTOs.Fiscal;

// ============================================================
// REQUEST DTOs
// ============================================================

/// <summary>
/// DTO para emissão de NF-e/NFC-e
/// </summary>
public class EmitirNFeDto
{
    public Guid SaleId { get; set; }
    public string InvoiceType { get; set; } = "NFCE"; // NFE, NFCE
    public int? Series { get; set; } // Se null, usa série padrão
    public string? NatureOperation { get; set; } // Natureza da operação
    public string? AdditionalInfo { get; set; } // Informações complementares
    public bool PrintDanfe { get; set; } = true;
}

/// <summary>
/// DTO para cancelamento de NF-e/NFC-e
/// </summary>
public class CancelarNFeDto
{
    public Guid FiscalInvoiceId { get; set; }
    public string Justification { get; set; } = ""; // Mínimo 15 caracteres
}

/// <summary>
/// DTO para inutilização de numeração
/// </summary>
public class InutilizarNumeracaoDto
{
    public string InvoiceType { get; set; } = "NFE";
    public int Series { get; set; }
    public int StartNumber { get; set; }
    public int EndNumber { get; set; }
    public string Justification { get; set; } = ""; // Mínimo 15 caracteres
}

/// <summary>
/// DTO para configuração fiscal
/// </summary>
public class FiscalConfigDto
{
    // Dados do Emitente (NOVO)
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }

    // Ambiente
    public string Environment { get; set; } = "HOMOLOGACAO";
    public string Uf { get; set; } = "SP";

    // Séries
    public int NfeSeries { get; set; } = 1;
    public int NfeLastNumber { get; set; } = 0;
    public int NfceSeries { get; set; } = 1;
    public int NfceLastNumber { get; set; } = 0;

    // CSC para NFC-e
    public string? CscId { get; set; }
    public string? CscToken { get; set; }

    // Regime Tributário
    public string TaxRegime { get; set; } = "SIMPLES_NACIONAL";

    // CFOP padrão
    public string DefaultCfopVenda { get; set; } = "5102";
    public string DefaultCfopManipulacao { get; set; } = "5101";
    public string DefaultNcmManipulacao { get; set; } = "30049099";

    // Provedor
    public string Provider { get; set; } = "INTERNO";
    public string? ProviderApiKey { get; set; }
    public string? ProviderApiSecret { get; set; }

    // Opções
    public bool PrintDanfeAuto { get; set; } = true;
    public bool ContingencyEnabled { get; set; } = true;
    public string DefaultNature { get; set; } = "VENDA DE MERCADORIA";
    public string? DefaultAdditionalInfo { get; set; }
}

/// <summary>
/// DTO para upload de certificado
/// </summary>
public class UploadCertificateDto
{
    public string CertificateBase64 { get; set; } = "";
    public string Password { get; set; } = "";
}

// ============================================================
// RESPONSE DTOs
// ============================================================

/// <summary>
/// Resultado da emissão de NF-e/NFC-e
/// </summary>
public class NFeResultDto
{
    public bool Success { get; set; }
    public Guid? FiscalInvoiceId { get; set; }
    public string? InvoiceKey { get; set; } // Chave de acesso (44 dígitos)
    public int InvoiceNumber { get; set; }
    public int Series { get; set; }
    public string? Protocol { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string? XmlPath { get; set; }
    public string? XmlUrl { get; set; }
    public string? DanfeUrl { get; set; }
    public string? QrCodeUrl { get; set; } // Para NFC-e
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool InContingency { get; set; }
}

/// <summary>
/// Resultado do cancelamento
/// </summary>
public class CancelamentoResultDto
{
    public bool Success { get; set; }
    public string? Protocol { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Resultado da inutilização
/// </summary>
public class InutilizacaoResultDto
{
    public bool Success { get; set; }
    public string? Protocol { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO para listagem de notas fiscais
/// </summary>
public class FiscalInvoiceListDto
{
    public Guid Id { get; set; }
    public int InvoiceNumber { get; set; }
    public int Series { get; set; }
    public string InvoiceType { get; set; } = "";
    public string? InvoiceKey { get; set; }
    public string? SaleCode { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssueDate { get; set; }
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public string StatusColor { get; set; } = "";
    public bool CanCancel { get; set; }
    public bool HasDanfe { get; set; }
    public bool HasXml { get; set; }

    // Formatadores
    public string TotalAmountFormatted => TotalAmount.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public string IssueDateFormatted => IssueDate.ToString("dd/MM/yyyy HH:mm");
    public string StatusBadge => Status switch
    {
        "AUTORIZADO" => "success",
        "PENDENTE" => "warning",
        "CANCELADO" => "secondary",
        "REJEITADO" => "danger",
        _ => "light"
    };
}

/// <summary>
/// DTO detalhado de nota fiscal
/// </summary>
public class FiscalInvoiceDetailDto
{
    public Guid Id { get; set; }
    public int InvoiceNumber { get; set; }
    public int Series { get; set; }
    public string InvoiceType { get; set; } = "";
    public string? InvoiceKey { get; set; }
    public string? Protocol { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string Status { get; set; } = "";

    // Dados da venda
    public Guid? SaleId { get; set; }
    public string? SaleCode { get; set; }
    public DateTime? SaleDate { get; set; }

    // Dados do cliente
    public string? CustomerName { get; set; }
    public string? CustomerCpf { get; set; }
    public string? CustomerCpfCnpj { get; set; }
    public string? CustomerAddress { get; set; }

    // Cancelamento
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }

    // Itens
    public List<FiscalInvoiceItemDto> Items { get; set; } = new();

    // Arquivos
    public string? XmlUrl { get; set; }
    public string? DanfeUrl { get; set; }
    public string? QrCodeData { get; set; }

    // Erros
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO de item da nota fiscal
/// </summary>
public class FiscalInvoiceItemDto
{
    public int ItemNumber { get; set; }
    public string Description { get; set; } = "";
    public string? Ncm { get; set; }
    public string Cfop { get; set; } = "";
    public string Unit { get; set; } = "UN";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }

    // Tributação
    public decimal IcmsValue { get; set; }
    public decimal PisValue { get; set; }
    public decimal CofinsValue { get; set; }
}

/// <summary>
/// Status da configuração fiscal
/// </summary>
public class FiscalConfigStatusDto
{
    public bool IsConfigured { get; set; }
    public bool HasValidCertificate { get; set; }
    public DateTime? CertificateExpiry { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Environment { get; set; } = "";
    public string Provider { get; set; } = "";
    public string TaxRegime { get; set; } = "";
    public int NfeLastNumber { get; set; }
    public int NfceLastNumber { get; set; }
    public bool ContingencyEnabled { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Estatísticas fiscais
/// </summary>
public class FiscalStatsDto
{
    public int TotalNFeEmitidas { get; set; }
    public int TotalNFCeEmitidas { get; set; }
    public int TotalCanceladas { get; set; }
    public int TotalPendentes { get; set; }
    public int TotalErros { get; set; }
    public decimal TotalFaturado { get; set; }

    // Por período - Hoje
    public int NFeHoje { get; set; }
    public int NFCeHoje { get; set; }
    public decimal FaturamentoHoje { get; set; }

    // Por período - Mês
    public int NFeMes { get; set; }
    public int NFCeMes { get; set; }
    public decimal FaturamentoMes { get; set; }

    // Pendências
    public int NotasPendentes { get; set; }
    public int NotasComErro { get; set; }

    // Formatadores
    public string FaturamentoHojeFormatado => FaturamentoHoje.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public string FaturamentoMesFormatado => FaturamentoMes.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}

/// <summary>
/// Item da fila de processamento
/// </summary>
public class FiscalQueueItemDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string SaleCode { get; set; } = "";
    public string InvoiceType { get; set; } = "";
    public string Status { get; set; } = "";
    public int Attempts { get; set; }
    public DateTime? LastAttempt { get; set; }
    public DateTime? NextAttempt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Log fiscal para exibição
/// </summary>
public class FiscalLogDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string? EventDescription { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public int? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================
// DADOS PARA GERAÇÃO DE XML
// ============================================================

/// <summary>
/// Dados do emitente para NF-e
/// </summary>
public class EmitenteNFeDto
{
    public string Cnpj { get; set; } = "";
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public string? InscricaoMunicipal { get; set; }
    public string Crt { get; set; } = "1"; // 1=Simples, 2=Simples Excesso, 3=Normal

    // Endereço
    public string Logradouro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = "";
    public string CodigoMunicipio { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string? Telefone { get; set; }
}

/// <summary>
/// Dados do destinatário para NF-e
/// </summary>
public class DestinatarioNFeDto
{
    public string? CpfCnpj { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? InscricaoEstadual { get; set; }
    public bool IsConsumidorFinal { get; set; } = true;

    // Endereço (opcional para NFC-e com valor baixo)
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? CodigoMunicipio { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public string? Telefone { get; set; }
}

/// <summary>
/// Dados de pagamento para NF-e
/// </summary>
public class PagamentoNFeDto
{
    public string FormaPagamento { get; set; } = "01"; // 01=Dinheiro, 02=Cheque, 03=Cartão Crédito, 04=Cartão Débito, 05=Crédito Loja, 15=Boleto, 17=PIX, 99=Outros
    public decimal Valor { get; set; }
    public string? BandeiraCartao { get; set; } // 01=Visa, 02=Master, etc
    public string? AutorizacaoCartao { get; set; }
    public string? CnpjOperadora { get; set; }
    public decimal? Troco { get; set; }
}