using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace STAYTRUST.Services
{
    public class SightengineImageValidationService : IImageValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUser;
        private readonly string _apiSecret;

        public SightengineImageValidationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiUser = config["Sightengine:ApiUser"] ?? "";
            _apiSecret = config["Sightengine:ApiSecret"] ?? "";
        }

        public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl)
        {
            var result = new ImageValidationResult { IsValid = true };

            try
            {
                if (string.IsNullOrEmpty(_apiUser) || string.IsNullOrEmpty(_apiSecret))
                {
                    result.IsValid = true;
                    return result;
                }

                // Call the real API to detect inappropriate content
                var requestUrl = $"https://api.sightengine.com/1.0/check.json?models=nudity,wad,offensive,scam,gore&api_user={_apiUser}&api_secret={_apiSecret}&url={Uri.EscapeDataString(imageUrl)}";
                
                var response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("status", out var status) && status.GetString() == "success")
                    {
                        bool isSafe = true;
                        
                        // Basic checking across multiple models
                        if (doc.RootElement.TryGetProperty("nudity", out var nudity) && nudity.TryGetProperty("safe", out var safeScore))
                        {
                            if (safeScore.GetDouble() < 0.5) isSafe = false;
                        }
                        
                        // Check for weapons, alcohol, drugs (WAD model)
                        if (doc.RootElement.TryGetProperty("weapon", out var weapon) && weapon.GetDouble() > 0.5) isSafe = false;
                        if (doc.RootElement.TryGetProperty("alcohol", out var alcohol) && alcohol.GetDouble() > 0.5) isSafe = false;
                        if (doc.RootElement.TryGetProperty("drugs", out var drugs) && drugs.GetDouble() > 0.5) isSafe = false;
                        
                        result.IsValid = isSafe;
                        if (!isSafe) result.ErrorMessage = "Image contains inappropriate content.";
                    }
                }
                else
                {
                    // If API fails, we shouldn't block the user, log it but allow listing
                    result.IsValid = true; 
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Image validation failed: {ex.Message}";
            }

            return result;
        }
    }
}
