using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTOs.Prescriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DTOs;

namespace Service.Prescriptions;

/// <summary>
/// Serviço de OCR para prescrições usando OpenAI Vision API (GPT-4o)
/// </summary>
public class OpenAIPrescriptionParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIPrescriptionParserService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private const string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";

    public OpenAIPrescriptionParserService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIPrescriptionParserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = configuration["OpenAI:Model"] ?? "gpt-4o";

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    /// <summary>
    /// Processa uma imagem de prescrição e extrai os dados
    /// </summary>
    public async Task<OcrPrescriptionResultDto> ParsePrescriptionAsync(string base64Image, string imageType)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey não configurada — OCR de prescrição indisponível");
        }

        try
        {
            _logger.LogInformation("Iniciando OCR de prescrição...");
            
            // Garantir formato correto do base64
            var imageData = base64Image;
            if (imageData.Contains(","))
                imageData = imageData.Split(',')[1];
            
            // Determinar media type
            var mediaType = imageType.ToLower() switch
            {
                "image/jpeg" or "image/jpg" => "image/jpeg",
                "image/png" => "image/png",
                "application/pdf" => "image/png", // PDF precisa ser convertido antes
                _ => "image/jpeg"
            };
            
            var request = new OpenAIVisionRequest
            {
                Model = _model,
                MaxTokens = 2000,
                Messages = new List<OpenAIMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = GetSystemPrompt()
                    },
                    new()
                    {
                        Role = "user",
                        Content = new List<object>
                        {
                            new { type = "text", text = GetUserPrompt() },
                            new 
                            { 
                                type = "image_url", 
                                image_url = new 
                                { 
                                    url = $"data:{mediaType};base64,{imageData}",
                                    detail = "high"
                                } 
                            }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Enviando requisição para OpenAI...");
            var response = await _httpClient.PostAsync(OpenAiEndpoint, httpContent);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro OpenAI: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Erro na API OpenAI: {response.StatusCode}");
            }

            var openAiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var assistantMessage = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            
            if (string.IsNullOrEmpty(assistantMessage))
                throw new Exception("Resposta vazia da OpenAI");

            _logger.LogDebug("Resposta OpenAI: {Response}", assistantMessage);

            // Extrair JSON da resposta
            var result = ParseOpenAIResponse(assistantMessage);
            
            _logger.LogInformation("OCR concluído. {ItemCount} itens extraídos.", result.Items.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar OCR de prescrição");
            throw;
        }
    }

    private string GetSystemPrompt()
    {
        return @"Você é um assistente especializado em ler prescrições médicas brasileiras de farmácia de manipulação.

REGRAS IMPORTANTES:
- Extraia TODOS os componentes/ingredientes da fórmula magistral
- Mantenha os nomes EXATAMENTE como escritos na receita
- Identifique concentrações/quantidades com suas unidades (%, mg, g, mL, mcg, UI)
- Reconheça 'qsp' ou 'qs' como 'quantidade suficiente para'
- Identifique a forma farmacêutica (cápsulas, creme, loção, gel, pomada, solução)
- Extraia instruções de uso/posologia
- Identifique dados do médico (nome, CRM, RQE, especialidade)
- Se houver nome do paciente, extraia
- Identifique tipo de uso: ORAL, TOPICO, LOCAL, OFTALMICO, NASAL, etc

FORMATO DE RESPOSTA:
Retorne APENAS um JSON válido, sem markdown, sem explicações adicionais.";
    }

    private string GetUserPrompt()
    {
        return @"Analise esta receita médica e extraia as informações no seguinte formato JSON:

{
  ""doctor"": {
    ""name"": ""Nome completo do médico"",
    ""crm"": ""número do CRM"",
    ""crmState"": ""UF do CRM"",
    ""rqe"": ""número RQE se houver"",
    ""specialty"": ""especialidade se identificável""
  },
  ""patient"": {
    ""name"": ""nome do paciente se houver"",
    ""usage"": ""ORAL ou TOPICO ou LOCAL etc""
  },
  ""prescriptionDate"": ""DD/MM/AAAA"",
  ""pharmaceuticalForm"": ""CÁPSULAS ou CREME ou LOÇÃO etc"",
  ""totalQuantity"": ""50g ou 90 cápsulas etc"",
  ""items"": [
    {
      ""name"": ""nome do componente"",
      ""quantity"": ""valor numérico ou qsp"",
      ""unit"": ""% ou mg ou g ou mL etc"",
      ""rawText"": ""texto original da linha"",
      ""confidence"": 0.95,
      ""isQsp"": false
    }
  ],
  ""instructions"": ""posologia/modo de usar"",
  ""overallConfidence"": 0.85,
  ""rawText"": ""texto completo extraído""
}

IMPORTANTE:
- Extraia TODOS os ingredientes, mesmo os de difícil leitura
- Para itens com qsp, coloque isQsp: true
- confidence de 0 a 1 indica certeza da leitura
- Se não conseguir ler algo, indique com confidence baixo mas tente extrair";
    }

    private OcrPrescriptionResultDto ParseOpenAIResponse(string response)
    {
        try
        {
            // Tentar extrair JSON se estiver envolto em markdown
            var jsonContent = response;
            
            if (response.Contains("```json"))
            {
                var start = response.IndexOf("```json") + 7;
                var end = response.IndexOf("```", start);
                if (end > start)
                    jsonContent = response.Substring(start, end - start).Trim();
            }
            else if (response.Contains("```"))
            {
                var start = response.IndexOf("```") + 3;
                var end = response.IndexOf("```", start);
                if (end > start)
                    jsonContent = response.Substring(start, end - start).Trim();
            }

            var result = JsonSerializer.Deserialize<OcrPrescriptionResultDto>(jsonContent, 
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            return result ?? new OcrPrescriptionResultDto
            {
                OverallConfidence = 0,
                RawText = response
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Falha ao parsear JSON da resposta OpenAI. Tentando extração manual...");
            
            // Retornar resultado parcial
            return new OcrPrescriptionResultDto
            {
                OverallConfidence = 0.3,
                RawText = response,
                Items = new List<OcrIngredientDto>()
            };
        }
    }
}

// Classes auxiliares para serialização
internal class OpenAIVisionRequest
{
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
    
    public List<OpenAIMessage> Messages { get; set; } = new();
}

internal class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Content { get; set; }
}

internal class OpenAIResponse
{
    public List<OpenAIChoice>? Choices { get; set; }
}

internal class OpenAIChoice
{
    public OpenAIMessageResponse? Message { get; set; }
}

internal class OpenAIMessageResponse
{
    public string? Content { get; set; }
}
