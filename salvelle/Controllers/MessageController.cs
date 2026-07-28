using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using Service.Notifications;

namespace Controllers
{
    [ApiController]
    [Route("api/wa")]
    public class MessageController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly WhatsAppService _whatsAppService;

        public MessageController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            WhatsAppService whatsAppService)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _whatsAppService = whatsAppService;
        }

        // Diagnóstico do caminho real usado em prod (WhatsAppService → instância "pharm").
        // GET /api/wa/diag?phone=11975903732
        [HttpGet("diag")]
        public async Task<IActionResult> Diag([FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "missing_phone" });

            var apiKeyRaw = _config["AtentBot:ApiKey"];
            var apiUrl = _config["AtentBot:ApiUrl"] ?? "https://api.atentbot.com/message/sendText/pharm";
            var apiKeyMask = string.IsNullOrEmpty(apiKeyRaw)
                ? "<missing>"
                : apiKeyRaw.Length > 4 ? "****" + apiKeyRaw[^4..] : "****";

            var (success, message) = await _whatsAppService.SendMessageAsync(phone, "[diag] teste de envio");
            return Ok(new
            {
                phoneInput = phone,
                apiKeyMask,
                apiUrl,
                success,
                providerResponse = message
            });
        }

        public record MessageRequest(string Number, string Text);

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] MessageRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Number) || string.IsNullOrWhiteSpace(req.Text))
                return BadRequest(new { error = "missing_number_or_text" });

            var apiKey = _config["AtentBot:ApiKey"];
            var baseUrl = _config["AtentBot:BaseUrl"] ?? "https://api.atentbot.com";
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, new { error = "atentbot_apikey_not_configured" });

            // monta requisi��o
            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl.TrimEnd('/')}/message/sendText/crescer";
            var payload = new { number = req.Number, text = req.Text };

            using var http = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            http.Headers.Add("apikey", apiKey);

            var resp = await client.SendAsync(http, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, new { error = "whatsapp_send_failed", provider = body });

            return Ok(new { status = "sent", provider = body });
        }
    }
}

