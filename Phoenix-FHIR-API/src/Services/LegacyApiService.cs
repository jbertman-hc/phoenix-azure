using System.Text.Json;
using Phoenix_FHIR_API.Models;

namespace Phoenix_FHIR_API.Services
{
    /// <summary>
    /// Service for accessing the legacy healthcare API
    /// </summary>
    public class LegacyApiService : ILegacyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseApiUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public LegacyApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseApiUrl = _configuration["LegacyApi:BaseUrl"] ?? "https://apiserviceswin20250318.azurewebsites.net/api/";
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get patient demographics by ID
        /// </summary>
        public async Task<DemographicsDomain?> GetPatientByIdAsync(int patientId)
        {
            var url = $"{_baseApiUrl}Demographics/{patientId}";
            Console.WriteLine($"Requesting URL: {url}");
            
            try
            {
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    // Check if the response is 204 No Content
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Console.WriteLine($"No content returned for patient ID {patientId}");
                        return null;
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response content: {content}");
                    
                    if (string.IsNullOrEmpty(content))
                    {
                        Console.WriteLine("Response content is empty");
                        return null;
                    }
                    
                    var result = JsonSerializer.Deserialize<DemographicsDomain>(content, _jsonOptions);
                    Console.WriteLine($"Deserialized patient: {result?.patientID}, {result?.firstName} {result?.lastName}");
                    return result;
                }
                else
                {
                    Console.WriteLine($"Error response: {await response.Content.ReadAsStringAsync()}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Get patient allergies by patient ID
        /// </summary>
        public async Task<List<ListAllergiesDomain>> GetPatientAllergiesAsync(int patientId)
        {
            var url = $"{_baseApiUrl}ListAllergies/{patientId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ListAllergiesDomain>>(content, _jsonOptions) ?? new List<ListAllergiesDomain>();
            }
            
            return new List<ListAllergiesDomain>();
        }

        /// <summary>
        /// Get patient medications by patient ID
        /// </summary>
        public async Task<List<ListMedsDomain>> GetPatientMedicationsAsync(int patientId)
        {
            var url = $"{_baseApiUrl}ListMEDS/{patientId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ListMedsDomain>>(content, _jsonOptions) ?? new List<ListMedsDomain>();
            }
            
            return new List<ListMedsDomain>();
        }

        /// <summary>
        /// Get patient problems by patient ID
        /// </summary>
        public async Task<List<ListProblemDomain>> GetPatientProblemsAsync(int patientId)
        {
            var url = $"{_baseApiUrl}ListProblem/{patientId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ListProblemDomain>>(content, _jsonOptions) ?? new List<ListProblemDomain>();
            }
            
            return new List<ListProblemDomain>();
        }

        /// <summary>
        /// Get patient clinical notes by patient ID
        /// </summary>
        public async Task<List<SOAPDomain>> GetPatientClinicalNotesAsync(int patientId)
        {
            var url = $"{_baseApiUrl}SOAP/{patientId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<SOAPDomain>>(content, _jsonOptions) ?? new List<SOAPDomain>();
            }
            
            return new List<SOAPDomain>();
        }

        /// <summary>
        /// Get provider by ID
        /// </summary>
        public async Task<ProviderIndexDomain?> GetProviderByIdAsync(int providerId)
        {
            var url = $"{_baseApiUrl}ProviderIndex/{providerId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProviderIndexDomain>(content, _jsonOptions);
            }
            
            return null;
        }

        /// <summary>
        /// Get all providers
        /// </summary>
        public async Task<List<ProviderIndexDomain>> GetAllProvidersAsync()
        {
            var url = $"{_baseApiUrl}ProviderIndex";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ProviderIndexDomain>>(content, _jsonOptions) ?? new List<ProviderIndexDomain>();
            }
            
            return new List<ProviderIndexDomain>();
        }

        /// <summary>
        /// Get practice information by ID
        /// </summary>
        public async Task<PracticeInfoDomain?> GetPracticeInfoAsync(int practiceId)
        {
            var url = $"{_baseApiUrl}PracticeInfo/{practiceId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PracticeInfoDomain>(content, _jsonOptions);
            }
            
            return null;
        }

        /// <summary>
        /// Get all locations
        /// </summary>
        public async Task<List<LocationsDomain>> GetLocationsAsync()
        {
            var url = $"{_baseApiUrl}Locations";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<LocationsDomain>>(content, _jsonOptions) ?? new List<LocationsDomain>();
            }
            
            return new List<LocationsDomain>();
        }

        /// <summary>
        /// Get location by ID
        /// </summary>
        public async Task<LocationsDomain?> GetLocationByIdAsync(string locationId)
        {
            var url = $"{_baseApiUrl}Locations/{locationId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LocationsDomain>(content, _jsonOptions);
            }
            
            return null;
        }

        /// <summary>
        /// Get all facilities
        /// </summary>
        public async Task<List<FacilitiesDomain>> GetFacilitiesAsync()
        {
            var url = $"{_baseApiUrl}Facilities";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<FacilitiesDomain>>(content, _jsonOptions) ?? new List<FacilitiesDomain>();
            }
            
            return new List<FacilitiesDomain>();
        }

        /// <summary>
        /// Get facility by ID
        /// </summary>
        public async Task<FacilitiesDomain?> GetFacilityByIdAsync(string facilityId)
        {
            var url = $"{_baseApiUrl}Facilities/{facilityId}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FacilitiesDomain>(content, _jsonOptions);
            }
            
            return null;
        }

        /// <summary>
        /// Get patient appointments by patient ID
        /// </summary>
        public async Task<List<SchedulingDomain>> GetPatientAppointmentsAsync(int patientId)
        {
            var url = $"{_baseApiUrl}Scheduling";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var allAppointments = JsonSerializer.Deserialize<List<SchedulingDomain>>(content, _jsonOptions) ?? new List<SchedulingDomain>();
                return allAppointments.Where(a => a.patientID == patientId).ToList();
            }
            
            return new List<SchedulingDomain>();
        }

        /// <summary>
        /// Get all patients
        /// </summary>
        public async Task<List<DemographicsDomain>> GetAllPatientsAsync()
        {
            var patients = new Dictionary<int, DemographicsDomain>();
            
            // Strategy 1: Try to get patients from PatientIndex endpoint
            await GetPatientsFromPatientIndex(patients);
            
            // Strategy 2: If no patients found, try specific patient IDs
            if (patients.Count == 0)
            {
                await GetPatientsFromSpecificIds(patients);
            }
            
            // Strategy 3: If still no patients found, try addendum records
            if (patients.Count == 0)
            {
                await GetPatientsFromAddendum(patients);
            }
            
            return patients.Values.ToList();
        }
        
        /// <summary>
        /// Get patients from the PatientIndex endpoint
        /// </summary>
        private async Task GetPatientsFromPatientIndex(Dictionary<int, DemographicsDomain> patients)
        {
            try
            {
                var url = $"{_baseApiUrl}PatientIndex";
                Console.WriteLine($"Requesting URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response content: {content}");
                    
                    if (!string.IsNullOrWhiteSpace(content) && content != "[]" && content != "{}")
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(content))
                            {
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
                                                var patient = await GetPatientByIdAsync(patientId);
                                                if (patient != null)
                                                {
                                                    patients[patientId] = patient;
                                                    Console.WriteLine($"Added patient from PatientIndex: ID={patientId}");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error processing patient index element: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error parsing patient index JSON: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve patient index. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting patient index: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get patients from specific IDs
        /// </summary>
        private async Task GetPatientsFromSpecificIds(Dictionary<int, DemographicsDomain> patients)
        {
            // Try a range of patient IDs
            int[] patientIds = new int[] { 
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 
                1001, 1002, 1003, 1004, 1005, 
                2001, 2002, 2003, 2004, 2005,
                3001, 3002, 3003, 3004, 3005,
                4001, 4002, 4003, 4004, 4005,
                5001, 5002, 5003, 5004, 5005
            };
            
            foreach (var patientId in patientIds)
            {
                try
                {
                    // Skip if we already have this patient
                    if (patients.ContainsKey(patientId))
                    {
                        continue;
                    }
                    
                    var patient = await GetPatientByIdAsync(patientId);
                    if (patient != null)
                    {
                        patients[patientId] = patient;
                        Console.WriteLine($"Added patient from specific ID: ID={patientId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting patient {patientId}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get patients from addendum records
        /// </summary>
        private async Task GetPatientsFromAddendum(Dictionary<int, DemographicsDomain> patients)
        {
            // Fetch patient information from addendum records
            // We'll check the first 20 addendum records to find unique patients
            for (int i = 1; i <= 20; i++)
            {
                try
                {
                    var url = $"{_baseApiUrl}addendum/{i}";
                    Console.WriteLine($"Requesting URL: {url}");
                    
                    var response = await _httpClient.GetAsync(url);
                    Console.WriteLine($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response content: {content}");
                        
                        if (!string.IsNullOrWhiteSpace(content) && content != "{}")
                        {
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(content))
                                {
                                    if (doc.RootElement.TryGetProperty("patID", out JsonElement patIdElement) &&
                                        patIdElement.ValueKind == JsonValueKind.Number)
                                    {
                                        int patientId = patIdElement.GetInt32();
                                        
                                        // Skip if we already have this patient
                                        if (patients.ContainsKey(patientId))
                                        {
                                            Console.WriteLine($"Patient {patientId} already in list, skipping");
                                            continue;
                                        }
                                        
                                        // Try to get patient demographics
                                        var patient = await GetPatientByIdAsync(patientId);
                                        if (patient == null)
                                        {
                                            // Create a basic patient record from addendum data
                                            patient = new DemographicsDomain { patientID = patientId };
                                            
                                            // Extract patient name from the patientName field
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
                                        }
                                        
                                        // Only add the patient if we have a valid ID
                                        if (patient.patientID > 0)
                                        {
                                            patients[patientId] = patient;
                                            Console.WriteLine($"Added patient from addendum {i}: ID={patient.patientID}");
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"Error parsing addendum {i} JSON: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve addendum {i}. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting addendum {i}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Parse patient name from addendum
        /// </summary>
        private void ParsePatientNameFromAddendum(string fullNameWithDob, DemographicsDomain patient)
        {
            try
            {
                // Format: "LASTNAME, FIRSTNAME (DOB: MM/DD/YYYY)"
                var nameParts = fullNameWithDob.Split(new[] { ',' }, 2);
                
                if (nameParts.Length >= 1)
                {
                    patient.lastName = nameParts[0].Trim();
                    
                    if (nameParts.Length >= 2)
                    {
                        // Extract first name (might include DOB in parentheses)
                        var firstNameWithDob = nameParts[1].Trim();
                        
                        // Extract DOB if present
                        var dobMatch = System.Text.RegularExpressions.Regex.Match(firstNameWithDob, @"\(DOB:\s*(\d{1,2}/\d{1,2}/\d{4})\)");
                        if (dobMatch.Success && dobMatch.Groups.Count > 1)
                        {
                            var dobString = dobMatch.Groups[1].Value;
                            if (DateTime.TryParse(dobString, out DateTime dob))
                            {
                                patient.birthDate = dob;
                            }
                            
                            // Remove DOB part from first name
                            firstNameWithDob = firstNameWithDob.Replace(dobMatch.Value, "").Trim();
                        }
                        
                        patient.firstName = firstNameWithDob;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing patient name from addendum: {ex.Message}");
            }
        }
    }
}
