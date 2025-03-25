using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepositoryExplorerController : ControllerBase
    {
        private readonly ILogger<RepositoryExplorerController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://apiserviceswin20250318.azurewebsites.net/api";

        public RepositoryExplorerController(ILogger<RepositoryExplorerController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        [Route("available")]
        public async Task<IActionResult> GetAvailableRepositories()
        {
            try
            {
                // Get repositories from the Azure API
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/repositories");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var repositories = JsonSerializer.Deserialize<List<object>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(repositories);
                }
                
                // Fallback to mock repositories if the API call fails
                var mockRepositories = GetMockRepositories();
                return Ok(mockRepositories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available repositories");
                var mockRepositories = GetMockRepositories();
                return Ok(mockRepositories);
            }
        }

        [HttpGet]
        [Route("repository/{repositoryName}")]
        public async Task<IActionResult> GetRepositoryDetails(string repositoryName)
        {
            try
            {
                // Get repository details from the Azure API
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/repository/{repositoryName}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var repository = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(repository);
                }
                
                // Fallback to mock repository details if the API call fails
                var mockRepositories = GetMockRepositories();
                var mockRepository = mockRepositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (mockRepository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                return Ok(mockRepository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository details for {repositoryName}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("repository/{repositoryName}/methods")]
        public async Task<IActionResult> GetRepositoryMethods(string repositoryName)
        {
            try
            {
                // Get repository methods from the Azure API
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/repository/{repositoryName}/methods");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var methods = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(methods);
                }
                
                // Fallback to mock repository methods if the API call fails
                var mockRepositories = GetMockRepositories();
                var mockRepository = mockRepositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (mockRepository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                return Ok(mockRepository.Methods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository methods for {repositoryName}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{repositoryName}/execute/{methodName}")]
        public async Task<IActionResult> ExecuteRepositoryMethodDirect(string repositoryName, string methodName, string parameter = "")
        {
            try
            {
                // Execute the repository method using the Azure API
                var url = $"{_apiBaseUrl}/repository/{repositoryName}/{methodName}";
                
                if (!string.IsNullOrEmpty(parameter))
                {
                    url += $"?parameters={parameter}";
                }
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(result);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"Method '{methodName}' not found in repository '{repositoryName}' or patient not found");
                }
                
                return StatusCode((int)response.StatusCode, $"Error from API: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing repository method {methodName} on {repositoryName}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("repository/{repositoryName}/data")]
        public async Task<IActionResult> ExploreRepository(string repositoryName)
        {
            try
            {
                // Get repository data from the Azure API
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/repository/{repositoryName}/data");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(data);
                }
                
                // Fallback to mock repository data if the API call fails
                var mockRepositories = GetMockRepositories();
                var mockRepository = mockRepositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (mockRepository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                return Ok(mockRepository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exploring repository {repositoryName}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("explore/{repositoryName}/{methodName}")]
        public async Task<IActionResult> ExecuteRepositoryMethod(string repositoryName, string methodName, [FromQuery] string parameters = "")
        {
            try
            {
                // Execute the repository method using the Azure API
                var url = $"{_apiBaseUrl}/repository/{repositoryName}/{methodName}";
                
                if (!string.IsNullOrEmpty(parameters))
                {
                    url += $"?parameters={parameters}";
                }
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<object>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Ok(result);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"Method '{methodName}' not found in repository '{repositoryName}' or patient not found");
                }
                
                return StatusCode((int)response.StatusCode, $"Error from API: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing repository method {methodName} on {repositoryName}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to get mock repositories if the API call fails
        private List<dynamic> GetMockRepositories()
        {
            var repositories = new List<dynamic>
            {
                new
                {
                    Name = "PatientRepository",
                    Description = "Repository for patient demographic information",
                    Methods = new List<string> { "GetPatient", "GetAllPatients", "SearchPatients" }
                },
                new
                {
                    Name = "MedicalRecordRepository",
                    Description = "Repository for patient medical records",
                    Methods = new List<string> { "GetMedicalRecord", "GetMedicalHistory" }
                },
                new
                {
                    Name = "AppointmentRepository",
                    Description = "Repository for patient appointments",
                    Methods = new List<string> { "GetAppointments", "ScheduleAppointment", "CancelAppointment" }
                },
                new
                {
                    Name = "MedicationRepository",
                    Description = "Repository for patient medications",
                    Methods = new List<string> { "GetMedications", "GetMedicationHistory" }
                },
                new
                {
                    Name = "LabResultRepository",
                    Description = "Repository for patient lab results",
                    Methods = new List<string> { "GetLabResults", "GetRecentLabResults" }
                },
                new
                {
                    Name = "DemographicsRepository",
                    Description = "Repository for patient demographics",
                    Methods = new List<string> { "GetDemographics" }
                }
            };
            
            return repositories;
        }
    }
}
