using System.Text;

namespace Services.Integration;

/// <summary>
/// Serviço de impressão térmica para cupons e etiquetas
/// Suporta: ESC/POS (Epson), ZPL (Zebra), PPLA (Argox)
/// </summary>
public class ThermalPrinterService
{
    private readonly ILogger<ThermalPrinterService> _logger;
    private readonly ThermalPrinterSettings _settings;

    public ThermalPrinterService(
        ILogger<ThermalPrinterService> logger,
        ThermalPrinterSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    #region Impressão de Cupom (80mm)

    /// <summary>
    /// Imprime cupom não-fiscal
    /// </summary>
    public async Task<(bool Success, string Message)> PrintCupomAsync(CupomPrintData cupom)
    {
        try
        {
            var commands = BuildEscPosCupom(cupom);
            return await SendToPrinterAsync(commands, _settings.CupomPrinterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir cupom");
            return (false, ex.Message);
        }
    }

    private byte[] BuildEscPosCupom(CupomPrintData cupom)
    {
        var ms = new MemoryStream();
        
        // ESC/POS Commands
        var ESC = (byte)0x1B;
        var GS = (byte)0x1D;
        var LF = (byte)0x0A;

        // Inicializar impressora
        ms.WriteByte(ESC); ms.WriteByte((byte)'@');

        // Centralizar
        ms.WriteByte(ESC); ms.WriteByte((byte)'a'); ms.WriteByte(1);

        // Negrito ON
        ms.WriteByte(ESC); ms.WriteByte((byte)'E'); ms.WriteByte(1);

        // Nome da farmácia (fonte dupla altura)
        ms.WriteByte(GS); ms.WriteByte((byte)'!'); ms.WriteByte(0x11);
        WriteText(ms, cupom.NomeFarmacia ?? "FARMÁCIA");
        ms.WriteByte(LF);

        // Voltar fonte normal
        ms.WriteByte(GS); ms.WriteByte((byte)'!'); ms.WriteByte(0);
        ms.WriteByte(ESC); ms.WriteByte((byte)'E'); ms.WriteByte(0);

        // CNPJ
        if (!string.IsNullOrEmpty(cupom.CNPJ))
        {
            WriteText(ms, $"CNPJ: {cupom.CNPJ}");
            ms.WriteByte(LF);
        }

        // Endereço
        if (!string.IsNullOrEmpty(cupom.Endereco))
        {
            WriteText(ms, cupom.Endereco);
            ms.WriteByte(LF);
        }

        // Linha separadora
        ms.WriteByte(LF);
        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);

        // Título do cupom
        WriteText(ms, "CUPOM NAO FISCAL");
        ms.WriteByte(LF);
        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);

        // Alinhar à esquerda
        ms.WriteByte(ESC); ms.WriteByte((byte)'a'); ms.WriteByte(0);

        // Número e data
        WriteText(ms, $"No: {cupom.Numero}");
        ms.WriteByte(LF);
        WriteText(ms, $"Data: {cupom.Data:dd/MM/yyyy HH:mm}");
        ms.WriteByte(LF);

        // Cliente
        if (!string.IsNullOrEmpty(cupom.Cliente))
        {
            WriteText(ms, $"Cliente: {cupom.Cliente}");
            ms.WriteByte(LF);
        }

        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);

        // Cabeçalho itens
        WriteText(ms, "ITEM                    QTD   UNIT    TOTAL");
        ms.WriteByte(LF);
        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);

        // Itens
        foreach (var item in cupom.Itens)
        {
            // Descrição (truncar se necessário)
            var desc = item.Descricao.Length > 20 
                ? item.Descricao[..20] 
                : item.Descricao.PadRight(20);
            
            var qtd = item.Quantidade.ToString("N2").PadLeft(5);
            var unit = item.ValorUnitario.ToString("N2").PadLeft(7);
            var total = item.ValorTotal.ToString("N2").PadLeft(8);

            WriteText(ms, $"{desc} {qtd} {unit} {total}");
            ms.WriteByte(LF);
        }

        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);

        // Totais - alinhar à direita
        ms.WriteByte(ESC); ms.WriteByte((byte)'a'); ms.WriteByte(2);

        if (cupom.Subtotal > 0)
        {
            WriteText(ms, $"SUBTOTAL: R$ {cupom.Subtotal:N2}");
            ms.WriteByte(LF);
        }

        if (cupom.Desconto > 0)
        {
            WriteText(ms, $"DESCONTO: R$ {cupom.Desconto:N2}");
            ms.WriteByte(LF);
        }

        // Total em negrito e maior
        ms.WriteByte(ESC); ms.WriteByte((byte)'E'); ms.WriteByte(1);
        ms.WriteByte(GS); ms.WriteByte((byte)'!'); ms.WriteByte(0x11);
        WriteText(ms, $"TOTAL: R$ {cupom.Total:N2}");
        ms.WriteByte(LF);
        ms.WriteByte(GS); ms.WriteByte((byte)'!'); ms.WriteByte(0);
        ms.WriteByte(ESC); ms.WriteByte((byte)'E'); ms.WriteByte(0);

        // Formas de pagamento
        if (cupom.Pagamentos?.Any() == true)
        {
            ms.WriteByte(LF);
            ms.WriteByte(ESC); ms.WriteByte((byte)'a'); ms.WriteByte(0);
            WriteText(ms, new string('-', 48));
            ms.WriteByte(LF);
            WriteText(ms, "PAGAMENTO:");
            ms.WriteByte(LF);

            foreach (var pag in cupom.Pagamentos)
            {
                WriteText(ms, $"  {pag.Forma}: R$ {pag.Valor:N2}");
                ms.WriteByte(LF);
            }

            if (cupom.Troco > 0)
            {
                WriteText(ms, $"  TROCO: R$ {cupom.Troco:N2}");
                ms.WriteByte(LF);
            }
        }

        // Rodapé centralizado
        ms.WriteByte(LF);
        ms.WriteByte(ESC); ms.WriteByte((byte)'a'); ms.WriteByte(1);
        WriteText(ms, new string('-', 48));
        ms.WriteByte(LF);
        WriteText(ms, "DOCUMENTO SEM VALOR FISCAL");
        ms.WriteByte(LF);
        WriteText(ms, "Obrigado pela preferencia!");
        ms.WriteByte(LF);

        // QR Code (se habilitado)
        if (!string.IsNullOrEmpty(cupom.QRCodeData))
        {
            ms.WriteByte(LF);
            BuildQRCode(ms, cupom.QRCodeData);
        }

        // Cortar papel
        ms.WriteByte(LF);
        ms.WriteByte(LF);
        ms.WriteByte(LF);
        ms.WriteByte(GS); ms.WriteByte((byte)'V'); ms.WriteByte(66); ms.WriteByte(0);

        return ms.ToArray();
    }

    #endregion

    #region Impressão de Etiqueta (ZPL/Zebra)

    /// <summary>
    /// Imprime etiqueta de produto manipulado
    /// </summary>
    public async Task<(bool Success, string Message)> PrintEtiquetaAsync(EtiquetaPrintData etiqueta)
    {
        try
        {
            var zpl = BuildZplEtiqueta(etiqueta);
            var bytes = Encoding.UTF8.GetBytes(zpl);
            return await SendToPrinterAsync(bytes, _settings.EtiquetaPrinterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir etiqueta");
            return (false, ex.Message);
        }
    }

    private string BuildZplEtiqueta(EtiquetaPrintData etiqueta)
    {
        var sb = new StringBuilder();

        // Início do label
        sb.AppendLine("^XA");

        // Configurações
        sb.AppendLine("^PW400");  // Largura 400 dots (50mm)
        sb.AppendLine("^LL320"); // Altura 320 dots (40mm)
        sb.AppendLine("^LH0,0"); // Home position

        // Nome da farmácia (topo)
        sb.AppendLine("^FO10,10^A0N,20,20^FD" + (etiqueta.NomeFarmacia ?? "FARMACIA") + "^FS");

        // Nome do produto (maior)
        sb.AppendLine("^FO10,40^A0N,28,28^FD" + TruncateZpl(etiqueta.NomeProduto, 25) + "^FS");

        // Composição
        if (!string.IsNullOrEmpty(etiqueta.Composicao))
        {
            var linhas = WrapText(etiqueta.Composicao, 40);
            int y = 75;
            foreach (var linha in linhas.Take(3))
            {
                sb.AppendLine($"^FO10,{y}^A0N,16,16^FD{TruncateZpl(linha, 40)}^FS");
                y += 18;
            }
        }

        // Posologia
        if (!string.IsNullOrEmpty(etiqueta.Posologia))
        {
            sb.AppendLine("^FO10,140^A0N,18,18^FDUso: " + TruncateZpl(etiqueta.Posologia, 35) + "^FS");
        }

        // Via de administração
        if (!string.IsNullOrEmpty(etiqueta.ViaAdministracao))
        {
            sb.AppendLine("^FO10,165^A0N,16,16^FDVia: " + etiqueta.ViaAdministracao + "^FS");
        }

        // Quantidade
        sb.AppendLine("^FO10,190^A0N,18,18^FDQtd: " + etiqueta.Quantidade + " " + etiqueta.Unidade + "^FS");

        // Paciente
        if (!string.IsNullOrEmpty(etiqueta.NomePaciente))
        {
            sb.AppendLine("^FO10,215^A0N,18,18^FDPaciente: " + TruncateZpl(etiqueta.NomePaciente, 25) + "^FS");
        }

        // Linha separadora
        sb.AppendLine("^FO10,240^GB380,1,1^FS");

        // Lote e Validade
        sb.AppendLine("^FO10,250^A0N,18,18^FDLote: " + etiqueta.Lote + "^FS");
        sb.AppendLine("^FO200,250^A0N,18,18^FDVal: " + etiqueta.Validade?.ToString("dd/MM/yyyy") + "^FS");

        // Data de manipulação
        sb.AppendLine("^FO10,275^A0N,16,16^FDManip: " + etiqueta.DataManipulacao?.ToString("dd/MM/yyyy") + "^FS");

        // Farmacêutico responsável
        if (!string.IsNullOrEmpty(etiqueta.FarmaceuticoRT))
        {
            sb.AppendLine("^FO10,295^A0N,14,14^FDRT: " + TruncateZpl(etiqueta.FarmaceuticoRT, 30) + "^FS");
        }

        // Código de barras (se houver)
        if (!string.IsNullOrEmpty(etiqueta.CodigoBarras))
        {
            sb.AppendLine("^FO280,250^BCN,40,N,N,N^FD" + etiqueta.CodigoBarras + "^FS");
        }

        // Fim do label
        sb.AppendLine("^XZ");

        return sb.ToString();
    }

    /// <summary>
    /// Imprime etiqueta de matéria-prima/lote
    /// </summary>
    public async Task<(bool Success, string Message)> PrintEtiquetaLoteAsync(EtiquetaLotePrintData etiqueta)
    {
        try
        {
            var zpl = BuildZplEtiquetaLote(etiqueta);
            var bytes = Encoding.UTF8.GetBytes(zpl);
            return await SendToPrinterAsync(bytes, _settings.EtiquetaPrinterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir etiqueta de lote");
            return (false, ex.Message);
        }
    }

    private string BuildZplEtiquetaLote(EtiquetaLotePrintData etiqueta)
    {
        var sb = new StringBuilder();

        sb.AppendLine("^XA");
        sb.AppendLine("^PW400");
        sb.AppendLine("^LL200");

        // Nome da matéria-prima
        sb.AppendLine("^FO10,10^A0N,24,24^FD" + TruncateZpl(etiqueta.NomeMateriaPrima, 30) + "^FS");

        // DCB
        if (!string.IsNullOrEmpty(etiqueta.CodigoDCB))
        {
            sb.AppendLine("^FO10,40^A0N,16,16^FDDCB: " + etiqueta.CodigoDCB + "^FS");
        }

        // Lote
        sb.AppendLine("^FO10,65^A0N,20,20^FDLote: " + etiqueta.NumeroLote + "^FS");

        // Validade (destaque se próximo)
        var diasParaVencer = (etiqueta.Validade - DateTime.Today).Days;
        sb.AppendLine("^FO10,95^A0N,20,20^FDValidade: " + etiqueta.Validade.ToString("dd/MM/yyyy") + "^FS");

        // Fornecedor
        if (!string.IsNullOrEmpty(etiqueta.Fornecedor))
        {
            sb.AppendLine("^FO10,125^A0N,16,16^FDForn: " + TruncateZpl(etiqueta.Fornecedor, 30) + "^FS");
        }

        // Quantidade
        sb.AppendLine("^FO10,150^A0N,18,18^FDQtd: " + etiqueta.Quantidade.ToString("N2") + " " + etiqueta.Unidade + "^FS");

        // Código de barras
        if (!string.IsNullOrEmpty(etiqueta.CodigoBarras))
        {
            sb.AppendLine("^FO250,90^BCN,50,N,N,N^FD" + etiqueta.CodigoBarras + "^FS");
        }

        // Status de quarentena
        if (etiqueta.EmQuarentena)
        {
            sb.AppendLine("^FO280,10^A0N,24,24^FR^FDQUARENTENA^FS");
        }

        sb.AppendLine("^XZ");

        return sb.ToString();
    }

    #endregion

    #region Helpers

    private async Task<(bool Success, string Message)> SendToPrinterAsync(byte[] data, string? printerName)
    {
        try
        {
            if (string.IsNullOrEmpty(printerName))
                printerName = _settings.CupomPrinterName ?? "DefaultPrinter";

            // Modo mock para desenvolvimento
            // Em produção, use biblioteca como ESC/POS.NET ou envie para porta LPT/USB
            _logger.LogInformation("Impressão enviada para {Printer} ({Bytes} bytes) [SIMULAÇÃO]", 
                printerName, data.Length);
            
            await Task.Delay(100); // Simula tempo de impressão
            
            return (true, $"Enviado para {printerName} (simulação)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar para impressora");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Lista impressoras instaladas (simulado para desenvolvimento)
    /// </summary>
    public static string[] GetInstalledPrinters()
    {
        // Em produção com System.Drawing.Printing:
        // return PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
        return new[] { "EPSON TM-T20", "Zebra ZD220", "Bematech MP-4200", "DefaultPrinter" };
    }

    private string? GetDefaultPrinter()
    {
        // Em produção: new PrinterSettings().PrinterName
        return "DefaultPrinter";
    }

    private void WriteText(MemoryStream ms, string text)
    {
        var bytes = Encoding.GetEncoding("IBM850").GetBytes(text);
        ms.Write(bytes, 0, bytes.Length);
    }

    private void BuildQRCode(MemoryStream ms, string data)
    {
        var GS = (byte)0x1D;
        
        // QR Code: Model
        ms.WriteByte(GS); ms.WriteByte((byte)'('); ms.WriteByte((byte)'k');
        ms.WriteByte(4); ms.WriteByte(0); ms.WriteByte(49); ms.WriteByte(65); ms.WriteByte(50); ms.WriteByte(0);

        // QR Code: Size
        ms.WriteByte(GS); ms.WriteByte((byte)'('); ms.WriteByte((byte)'k');
        ms.WriteByte(3); ms.WriteByte(0); ms.WriteByte(49); ms.WriteByte(67); ms.WriteByte(6);

        // QR Code: Error correction
        ms.WriteByte(GS); ms.WriteByte((byte)'('); ms.WriteByte((byte)'k');
        ms.WriteByte(3); ms.WriteByte(0); ms.WriteByte(49); ms.WriteByte(69); ms.WriteByte(49);

        // QR Code: Store data
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var len = dataBytes.Length + 3;
        ms.WriteByte(GS); ms.WriteByte((byte)'('); ms.WriteByte((byte)'k');
        ms.WriteByte((byte)(len % 256)); ms.WriteByte((byte)(len / 256));
        ms.WriteByte(49); ms.WriteByte(80); ms.WriteByte(48);
        ms.Write(dataBytes, 0, dataBytes.Length);

        // QR Code: Print
        ms.WriteByte(GS); ms.WriteByte((byte)'('); ms.WriteByte((byte)'k');
        ms.WriteByte(3); ms.WriteByte(0); ms.WriteByte(49); ms.WriteByte(81); ms.WriteByte(48);
    }

    private string TruncateZpl(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("^", "").Replace("~", ""); // Caracteres especiais ZPL
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if ((currentLine + " " + word).Trim().Length <= maxWidth)
            {
                currentLine = (currentLine + " " + word).Trim();
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    #endregion
}

#region Settings e DTOs

public class ThermalPrinterSettings
{
    public string? CupomPrinterName { get; set; }
    public string? EtiquetaPrinterName { get; set; }
    public int CupomWidth { get; set; } = 80; // mm
    public int EtiquetaWidth { get; set; } = 50; // mm
}

public class CupomPrintData
{
    public string? NomeFarmacia { get; set; }
    public string? CNPJ { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public DateTime Data { get; set; }
    public string? Cliente { get; set; }
    public List<CupomItemPrint> Itens { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public List<CupomPagamentoPrint>? Pagamentos { get; set; }
    public decimal Troco { get; set; }
    public string? QRCodeData { get; set; }
}

public class CupomItemPrint
{
    public string Descricao { get; set; } = "";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class CupomPagamentoPrint
{
    public string Forma { get; set; } = "";
    public decimal Valor { get; set; }
}

public class EtiquetaPrintData
{
    public string? NomeFarmacia { get; set; }
    public string NomeProduto { get; set; } = "";
    public string? Composicao { get; set; }
    public string? Posologia { get; set; }
    public string? ViaAdministracao { get; set; }
    public string Quantidade { get; set; } = "";
    public string Unidade { get; set; } = "";
    public string? NomePaciente { get; set; }
    public string Lote { get; set; } = "";
    public DateTime? Validade { get; set; }
    public DateTime? DataManipulacao { get; set; }
    public string? FarmaceuticoRT { get; set; }
    public string? CodigoBarras { get; set; }
}

public class EtiquetaLotePrintData
{
    public string NomeMateriaPrima { get; set; } = "";
    public string? CodigoDCB { get; set; }
    public string NumeroLote { get; set; } = "";
    public DateTime Validade { get; set; }
    public string? Fornecedor { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "";
    public string? CodigoBarras { get; set; }
    public bool EmQuarentena { get; set; }
}

#endregion

// NOTA: Para produção com impressão real, considere:
// 1. Pacote NuGet: ESCPOS.NET para impressoras térmicas
// 2. Pacote NuGet: System.Drawing.Common para PrinterSettings
// 3. Envio direto para porta LPT ou USB
// 
// Exemplo com ESCPOS.NET:
// var printer = new SerialPrinter("COM1", 9600);
// printer.Write(data);
// printer.FullPaperCut();
