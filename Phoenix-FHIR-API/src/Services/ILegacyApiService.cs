using System.Text.Json;
using Phoenix_FHIR_API.Models;

namespace Phoenix_FHIR_API.Services
{
    /// <summary>
    /// Interface for accessing the legacy healthcare API
    /// </summary>
    public interface ILegacyApiService
    {
        // Patient-related methods
        Task<DemographicsDomain?> GetPatientByIdAsync(int patientId);
        Task<List<DemographicsDomain>> GetAllPatientsAsync();
        Task<List<ListAllergiesDomain>> GetPatientAllergiesAsync(int patientId);
        Task<List<ListMedsDomain>> GetPatientMedicationsAsync(int patientId);
        Task<List<ListProblemDomain>> GetPatientProblemsAsync(int patientId);
        Task<List<SOAPDomain>> GetPatientClinicalNotesAsync(int patientId);
        
        // Practitioner-related methods
        Task<ProviderIndexDomain?> GetProviderByIdAsync(int providerId);
        Task<List<ProviderIndexDomain>> GetAllProvidersAsync();
        
        // Organization-related methods
        Task<PracticeInfoDomain?> GetPracticeInfoAsync(int practiceId);
        
        // Location-related methods
        Task<List<LocationsDomain>> GetLocationsAsync();
        Task<LocationsDomain?> GetLocationByIdAsync(string locationId);
        Task<List<FacilitiesDomain>> GetFacilitiesAsync();
        Task<FacilitiesDomain?> GetFacilityByIdAsync(string facilityId);
        
        // Scheduling-related methods
        Task<List<SchedulingDomain>> GetPatientAppointmentsAsync(int patientId);
    }
}
