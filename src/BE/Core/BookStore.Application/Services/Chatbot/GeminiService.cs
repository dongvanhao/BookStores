using BookStore.Application.IService.Chatbot;
using BookStore.Application.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookStore.Application.Services.Chatbot
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _options;

        public GeminiService(HttpClient http, IOptions<GeminiOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<string> AskAsync(string prompt)
        {
            // 1. Lấy Key và Model từ Options (được inject từ appsettings/docker env)
            var apiKey = _options.ApiKey;
            var model = _options.Model; // Ví dụ: gemini-1.5-flash

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Gemini API Key is missing in configuration.");
            }

            // 2. Build URL động
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var response = await _http.PostAsJsonAsync(url, body);

            // 3. Log lỗi chi tiết nếu thất bại (giúp debug dễ hơn)
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error ({response.StatusCode}): {errorContent}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            // 4. Parse an toàn
            try
            {
                return json
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()!;
            }
            catch
            {
                return "Xin lỗi, hệ thống không đọc được câu trả lời từ AI.";
            }
        }
    }
}