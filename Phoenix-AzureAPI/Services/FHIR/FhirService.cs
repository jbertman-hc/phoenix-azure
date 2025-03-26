using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Validation;
using Microsoft.Extensions.Logging;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Service for FHIR resource operations including serialization, deserialization, and validation
    /// </summary>
    public class FhirService : IFhirService
    {
        private readonly ILogger<FhirService> _logger;
        private readonly FhirJsonSerializer _serializer;
        private readonly FhirJsonParser _parser;

        public FhirService(ILogger<FhirService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = new FhirJsonSerializer(new SerializerSettings
            {
                Pretty = true
            });
            _parser = new FhirJsonParser();
        }

        /// <summary>
        /// Serializes a FHIR resource to JSON
        /// </summary>
        /// <param name="resource">The FHIR resource to serialize</param>
        /// <returns>JSON string representation of the resource</returns>
        public string SerializeToJson(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            try
            {
                return _serializer.SerializeToString(resource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing FHIR resource to JSON");
                throw new InvalidOperationException("Failed to serialize FHIR resource", ex);
            }
        }

        /// <summary>
        /// Deserializes JSON to a FHIR resource
        /// </summary>
        /// <typeparam name="T">The type of FHIR resource</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>The deserialized FHIR resource</returns>
        public T DeserializeFromJson<T>(string json) where T : Resource
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            try
            {
                return _parser.Parse<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing JSON to FHIR resource");
                throw new InvalidOperationException("Failed to deserialize JSON to FHIR resource", ex);
            }
        }

        /// <summary>
        /// Validates a FHIR resource
        /// </summary>
        /// <param name="resource">The FHIR resource to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        public ValidationResult Validate(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            try
            {
                // Create a basic validation outcome
                var outcome = new OperationOutcome();
                
                // Perform basic validation by serializing and deserializing
                try
                {
                    var json = SerializeToJson(resource);
                    var type = resource.GetType();
                    var method = typeof(FhirJsonParser).GetMethod("Parse").MakeGenericMethod(type);
                    method.Invoke(_parser, new object[] { json });
                }
                catch (Exception ex)
                {
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Error,
                        Code = OperationOutcome.IssueType.Invalid,
                        Diagnostics = $"Resource failed validation: {ex.Message}"
                    });
                }
                
                var result = new ValidationResult
                {
                    Success = !outcome.Issue.Any(i => i.Severity == OperationOutcome.IssueSeverity.Error || 
                                                    i.Severity == OperationOutcome.IssueSeverity.Fatal)
                };

                if (!result.Success)
                {
                    var errorIssues = outcome.Issue.Where(i => i.Severity == OperationOutcome.IssueSeverity.Error || 
                                                             i.Severity == OperationOutcome.IssueSeverity.Fatal);
                    
                    result.ErrorMessage = string.Join("; ", errorIssues.Select(i => i.Details?.Text ?? i.Diagnostics));
                    result.Issues = outcome.Issue.Select(i => $"{i.Severity}: {i.Details?.Text ?? i.Diagnostics}").ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating FHIR resource");
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = $"Validation error: {ex.Message}",
                    Issues = new List<string> { ex.ToString() }
                };
            }
        }
    }
}
