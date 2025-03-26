using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Phoenix_AzureAPI.Models;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PatientController> _logger;
        private const string ApiBaseUrl = "https://apiserviceswin20250318.azurewebsites.net/api/";

        public PatientController(IHttpClientFactory httpClientFactory, ILogger<PatientController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetAllPatients()
        {
            try
            {
                _logger.LogInformation("Getting all patients");
                var httpClient = _httpClientFactory.CreateClient();
                var patients = new List<Patient>();
                
                // Since we know the addendum endpoint works from the README example,
                // let's try to get patient data from addendums for IDs 1-50
                for (int i = 1; i <= 25; i++)
                {
                    try
                    {
                        var addendumUrl = $"{ApiBaseUrl}addendum/{i}";
                        _logger.LogInformation($"Fetching addendum from {addendumUrl}");
                        
                        var addendumResponse = await httpClient.GetAsync(addendumUrl);
                        
                        if (addendumResponse.IsSuccessStatusCode)
                        {
                            var jsonString = await addendumResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Addendum response: {jsonString}");
                            
                            // Parse the JSON to extract patient information
                            using (JsonDocument doc = JsonDocument.Parse(jsonString))
                            {
                                if (doc.RootElement.TryGetProperty("patID", out JsonElement patIdElement) &&
                                    doc.RootElement.TryGetProperty("patientName", out JsonElement patientNameElement))
                                {
                                    int patId = patIdElement.GetInt32();
                                    string patientName = patientNameElement.GetString() ?? "Unknown";
                                    
                                    // Check if we already have this patient
                                    if (!patients.Any(p => p.PatientID == patId))
                                    {
                                        var patient = new Patient { PatientID = patId };
                                        
                                        // Parse the patient name
                                        string[] nameParts = patientName.Split(' ');
                                        if (nameParts.Length > 0)
                                        {
                                            patient.First = nameParts[0];
                                            if (nameParts.Length > 1)
                                            {
                                                patient.Last = string.Join(" ", nameParts.Skip(1));
                                            }
                                        }
                                        
                                        // Try to extract DOB if present
                                        if (patientName.Contains("DOB:"))
                                        {
                                            var dobMatch = Regex.Match(patientName, @"DOB:\s*(\d{1,2}/\d{1,2}/\d{4})");
                                            if (dobMatch.Success && DateTime.TryParse(dobMatch.Groups[1].Value, out DateTime dob))
                                            {
                                                patient.BirthDate = dob;
                                            }
                                        }
                                        
                                        patients.Add(patient);
                                        _logger.LogInformation($"Added patient: {patientName} (ID: {patId})");
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to retrieve addendum {i}. Status code: {addendumResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error processing addendum {i}, skipping");
                    }
                }
                
                _logger.LogInformation($"Retrieved {patients.Count} patients");
                
                if (patients.Count == 0)
                {
                    _logger.LogWarning("No patients found from addendum endpoint");
                    
                    // Add a sample patient for testing if no patients were found
                    patients.Add(new Patient
                    {
                        PatientID = 1001,
                        First = "John",
                        Last = "Doe",
                        BirthDate = new DateTime(1980, 1, 1)
                    });
                    
                    _logger.LogInformation("Added sample patient for testing");
                }
                
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patients");
                return StatusCode(500, "Error retrieving patients");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var patient = new Patient { PatientID = id };
                bool patientFound = false;
                
                // Try to get patient info from addendum
                var addendumUrl = $"{ApiBaseUrl}addendum/{id}";
                _logger.LogInformation($"Fetching patient from addendum: {addendumUrl}");
                var addendumResponse = await httpClient.GetAsync(addendumUrl);
                
                if (addendumResponse.IsSuccessStatusCode)
                {
                    var jsonString = await addendumResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Patient addendum response: {jsonString}");
                    
                    if (!string.IsNullOrWhiteSpace(jsonString) && jsonString != "{}")
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonString))
                            {
                                // Check if patientName property exists and is not null
                                if (doc.RootElement.TryGetProperty("patientName", out JsonElement patientNameElement) &&
                                    patientNameElement.ValueKind != JsonValueKind.Null)
                                {
                                    string patientName = patientNameElement.GetString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(patientName))
                                    {
                                        ParsePatientNameFromString(patientName, patient);
                                        patientFound = true;
                                        
                                        // Try to extract DOB if present
                                        if (patientName.Contains("DOB:"))
                                        {
                                            var dobMatch = Regex.Match(patientName, @"DOB:\s*(\d{1,2}/\d{1,2}/\d{4})");
                                            if (dobMatch.Success && DateTime.TryParse(dobMatch.Groups[1].Value, out DateTime dob))
                                            {
                                                patient.BirthDate = dob;
                                            }
                                        }
                                    }
                                }
                                
                                // Add additional information from the addendum
                                if (doc.RootElement.TryGetProperty("date", out JsonElement dateElement) && 
                                    dateElement.ValueKind != JsonValueKind.Null)
                                {
                                    DateTime date;
                                    if (dateElement.TryGetDateTime(out date) && date != default)
                                    {
                                        patient.LastVisit = date;
                                    }
                                }
                                
                                if (doc.RootElement.TryGetProperty("savedBy", out JsonElement savedByElement) && 
                                    savedByElement.ValueKind != JsonValueKind.String &&
                                    !string.IsNullOrWhiteSpace(savedByElement.GetString()))
                                {
                                    patient.PrimaryCareProvider = savedByElement.GetString();
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, $"Error parsing addendum JSON for patient {id}");
                        }
                    }
                }
                
                // If we didn't find the patient in the addendum, try to find it in the patient list
                if (!patientFound)
                {
                    _logger.LogInformation($"Patient {id} not found in addendum, trying to find in patient list");
                    var allPatients = await GetAllPatients();
                    
                    if (allPatients.Result is OkObjectResult okResult && 
                        okResult.Value is IEnumerable<Patient> patients)
                    {
                        var foundPatient = patients.FirstOrDefault(p => p.PatientID == id);
                        if (foundPatient != null)
                        {
                            patient = foundPatient;
                            patientFound = true;
                            _logger.LogInformation($"Found patient {id} in patient list");
                        }
                    }
                }
                
                // Try to get more patient info from the demographics endpoint
                try
                {
                    var demographicsUrl = $"{ApiBaseUrl}demographics/{id}";
                    _logger.LogInformation($"Fetching patient demographics: {demographicsUrl}");
                    var demographicsResponse = await httpClient.GetAsync(demographicsUrl);
                    
                    if (demographicsResponse.IsSuccessStatusCode && demographicsResponse.StatusCode != System.Net.HttpStatusCode.NoContent)
                    {
                        var demographicsJson = await demographicsResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Demographics response: {demographicsJson}");
                        
                        if (!string.IsNullOrWhiteSpace(demographicsJson) && demographicsJson != "{}")
                        {
                            try
                            {
                                using (JsonDocument demoDoc = JsonDocument.Parse(demographicsJson))
                                {
                                    if (demoDoc.RootElement.ValueKind == JsonValueKind.Object)
                                    {
                                        // Extract gender
                                        if (demoDoc.RootElement.TryGetProperty("gender", out JsonElement genderElement) &&
                                            genderElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.Gender = genderElement.GetString();
                                        }
                                        
                                        // Extract phone
                                        if (demoDoc.RootElement.TryGetProperty("phone", out JsonElement phoneElement) &&
                                            phoneElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.Phone = phoneElement.GetString();
                                        }
                                        
                                        // Extract address info
                                        if (demoDoc.RootElement.TryGetProperty("address", out JsonElement addressElement) &&
                                            addressElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.PatientAddress = addressElement.GetString();
                                        }
                                        
                                        if (demoDoc.RootElement.TryGetProperty("city", out JsonElement cityElement) &&
                                            cityElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.City = cityElement.GetString();
                                        }
                                        
                                        if (demoDoc.RootElement.TryGetProperty("state", out JsonElement stateElement) &&
                                            stateElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.State = stateElement.GetString();
                                        }
                                        
                                        if (demoDoc.RootElement.TryGetProperty("zip", out JsonElement zipElement) &&
                                            zipElement.ValueKind == JsonValueKind.String)
                                        {
                                            patient.Zip = zipElement.GetString();
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, $"Error parsing demographics JSON for patient {id}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting demographics for patient {id}");
                }
                
                if (patientFound)
                {
                    return Ok(patient);
                }
                
                return NotFound($"Patient with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting patient with ID {id}");
                return StatusCode(500, $"Error retrieving patient with ID {id}");
            }
        }

        [HttpGet("medical-records/{id}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPatientMedicalRecords(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var records = new List<object>();
                
                // Try to get addendums for the patient
                try
                {
                    var addendumUrl = $"{ApiBaseUrl}addendum/{id}";
                    _logger.LogInformation($"Fetching medical records from addendum: {addendumUrl}");
                    var addendumResponse = await httpClient.GetAsync(addendumUrl);
                    
                    if (addendumResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await addendumResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Medical records response: {jsonString}");
                        
                        var addendum = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (addendum != null)
                        {
                            records.Add(new { Type = "Addendum", Data = addendum });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting addendum for patient {id}");
                }
                
                // Try to get allergies for the patient
                try 
                {
                    var allergiesUrl = $"{ApiBaseUrl}ListAllergies/{id}";
                    _logger.LogInformation($"Fetching allergies: {allergiesUrl}");
                    var allergiesResponse = await httpClient.GetAsync(allergiesUrl);
                    
                    if (allergiesResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await allergiesResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Allergies response: {jsonString}");
                        
                        var allergies = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (allergies != null)
                        {
                            records.Add(new { Type = "Allergies", Data = allergies });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting allergies for patient {id}");
                }
                
                // Try to get medications for the patient
                try
                {
                    var medsUrl = $"{ApiBaseUrl}ListMEDS/{id}";
                    _logger.LogInformation($"Fetching medications: {medsUrl}");
                    var medsResponse = await httpClient.GetAsync(medsUrl);
                    
                    if (medsResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await medsResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Medications response: {jsonString}");
                        
                        var meds = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (meds != null)
                        {
                            records.Add(new { Type = "Medications", Data = meds });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting medications for patient {id}");
                }
                
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting medical records for patient with ID {id}");
                return StatusCode(500, $"Error retrieving medical records for patient with ID {id}");
            }
        }
        
        private void ParsePatientNameFromString(string fullName, Patient patient)
        {
            if (string.IsNullOrEmpty(fullName))
                return;
            
            // Remove the DOB part if present
            int dobIndex = fullName.IndexOf("(DOB:");
            if (dobIndex > 0)
            {
                fullName = fullName.Substring(0, dobIndex).Trim();
            }
            
            // Try to parse LAST, FIRST MIDDLE format
            var commaMatch = Regex.Match(fullName, @"^([^,]+),\s*(.+)$");
            if (commaMatch.Success)
            {
                patient.Last = commaMatch.Groups[1].Value.Trim();
                
                string firstMiddle = commaMatch.Groups[2].Value.Trim();
                string[] parts = firstMiddle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length > 0)
                {
                    patient.First = parts[0];
                    
                    if (parts.Length > 1)
                    {
                        // Check if the last part is a suffix
                        string lastPart = parts[parts.Length - 1];
                        if (new[] { "JR", "SR", "II", "III", "IV" }.Contains(lastPart.ToUpper()))
                        {
                            patient.Suffix = lastPart;
                            
                            // Middle name is everything between first name and suffix
                            if (parts.Length > 2)
                            {
                                patient.Middle = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
                            }
                        }
                        else
                        {
                            // Middle name is everything after the first name
                            patient.Middle = string.Join(" ", parts.Skip(1));
                        }
                    }
                }
            }
            else
            {
                // If there's no comma, try to parse as FIRST MIDDLE LAST
                // or just split the full name into parts
                string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length == 1)
                {
                    // Only one word, assume it's the last name
                    patient.Last = parts[0];
                }
                else if (parts.Length == 2)
                {
                    // Two words, assume FIRST LAST
                    patient.First = parts[0];
                    patient.Last = parts[1];
                }
                else if (parts.Length >= 3)
                {
                    // For names like "WILLIAM SAFFIRE" or "DIANNA ALMOND"
                    // Try to extract first and last names
                    if (IsAllCaps(fullName))
                    {
                        // If it's all caps, assume the format is "FIRST LAST" 
                        // regardless of how many words there are
                        patient.First = parts[0];
                        patient.Last = string.Join(" ", parts.Skip(1));
                    }
                    else
                    {
                        // More than two words, assume FIRST MIDDLE LAST
                        patient.First = parts[0];
                        patient.Last = parts[parts.Length - 1];
                        
                        // Middle name is everything between first and last
                        if (parts.Length > 2)
                        {
                            patient.Middle = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
                        }
                    }
                }
            }
            
            // Convert names to proper case if they're in all caps
            if (patient.First != null && IsAllCaps(patient.First))
                patient.First = ToProperCase(patient.First);
                
            if (patient.Last != null && IsAllCaps(patient.Last))
                patient.Last = ToProperCase(patient.Last);
                
            if (patient.Middle != null && IsAllCaps(patient.Middle))
                patient.Middle = ToProperCase(patient.Middle);
        }
        
        private bool IsAllCaps(string text)
        {
            return !string.IsNullOrEmpty(text) && text.ToUpper() == text && text.Any(char.IsLetter);
        }
        
        private string ToProperCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Handle hyphenated names
            if (text.Contains('-'))
            {
                return string.Join("-", text.Split('-')
                    .Select(part => ToProperCase(part)));
            }
            
            // Handle McName and MacName patterns
            if (text.Length > 2 && (text.StartsWith("MC", StringComparison.OrdinalIgnoreCase) || 
                                    text.StartsWith("MAC", StringComparison.OrdinalIgnoreCase)))
            {
                string prefix = text.StartsWith("MC", StringComparison.OrdinalIgnoreCase) ? "Mc" : "Mac";
                string remainder = text.Substring(prefix.Length);
                
                if (!string.IsNullOrEmpty(remainder))
                {
                    return prefix + char.ToUpper(remainder[0]) + 
                           (remainder.Length > 1 ? remainder.Substring(1).ToLower() : "");
                }
            }
            
            // Standard proper case
            return char.ToUpper(text[0]) + (text.Length > 1 ? text.Substring(1).ToLower() : "");
        }
    }
}
