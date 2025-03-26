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
        string SerializeToJson(Base resource);

        /// <summary>
        /// Deserializes a JSON string to a FHIR resource
        /// </summary>
        T DeserializeFromJson<T>(string json) where T : Base;
        
        /// <summary>
        /// Parses a JSON string to a FHIR resource
        /// </summary>
        Resource ParseResource(string json);

        /// <summary>
        /// Validates a FHIR resource against standard profiles
        /// </summary>
        ValidationResult Validate(Base resource);
    }

    /// <summary>
    /// Result of a FHIR resource validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation was successful (no errors)
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// The full FHIR OperationOutcome from validation
        /// </summary>
        public OperationOutcome? OperationOutcome { get; set; }
    }
}
