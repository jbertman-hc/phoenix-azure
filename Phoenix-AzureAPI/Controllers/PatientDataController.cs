using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using Phoenix_AzureAPI.Services;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientDataController : ControllerBase
    {
        private readonly PatientDataService _patientDataService;
        private readonly IHttpClientFactory _httpClientFactory;

        public PatientDataController(PatientDataService patientDataService, IHttpClientFactory httpClientFactory)
        {
            _patientDataService = patientDataService ?? throw new ArgumentNullException(nameof(patientDataService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Get comprehensive patient data from all repositories
        /// </summary>
        /// <param name="patientId">The patient identifier</param>
        /// <returns>Comprehensive patient data</returns>
        [HttpGet("comprehensive/{patientId}")]
        public async Task<IActionResult> GetComprehensivePatientData(int patientId)
        {
            try
            {
                var patientData = await _patientDataService.GetComprehensivePatientData(patientId);
                return Ok(patientData);
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get a map of all repositories and methods needed for patient data
        /// </summary>
        /// <returns>Dictionary of repositories and their methods</returns>
        [HttpGet("repositories-map")]
        public async Task<IActionResult> GetPatientDataRepositoriesMap()
        {
            try
            {
                // Try to get the repositories map from the Azure API
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://apiserviceswin20250318.azurewebsites.net/api/RepositoryExplorer/repositories");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var repositories = JsonSerializer.Deserialize<List<object>>(content, options);
                    
                    // Convert to the format expected by the client
                    var repositoriesMap = new Dictionary<string, List<string>>();
                    foreach (var repo in repositories)
                    {
                        var repoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(repo.ToString(), options);
                        if (repoDict.TryGetValue("name", out var nameObj) && repoDict.TryGetValue("methods", out var methodsObj))
                        {
                            var name = nameObj.ToString();
                            var methods = JsonSerializer.Deserialize<List<string>>(methodsObj.ToString(), options);
                            repositoriesMap[name] = methods;
                        }
                    }
                    
                    return Ok(repositoriesMap);
                }
                
                // Fallback to local implementation if the API call fails
                var localRepositoriesMap = await _patientDataService.GetPatientDataRepositoriesMap();
                return Ok(localRepositoriesMap);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
