using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phoenix_AzureAPI.Services
{
    /// <summary>
    /// Service for aggregating patient data from multiple repositories
    /// </summary>
    public class PatientDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "http://localhost:5300/api";

        public PatientDataService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Get comprehensive patient data by aggregating information from multiple repositories
        /// </summary>
        /// <param name="patientId">The patient identifier</param>
        /// <returns>A comprehensive patient data object</returns>
        public async Task<object> GetComprehensivePatientData(int patientId)
        {
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
                var medications = await GetRepositoryData("PatientMedicationRepository", "GetMedications", patientId.ToString());
                patientData["Medications"] = medications;

                // Get allergies
                var allergies = await GetRepositoryData("PatientAllergyRepository", "GetAllergies", patientId.ToString());
                patientData["Allergies"] = allergies;

                // Get vital signs
                var vitalSigns = await GetRepositoryData("PatientVitalSignsRepository", "GetVitalSigns", patientId.ToString());
                patientData["VitalSigns"] = vitalSigns;

                // Get insurance information
                var insurance = await GetRepositoryData("PatientInsuranceRepository", "GetInsurance", patientId.ToString());
                patientData["Insurance"] = insurance;

                // Get appointments
                var appointments = await GetRepositoryData("SchedulingRepository", "GetAppointments", patientId.ToString());
                patientData["Appointments"] = appointments;

                // Get billing information
                var billing = await GetRepositoryData("BillingRepository", "GetBillingInfo", patientId.ToString());
                patientData["Billing"] = billing;

                // Get lab results
                var labResults = await GetRepositoryData("LabResultsRepository", "GetLabResults", patientId.ToString());
                patientData["LabResults"] = labResults;

                // Get imaging results
                var imagingResults = await GetRepositoryData("ImagingRepository", "GetImagingResults", patientId.ToString());
                patientData["ImagingResults"] = imagingResults;

                return patientData;
            }
            catch (Exception ex)
            {
                patientData["Error"] = $"Error retrieving comprehensive patient data: {ex.Message}";
                return patientData;
            }
        }

        /// <summary>
        /// Get data from a specific repository
        /// </summary>
        /// <param name="repositoryName">Name of the repository</param>
        /// <param name="methodName">Method to call on the repository</param>
        /// <param name="parameter">Parameter to pass to the method</param>
        /// <returns>Repository data</returns>
        private async Task<object> GetRepositoryData(string repositoryName, string methodName, string parameter)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiBaseUrl}/RepositoryExplorer/explore/{repositoryName}/{methodName}?parameters={parameter}";
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<object>(content, options) ?? 
                           new { Message = $"No data returned from {repositoryName}.{methodName}" };
                }
                
                return new { Error = $"Failed to retrieve data from {repositoryName}.{methodName}", StatusCode = response.StatusCode };
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }

        /// <summary>
        /// Get a list of all repositories and methods needed for comprehensive patient data
        /// </summary>
        /// <returns>Dictionary of repositories and their methods</returns>
        public Task<Dictionary<string, List<string>>> GetPatientDataRepositoriesMap()
        {
            var repositoriesMap = new Dictionary<string, List<string>>
            {
                { "DemographicsRepository", new List<string> { "GetDemographics", "UpdateDemographics" } },
                { "PatientMedicalRecordRepository", new List<string> { "GetMedicalRecords", "AddMedicalRecord" } },
                { "PatientMedicationRepository", new List<string> { "GetMedications", "AddMedication", "UpdateMedication", "DiscontinueMedication" } },
                { "PatientAllergyRepository", new List<string> { "GetAllergies", "AddAllergy", "UpdateAllergy", "RemoveAllergy" } },
                { "PatientVitalSignsRepository", new List<string> { "GetVitalSigns", "AddVitalSign" } },
                { "PatientInsuranceRepository", new List<string> { "GetInsurance", "UpdateInsurance" } },
                { "SchedulingRepository", new List<string> { "GetAppointments", "ScheduleAppointment", "CancelAppointment" } },
                { "BillingRepository", new List<string> { "GetBillingInfo", "AddCharge", "ProcessPayment" } },
                { "LabResultsRepository", new List<string> { "GetLabResults", "AddLabResult" } },
                { "ImagingRepository", new List<string> { "GetImagingResults", "AddImagingResult" } },
                { "PatientPortalRepository", new List<string> { "GetPatientPortalAccess", "EnablePatientPortalAccess" } },
                { "PatientConsentRepository", new List<string> { "GetConsents", "AddConsent" } },
                { "PatientFamilyHistoryRepository", new List<string> { "GetFamilyHistory", "UpdateFamilyHistory" } },
                { "PatientSocialHistoryRepository", new List<string> { "GetSocialHistory", "UpdateSocialHistory" } },
                { "PatientImmunizationRepository", new List<string> { "GetImmunizations", "AddImmunization" } },
                { "PatientProblemListRepository", new List<string> { "GetProblemList", "AddProblem", "ResolveProblem" } }
            };
            
            return Task.FromResult(repositoriesMap);
        }
    }
}
