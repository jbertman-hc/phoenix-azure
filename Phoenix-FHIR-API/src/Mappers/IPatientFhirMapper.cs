using Hl7.Fhir.Model;
using Phoenix_FHIR_API.Models;

namespace Phoenix_FHIR_API.Mappers
{
    /// <summary>
    /// Interface for mapping between legacy patient data and FHIR Patient resources
    /// </summary>
    public interface IPatientFhirMapper : IFhirMapper<Patient, DemographicsDomain>
    {
        /// <summary>
        /// Creates a complete Patient resource with all available information
        /// </summary>
        /// <param name="demographics">Patient demographics</param>
        /// <returns>A complete FHIR Patient resource</returns>
        Task<Patient> CreatePatientResourceAsync(DemographicsDomain demographics);
    }
}
