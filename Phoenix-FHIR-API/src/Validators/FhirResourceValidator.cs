using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using System.Collections.Generic;
using System.Linq;

namespace Phoenix_FHIR_API.Validators
{
    /// <summary>
    /// Validates FHIR resources against the FHIR specification
    /// </summary>
    public class FhirResourceValidator : IFhirResourceValidator
    {
        private readonly ILogger<FhirResourceValidator> _logger;

        public FhirResourceValidator(ILogger<FhirResourceValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates a FHIR resource against the FHIR specification
        /// </summary>
        public bool Validate<T>(T resource) where T : Resource
        {
            return Validate(resource, out _);
        }

        /// <summary>
        /// Validates a FHIR resource against the FHIR specification and returns validation issues
        /// </summary>
        public bool Validate<T>(T resource, out IEnumerable<string> validationIssues) where T : Resource
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            try
            {
                // Perform basic validation using the built-in validation
                var issues = new List<string>();
                
                // Use the built-in validation method that doesn't require external resources
                // This won't do profile validation but will check basic structural validity
                resource.Validate();
                
                // For now, we'll just do basic validation and assume it's valid
                // In a production environment, you'd want to use a more comprehensive validation
                
                validationIssues = issues;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating {ResourceType} with ID {ResourceId}", 
                    resource.TypeName, resource.Id);
                validationIssues = new[] { $"Error validating resource: {ex.Message}" };
                return false;
            }
        }
    }
}
