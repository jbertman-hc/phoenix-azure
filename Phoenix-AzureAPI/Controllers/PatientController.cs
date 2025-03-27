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
                _logger.LogInformation("Getting all patients from PatientIndex");
                var httpClient = _httpClientFactory.CreateClient();
                var patients = new Dictionary<int, Patient>();
                
                // We'll use the PatientIndex endpoint to get a list of patients
                // This endpoint might return a list of patient IDs that we can use to fetch details
                var patientIndexUrl = $"{ApiBaseUrl}PatientIndex";
                _logger.LogInformation($"Fetching patient index: {patientIndexUrl}");
                
                try
                {
                    var patientIndexResponse = await httpClient.GetAsync(patientIndexUrl);
                    
                    if (patientIndexResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await patientIndexResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Patient index response received");
                        
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonString))
                            {
                                // Process the patient index response
                                // The exact structure depends on the API response format
                                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var patientElement in doc.RootElement.EnumerateArray())
                                    {
                                        try
                                        {
                                            // Extract patient ID from the index
                                            if (patientElement.TryGetProperty("patID", out JsonElement patIdElement) &&
                                                patIdElement.ValueKind == JsonValueKind.Number)
                                            {
                                                int patientId = patIdElement.GetInt32();
                                                
                                                // Skip if we already have this patient
                                                if (patients.ContainsKey(patientId))
                                                {
                                                    continue;
                                                }
                                                
                                                // Fetch patient demographics
                                                await FetchPatientDemographics(httpClient, patientId, patients);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Error processing patient index element");
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Error parsing patient index JSON");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to retrieve patient index. Status code: {patientIndexResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting patient index");
                }
                
                // If we couldn't get any patients from PatientIndex, try using addendum records
                if (patients.Count == 0)
                {
                    _logger.LogInformation("No patients found from PatientIndex, trying addendum records");
                    await GetPatientsFromAddendum(httpClient, patients);
                }
                
                if (patients.Count == 0)
                {
                    _logger.LogWarning("No patients found from any API endpoints");
                }
                
                _logger.LogInformation($"Returning {patients.Count} patients");
                return Ok(patients.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patients");
                return StatusCode(500, "Error retrieving patients");
            }
        }
        
        private async Task FetchPatientDemographics(HttpClient httpClient, int patientId, Dictionary<int, Patient> patients)
        {
            try
            {
                var demographicsUrl = $"{ApiBaseUrl}Demographics/{patientId}";
                _logger.LogInformation($"Fetching demographics for patient {patientId}: {demographicsUrl}");
                
                var demographicsResponse = await httpClient.GetAsync(demographicsUrl);
                
                if (demographicsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await demographicsResponse.Content.ReadAsStringAsync();
                    
                    if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "{}")
                    {
                        _logger.LogWarning($"Empty demographics response for patient {patientId}");
                        return;
                    }
                    
                    _logger.LogInformation($"Demographics response received for patient {patientId}");
                    
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            var patient = new Patient { PatientID = patientId };
                            
                            // Extract first name
                            if (doc.RootElement.TryGetProperty("firstName", out JsonElement firstNameElement) &&
                                firstNameElement.ValueKind == JsonValueKind.String)
                            {
                                patient.First = firstNameElement.GetString();
                            }
                            
                            // Extract last name
                            if (doc.RootElement.TryGetProperty("lastName", out JsonElement lastNameElement) &&
                                lastNameElement.ValueKind == JsonValueKind.String)
                            {
                                patient.Last = lastNameElement.GetString();
                            }
                            
                            // Extract gender
                            if (doc.RootElement.TryGetProperty("gender", out JsonElement genderElement) &&
                                genderElement.ValueKind == JsonValueKind.String)
                            {
                                patient.Gender = genderElement.GetString();
                            }
                            
                            // Extract birth date
                            if (doc.RootElement.TryGetProperty("dob", out JsonElement dobElement) &&
                                dobElement.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(dobElement.GetString(), out DateTime birthDate))
                                {
                                    patient.BirthDate = birthDate;
                                }
                            }
                            
                            // Extract address
                            if (doc.RootElement.TryGetProperty("address", out JsonElement addressElement) &&
                                addressElement.ValueKind == JsonValueKind.String)
                            {
                                patient.PatientAddress = addressElement.GetString();
                            }
                            
                            // Extract city
                            if (doc.RootElement.TryGetProperty("city", out JsonElement cityElement) &&
                                cityElement.ValueKind == JsonValueKind.String)
                            {
                                patient.City = cityElement.GetString();
                            }
                            
                            // Extract state
                            if (doc.RootElement.TryGetProperty("state", out JsonElement stateElement) &&
                                stateElement.ValueKind == JsonValueKind.String)
                            {
                                patient.State = stateElement.GetString();
                            }
                            
                            // Extract zip
                            if (doc.RootElement.TryGetProperty("zip", out JsonElement zipElement) &&
                                zipElement.ValueKind == JsonValueKind.String)
                            {
                                patient.Zip = zipElement.GetString();
                            }
                            
                            // Extract phone
                            if (doc.RootElement.TryGetProperty("phone", out JsonElement phoneElement) &&
                                phoneElement.ValueKind == JsonValueKind.String)
                            {
                                patient.Phone = phoneElement.GetString();
                            }
                            
                            // Extract email
                            if (doc.RootElement.TryGetProperty("email", out JsonElement emailElement) &&
                                emailElement.ValueKind == JsonValueKind.String)
                            {
                                patient.Email = emailElement.GetString();
                            }
                            
                            // Only add the patient if we have a valid ID and at least a name
                            if (patient.PatientID > 0 && (!string.IsNullOrWhiteSpace(patient.First) || !string.IsNullOrWhiteSpace(patient.Last)))
                            {
                                patients[patientId] = patient;
                                _logger.LogInformation($"Added patient from demographics: ID={patient.PatientID}, Name={patient.First} {patient.Last}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, $"Error parsing demographics JSON for patient {patientId}");
                    }
                }
                else if (demographicsResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Demographics not found for patient {patientId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to retrieve demographics for patient {patientId}. Status code: {demographicsResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error getting demographics for patient {patientId}");
            }
        }
        
        private async Task GetPatientsFromAddendum(HttpClient httpClient, Dictionary<int, Patient> patients)
        {
            // Fetch patient information from addendum records
            // We'll check the first 20 addendum records to find unique patients
            for (int i = 1; i <= 20; i++)
            {
                try
                {
                    var addendumUrl = $"{ApiBaseUrl}addendum/{i}";
                    _logger.LogInformation($"Fetching addendum {i}: {addendumUrl}");
                    
                    var addendumResponse = await httpClient.GetAsync(addendumUrl);
                    
                    if (addendumResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await addendumResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Addendum {i} response received");
                        
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonString))
                            {
                                if (doc.RootElement.TryGetProperty("patID", out JsonElement patIdElement) &&
                                    patIdElement.ValueKind == JsonValueKind.Number)
                                {
                                    int patientId = patIdElement.GetInt32();
                                    
                                    // Skip if we already have this patient
                                    if (patients.ContainsKey(patientId))
                                    {
                                        _logger.LogInformation($"Patient {patientId} already in list, skipping");
                                        continue;
                                    }
                                    
                                    // Create a new patient
                                    var patient = new Patient { PatientID = patientId };
                                    
                                    // Extract patient name and DOB from the patientName field
                                    if (doc.RootElement.TryGetProperty("patientName", out JsonElement nameElement) &&
                                        nameElement.ValueKind == JsonValueKind.String)
                                    {
                                        string fullNameWithDob = nameElement.GetString() ?? "";
                                        
                                        if (!string.IsNullOrWhiteSpace(fullNameWithDob))
                                        {
                                            // Parse patient name (format: "LASTNAME, FIRSTNAME (DOB: MM/DD/YYYY)")
                                            ParsePatientNameFromAddendum(fullNameWithDob, patient);
                                        }
                                    }
                                    
                                    // Only add the patient if we have a valid ID
                                    if (patient.PatientID > 0)
                                    {
                                        patients[patientId] = patient;
                                        _logger.LogInformation($"Added patient from addendum {i}: ID={patient.PatientID}, Name={patient.First} {patient.Last}");
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, $"Error parsing addendum {i} JSON");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to retrieve addendum {i}. Status code: {addendumResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting addendum {i}");
                }
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            try
            {
                _logger.LogInformation($"Getting patient with ID: {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var patient = new Patient { PatientID = id };
                bool patientFound = false;
                
                // Try to get patient info from demographics endpoint directly
                try
                {
                    var demographicsUrl = $"{ApiBaseUrl}Demographics/{id}";
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
                                    // Extract patient name
                                    if (demoDoc.RootElement.TryGetProperty("firstName", out JsonElement firstNameElement) &&
                                        firstNameElement.ValueKind == JsonValueKind.String)
                                    {
                                        patient.First = firstNameElement.GetString();
                                    }
                                    
                                    if (demoDoc.RootElement.TryGetProperty("lastName", out JsonElement lastNameElement) &&
                                        lastNameElement.ValueKind == JsonValueKind.String)
                                    {
                                        patient.Last = lastNameElement.GetString();
                                    }
                                    
                                    // Extract gender
                                    if (demoDoc.RootElement.TryGetProperty("gender", out JsonElement genderElement) &&
                                        genderElement.ValueKind == JsonValueKind.String)
                                    {
                                        patient.Gender = genderElement.GetString();
                                    }
                                    
                                    // Extract birth date
                                    if (demoDoc.RootElement.TryGetProperty("dob", out JsonElement birthDateElement) &&
                                        birthDateElement.ValueKind == JsonValueKind.String)
                                    {
                                        if (DateTime.TryParse(birthDateElement.GetString(), out DateTime birthDate))
                                        {
                                            patient.BirthDate = birthDate;
                                        }
                                    }
                                    
                                    // Extract phone
                                    if (demoDoc.RootElement.TryGetProperty("phone", out JsonElement phoneElement) &&
                                        phoneElement.ValueKind == JsonValueKind.String)
                                    {
                                        patient.Phone = phoneElement.GetString();
                                    }
                                    
                                    // Extract email
                                    if (demoDoc.RootElement.TryGetProperty("email", out JsonElement emailElement) &&
                                        emailElement.ValueKind == JsonValueKind.String)
                                    {
                                        patient.Email = emailElement.GetString();
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
                
                // If we didn't find the patient in demographics, try to find it in the patient list
                if (!patientFound)
                {
                    _logger.LogInformation($"Patient {id} not found in demographics, trying to find in patient list");
                    var allPatientsResult = await GetAllPatients();
                    
                    if (allPatientsResult.Result is OkObjectResult okResult && 
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
                    var medicationsUrl = $"{ApiBaseUrl}ListMEDS/{id}";
                    _logger.LogInformation($"Fetching medications: {medicationsUrl}");
                    var medicationsResponse = await httpClient.GetAsync(medicationsUrl);
                    
                    if (medicationsResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await medicationsResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Medications response: {jsonString}");
                        
                        var medications = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (medications != null)
                        {
                            records.Add(new { Type = "Medications", Data = medications });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting medications for patient {id}");
                }
                
                // Try to get problems for the patient
                try
                {
                    var problemsUrl = $"{ApiBaseUrl}ListProblem/{id}";
                    _logger.LogInformation($"Forwarding request to ListProblem endpoint for patient {id}");
                    var problemsResponse = await httpClient.GetAsync(problemsUrl);
                    
                    if (problemsResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await problemsResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Successfully retrieved problems for patient {id}");
                        
                        var problems = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (problems != null)
                        {
                            records.Add(new { Type = "Problems", Data = problems });
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to retrieve problems for patient {id}. Status code: {problemsResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error forwarding request to ListProblem endpoint for patient {id}");
                }
                
                // Try to get notes for the patient
                try
                {
                    var notesUrl = $"{ApiBaseUrl}notes/{id}";
                    _logger.LogInformation($"Fetching notes: {notesUrl}");
                    var notesResponse = await httpClient.GetAsync(notesUrl);
                    
                    if (notesResponse.IsSuccessStatusCode)
                    {
                        var jsonString = await notesResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Notes response: {jsonString}");
                        
                        var notes = JsonSerializer.Deserialize<object>(jsonString);
                        
                        if (notes != null)
                        {
                            records.Add(new { Type = "Notes", Data = notes });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error getting notes for patient {id}");
                }
                
                _logger.LogInformation($"Retrieved {records.Count} medical record types for patient {id}");
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting medical records for patient {id}");
                return StatusCode(500, $"Error retrieving medical records for patient {id}");
            }
        }
        
        // Add a new method to forward requests to the Demographics endpoint
        [HttpGet]
        [Route("/api/Demographics/{id}")]
        public async Task<IActionResult> GetDemographics(int id)
        {
            try
            {
                _logger.LogInformation($"Forwarding request to Demographics endpoint for patient {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}Demographics/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully retrieved demographics for patient {id}");
                    return Content(content, "application/json");
                }
                
                _logger.LogWarning($"Failed to retrieve demographics for patient {id}. Status code: {response.StatusCode}");
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error forwarding request to Demographics endpoint for patient {id}");
                return StatusCode(500, "Error retrieving demographics");
            }
        }
        
        // Add a new method to forward requests to the ListAllergies endpoint
        [HttpGet]
        [Route("/api/ListAllergies/{id}")]
        public async Task<IActionResult> GetAllergies(int id)
        {
            try
            {
                _logger.LogInformation($"Forwarding request to ListAllergies endpoint for patient {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}ListAllergies/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully retrieved allergies for patient {id}");
                    return Content(content, "application/json");
                }
                
                _logger.LogWarning($"Failed to retrieve allergies for patient {id}. Status code: {response.StatusCode}");
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error forwarding request to ListAllergies endpoint for patient {id}");
                return StatusCode(500, "Error retrieving allergies");
            }
        }
        
        // Add a new method to forward requests to the ListMedications endpoint
        [HttpGet]
        [Route("/api/ListMEDS/{id}")]
        public async Task<IActionResult> GetMedications(int id)
        {
            try
            {
                _logger.LogInformation($"Forwarding request to ListMEDS endpoint for patient {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}ListMEDS/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully retrieved medications for patient {id}");
                    return Content(content, "application/json");
                }
                
                _logger.LogWarning($"Failed to retrieve medications for patient {id}. Status code: {response.StatusCode}");
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error forwarding request to ListMEDS endpoint for patient {id}");
                return StatusCode(500, "Error retrieving medications");
            }
        }
        
        // Add a new method to forward requests to the ListProblem endpoint
        [HttpGet]
        [Route("/api/ListProblem/{id}")]
        public async Task<IActionResult> GetProblems(int id)
        {
            try
            {
                _logger.LogInformation($"Forwarding request to ListProblem endpoint for patient {id}");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}ListProblem/{id}";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully retrieved problems for patient {id}");
                    return Content(content, "application/json");
                }
                
                _logger.LogWarning($"Failed to retrieve problems for patient {id}. Status code: {response.StatusCode}");
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error forwarding request to ListProblem endpoint for patient {id}");
                return StatusCode(500, "Error retrieving problems");
            }
        }
        
        // Add a new method to forward requests to the PatientIndex endpoint
        [HttpGet]
        [Route("/api/PatientIndex")]
        public async Task<IActionResult> GetPatientIndex()
        {
            try
            {
                _logger.LogInformation("Forwarding request to PatientIndex endpoint");
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{ApiBaseUrl}PatientIndex";
                
                _logger.LogInformation($"Requesting: {url}");
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully retrieved patient index");
                    return Content(content, "application/json");
                }
                
                _logger.LogWarning($"Failed to retrieve patient index. Status code: {response.StatusCode}");
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding request to PatientIndex endpoint");
                return StatusCode(500, "Error retrieving patient index");
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
        
        private void ParsePatientNameFromAddendum(string fullNameWithDob, Patient patient)
        {
            try
            {
                // Format: "LASTNAME, FIRSTNAME (DOB: MM/DD/YYYY)"
                int commaIndex = fullNameWithDob.IndexOf(',');
                if (commaIndex > 0)
                {
                    patient.Last = fullNameWithDob.Substring(0, commaIndex).Trim();
                    
                    int dobIndex = fullNameWithDob.IndexOf("(DOB:", commaIndex);
                    if (dobIndex > commaIndex)
                    {
                        patient.First = fullNameWithDob.Substring(commaIndex + 1, dobIndex - commaIndex - 1).Trim();
                        
                        // Extract DOB
                        int dobStartIndex = dobIndex + 6; // Length of "(DOB: "
                        int dobEndIndex = fullNameWithDob.IndexOf(')', dobStartIndex);
                        if (dobEndIndex > dobStartIndex)
                        {
                            string dobString = fullNameWithDob.Substring(dobStartIndex, dobEndIndex - dobStartIndex).Trim();
                            if (DateTime.TryParse(dobString, out DateTime dob))
                            {
                                patient.BirthDate = dob;
                            }
                        }
                    }
                    else
                    {
                        patient.First = fullNameWithDob.Substring(commaIndex + 1).Trim();
                    }
                }
                else
                {
                    // If no comma found, just use the whole string as the name
                    patient.Last = fullNameWithDob.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error parsing patient name from addendum: {fullNameWithDob}");
            }
        }
    }
}
