using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net;
using Phoenix_AzureAPI.Models;
using Microsoft.Extensions.Logging;

namespace Phoenix_AzureAPI.Services
{
    /// <summary>
    /// Service for aggregating patient data from multiple repositories
    /// </summary>
    public class PatientDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "https://apiserviceswin20250318.azurewebsites.net/api";
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<PatientDataService> _logger;

        public PatientDataService(IHttpClientFactory httpClientFactory, ILogger<PatientDataService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get a patient by ID from the API
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <returns>A Patient object</returns>
        public async Task<Models.Patient> GetPatientByIdAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // First try the direct Patient endpoint
                var url = $"{_apiBaseUrl}/Patient/{id}";
                
                _logger.LogInformation($"Fetching patient from: {url}");
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Patient data received: {content.Substring(0, Math.Min(content.Length, 100))}...");
                    
                    var patient = JsonSerializer.Deserialize<Models.Patient>(content, _jsonOptions);
                    return patient ?? throw new Exception($"Failed to deserialize patient with ID {id}");
                }
                
                // If direct endpoint fails, try to find the patient in the addendum data
                _logger.LogInformation($"Patient not found at direct endpoint, trying addendum lookup for ID {id}");
                
                // Loop through addendum entries to find matching patient
                for (int i = 1; i <= 25; i++)
                {
                    try
                    {
                        var addendumUrl = $"{_apiBaseUrl}/addendum/{i}";
                        _logger.LogInformation($"Checking addendum {i} for patient {id}");
                        
                        var addendumResponse = await client.GetAsync(addendumUrl);
                        
                        if (addendumResponse.IsSuccessStatusCode)
                        {
                            var addendumContent = await addendumResponse.Content.ReadAsStringAsync();
                            
                            // Check if this contains patient data
                            if (addendumContent.Contains("patID") && addendumContent.Contains("patientName"))
                            {
                                // Parse the addendum data
                                try {
                                    var addendumData = JsonSerializer.Deserialize<JsonDocument>(addendumContent, _jsonOptions).RootElement;
                                    
                                    // Check if the required properties exist
                                    if (addendumData.TryGetProperty("patID", out JsonElement patIdElement) && 
                                        addendumData.TryGetProperty("patientName", out JsonElement patientNameElement))
                                    {
                                        var patId = patIdElement.GetInt32();
                                        
                                        // If this is the patient we're looking for
                                        if (patId == id)
                                        {
                                            var patientName = patientNameElement.GetString() ?? "";
                                            
                                            // Split the name into parts
                                            string[] nameParts = patientName?.Split(' ') ?? new string[0];
                                            string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                                            string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                                            
                                            // Create a patient object from the addendum data
                                            return new Models.Patient
                                            {
                                                PatientID = patId,
                                                First = firstName,
                                                Last = lastName,
                                                // Set a default birthdate if needed for FHIR
                                                BirthDate = DateTime.UtcNow.AddYears(-40)
                                            };
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error parsing addendum {i} while looking for patient {id}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing addendum {i} while looking for patient {id}");
                        // Continue with the next addendum
                    }
                }
                
                // If we get here, the patient wasn't found
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Try to get a list of all patients to show available IDs in the error
                    var allPatients = await GetAllPatientsAsync();
                    var availableIds = string.Join(", ", allPatients.Select(p => p.PatientID));
                    throw new Exception($"Patient with ID {id} not found. Available IDs: {availableIds}");
                }
                
                throw new Exception($"Failed to retrieve patient with ID {id}. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient with ID {id}");
                throw new Exception($"Error retrieving patient with ID {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all patient IDs from the API
        /// </summary>
        /// <returns>A list of patient IDs</returns>
        public async Task<IEnumerable<string>> GetAllPatientIdsAsync()
        {
            try
            {
                var patients = await GetAllPatientsAsync();
                return patients.Select(p => p.PatientID.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving all patient IDs");
                throw new Exception($"Error retrieving patient IDs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all patients from the API
        /// </summary>
        /// <returns>A list of Patient objects</returns>
        public async Task<IEnumerable<Models.Patient>> GetAllPatientsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var patients = new List<Models.Patient>();
                
                // We'll fetch patients from the addendum endpoint since the /Patient endpoint isn't working
                _logger.LogInformation("Fetching patients from addendum endpoint");
                
                // Fetch a reasonable number of patients (we know there are about 22 based on the console log)
                for (int i = 1; i <= 25; i++)
                {
                    try
                    {
                        var addendumUrl = $"{_apiBaseUrl}/addendum/{i}";
                        _logger.LogInformation($"Fetching addendum from {addendumUrl}");
                        
                        var addendumResponse = await client.GetAsync(addendumUrl);
                        
                        if (addendumResponse.IsSuccessStatusCode)
                        {
                            var addendumContent = await addendumResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Addendum response: {addendumContent}");
                            
                            // Check if this is a patient record
                            if (addendumContent.Contains("patID") && addendumContent.Contains("patientName"))
                            {
                                // Create a patient from the addendum data
                                try {
                                    var addendumData = JsonSerializer.Deserialize<JsonDocument>(addendumContent, _jsonOptions).RootElement;
                                    
                                    // Check if the required properties exist
                                    if (addendumData.TryGetProperty("patID", out JsonElement patIdElement) && 
                                        addendumData.TryGetProperty("patientName", out JsonElement patientNameElement))
                                    {
                                        var patId = patIdElement.GetInt32();
                                        var patientName = patientNameElement.GetString() ?? "";
                                        
                                        // Split the name into parts
                                        string[] nameParts = patientName?.Split(' ') ?? new string[0];
                                        string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                                        string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                                        
                                        // Create a new patient
                                        var patient = new Models.Patient
                                        {
                                            PatientID = patId,
                                            First = firstName,
                                            Last = lastName
                                        };
                                        
                                        patients.Add(patient);
                                        _logger.LogInformation($"Added patient: {patientName} (ID: {patId})");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error parsing addendum {i}");
                                }
                            }
                        }
                        else if (addendumResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // Skip if not found
                            _logger.LogInformation($"No addendum found for ID {i}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to retrieve addendum {i}. Status code: {addendumResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing addendum {i}");
                        // Continue with the next patient
                    }
                }
                
                _logger.LogInformation($"Retrieved {patients.Count} patients from addendum endpoint");
                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients");
                throw new Exception($"Error retrieving patients: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get comprehensive patient data by aggregating information from multiple repositories
        /// </summary>
        /// <param name="patientId">The patient identifier</param>
        /// <returns>A comprehensive patient data object</returns>
        public async Task<object> GetComprehensivePatientData(int patientId)
        {
            // First validate that the patient exists
            var patientExists = await ValidatePatientExists(patientId);
            if (!patientExists)
            {
                throw new Exception($"Patient with ID {patientId} not found");
            }

            // Create a dictionary to store all patient data
            var patientData = new Dictionary<string, object>();

            try
            {
                // Get demographics data
                var demographics = await GetRepositoryData("DemographicsRepository", "GetDemographics", patientId.ToString());
                patientData["Demographics"] = demographics;

                // Get medical records
                var medicalRecords = await GetRepositoryData("PatientMedicalRecordRepository", "GetMedicalRecords", patientId.ToString());
                patientData["MedicalRecords"] = medicalRecords;

                // Get medications
                var medications = await GetRepositoryData("MedicationRepository", "GetMedications", patientId.ToString());
                patientData["Medications"] = medications;

                // Get allergies
                var allergies = await GetRepositoryData("AllergyRepository", "GetAllergies", patientId.ToString());
                patientData["Allergies"] = allergies;

                // Get insurance information
                var insurance = await GetRepositoryData("InsuranceRepository", "GetInsuranceInfo", patientId.ToString());
                patientData["Insurance"] = insurance;

                return patientData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving comprehensive patient data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate that a patient exists
        /// </summary>
        /// <param name="patientId">The patient identifier</param>
        /// <returns>True if the patient exists, false otherwise</returns>
        public async Task<bool> ValidatePatientExists(int patientId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiBaseUrl}/Patient/{patientId}";
                
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get data from a specific repository
        /// </summary>
        /// <param name="repositoryName">Name of the repository</param>
        /// <param name="methodName">Method to call on the repository</param>
        /// <param name="parameter">Parameter to pass to the method</param>
        /// <returns>Repository data</returns>
        public async Task<object> GetRepositoryData(string repositoryName, string methodName, string parameter)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiBaseUrl}/RepositoryExplorer/explore/{repositoryName}/{methodName}?parameter={parameter}";
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Try to parse as JSON
                    try
                    {
                        return JsonSerializer.Deserialize<object>(content, _jsonOptions);
                    }
                    catch
                    {
                        // If not valid JSON, return as string
                        return content;
                    }
                }
                
                return $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Get a list of all repositories and methods needed for comprehensive patient data
        /// </summary>
        /// <returns>Dictionary of repositories and their methods</returns>
        public Dictionary<string, List<string>> GetPatientDataRepositoriesMap()
        {
            return new Dictionary<string, List<string>>
            {
                { "DemographicsRepository", new List<string> { "GetDemographics" } },
                { "PatientMedicalRecordRepository", new List<string> { "GetMedicalRecords" } },
                { "MedicationRepository", new List<string> { "GetMedications" } },
                { "AllergyRepository", new List<string> { "GetAllergies" } },
                { "InsuranceRepository", new List<string> { "GetInsuranceInfo" } }
            };
        }
    }
}
