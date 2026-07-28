// ============================================
// ViewModels/Fiscal/NFCeViewModel.cs
// ============================================

using Models;
using Models.Fiscal;

namespace ViewModels.Fiscal;

/// <summary>
/// ViewModel completo para renderização de NFC-e
/// </summary>
public class NFCeViewModel
{
    public Guid Id { get; set; }
    public string Numero { get; set; } = "";
    public string Serie { get; set; } = "001";
    public string ChaveAcesso { get; set; } = "";
    public string ChaveAcessoFormatada => FormatarChave(ChaveAcesso);
    public string? Protocolo { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Now;
    public DateTime? DataAutorizacao { get; set; }
    public string Status { get; set; } = "AUTORIZADO";
    public bool EmitidaEmContingencia { get; set; }
    public bool IsMock { get; set; }
    
    public NFCeEmitenteViewModel Emitente { get; set; } = new();
    public NFCeDestinatarioViewModel Destinatario { get; set; } = new();
    public List<NFCeItemViewModel> Itens { get; set; } = new();
    public NFCeTotaisViewModel Totais { get; set; } = new();
    public List<NFCePagamentoViewModel> Pagamentos { get; set; } = new();
    public NFCeTributosViewModel? Tributos { get; set; }
    
    public string? QRCodeBase64 { get; set; }
    public string? QRCodeUrl { get; set; }
    public string UrlConsulta { get; set; } = "www.nfce.fazenda.sp.gov.br";
    
    public string MensagemFiscal { get; set; } = "Documento emitido por ME ou EPP optante pelo Simples Nacional. Não gera direito a crédito fiscal de IPI.";
    public string? InformacoesAdicionais { get; set; }
    
    private static string FormatarChave(string chave)
    {
        if (string.IsNullOrEmpty(chave)) return "";
        chave = chave.Replace(" ", "");
        var grupos = new List<string>();
        for (int i = 0; i < chave.Length; i += 4)
        {
            grupos.Add(chave.Substring(i, Math.Min(4, chave.Length - i)));
        }
        return string.Join(" ", grupos);
    }
}

public class NFCeEmitenteViewModel
{
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CNPJ { get; set; } = "";
    public string? IE { get; set; }
    public string? IM { get; set; }
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string UF { get; set; } = "";
    public string CEP { get; set; } = "";
    public string? Fone { get; set; }
}

public class NFCeDestinatarioViewModel
{
    public string? Nome { get; set; }
    public string? CPF { get; set; }
    public string? CNPJ { get; set; }
    public string? Endereco { get; set; }
}

public class NFCeItemViewModel
{
    public int Sequencia { get; set; }
    public string Codigo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string NCM { get; set; } = "";
    public string CFOP { get; set; } = "5102";
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorTotal { get; set; }
}

public class NFCeTotaisViewModel
{
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
    public decimal Troco { get; set; }
}

public class NFCePagamentoViewModel
{
    public string Tipo { get; set; } = "01";
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public string? Detalhes { get; set; }
}

public class NFCeTributosViewModel
{
    public decimal Federal { get; set; }
    public decimal Estadual { get; set; }
    public decimal Municipal { get; set; }
    public decimal Total { get; set; }
    public decimal Percentual { get; set; }
}

// ============================================
// FACTORY
// ============================================

public static class NFCeViewModelFactory
{
    /// <summary>
    /// Cria uma NFC-e MOCK para demonstração
    /// </summary>
    public static NFCeViewModel CreateMock(
        string? razaoSocial = null,
        string? nomeFantasia = null,
        string? cnpj = null)
    {
        var now = DateTime.Now;
        var random = new Random();
        var numero = random.Next(1, 9999).ToString("D6");
        
        return new NFCeViewModel
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            Serie = "001",
            ChaveAcesso = GerarChaveAcessoMock(now, numero),
            Protocolo = $"1{now:yy}{random.Next(100000000, 999999999)}",
            DataEmissao = now,
            DataAutorizacao = now.AddSeconds(3),
            Status = "AUTORIZADO",
            IsMock = true,
            
            Emitente = new NFCeEmitenteViewModel
            {
                RazaoSocial = razaoSocial ?? "Farmácia Teste LTDA",
                NomeFantasia = nomeFantasia ?? "Farmácia Salvelle",
                CNPJ = FormatCNPJ(cnpj ?? "12345678000190"),
                IE = "123.456.789.012",
                Endereco = "Rua Principal, 100",
                Bairro = "Centro",
                Cidade = "São Paulo",
                UF = "SP",
                CEP = "01000-000",
                Fone = "(11) 3333-4444"
            },
            
            Destinatario = new NFCeDestinatarioViewModel
            {
                Nome = "CONSUMIDOR NÃO IDENTIFICADO"
            },
            
            Itens = new List<NFCeItemViewModel>
            {
                new() {
                    Sequencia = 1,
                    Codigo = "M001",
                    Descricao = "MANIPULAÇÃO - Creme Hidratante com Ureia 10%",
                    NCM = "33049990",
                    CFOP = "5102",
                    Unidade = "UN",
                    Quantidade = 1,
                    ValorUnitario = 85.00m,
                    ValorTotal = 85.00m
                },
                new() {
                    Sequencia = 2,
                    Codigo = "M002",
                    Descricao = "MANIPULAÇÃO - Vitamina C 500mg - 60 cápsulas",
                    NCM = "30049099",
                    CFOP = "5102",
                    Unidade = "UN",
                    Quantidade = 1,
                    ValorUnitario = 45.00m,
                    ValorTotal = 45.00m
                },
                new() {
                    Sequencia = 3,
                    Codigo = "P001",
                    Descricao = "Álcool Gel 70% 500ml",
                    NCM = "38089490",
                    CFOP = "5102",
                    Unidade = "UN",
                    Quantidade = 2,
                    ValorUnitario = 12.50m,
                    ValorTotal = 25.00m
                }
            },
            
            Totais = new NFCeTotaisViewModel
            {
                Subtotal = 155.00m,
                Desconto = 5.00m,
                Total = 150.00m,
                Troco = 0
            },
            
            Pagamentos = new List<NFCePagamentoViewModel>
            {
                new() {
                    Tipo = "03",
                    Descricao = "Cartão de Crédito",
                    Valor = 150.00m,
                    Detalhes = "Bandeira: VISA | Aut: 123456 | 2x"
                }
            },
            
            Tributos = new NFCeTributosViewModel
            {
                Federal = 15.75m,
                Estadual = 27.00m,
                Municipal = 3.00m,
                Total = 45.75m,
                Percentual = 30.50m
            },
            
            UrlConsulta = "www.nfce.fazenda.sp.gov.br",
            MensagemFiscal = "Documento emitido por ME ou EPP optante pelo Simples Nacional. Não gera direito a crédito fiscal de IPI."
        };
    }
    
    /// <summary>
    /// Cria NFC-e a partir de uma venda real
    /// Propriedades do FiscalInvoice:
    /// - InvoiceNumber (int)
    /// - Series (int)
    /// - InvoiceKey (string) - NÃO AccessKey
    /// - Protocol (string) - NÃO ProtocolNumber
    /// </summary>
    public static NFCeViewModel FromSale(
        Sale sale,
        Establishment establishment,
        Customer? customer = null,
        FiscalInvoice? fiscalDoc = null)
    {
        var vm = new NFCeViewModel
        {
            Id = fiscalDoc?.Id ?? Guid.NewGuid(),
            Numero = fiscalDoc?.InvoiceNumber.ToString("D6") ?? "000000",
            Serie = fiscalDoc?.Series.ToString("D3") ?? "001",
            ChaveAcesso = fiscalDoc?.InvoiceKey ?? "",
            Protocolo = fiscalDoc?.Protocol,
            DataEmissao = fiscalDoc?.IssueDate ?? sale.SaleDate,
            DataAutorizacao = fiscalDoc?.AuthorizationDate,
            Status = fiscalDoc?.Status ?? "PENDENTE",
            IsMock = fiscalDoc == null,
            
            Emitente = CreateEmitenteFromEstablishment(establishment),
            
            Destinatario = new NFCeDestinatarioViewModel
            {
                Nome = customer?.FullName ?? "CONSUMIDOR NÃO IDENTIFICADO",
                CPF = customer?.Cpf
            },
            
            Totais = new NFCeTotaisViewModel
            {
                Subtotal = sale.Subtotal,
                Desconto = sale.DiscountAmount,
                Total = sale.TotalAmount,
                Troco = sale.ChangeAmount ?? 0
            }
        };
        
        // Itens da venda - SaleItem usa ItemCode e DiscountAmount
        if (sale.Items != null)
        {
            int seq = 1;
            foreach (var item in sale.Items)
            {
                vm.Itens.Add(new NFCeItemViewModel
                {
                    Sequencia = seq++,
                    Codigo = $"I{seq:D3}",
                    Descricao = item.Description ?? "",
                    NCM = "00000000",
                    CFOP = "5102",
                    Unidade = "UN",
                    Quantidade = item.Quantity,
                    ValorUnitario = item.UnitPrice,
                    ValorDesconto = item.DiscountAmount,
                    ValorTotal = item.TotalPrice
                });
            }
        }
        
        // Pagamento da venda
        vm.Pagamentos.Add(new NFCePagamentoViewModel
        {
            Tipo = MapPaymentType(sale.PaymentMethod),
            Descricao = MapPaymentDescription(sale.PaymentMethod),
            Valor = sale.TotalAmount
        });
        
        vm.UrlConsulta = GetUrlConsultaByUF(establishment.State ?? "SP");
        
        return vm;
    }
    
    /// <summary>
    /// Cria NFC-e a partir de um documento fiscal existente (sem Sale)
    /// FiscalInvoice NÃO tem: CustomerName, CustomerDocument, DiscountAmount, PaymentMethod, QrCodeUrl
    /// FiscalInvoiceItem usa: ItemNumber (int), Discount (não DiscountAmount)
    /// </summary>
    public static NFCeViewModel FromFiscalInvoice(
        FiscalInvoice fiscalDoc,
        Establishment establishment)
    {
        var vm = new NFCeViewModel
        {
            Id = fiscalDoc.Id,
            Numero = fiscalDoc.InvoiceNumber.ToString("D6"),
            Serie = fiscalDoc.Series.ToString("D3"),
            ChaveAcesso = fiscalDoc.InvoiceKey ?? "",
            Protocolo = fiscalDoc.Protocol,
            DataEmissao = fiscalDoc.IssueDate,
            DataAutorizacao = fiscalDoc.AuthorizationDate,
            Status = fiscalDoc.Status,
            IsMock = false,
            
            Emitente = CreateEmitenteFromEstablishment(establishment),
            
            // FiscalInvoice não tem CustomerName/CustomerDocument
            Destinatario = new NFCeDestinatarioViewModel
            {
                Nome = "CONSUMIDOR"
            },
            
            // FiscalInvoice não tem DiscountAmount
            Totais = new NFCeTotaisViewModel
            {
                Subtotal = fiscalDoc.TotalAmount,
                Desconto = 0,
                Total = fiscalDoc.TotalAmount
            }
        };
        
        // Itens do documento fiscal
        // FiscalInvoiceItem usa: ItemNumber (int), Discount (não DiscountAmount), não tem ProductCode
        if (fiscalDoc.Items != null)
        {
            foreach (var item in fiscalDoc.Items)
            {
                vm.Itens.Add(new NFCeItemViewModel
                {
                    Sequencia = item.ItemNumber,
                    Codigo = item.ItemNumber.ToString("D3"),
                    Descricao = item.Description,
                    NCM = item.Ncm ?? "00000000",
                    CFOP = item.Cfop,
                    Unidade = item.Unit,
                    Quantidade = item.Quantity,
                    ValorUnitario = item.UnitPrice,
                    ValorDesconto = item.Discount, // FiscalInvoiceItem usa Discount
                    ValorTotal = item.TotalPrice
                });
            }
        }
        
        // FiscalInvoice não tem PaymentMethod
        vm.Pagamentos.Add(new NFCePagamentoViewModel
        {
            Tipo = "99",
            Descricao = "Outros",
            Valor = fiscalDoc.TotalAmount
        });
        
        vm.UrlConsulta = GetUrlConsultaByUF(establishment.State ?? "SP");
        
        return vm;
    }
    
    private static NFCeEmitenteViewModel CreateEmitenteFromEstablishment(Establishment establishment)
    {
        return new NFCeEmitenteViewModel
        {
            RazaoSocial = establishment.RazaoSocial ?? "",
            NomeFantasia = establishment.NomeFantasia ?? "",
            CNPJ = FormatCNPJ(establishment.Cnpj ?? ""),
            IE = establishment.InscricaoEstadual,
            Endereco = BuildEndereco(establishment.Street, establishment.Number),
            Bairro = establishment.Neighborhood ?? "",
            Cidade = establishment.City ?? "",
            UF = establishment.State ?? "",
            CEP = FormatCEP(establishment.PostalCode ?? ""),
            Fone = establishment.Phone
        };
    }
    
    private static string BuildEndereco(string? street, string? number)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(number)) parts.Add(number);
        return string.Join(", ", parts);
    }
    
    private static string GerarChaveAcessoMock(DateTime data, string numero)
    {
        var uf = "35";
        var aamm = data.ToString("yyMM");
        var cnpj = "12345678000190";
        var mod = "65";
        var serie = "001";
        var nNF = numero.PadLeft(9, '0');
        var tpEmis = "1";
        var cNF = new Random().Next(10000000, 99999999).ToString();
        
        var chave = $"{uf}{aamm}{cnpj}{mod}{serie}{nNF}{tpEmis}{cNF}";
        var dv = CalcularDV(chave);
        
        return chave + dv;
    }
    
    private static string CalcularDV(string chave)
    {
        int soma = 0;
        int peso = 2;
        for (int i = chave.Length - 1; i >= 0; i--)
        {
            soma += int.Parse(chave[i].ToString()) * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        int resto = soma % 11;
        int dv = resto < 2 ? 0 : 11 - resto;
        return dv.ToString();
    }
    
    private static string FormatCNPJ(string cnpj)
    {
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        if (cnpj.Length != 14) return cnpj;
        return $"{cnpj.Substring(0,2)}.{cnpj.Substring(2,3)}.{cnpj.Substring(5,3)}/{cnpj.Substring(8,4)}-{cnpj.Substring(12,2)}";
    }
    
    private static string FormatCEP(string cep)
    {
        cep = new string(cep.Where(char.IsDigit).ToArray());
        if (cep.Length != 8) return cep;
        return $"{cep.Substring(0,5)}-{cep.Substring(5,3)}";
    }
    
    private static string MapPaymentType(string? method)
    {
        return method?.ToUpper() switch
        {
            "DINHEIRO" => "01",
            "CHEQUE" => "02",
            "CARTAO_CREDITO" => "03",
            "CARTAO_DEBITO" => "04",
            "CREDITO_LOJA" => "05",
            "PIX" => "17",
            "TRANSFERENCIA" => "18",
            _ => "99"
        };
    }
    
    private static string MapPaymentDescription(string? method)
    {
        return method?.ToUpper() switch
        {
            "DINHEIRO" => "Dinheiro",
            "CARTAO_CREDITO" => "Cartão de Crédito",
            "CARTAO_DEBITO" => "Cartão de Débito",
            "PIX" => "PIX",
            "TRANSFERENCIA" => "Transferência Bancária",
            _ => method ?? "Outros"
        };
    }
    
    private static string GetUrlConsultaByUF(string uf)
    {
        return uf.ToUpper() switch
        {
            "SP" => "www.nfce.fazenda.sp.gov.br",
            "RJ" => "www.fazenda.rj.gov.br/nfce/consulta",
            "MG" => "nfce.fazenda.mg.gov.br/portalnfce",
            "RS" => "www.sefaz.rs.gov.br/NFE/NFE-NFC.aspx",
            "PR" => "www.fazenda.pr.gov.br/nfce",
            "SC" => "www.sef.sc.gov.br/nfce/consulta",
            "BA" => "nfe.sefaz.ba.gov.br/servicos/nfce/default.aspx",
            _ => "www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx"
        };
    }
}
