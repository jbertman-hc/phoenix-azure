using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using Hl7.Fhir.Specification.Source;

namespace Phoenix_FHIR_API.Validators
{
    /// <summary>
    /// Validates FHIR resources against the FHIR specification
    /// </summary>
    public class FhirResourceValidator : IFhirResourceValidator
    {
        private readonly Validator _validator;
        private readonly ILogger<FhirResourceValidator> _logger;

        public FhirResourceValidator(ILogger<FhirResourceValidator> logger)
        {
            _logger = logger;
            
            // Create a validator with the built-in validation resources
            var source = new CachedResolver(
                new MultiResolver(
                    ZipSource.CreateValidationSource(),
                    new DirectorySource("ValidationProfiles", includeSubdirectories: true)
                )
            );
            
            _validator = new Validator(source);
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
                // Perform the validation
                var outcome = _validator.Validate(resource);
                
                // Extract validation issues
                var issues = outcome.Issue
                    .Where(i => i.Severity == OperationOutcome.IssueSeverity.Error || 
                                i.Severity == OperationOutcome.IssueSeverity.Fatal)
                    .Select(i => $"{i.Severity}: {i.Details?.Text ?? i.Diagnostics} at {i.Location?.FirstOrDefault() ?? "unknown location"}")
                    .ToList();

                // Log validation issues
                foreach (var issue in issues)
                {
                    _logger.LogWarning("Validation issue for {ResourceType} with ID {ResourceId}: {Issue}", 
                        resource.TypeName, resource.Id, issue);
                }

                validationIssues = issues;
                return !issues.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating {ResourceType} with ID {ResourceId}", 
                    resource.TypeName, resource.Id);
                validationIssues = new[] { $"Validation error: {ex.Message}" };
                return false;
            }
        }
    }
}
