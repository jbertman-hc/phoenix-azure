using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Interface for FHIR service operations
    /// </summary>
    public interface IFhirService
    {
        /// <summary>
        /// Serializes a FHIR resource to JSON
        /// </summary>
        /// <param name="resource">The FHIR resource to serialize</param>
        /// <returns>JSON string representation of the resource</returns>
        string SerializeToJson(Resource resource);

        /// <summary>
        /// Deserializes JSON to a FHIR resource
        /// </summary>
        /// <typeparam name="T">The type of FHIR resource</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>The deserialized FHIR resource</returns>
        T DeserializeFromJson<T>(string json) where T : Resource;

        /// <summary>
        /// Validates a FHIR resource
        /// </summary>
        /// <param name="resource">The FHIR resource to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        ValidationResult Validate(Resource resource);
    }

    /// <summary>
    /// Result of a FHIR resource validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Detailed validation issues
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();
    }
}
