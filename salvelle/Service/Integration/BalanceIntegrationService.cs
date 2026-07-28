using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Options;

namespace Services.Integration;

/// <summary>
/// Serviço de integração com balanças analíticas
/// Suporta protocolos: Toledo, Filizola, Marte, Shimadzu
/// </summary>
public class BalanceIntegrationService : IDisposable
{
    private readonly ILogger<BalanceIntegrationService> _logger;
    private readonly BalanceSettings _settings;
    private SerialPort? _serialPort;
    private bool _isConnected;

    public BalanceIntegrationService(
        ILogger<BalanceIntegrationService> logger,
        IOptions<BalanceSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Conecta à balança via porta serial
    /// </summary>
    public async Task<(bool Success, string Message)> ConnectAsync()
    {
        try
        {
            if (_isConnected && _serialPort?.IsOpen == true)
                return (true, "Já conectado");

            _serialPort = new SerialPort
            {
                PortName = _settings.PortName,
                BaudRate = _settings.BaudRate,
                DataBits = _settings.DataBits,
                Parity = Enum.Parse<Parity>(_settings.Parity),
                StopBits = Enum.Parse<StopBits>(_settings.StopBits),
                Handshake = Handshake.None,
                ReadTimeout = _settings.ReadTimeoutMs,
                WriteTimeout = _settings.WriteTimeoutMs,
                Encoding = Encoding.ASCII
            };

            _serialPort.Open();
            _isConnected = true;

            _logger.LogInformation("Balança conectada: {Port} @ {BaudRate}bps", 
                _settings.PortName, _settings.BaudRate);

            return (true, $"Conectado em {_settings.PortName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar balança em {Port}", _settings.PortName);
            return (false, $"Erro: {ex.Message}");
        }
    }

    /// <summary>
    /// Desconecta da balança
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _isConnected = false;
            _logger.LogInformation("Balança desconectada");
        }
    }

    /// <summary>
    /// Lê o peso atual da balança
    /// </summary>
    public async Task<BalanceReadingResult> ReadWeightAsync()
    {
        if (!_isConnected || _serialPort?.IsOpen != true)
        {
            return new BalanceReadingResult
            {
                Success = false,
                ErrorMessage = "Balança não conectada"
            };
        }

        try
        {
            // Limpar buffer
            _serialPort.DiscardInBuffer();

            // Enviar comando de leitura (varia por protocolo)
            var command = GetReadCommand();
            _serialPort.Write(command);

            // Aguardar resposta
            await Task.Delay(_settings.ReadDelayMs);

            // Ler resposta
            var response = _serialPort.ReadExisting();

            if (string.IsNullOrEmpty(response))
            {
                return new BalanceReadingResult
                {
                    Success = false,
                    ErrorMessage = "Sem resposta da balança"
                };
            }

            // Parsear resposta conforme protocolo
            var result = ParseResponse(response);
            
            _logger.LogDebug("Leitura balança: {Weight}{Unit} - Estável: {Stable}", 
                result.Weight, result.Unit, result.IsStable);

            return result;
        }
        catch (TimeoutException)
        {
            return new BalanceReadingResult
            {
                Success = false,
                ErrorMessage = "Timeout na leitura"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na leitura da balança");
            return new BalanceReadingResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Zera/Tara a balança
    /// </summary>
    public async Task<(bool Success, string Message)> TareAsync()
    {
        if (!_isConnected || _serialPort?.IsOpen != true)
            return (false, "Balança não conectada");

        try
        {
            var command = GetTareCommand();
            _serialPort.Write(command);
            await Task.Delay(500);

            _logger.LogInformation("Comando de tara enviado");
            return (true, "Tara realizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao tarar balança");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Lista portas seriais disponíveis
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    /// <summary>
    /// Testa conexão com a balança
    /// </summary>
    public async Task<(bool Success, string Message, BalanceReadingResult? Reading)> TestConnectionAsync()
    {
        var connectResult = await ConnectAsync();
        if (!connectResult.Success)
            return (false, connectResult.Message, null);

        var reading = await ReadWeightAsync();
        
        return (reading.Success, 
            reading.Success ? "Conexão OK" : reading.ErrorMessage ?? "Erro", 
            reading);
    }

    #region Comandos por Protocolo

    private string GetReadCommand()
    {
        return _settings.Protocol.ToUpper() switch
        {
            "TOLEDO" => "P\r\n",           // Toledo Prix
            "FILIZOLA" => "P\r",           // Filizola
            "MARTE" => "S\r\n",            // Marte
            "SHIMADZU" => "Q\r\n",         // Shimadzu
            "OHAUS" => "IP\r\n",           // Ohaus
            "GEHAKA" => "P\r\n",           // Gehaka
            _ => "P\r\n"                   // Padrão
        };
    }

    private string GetTareCommand()
    {
        return _settings.Protocol.ToUpper() switch
        {
            "TOLEDO" => "T\r\n",
            "FILIZOLA" => "T\r",
            "MARTE" => "T\r\n",
            "SHIMADZU" => "Z\r\n",
            "OHAUS" => "T\r\n",
            "GEHAKA" => "T\r\n",
            _ => "T\r\n"
        };
    }

    private BalanceReadingResult ParseResponse(string response)
    {
        var result = new BalanceReadingResult { RawResponse = response };

        try
        {
            // Limpar resposta
            response = response.Trim();

            // Parsear conforme protocolo
            switch (_settings.Protocol.ToUpper())
            {
                case "TOLEDO":
                    // Formato: "ST,GS,+  0.000 kg"
                    ParseToledoResponse(response, result);
                    break;

                case "FILIZOLA":
                    // Formato: "   0.000 kg"
                    ParseFilizolaResponse(response, result);
                    break;

                case "MARTE":
                    // Formato: "S S     0.0000 g"
                    ParseMarteResponse(response, result);
                    break;

                case "SHIMADZU":
                    // Formato: "+    0.0000 g"
                    ParseShimadzuResponse(response, result);
                    break;

                default:
                    ParseGenericResponse(response, result);
                    break;
            }

            result.Success = true;
            result.ReadAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Erro ao parsear: {ex.Message}";
        }

        return result;
    }

    private void ParseToledoResponse(string response, BalanceReadingResult result)
    {
        // Formato Toledo: "ST,GS,+  0.000 kg" ou "US,GS,+  0.000 kg"
        result.IsStable = response.StartsWith("ST");
        
        var parts = response.Split(',');
        if (parts.Length >= 3)
        {
            var weightPart = parts[2].Trim();
            ExtractWeightAndUnit(weightPart, result);
        }
    }

    private void ParseFilizolaResponse(string response, BalanceReadingResult result)
    {
        // Formato simples: "   0.000 kg"
        result.IsStable = true;
        ExtractWeightAndUnit(response, result);
    }

    private void ParseMarteResponse(string response, BalanceReadingResult result)
    {
        // Formato: "S S     0.0000 g" (primeiro S = estável)
        result.IsStable = response.StartsWith("S S") || response.StartsWith("S  ");
        
        var weightPart = response.Length > 4 ? response[4..].Trim() : response;
        ExtractWeightAndUnit(weightPart, result);
    }

    private void ParseShimadzuResponse(string response, BalanceReadingResult result)
    {
        // Formato: "+    0.0000 g"
        result.IsStable = !response.Contains("?");
        ExtractWeightAndUnit(response, result);
    }

    private void ParseGenericResponse(string response, BalanceReadingResult result)
    {
        result.IsStable = true;
        ExtractWeightAndUnit(response, result);
    }

    private void ExtractWeightAndUnit(string text, BalanceReadingResult result)
    {
        // Remover sinais
        text = text.Replace("+", "").Replace("-", "").Trim();

        // Detectar unidade
        string[] units = { "kg", "g", "mg", "lb", "oz" };
        foreach (var unit in units)
        {
            if (text.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
            {
                result.Unit = unit.ToLower();
                text = text[..^unit.Length].Trim();
                break;
            }
        }

        // Parsear valor
        if (decimal.TryParse(text.Replace(",", "."), 
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, 
            out var weight))
        {
            result.Weight = weight;
        }
    }

    #endregion

    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
    }
}

/// <summary>
/// Configurações da balança
/// </summary>
public class BalanceSettings
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string Parity { get; set; } = "None";
    public string StopBits { get; set; } = "One";
    public string Protocol { get; set; } = "TOLEDO";
    public int ReadTimeoutMs { get; set; } = 3000;
    public int WriteTimeoutMs { get; set; } = 1000;
    public int ReadDelayMs { get; set; } = 200;
}

/// <summary>
/// Resultado da leitura da balança
/// </summary>
public class BalanceReadingResult
{
    public bool Success { get; set; }
    public decimal Weight { get; set; }
    public string Unit { get; set; } = "g";
    public bool IsStable { get; set; }
    public DateTime ReadAt { get; set; }
    public string? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
}
