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

        public PatientDataService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
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
        public async Task<Patient> GetPatientByIdAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Use the correct endpoint for patient data
                var url = $"{_apiBaseUrl}/Patient/{id}";
                
                Console.WriteLine($"Fetching patient from: {url}");
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Patient data received: {content.Substring(0, Math.Min(content.Length, 100))}...");
                    
                    var patient = JsonSerializer.Deserialize<Patient>(content, _jsonOptions);
                    return patient ?? throw new Exception($"Failed to deserialize patient with ID {id}");
                }
                
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Try to get a list of all patients to see what IDs are available
                    var allPatients = await GetAllPatientsAsync();
                    var availableIds = string.Join(", ", allPatients.Select(p => p.PatientID));
                    throw new Exception($"Patient with ID {id} not found. Available IDs: {availableIds}");
                }
                
                throw new Exception($"Failed to retrieve patient with ID {id}. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving patient with ID {id}: {ex.Message}");
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
                Console.WriteLine($"Error retrieving all patient IDs: {ex.Message}");
                throw new Exception($"Error retrieving patient IDs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all patients from the API
        /// </summary>
        /// <returns>A list of Patient objects</returns>
        public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiBaseUrl}/Patient";
                
                Console.WriteLine($"Fetching all patients from: {url}");
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"All patients data received: {content.Substring(0, Math.Min(content.Length, 100))}...");
                    
                    var patients = JsonSerializer.Deserialize<List<Patient>>(content, _jsonOptions);
                    return patients ?? new List<Patient>();
                }
                
                throw new Exception($"Failed to retrieve patients. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all patients: {ex.Message}");
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
