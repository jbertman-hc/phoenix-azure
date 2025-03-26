using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Validation;
using Microsoft.Extensions.Logging;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Rest;
using System.Text.RegularExpressions;

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
            
            // Initialize the FHIR serializer and parser
            _serializer = new FhirJsonSerializer(new SerializerSettings
            {
                Pretty = true
            });
            _parser = new FhirJsonParser();
            
            // Initialize validator components
            try
            {
                _logger.LogInformation("FHIR service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FHIR service");
                // Don't throw here to allow the service to continue without validation
            }
        }

        /// <summary>
        /// Serializes a FHIR resource to JSON
        /// </summary>
        public string SerializeToJson(Base resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
                
            return _serializer.SerializeToString(resource);
        }
        
        /// <summary>
        /// Deserializes a JSON string to a FHIR resource
        /// </summary>
        public T DeserializeFromJson<T>(string json) where T : Base
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
                
            return _parser.Parse<T>(json);
        }
        
        /// <summary>
        /// Parse a FHIR resource from JSON
        /// </summary>
        public Resource ParseResource(string json)
        {
            try
            {
                // First try to parse the resource using the FhirJsonParser
                var parser = new FhirJsonParser();
                var resource = parser.Parse<Resource>(json);
                
                // If it's a Bundle, pre-process any Patient resources to fix date formats
                if (resource is Bundle bundle && bundle.Entry != null)
                {
                    foreach (var entry in bundle.Entry)
                    {
                        if (entry.Resource is Patient patientInBundle)
                        {
                            // Fix datetime format issues in Patient resources
                            FixPatientDateFormats(patientInBundle);
                        }
                    }
                }
                else if (resource is Patient patient)
                {
                    // Fix datetime format issues in Patient resource
                    FixPatientDateFormats(patient);
                }
                
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing FHIR resource from JSON");
                throw new FormatException($"Invalid FHIR resource format: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Fix date format issues in Patient resources
        /// </summary>
        private void FixPatientDateFormats(Patient patient)
        {
            // Fix birthDate format if needed
            if (patient.BirthDate != null)
            {
                try
                {
                    // Handle different date formats
                    if (patient.BirthDate.Contains("T"))
                    {
                        // If it contains a time component, parse it and convert to date-only format
                        var date = DateTime.Parse(patient.BirthDate);
                        patient.BirthDate = date.ToString("yyyy-MM-dd");
                        _logger.LogInformation("Fixed birthDate format from datetime to date: {BirthDate}", patient.BirthDate);
                    }
                    else if (!Regex.IsMatch(patient.BirthDate, @"^\d{4}-\d{2}-\d{2}$"))
                    {
                        // If it's not in the yyyy-MM-dd format, try to parse and reformat it
                        var date = DateTime.Parse(patient.BirthDate);
                        patient.BirthDate = date.ToString("yyyy-MM-dd");
                        _logger.LogInformation("Reformatted birthDate to standard format: {BirthDate}", patient.BirthDate);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fixing birthDate format: {BirthDate}", patient.BirthDate);
                }
            }
            
            // Fix other date fields in Patient resource
            FixDateExtensions(patient);
        }
        
        /// <summary>
        /// Fix date format issues in extensions and other date fields
        /// </summary>
        private void FixDateExtensions(Resource resource)
        {
            // Process all extensions recursively if the resource has them
            // Not all FHIR resources have extensions, so we need to check for specific types
            if (resource is DomainResource domainResource && domainResource.Extension != null)
            {
                foreach (var extension in domainResource.Extension)
                {
                    // Fix date values in extensions
                    FixDateValueInExtension(extension);
                }
            }
            
            // For Patient resources, also check specific date fields
            if (resource is Patient patient)
            {
                // Fix deceased date if it's a datetime
                if (patient.Deceased is FhirDateTime deceasedDateTime)
                {
                    string value = deceasedDateTime.Value;
                    if (value != null && value.Contains("T"))
                    {
                        try
                        {
                            var date = DateTime.Parse(value);
                            patient.Deceased = new FhirDateTime(date.ToString("yyyy-MM-dd"));
                            _logger.LogInformation("Fixed deceased date format from datetime to date");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not parse deceased date: {DeceasedDate}", value);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fix date values in extensions
        /// </summary>
        private void FixDateValueInExtension(Extension extension)
        {
            // Check if this extension has a date value
            if (extension.Value is FhirDateTime dateTimeValue)
            {
                string value = dateTimeValue.Value;
                if (value != null && value.Contains("T"))
                {
                    try
                    {
                        var date = DateTime.Parse(value);
                        extension.Value = new FhirDateTime(date.ToString("yyyy-MM-dd"));
                        _logger.LogInformation("Fixed extension date format from datetime to date");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not parse extension date: {ExtensionDate}", value);
                    }
                }
            }
            
            // Recursively process nested extensions
            if (extension.Extension != null)
            {
                foreach (var nestedExtension in extension.Extension)
                {
                    FixDateValueInExtension(nestedExtension);
                }
            }
        }
        
        /// <summary>
        /// Validates a FHIR resource against standard profiles
        /// </summary>
        public ValidationResult Validate(Base resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            try
            {
                _logger.LogInformation($"Validating {resource.TypeName} resource");
                
                // Create a new operation outcome for validation results
                var outcome = new OperationOutcome();
                bool isValid = true;
                
                // Implement basic validation logic based on resource type
                if (resource is Patient patient)
                {
                    // Check for required fields in Patient resource
                    if (patient.Name == null || patient.Name.Count == 0)
                    {
                        isValid = false;
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.Required,
                            Diagnostics = "Patient resource must have at least one name",
                            Expression = new string[] { "Patient.name" }
                        });
                    }
                    
                    // Check if gender is valid
                    if (patient.Gender != null && 
                        patient.Gender != AdministrativeGender.Male && 
                        patient.Gender != AdministrativeGender.Female && 
                        patient.Gender != AdministrativeGender.Other && 
                        patient.Gender != AdministrativeGender.Unknown)
                    {
                        isValid = false;
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.CodeInvalid,
                            Diagnostics = "Patient gender must be a valid AdministrativeGender code",
                            Expression = new string[] { "Patient.gender" }
                        });
                    }
                    
                    // Check if birthDate is valid (if present)
                    if (patient.BirthDate != null)
                    {
                        try
                        {
                            // The birthDate should already be fixed by FixPatientDateFormats
                            // Just validate that it's a proper date
                            var date = DateTime.Parse(patient.BirthDate);
                            
                            // Check if date is in the future
                            if (date > DateTime.Now)
                            {
                                isValid = false;
                                outcome.Issue.Add(new OperationOutcome.IssueComponent
                                {
                                    Severity = OperationOutcome.IssueSeverity.Error,
                                    Code = OperationOutcome.IssueType.Invalid,
                                    Diagnostics = "Patient birthDate cannot be in the future",
                                    Expression = new string[] { "Patient.birthDate" }
                                });
                            }
                        }
                        catch
                        {
                            isValid = false;
                            outcome.Issue.Add(new OperationOutcome.IssueComponent
                            {
                                Severity = OperationOutcome.IssueSeverity.Error,
                                Code = OperationOutcome.IssueType.Invalid,
                                Diagnostics = "Patient birthDate is not a valid date format",
                                Expression = new string[] { "Patient.birthDate" }
                            });
                        }
                    }
                }
                else if (resource is Bundle bundle)
                {
                    // Validate Bundle resource
                    _logger.LogInformation($"Validating Bundle with {bundle.Entry?.Count ?? 0} entries");
                    
                    // Check if bundle type is specified
                    if (bundle.Type == null)
                    {
                        isValid = false;
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.Required,
                            Diagnostics = "Bundle must have a type specified",
                            Expression = new string[] { "Bundle.type" }
                        });
                    }
                    
                    // Check if bundle has entries
                    if (bundle.Entry == null || bundle.Entry.Count == 0)
                    {
                        // This is just a warning, not an error
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Warning,
                            Code = OperationOutcome.IssueType.Informational,
                            Diagnostics = "Bundle has no entries",
                            Expression = new string[] { "Bundle.entry" }
                        });
                    }
                    else
                    {
                        // Check each entry in the bundle
                        for (int i = 0; i < bundle.Entry.Count; i++)
                        {
                            var entry = bundle.Entry[i];
                            
                            // Check if entry has a resource
                            if (entry.Resource == null)
                            {
                                isValid = false;
                                outcome.Issue.Add(new OperationOutcome.IssueComponent
                                {
                                    Severity = OperationOutcome.IssueSeverity.Error,
                                    Code = OperationOutcome.IssueType.Required,
                                    Diagnostics = $"Bundle entry at position {i} must have a resource",
                                    Expression = new string[] { $"Bundle.entry[{i}].resource" }
                                });
                            }
                            
                            // Add information about the resource type in this entry
                            if (entry.Resource != null)
                            {
                                outcome.Issue.Add(new OperationOutcome.IssueComponent
                                {
                                    Severity = OperationOutcome.IssueSeverity.Information,
                                    Code = OperationOutcome.IssueType.Informational,
                                    Diagnostics = $"Bundle entry at position {i} contains a {entry.Resource.TypeName} resource",
                                    Expression = new string[] { $"Bundle.entry[{i}].resource" }
                                });
                            }
                        }
                        
                        // Add a summary of the bundle contents
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Information,
                            Code = OperationOutcome.IssueType.Informational,
                            Diagnostics = $"Bundle contains {bundle.Entry.Count} resources of type {(bundle.Entry.FirstOrDefault()?.Resource?.TypeName ?? "unknown")}"
                        });
                    }
                    
                    // Add information about the bundle type
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Information,
                        Code = OperationOutcome.IssueType.Informational,
                        Diagnostics = $"Bundle is of type {bundle.Type}"
                    });
                }
                else
                {
                    // For other resource types, add a warning that specific validation is not implemented
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Warning,
                        Code = OperationOutcome.IssueType.NotSupported,
                        Diagnostics = $"Detailed validation for {resource.TypeName} resources is not implemented yet. Only basic structure validation was performed."
                    });
                }
                
                // If no issues were found and the resource is valid, add a success message
                if (isValid && outcome.Issue.Count == 0)
                {
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Information,
                        Code = OperationOutcome.IssueType.Informational,
                        Diagnostics = $"{resource.TypeName} resource is valid"
                    });
                }
                
                return new ValidationResult
                {
                    IsValid = isValid,
                    OperationOutcome = outcome
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating {resource.TypeName} resource");
                
                // Create an error outcome
                var outcome = new OperationOutcome();
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Exception,
                    Diagnostics = $"Validation error: {ex.Message}"
                });
                
                return new ValidationResult
                {
                    IsValid = false,
                    OperationOutcome = outcome
                };
            }
        }

        /// <summary>
        /// Validates a FHIR resource against standard profiles
        /// </summary>
        /// <param name="resource">The FHIR resource to validate</param>
        /// <returns>An OperationOutcome with validation results</returns>
        public OperationOutcome ValidateResource(Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource), "Resource cannot be null");
            }

            _logger.LogInformation("Validating resource of type {ResourceType}", resource.TypeName);

            try
            {
                // Create a new OperationOutcome for validation results
                var outcome = new OperationOutcome
                {
                    Issue = new List<OperationOutcome.IssueComponent>()
                };
                
                // Check if resource has the required elements
                if (resource is Patient patient)
                {
                    // Validate Patient resource
                    ValidatePatientResource(patient, outcome);
                }
                else if (resource is Bundle bundle)
                {
                    // Validate Bundle resource
                    ValidateBundleResource(bundle, outcome);
                }
                else
                {
                    // Basic validation for other resource types
                    if (string.IsNullOrEmpty(resource.Id))
                    {
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Warning,
                            Code = OperationOutcome.IssueType.Required,
                            Diagnostics = $"Resource is missing an ID"
                        });
                    }
                }
                
                _logger.LogInformation("Validation complete. Found {IssueCount} issues", outcome.Issue.Count);
                
                // If no issues were found, add an information message
                if (outcome.Issue.Count == 0)
                {
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Information,
                        Code = OperationOutcome.IssueType.Informational,
                        Diagnostics = "Resource is valid"
                    });
                }
                
                return outcome;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating resource");
                
                // Create an error outcome
                var errorOutcome = new OperationOutcome
                {
                    Issue = new List<OperationOutcome.IssueComponent>
                    {
                        new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.Exception,
                            Diagnostics = $"Validation error: {ex.Message}"
                        }
                    }
                };
                
                return errorOutcome;
            }
        }

        private void ValidatePatientResource(Patient patient, OperationOutcome outcome)
        {
            // Check for required fields in Patient
            if (patient.Name == null || !patient.Name.Any())
            {
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = "Patient resource must have at least one name"
                });
            }
            
            // Check birthdate format
            if (patient.BirthDate != null)
            {
                try
                {
                    var date = DateTime.Parse(patient.BirthDate);
                }
                catch
                {
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Error,
                        Code = OperationOutcome.IssueType.Invalid,
                        Diagnostics = "Patient birthdate is not in a valid format"
                    });
                }
            }
            
            // Check for identifier
            if (patient.Identifier == null || !patient.Identifier.Any())
            {
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Warning,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = "Patient resource should have at least one identifier"
                });
            }
        }

        private void ValidateBundleResource(Bundle bundle, OperationOutcome outcome)
        {
            // Check if bundle has a type
            if (bundle.Type == null)
            {
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = "Bundle must have a type"
                });
            }
            
            // Check if bundle has entries
            if (bundle.Entry == null || !bundle.Entry.Any())
            {
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Information,
                    Code = OperationOutcome.IssueType.Informational,
                    Diagnostics = "Bundle has no entries"
                });
            }
            else
            {
                // Validate each entry in the bundle
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource == null)
                    {
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.Required,
                            Diagnostics = "Bundle entry is missing a resource"
                        });
                    }
                    else if (string.IsNullOrEmpty(entry.Resource.Id))
                    {
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Warning,
                            Code = OperationOutcome.IssueType.Required,
                            Diagnostics = $"Resource in bundle is missing an ID: {entry.Resource.TypeName}"
                        });
                    }
                }
            }
        }
        
        /// <summary>
        /// Validates and fixes a FHIR resource during creation
        /// </summary>
        /// <param name="resource">The resource to validate and fix</param>
        /// <returns>The resource with any fixes applied</returns>
        public Resource ValidateAndFixResource(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
                
            try
            {
                _logger.LogInformation($"Validating and fixing {resource.TypeName} resource during creation");
                
                // Apply specific fixes based on resource type
                if (resource is Patient patient)
                {
                    // Fix birthDate format
                    FixPatientDateFormats(patient);
                    
                    // Ensure required fields are present
                    if (patient.Name == null || patient.Name.Count == 0)
                    {
                        _logger.LogWarning("Patient resource missing required name field");
                        patient.Name = new List<HumanName>
                        {
                            new HumanName
                            {
                                Use = HumanName.NameUse.Official,
                                Family = "Unknown"
                            }
                        };
                    }
                    
                    // Ensure gender is valid
                    if (patient.Gender == null)
                    {
                        patient.Gender = AdministrativeGender.Unknown;
                    }
                }
                else if (resource is Bundle bundle)
                {
                    // Fix entries in the bundle
                    if (bundle.Entry == null)
                    {
                        bundle.Entry = new List<Bundle.EntryComponent>();
                    }
                    
                    // Process each entry in the bundle
                    for (int i = 0; i < bundle.Entry.Count; i++)
                    {
                        var entry = bundle.Entry[i];
                        
                        if (entry.Resource != null)
                        {
                            try
                            {
                                // Special handling for Patient resources in bundles
                                if (entry.Resource is Patient patientInBundle)
                                {
                                    // Fix date formats in Patient resources
                                    FixPatientDateFormats(patientInBundle);
                                }
                                else
                                {
                                    // Fix date extensions in other resource types
                                    FixDateExtensions(entry.Resource);
                                }
                                
                                // Ensure each resource has an ID
                                if (string.IsNullOrEmpty(entry.Resource.Id))
                                {
                                    entry.Resource.Id = Guid.NewGuid().ToString("N");
                                    _logger.LogInformation($"Added missing ID to {entry.Resource.TypeName} resource in bundle");
                                }
                                
                                // Ensure each entry has a fullUrl
                                if (string.IsNullOrEmpty(entry.FullUrl))
                                {
                                    entry.FullUrl = $"http://localhost:5300/api/fhir/{entry.Resource.TypeName}/{entry.Resource.Id}";
                                    _logger.LogInformation($"Added missing FullUrl to entry in bundle");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error processing entry {i} in bundle. Removing invalid entry.");
                                // Don't attempt to recursively validate this resource as it may cause infinite recursion
                                continue;
                            }
                        }
                    }
                    
                    // Ensure bundle type is specified
                    if (bundle.Type == null)
                    {
                        _logger.LogWarning("Bundle missing required type field, defaulting to searchset");
                        bundle.Type = Bundle.BundleType.Searchset;
                    }
                    
                    // Ensure bundle has a timestamp
                    if (bundle.Timestamp == null)
                    {
                        bundle.Timestamp = DateTime.UtcNow;
                    }
                    
                    // Ensure bundle has metadata
                    if (bundle.Meta == null)
                    {
                        bundle.Meta = new Meta
                        {
                            LastUpdated = DateTime.UtcNow,
                            Profile = new string[] { "http://hl7.org/fhir/StructureDefinition/Bundle" }
                        };
                    }
                    
                    // Ensure bundle has an ID
                    if (string.IsNullOrEmpty(bundle.Id))
                    {
                        bundle.Id = Guid.NewGuid().ToString("N");
                    }
                }
                else
                {
                    // For other resource types, at least fix date extensions
                    FixDateExtensions(resource);
                }
                
                // Ensure resource has an ID
                if (string.IsNullOrEmpty(resource.Id))
                {
                    resource.Id = Guid.NewGuid().ToString("N");
                    _logger.LogInformation($"Added missing ID to {resource.TypeName} resource");
                }
                
                // Ensure resource has metadata
                if (resource.Meta == null)
                {
                    resource.Meta = new Meta
                    {
                        LastUpdated = DateTime.UtcNow
                    };
                }
                
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating and fixing {resource.TypeName} resource");
                // Return the original resource if fixing failed
                return resource;
            }
        }
    }
}
