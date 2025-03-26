using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Phoenix_AzureAPI.Services;
using Phoenix_AzureAPI.Services.FHIR;
using Phoenix_AzureAPI.Models;
using DomainPatient = Phoenix_AzureAPI.Models.Patient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/fhir")]
    public class FhirController : ControllerBase
    {
        private readonly ILogger<FhirController> _logger;
        private readonly IFhirService _fhirService;
        private readonly IPatientFhirMapper _patientMapper;
        private readonly PatientDataService _patientDataService;

        public FhirController(
            ILogger<FhirController> logger,
            IFhirService fhirService,
            IPatientFhirMapper patientMapper,
            PatientDataService patientDataService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
            _patientMapper = patientMapper ?? throw new ArgumentNullException(nameof(patientMapper));
            _patientDataService = patientDataService ?? throw new ArgumentNullException(nameof(patientDataService));
        }

        /// <summary>
        /// Get a FHIR Patient resource by ID
        /// </summary>
        /// <param name="id">The patient ID (optional)</param>
        /// <returns>A FHIR Patient resource or a Bundle of all patients if no ID is provided</returns>
        [HttpGet("Patient/{id?}")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetPatient(int? id = null)
        {
            try
            {
                // If no ID is provided, return all patients
                if (!id.HasValue)
                {
                    return await GetAllPatients();
                }
                
                // Get the patient from the database
                var patient = await GetPatientFromDatabase(id.Value);
                
                // Map to FHIR Patient
                var fhirPatient = _patientMapper.MapToFhir(patient);
                
                // Serialize to JSON
                var json = _fhirService.SerializeToJson(fhirPatient);
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex) when (ex.Message.Contains("not found") || ex.Message.Contains("NotFound"))
            {
                _logger.LogWarning(ex, "Patient with ID {Id} not found", id);
                return NotFound($"Patient with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FHIR Patient with ID {Id}", id);
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all FHIR Patient resources
        /// </summary>
        /// <returns>A Bundle of FHIR Patient resources</returns>
        [HttpGet("Patient")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                // Get all patients from the database
                var patients = await GetAllPatientsFromDatabase();
                
                // Map to FHIR Patients
                var fhirPatients = patients.Select(p => _patientMapper.MapToFhir(p)).ToList();
                
                // Create a Bundle
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Searchset,
                    Total = fhirPatients.Count
                };
                
                // Add entries to the bundle
                foreach (var fhirPatient in fhirPatients)
                {
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        Resource = fhirPatient,
                        FullUrl = $"Patient/{fhirPatient.Id}"
                    });
                }
                
                // Serialize to JSON
                var json = _fhirService.SerializeToJson(bundle);
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all FHIR Patients");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the FHIR capability statement (metadata)
        /// </summary>
        /// <returns>A FHIR CapabilityStatement resource</returns>
        [HttpGet("metadata")]
        [Produces("application/fhir+json")]
        public IActionResult GetCapabilityStatement()
        {
            try
            {
                // Create a new capability statement
                var capabilityStatement = new CapabilityStatement
                {
                    Status = PublicationStatus.Active,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Kind = CapabilityStatementKind.Instance,
                    Software = new CapabilityStatement.SoftwareComponent
                    {
                        Name = "Phoenix-Azure FHIR API",
                        Version = "1.0.0"
                    },
                    FhirVersion = FHIRVersion.N4_0_1,
                    Format = new string[] { "application/fhir+json" },
                    Rest = new List<CapabilityStatement.RestComponent>
                    {
                        new CapabilityStatement.RestComponent
                        {
                            Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                            Resource = new List<CapabilityStatement.ResourceComponent>
                            {
                                new CapabilityStatement.ResourceComponent
                                {
                                    Type = ResourceType.Patient.ToString(),
                                    Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                                    Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                                    {
                                        new CapabilityStatement.ResourceInteractionComponent
                                        {
                                            Code = CapabilityStatement.TypeRestfulInteraction.Read
                                        },
                                        new CapabilityStatement.ResourceInteractionComponent
                                        {
                                            Code = CapabilityStatement.TypeRestfulInteraction.SearchType
                                        }
                                    }
                                }
                            },
                            Operation = new List<CapabilityStatement.OperationComponent>
                            {
                                new CapabilityStatement.OperationComponent
                                {
                                    Name = "validate",
                                    Definition = "http://hl7.org/fhir/OperationDefinition/Resource-validate"
                                }
                            }
                        }
                    }
                };

                // Serialize to JSON
                var json = _fhirService.SerializeToJson(capabilityStatement);
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating capability statement");
                return StatusCode(500, "Error generating capability statement");
            }
        }

        /// <summary>
        /// Validate a FHIR resource
        /// </summary>
        /// <returns>A FHIR OperationOutcome with validation results</returns>
        [HttpPost("$validate")]
        [Produces("application/fhir+json")]
        [Consumes("application/json", "application/fhir+json")]
        public IActionResult ValidateResource([FromBody] object resourceJson)
        {
            try
            {
                _logger.LogInformation($"Received request for validation");
                
                if (resourceJson == null)
                {
                    return BadRequest("No resource provided for validation");
                }

                // Convert the resource to a string if it's not already
                string jsonString = resourceJson.ToString();
                if (resourceJson is JsonElement jsonElement)
                {
                    jsonString = jsonElement.GetRawText();
                }
                else
                {
                    jsonString = System.Text.Json.JsonSerializer.Serialize(resourceJson);
                }
                
                _logger.LogInformation($"Parsed resource JSON: {jsonString}");
                
                Resource resource;
                try {
                    resource = _fhirService.ParseResource(jsonString);
                    _logger.LogInformation($"Successfully parsed resource of type: {resource.TypeName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing FHIR resource for validation");
                    
                    // Create an error outcome
                    var errorOutcome = new OperationOutcome
                    {
                        Issue = new List<OperationOutcome.IssueComponent>
                        {
                            new OperationOutcome.IssueComponent
                            {
                                Severity = OperationOutcome.IssueSeverity.Error,
                                Code = OperationOutcome.IssueType.Invalid,
                                Diagnostics = $"Invalid FHIR resource format: {ex.Message}"
                            }
                        }
                    };
                    
                    var errorJson = _fhirService.SerializeToJson(errorOutcome);
                    return BadRequest(errorJson);
                }
                
                // Validate the resource
                var validationResult = _fhirService.Validate(resource);
                
                // Return the validation outcome
                var json = _fhirService.SerializeToJson(validationResult.OperationOutcome ?? new OperationOutcome());
                
                // Return 422 if validation failed, 200 if successful
                if (!validationResult.IsValid)
                {
                    return StatusCode(422, json);
                }
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating FHIR resource");
                
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
                
                var errorJson = _fhirService.SerializeToJson(errorOutcome);
                return StatusCode(500, errorJson);
            }
        }

        /// <summary>
        /// Get all patient IDs
        /// </summary>
        /// <returns>A list of patient IDs</returns>
        [HttpGet("PatientIds")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAllPatientIds()
        {
            try
            {
                var patientIds = await _patientDataService.GetAllPatientIdsAsync();
                return Ok(patientIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patient IDs");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
            }
        }

        // Helper methods to get patient data
        private async Task<DomainPatient> GetPatientFromDatabase(int? id)
        {
            if (!id.HasValue)
            {
                throw new ArgumentNullException(nameof(id), "Patient ID is required");
            }

            try
            {
                // Use the PatientDataService to get actual patient data from the database
                return await _patientDataService.GetPatientByIdAsync(id.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient with ID {id} from database");
                throw;
            }
        }

        private async Task<List<DomainPatient>> GetAllPatientsFromDatabase()
        {
            try
            {
                // Use the PatientDataService to get all patients from the database
                var patients = await _patientDataService.GetAllPatientsAsync();
                return patients.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients from database");
                throw;
            }
        }
    }
}
