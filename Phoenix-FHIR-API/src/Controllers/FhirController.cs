using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Phoenix_FHIR_API.Mappers;
using Phoenix_FHIR_API.Services;
using Phoenix_FHIR_API.Validators;
using Swashbuckle.AspNetCore.Annotations;
using Newtonsoft.Json.Linq;

namespace Phoenix_FHIR_API.Controllers
{
    [ApiController]
    [Route("api/fhir")]
    public class FhirController : ControllerBase
    {
        private readonly ILegacyApiService _legacyApiService;
        private readonly IPatientFhirMapper _patientMapper;
        private readonly IFhirResourceValidator _validator;
        private readonly ILogger<FhirController> _logger;
        private readonly FhirJsonSerializer _serializer;

        public FhirController(
            ILegacyApiService legacyApiService,
            IPatientFhirMapper patientMapper,
            IFhirResourceValidator validator,
            ILogger<FhirController> logger)
        {
            _legacyApiService = legacyApiService;
            _patientMapper = patientMapper;
            _validator = validator;
            _logger = logger;
            _serializer = new FhirJsonSerializer(new SerializerSettings
            {
                Pretty = true
            });
        }

        /// <summary>
        /// Get a FHIR Patient resource by ID
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <returns>The FHIR Patient resource</returns>
        [HttpGet("Patient/{id}")]
        [SwaggerOperation(
            Summary = "Get a FHIR Patient resource by ID",
            Description = "Returns a FHIR Patient resource for the specified patient ID",
            OperationId = "GetPatient",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "The FHIR Patient resource", typeof(Patient))]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetPatient(int id)
        {
            try
            {
                // Get the patient demographics from the legacy API
                var demographics = await _legacyApiService.GetPatientByIdAsync(id);
                if (demographics == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // Map to a FHIR Patient resource
                var patient = await _patientMapper.CreatePatientResourceAsync(demographics);

                // Validate the FHIR resource
                if (!_validator.Validate(patient, out var validationIssues))
                {
                    _logger.LogWarning("Validation issues for Patient {PatientId}: {Issues}", 
                        id, string.Join(", ", validationIssues));
                }

                // Return the FHIR resource
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Patient {PatientId}", id);
                return StatusCode(500, $"Error getting Patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new FHIR Patient resource
        /// </summary>
        /// <param name="patient">The FHIR Patient resource to create</param>
        /// <returns>The created FHIR Patient resource</returns>
        [HttpPost("Patient")]
        [SwaggerOperation(
            Summary = "Create a new FHIR Patient resource",
            Description = "Creates a new FHIR Patient resource and returns the created resource",
            OperationId = "CreatePatient",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(201, "The created FHIR Patient resource", typeof(Patient))]
        [SwaggerResponse(400, "Invalid FHIR Patient resource")]
        [SwaggerResponse(500, "Internal server error")]
        [Consumes("application/fhir+json")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
        {
            try
            {
                // Validate the FHIR resource
                if (!_validator.Validate(patient, out var validationIssues))
                {
                    return BadRequest($"Invalid FHIR Patient resource: {string.Join(", ", validationIssues)}");
                }

                // Map back to legacy model
                var demographics = _patientMapper.MapBack(patient);

                // TODO: Implement create operation in legacy API service
                // For now, return the patient with a generated ID
                patient.Id = Guid.NewGuid().ToString();

                // Return the created resource
                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Patient");
                return StatusCode(500, $"Error creating Patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing FHIR Patient resource
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <param name="patient">The FHIR Patient resource to update</param>
        /// <returns>The updated FHIR Patient resource</returns>
        [HttpPut("Patient/{id}")]
        [SwaggerOperation(
            Summary = "Update an existing FHIR Patient resource",
            Description = "Updates an existing FHIR Patient resource and returns the updated resource",
            OperationId = "UpdatePatient",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "The updated FHIR Patient resource", typeof(Patient))]
        [SwaggerResponse(400, "Invalid FHIR Patient resource")]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error")]
        [Consumes("application/fhir+json")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] Patient patient)
        {
            try
            {
                // Check if the patient exists
                var demographics = await _legacyApiService.GetPatientByIdAsync(id);
                if (demographics == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // Ensure the ID in the path matches the ID in the resource
                if (patient.Id != id.ToString())
                {
                    return BadRequest("The ID in the path must match the ID in the resource");
                }

                // Validate the FHIR resource
                if (!_validator.Validate(patient, out var validationIssues))
                {
                    return BadRequest($"Invalid FHIR Patient resource: {string.Join(", ", validationIssues)}");
                }

                // Map back to legacy model
                var updatedDemographics = _patientMapper.MapBack(patient);

                // TODO: Implement update operation in legacy API service
                // For now, just return the updated patient
                
                // Return the updated resource
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Patient {PatientId}", id);
                return StatusCode(500, $"Error updating Patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a FHIR Patient resource
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("Patient/{id}")]
        [SwaggerOperation(
            Summary = "Delete a FHIR Patient resource",
            Description = "Deletes a FHIR Patient resource",
            OperationId = "DeletePatient",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(204, "Patient deleted successfully")]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            try
            {
                // Check if the patient exists
                var demographics = await _legacyApiService.GetPatientByIdAsync(id);
                if (demographics == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // TODO: Implement delete operation in legacy API service
                // For now, just return success

                // Return no content
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Patient {PatientId}", id);
                return StatusCode(500, $"Error deleting Patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a FHIR Bundle containing all resources for a patient
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <returns>The FHIR Bundle</returns>
        [HttpGet("Patient/{id}/$everything")]
        [SwaggerOperation(
            Summary = "Get a FHIR Bundle containing all resources for a patient",
            Description = "Returns a FHIR Bundle containing all resources for the specified patient",
            OperationId = "GetPatientBundle",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "The FHIR Bundle", typeof(Bundle))]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetPatientBundle(int id)
        {
            try
            {
                // Get the patient demographics from the legacy API
                var demographics = await _legacyApiService.GetPatientByIdAsync(id);
                if (demographics == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // Create a Bundle
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Collection,
                    Id = Guid.NewGuid().ToString(),
                    Meta = new Meta
                    {
                        LastUpdated = DateTime.UtcNow
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Add the Patient resource to the Bundle
                var patient = await _patientMapper.CreatePatientResourceAsync(demographics);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                    Resource = patient
                });

                // TODO: Add other resources to the Bundle (Conditions, AllergyIntolerances, etc.)
                // This will be implemented as we add more resource mappers

                // Return the Bundle
                return Ok(bundle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Patient Bundle for {PatientId}", id);
                return StatusCode(500, $"Error getting Patient Bundle: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all FHIR Patient resources
        /// </summary>
        /// <returns>A Bundle containing all FHIR Patient resources</returns>
        [HttpGet("Patient")]
        [SwaggerOperation(
            Summary = "Get all FHIR Patient resources",
            Description = "Returns a Bundle containing all FHIR Patient resources",
            OperationId = "GetAllPatients",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "A Bundle containing all FHIR Patient resources", typeof(Bundle))]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                // Get all patients from the legacy API
                var patients = await _legacyApiService.GetAllPatientsAsync();
                
                // Create a Bundle
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Searchset,
                    Id = Guid.NewGuid().ToString(),
                    Meta = new Meta
                    {
                        LastUpdated = DateTime.UtcNow
                    },
                    Timestamp = DateTime.UtcNow,
                    Total = patients.Count
                };
                
                // Add each patient to the Bundle
                foreach (var demographics in patients)
                {
                    var patient = await _patientMapper.CreatePatientResourceAsync(demographics);
                    
                    // Validate the FHIR resource
                    if (!_validator.Validate(patient, out var validationIssues))
                    {
                        _logger.LogWarning("Validation issues for Patient {PatientId}: {Issues}", 
                            demographics.patientID, string.Join(", ", validationIssues));
                    }
                    
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                        Resource = patient,
                        Search = new Bundle.SearchComponent
                        {
                            Mode = Bundle.SearchEntryMode.Match
                        }
                    });
                }
                
                return Ok(bundle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all Patients");
                return StatusCode(500, $"Error getting all Patients: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate a FHIR resource
        /// </summary>
        /// <param name="validationRequest">The resource to validate</param>
        /// <returns>OperationOutcome with validation results</returns>
        [HttpPost("$validate")]
        [SwaggerOperation(
            Summary = "Validate a FHIR resource",
            Description = "Validates a FHIR resource against profiles and returns an OperationOutcome with validation results",
            OperationId = "ValidateResource",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "Validation results", typeof(OperationOutcome))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(500, "Internal server error")]
        [Consumes("application/json", "application/fhir+json")]
        [Produces("application/fhir+json")]
        public IActionResult ValidateResource([FromBody] ValidationRequest validationRequest)
        {
            try
            {
                if (validationRequest?.ResourceToValidate == null)
                {
                    return BadRequest("No resource provided for validation");
                }

                // Create an OperationOutcome for the validation results
                var outcome = new OperationOutcome
                {
                    Id = Guid.NewGuid().ToString(),
                    Meta = new Meta
                    {
                        LastUpdated = DateTimeOffset.UtcNow
                    },
                    Issue = new List<OperationOutcome.IssueComponent>()
                };

                // Determine the resource type and validate
                var resourceJson = validationRequest.ResourceToValidate;
                
                try
                {
                    // Use FhirJsonParser to parse the resource
                    var parser = new FhirJsonParser();
                    Resource resource;
                    
                    if (resourceJson is JObject jsonObject)
                    {
                        // Convert JObject to string and parse
                        string jsonString = jsonObject.ToString();
                        _logger.LogInformation("Parsing resource from JSON: {Json}", jsonString.Substring(0, Math.Min(200, jsonString.Length)));
                        resource = parser.Parse<Resource>(jsonString);
                    }
                    else if (resourceJson is string jsonString)
                    {
                        // Parse directly from string
                        _logger.LogInformation("Parsing resource from string: {Json}", jsonString.Substring(0, Math.Min(200, jsonString.Length)));
                        resource = parser.Parse<Resource>(jsonString);
                    }
                    else
                    {
                        // For other types, try to convert to JObject first
                        var serializer = new FhirJsonSerializer();
                        string serialized = serializer.SerializeToString(resourceJson);
                        _logger.LogInformation("Parsing resource from serialized object: {Json}", serialized.Substring(0, Math.Min(200, serialized.Length)));
                        resource = parser.Parse<Resource>(serialized);
                    }
                    
                    var resourceType = resource.ResourceType.ToString();
                    _logger.LogInformation("Resource type detected: {ResourceType}", resourceType);

                    // Validate the resource
                    bool isValid = true;
                    IEnumerable<string> validationIssues = new List<string>();

                    // Handle different resource types
                    if (resource is Patient patient)
                    {
                        isValid = _validator.Validate(patient, out validationIssues);
                    }
                    else if (resource is Bundle bundle)
                    {
                        // For bundles, validate each entry
                        List<string> allIssues = new List<string>();
                        foreach (var entry in bundle.Entry)
                        {
                            if (entry.Resource is Patient bundlePatient)
                            {
                                IEnumerable<string> patientIssues;
                                bool patientValid = _validator.Validate(bundlePatient, out patientIssues);
                                if (!patientValid)
                                {
                                    isValid = false;
                                    allIssues.AddRange(patientIssues.Select(issue => $"Patient {bundlePatient.Id}: {issue}"));
                                }
                            }
                            // Add more resource types as they are implemented
                        }
                        validationIssues = allIssues;
                    }
                    else
                    {
                        // For unsupported resource types
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Warning,
                            Code = OperationOutcome.IssueType.NotSupported,
                            Details = new CodeableConcept
                            {
                                Text = $"Validation for resource type {resourceType} is not yet supported"
                            }
                        });
                    }

                    // Add validation issues to the outcome
                    foreach (var issue in validationIssues)
                    {
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Error,
                            Code = OperationOutcome.IssueType.Invalid,
                            Details = new CodeableConcept
                            {
                                Text = issue
                            }
                        });
                    }

                    // If no issues were found, add a success message
                    if (outcome.Issue.Count == 0)
                    {
                        outcome.Issue.Add(new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Information,
                            Code = OperationOutcome.IssueType.Informational,
                            Details = new CodeableConcept
                            {
                                Text = $"Resource validation successful"
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing or validating resource");
                    outcome.Issue.Add(new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Error,
                        Code = OperationOutcome.IssueType.Invalid,
                        Details = new CodeableConcept
                        {
                            Text = $"Error parsing or validating resource: {ex.Message}"
                        }
                    });
                }

                return Ok(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating resource");
                return StatusCode(500, $"Error validating resource: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Request model for resource validation
    /// </summary>
    public class ValidationRequest
    {
        /// <summary>
        /// The resource to validate
        /// </summary>
        public object? ResourceToValidate { get; set; }
    }
}
