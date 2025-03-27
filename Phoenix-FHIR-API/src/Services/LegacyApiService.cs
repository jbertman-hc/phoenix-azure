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
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DemographicsDomain>(content, _jsonOptions);
            }
            
            return null;
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
    }
}
