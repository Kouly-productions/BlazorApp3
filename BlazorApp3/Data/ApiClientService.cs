using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorApp3.Services
{
    public interface IApiClientService
    {
        Task<string> EncryptWithApiAsync(string textToEncrypt);
        Task<string> EncryptDatabaseApiAsync(string textToEncrypt);
    }

    public class ApiClientService : IApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IAsymmetricEncryptionService _asymmetricEncryptionService;
        private readonly string _apiBaseUrl;

        public ApiClientService(HttpClient httpClient,
                               IAsymmetricEncryptionService asymmetricEncryptionService,
                               IConfiguration configuration)
        {
            _httpClient = httpClient;
            _asymmetricEncryptionService = asymmetricEncryptionService;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7181";
        }

        public async Task<string> EncryptWithApiAsync(string textToEncrypt)
        {
            try
            {
                var request = new
                {
                    PublicKey = _asymmetricEncryptionService.PublicKey,
                    TextToEncrypt = textToEncrypt,
                    IncludeOriginal = false
                };

                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/encryption/encrypt", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EncryptionApiResponse>();
                    return result?.EncryptedText ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API request error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string> EncryptDatabaseApiAsync(string textToEncrypt)
        {
            try
            {
                var request = new
                {
                    PublicKey = _asymmetricEncryptionService.DbPublicKey,
                    TextToEncrypt = textToEncrypt,
                    IncludeOriginal = false
                };

                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/encryption/encrypt", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EncryptionApiResponse>();
                    return result?.EncryptedText ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API request error: {ex.Message}");
                return string.Empty;
            }
        }

        private class EncryptionApiResponse
        {
            public string EncryptedText { get; set; }
            public string OriginalText { get; set; }
        }

    }
}