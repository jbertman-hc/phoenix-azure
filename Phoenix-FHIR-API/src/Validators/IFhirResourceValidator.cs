using Hl7.Fhir.Model;

namespace Phoenix_FHIR_API.Validators
{
    /// <summary>
    /// Interface for validating FHIR resources
    /// </summary>
    public interface IFhirResourceValidator
    {
        /// <summary>
        /// Validates a FHIR resource against the FHIR specification
        /// </summary>
        /// <typeparam name="T">The type of FHIR resource</typeparam>
        /// <param name="resource">The resource to validate</param>
        /// <returns>True if the resource is valid, false otherwise</returns>
        bool Validate<T>(T resource) where T : Resource;
        
        /// <summary>
        /// Validates a FHIR resource against the FHIR specification and returns validation issues
        /// </summary>
        /// <typeparam name="T">The type of FHIR resource</typeparam>
        /// <param name="resource">The resource to validate</param>
        /// <param name="validationIssues">The validation issues</param>
        /// <returns>True if the resource is valid, false otherwise</returns>
        bool Validate<T>(T resource, out IEnumerable<string> validationIssues) where T : Resource;
    }
}
