using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Phoenix_AzureAPI.Models;

namespace Phoenix_AzureAPI.Services
{
    public interface IPracticeService
    {
        Task<IEnumerable<Practice>> GetAllPracticesAsync();
        Task<Practice> GetPracticeByIdAsync(int id);
        Task<Practice> CreatePracticeAsync(Practice practice);
        Task<Practice> UpdatePracticeAsync(int id, Practice practice);
        Task<bool> DeletePracticeAsync(int id);
    }

    public class PracticeService : IPracticeService
    {
        private readonly ILogger<PracticeService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string ApiBaseUrl = "https://apiserviceswin20250318.azurewebsites.net/api/";

        public PracticeService(ILogger<PracticeService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<IEnumerable<Practice>> GetAllPracticesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all practices from API");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Practice";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully retrieved practices");
                    
                    // If the content is empty, return an empty list
                    if (string.IsNullOrEmpty(content))
                    {
                        return new List<Practice>();
                    }
                    
                    // Deserialize the JSON response to a list of practices
                    var practices = JsonSerializer.Deserialize<List<Practice>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return practices ?? new List<Practice>();
                }
                
                _logger.LogWarning($"Failed to retrieve practices. Status code: {response.StatusCode}");
                return new List<Practice>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving practices from API");
                return new List<Practice>();
            }
        }

        public async Task<Practice> GetPracticeByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Retrieving practice with ID {id} from API");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Practice/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully retrieved practice with ID {id}");
                    
                    // If the content is empty, return null
                    if (string.IsNullOrEmpty(content))
                    {
                        return null;
                    }
                    
                    // Deserialize the JSON response to a practice
                    var practice = JsonSerializer.Deserialize<Practice>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return practice;
                }
                
                _logger.LogWarning($"Failed to retrieve practice with ID {id}. Status code: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving practice with ID {id} from API");
                return null;
            }
        }

        public async Task<Practice> CreatePracticeAsync(Practice practice)
        {
            try
            {
                _logger.LogInformation("Creating new practice");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Practice";
                
                _logger.LogInformation($"Requesting: {url}");
                var content = new StringContent(JsonSerializer.Serialize(practice), System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var newPractice = await JsonSerializer.DeserializeAsync<Practice>(await response.Content.ReadAsStreamAsync(), 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return newPractice;
                }
                
                _logger.LogWarning($"Failed to create practice. Status code: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating practice");
                return null;
            }
        }

        public async Task<Practice> UpdatePracticeAsync(int id, Practice practice)
        {
            try
            {
                _logger.LogInformation($"Updating practice with ID {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Practice/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var content = new StringContent(JsonSerializer.Serialize(practice), System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var updatedPractice = await JsonSerializer.DeserializeAsync<Practice>(await response.Content.ReadAsStreamAsync(), 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return updatedPractice;
                }
                
                _logger.LogWarning($"Failed to update practice with ID {id}. Status code: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating practice with ID {id}");
                return null;
            }
        }

        public async Task<bool> DeletePracticeAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting practice with ID {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Practice/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.DeleteAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete practice with ID {id}. Status code: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting practice with ID {id}");
                return false;
            }
        }
    }
}
