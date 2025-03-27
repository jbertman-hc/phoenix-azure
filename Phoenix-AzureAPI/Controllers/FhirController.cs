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
        public async System.Threading.Tasks.Task<IActionResult> GetPatient(int? id = null)
        {
            try
            {
                // If no ID is provided, return all patients
                if (!id.HasValue)
                {
                    return await GetAllPatients();
                }
                
                _logger.LogInformation($"Getting FHIR Patient with ID {id}");
                
                // Get the patient from the database
                DomainPatient patient = null;
                try
                {
                    patient = await GetPatientFromDatabase(id.Value);
                    _logger.LogInformation($"Successfully retrieved patient with ID {id} from database");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error retrieving patient with ID {id}");
                    return NotFound($"Patient with ID {id} not found");
                }
                
                // Map to FHIR Patient
                var fhirPatient = _patientMapper.MapToFhir(patient);
                _logger.LogInformation($"Mapped patient {id} to FHIR resource with ID {fhirPatient.Id}");
                
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
        [ProducesResponseType(typeof(Bundle), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/fhir+json")] // This specifies that we return FHIR-formatted JSON
        public async System.Threading.Tasks.Task<IActionResult> GetAllPatients()
        {
            try
            {
                _logger.LogInformation("Getting all FHIR Patients");
                
                // Step 1: Get all patients from the database as domain models
                // This calls the GetAllPatientsFromDatabase method which uses PatientDataService
                // to fetch patients from the addendum endpoint
                var patients = await GetAllPatientsFromDatabase();
                
                _logger.LogInformation($"Retrieved {patients.Count} patients from database");
                
                // Step 2: Map domain Patient models to FHIR Patient resources
                // The PatientMapper converts our internal Patient model to the FHIR standard format
                var fhirPatients = patients.Select(p => _patientMapper.MapToFhir(p)).ToList();
                
                _logger.LogInformation($"Mapped {fhirPatients.Count} patients to FHIR resources");
                
                // Step 3: Create a FHIR Bundle to contain all the Patient resources
                // Use the Firely SDK's built-in methods to ensure FHIR compliance
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Searchset,
                    Total = fhirPatients.Count,
                    Timestamp = DateTimeOffset.UtcNow
                };
                
                // Add a self link (required for searchsets)
                bundle.Link = new List<Bundle.LinkComponent>
                {
                    new Bundle.LinkComponent
                    {
                        Relation = "self",
                        Url = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}"
                    }
                };
                
                // Step 4: Add each FHIR Patient resource to the Bundle as an entry
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var seenIds = new HashSet<string>(); // Track IDs to ensure uniqueness
                
                foreach (var fhirPatient in fhirPatients)
                {
                    var patientId = fhirPatient.Id;
                    
                    // If we've seen this ID before, make it unique with a version ID
                    if (seenIds.Contains(patientId))
                    {
                        if (fhirPatient.Meta == null)
                            fhirPatient.Meta = new Meta();
                            
                        fhirPatient.Meta.VersionId = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        seenIds.Add(patientId);
                    }
                    
                    // Create an absolute URL for the fullUrl (required by FHIR spec)
                    var fullUrl = $"{baseUrl}/api/fhir/Patient/{patientId}";
                    
                    // Add entry to the bundle with search mode
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        Resource = fhirPatient,
                        FullUrl = fullUrl,
                        Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
                    });
                }
                
                _logger.LogInformation($"Created FHIR Bundle with {bundle.Entry.Count} entries");
                
                // Step 5: Serialize the Bundle to FHIR JSON format
                // This ensures the output conforms to the FHIR specification
                var json = _fhirService.SerializeToJson(bundle);
                
                // Step 6: Return the serialized Bundle with the correct FHIR content type
                // The content type is important for FHIR clients to recognize this as FHIR data
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
        public IActionResult ValidateResource([FromBody] object requestBody)
        {
            try
            {
                _logger.LogInformation("Received request for validation");
                
                if (requestBody == null)
                {
                    _logger.LogWarning("No request body provided for validation");
                    return BadRequest("No resource provided for validation");
                }

                // Convert the request body to a string
                string jsonString;
                
                try
                {
                    // Handle different request formats
                    if (requestBody is JObject jObject)
                    {
                        // Check if the request has a resourceToValidate property
                        if (jObject.TryGetValue("resourceToValidate", out JToken resourceToken))
                        {
                            _logger.LogInformation("Found resourceToValidate in request");
                            jsonString = resourceToken.ToString();
                        }
                        else
                        {
                            // If not, assume the entire request is the resource
                            jsonString = jObject.ToString();
                            _logger.LogInformation("Using entire request as resource");
                        }
                    }
                    else
                    {
                        // Direct serialization of the request body
                        jsonString = System.Text.Json.JsonSerializer.Serialize(requestBody);
                        _logger.LogInformation("Serialized request body directly");
                    }
                    
                    _logger.LogInformation($"Resource JSON length: {jsonString.Length} characters");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request body");
                    return BadRequest($"Invalid request format: {ex.Message}");
                }
                
                Resource resource;
                try {
                    // Try to parse the resource with explicit type information
                    JObject jsonObject = JObject.Parse(jsonString);
                    
                    // Get the resource type from the JSON
                    string resourceType = null;
                    if (jsonObject.TryGetValue("resourceType", out JToken resourceTypeToken))
                    {
                        resourceType = resourceTypeToken.ToString();
                        _logger.LogInformation($"Resource type from JSON: {resourceType}");
                    }
                    else
                    {
                        _logger.LogWarning("No resourceType found in JSON");
                        return BadRequest("Invalid FHIR resource: missing resourceType property");
                    }
                    
                    // Parse the resource based on its type
                    switch (resourceType)
                    {
                        case "Patient":
                            resource = _fhirService.ParseResource<Hl7.Fhir.Model.Patient>(jsonString);
                            break;
                        case "Bundle":
                            resource = _fhirService.ParseResource<Hl7.Fhir.Model.Bundle>(jsonString);
                            break;
                        case "CapabilityStatement":
                            resource = _fhirService.ParseResource<Hl7.Fhir.Model.CapabilityStatement>(jsonString);
                            break;
                        default:
                            // For unknown types, try generic parsing
                            resource = _fhirService.ParseResource(jsonString);
                            break;
                    }
                    
                    _logger.LogInformation("Successfully parsed resource of type: {ResourceType}", resource.TypeName);
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
        public async System.Threading.Tasks.Task<IActionResult> GetAllPatientIds()
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

        /// <summary>
        /// Get a complete FHIR Bundle containing all resources for a patient
        /// </summary>
        /// <param name="id">The patient ID</param>
        /// <returns>A FHIR Bundle containing the Patient resource and all related resources</returns>
        [HttpGet("Bundle/Patient/{id}")]
        [Produces("application/fhir+json")]
        public async System.Threading.Tasks.Task<IActionResult> GetPatientBundle(int id)
        {
            try
            {
                _logger.LogInformation($"Getting complete FHIR Bundle for Patient with ID {id}");
                
                // Get the patient from the database
                DomainPatient patient = null;
                try
                {
                    patient = await GetPatientFromDatabase(id);
                    _logger.LogInformation($"Successfully retrieved patient with ID {id} from database");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error retrieving patient with ID {id}");
                    return NotFound($"Patient with ID {id} not found");
                }
                
                // Create a new FHIR Bundle
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
                
                // Add the Patient resource to the bundle
                var fhirPatient = _patientMapper.MapToFhir(patient);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{Guid.NewGuid()}",
                    Resource = fhirPatient
                });
                
                // Get all medical records for the patient
                var medicalRecords = await GetPatientMedicalRecords(id);
                
                // Add each medical record as a resource to the bundle
                if (medicalRecords != null)
                {
                    // Add allergies as AllergyIntolerance resources
                    var allergies = medicalRecords.FirstOrDefault(r => r.Type == "Allergies")?.Data;
                    if (allergies != null)
                    {
                        await AddAllergiesToBundle(bundle, allergies, id);
                    }
                    
                    // Add medications as MedicationStatement resources
                    var medications = medicalRecords.FirstOrDefault(r => r.Type == "Medications")?.Data;
                    if (medications != null)
                    {
                        await AddMedicationsToBundle(bundle, medications, id);
                    }
                    
                    // Add problems as Condition resources
                    var problems = medicalRecords.FirstOrDefault(r => r.Type == "Problems")?.Data;
                    if (problems != null)
                    {
                        await AddProblemsToBundle(bundle, problems, id);
                    }
                    
                    // Add notes as DocumentReference resources
                    var notes = medicalRecords.FirstOrDefault(r => r.Type == "Notes")?.Data;
                    if (notes != null)
                    {
                        await AddNotesToBundle(bundle, notes, id);
                    }
                }
                
                // Serialize to JSON
                var json = _fhirService.SerializeToJson(bundle);
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting FHIR Bundle for patient with ID {id}");
                return StatusCode(500, $"Error retrieving FHIR Bundle for patient with ID {id}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get all medical records for a patient
        /// </summary>
        private async System.Threading.Tasks.Task<List<MedicalRecordData>> GetPatientMedicalRecords(int patientId)
        {
            try
            {
                // Use the PatientController to get medical records
                var patientController = new PatientController(
                    _logger,
                    HttpContext.RequestServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory);
                
                var result = await patientController.GetPatientMedicalRecords(patientId);
                
                if (result.Result is OkObjectResult okResult)
                {
                    return okResult.Value as List<MedicalRecordData>;
                }
                
                return new List<MedicalRecordData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting medical records for patient {patientId}");
                return new List<MedicalRecordData>();
            }
        }
        
        /// <summary>
        /// Add allergies to the FHIR Bundle as AllergyIntolerance resources
        /// </summary>
        private async System.Threading.Tasks.Task AddAllergiesToBundle(Bundle bundle, object allergiesData, int patientId)
        {
            try
            {
                // Convert allergies data to a list of objects
                var allergies = ConvertToList(allergiesData);
                
                foreach (var allergyObj in allergies)
                {
                    // Create a new AllergyIntolerance resource
                    var allergy = new AllergyIntolerance
                    {
                        Id = Guid.NewGuid().ToString(),
                        Meta = new Meta
                        {
                            LastUpdated = DateTime.UtcNow
                        },
                        Patient = new ResourceReference($"Patient/{patientId}"),
                        RecordedDate = DateTime.UtcNow
                    };
                    
                    // Extract allergy properties using dynamic object
                    dynamic allergyDynamic = JObject.FromObject(allergyObj);
                    
                    // Set the allergy code
                    try
                    {
                        string allergyName = allergyDynamic.allergyName ?? allergyDynamic.substance ?? "Unknown";
                        allergy.Code = new CodeableConcept
                        {
                            Text = allergyName
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the allergy reaction
                    try
                    {
                        string reaction = allergyDynamic.reaction ?? "Not specified";
                        allergy.Reaction = new List<AllergyIntolerance.ReactionComponent>
                        {
                            new AllergyIntolerance.ReactionComponent
                            {
                                Description = reaction
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the allergy status
                    try
                    {
                        string status = allergyDynamic.status ?? "active";
                        allergy.ClinicalStatus = new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new Coding
                                {
                                    System = "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical",
                                    Code = status.ToLower() == "active" ? "active" : "inactive",
                                    Display = status.ToLower() == "active" ? "Active" : "Inactive"
                                }
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Add the allergy to the bundle
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{allergy.Id}",
                        Resource = allergy
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding allergies to FHIR Bundle");
            }
        }
        
        /// <summary>
        /// Add medications to the FHIR Bundle as MedicationStatement resources
        /// </summary>
        private async System.Threading.Tasks.Task AddMedicationsToBundle(Bundle bundle, object medicationsData, int patientId)
        {
            try
            {
                // Convert medications data to a list of objects
                var medications = ConvertToList(medicationsData);
                
                foreach (var medicationObj in medications)
                {
                    // Create a new MedicationStatement resource
                    var medicationStatement = new MedicationStatement
                    {
                        Id = Guid.NewGuid().ToString(),
                        Meta = new Meta
                        {
                            LastUpdated = DateTime.UtcNow
                        },
                        Subject = new ResourceReference($"Patient/{patientId}"),
                        DateAsserted = DateTime.UtcNow
                    };
                    
                    // Extract medication properties using dynamic object
                    dynamic medicationDynamic = JObject.FromObject(medicationObj);
                    
                    // Set the medication code
                    try
                    {
                        string medicationName = medicationDynamic.medicationName ?? medicationDynamic.name ?? "Unknown";
                        medicationStatement.Medication = new CodeableConcept
                        {
                            Text = medicationName
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the dosage
                    try
                    {
                        string dosage = medicationDynamic.dosage ?? medicationDynamic.dose ?? "Not specified";
                        string frequency = medicationDynamic.frequency ?? medicationDynamic.schedule ?? "Not specified";
                        
                        medicationStatement.DosageInstruction = new List<Dosage>
                        {
                            new Dosage
                            {
                                Text = $"{dosage}, {frequency}"
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the status
                    try
                    {
                        string status = medicationDynamic.status ?? "active";
                        medicationStatement.Status = status.ToLower() == "active" 
                            ? MedicationStatement.MedicationStatementStatus.Active 
                            : MedicationStatement.MedicationStatementStatus.Inactive;
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Add the medication to the bundle
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{medicationStatement.Id}",
                        Resource = medicationStatement
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medications to FHIR Bundle");
            }
        }
        
        /// <summary>
        /// Add problems to the FHIR Bundle as Condition resources
        /// </summary>
        private async System.Threading.Tasks.Task AddProblemsToBundle(Bundle bundle, object problemsData, int patientId)
        {
            try
            {
                // Convert problems data to a list of objects
                var problems = ConvertToList(problemsData);
                
                foreach (var problemObj in problems)
                {
                    // Create a new Condition resource
                    var condition = new Condition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Meta = new Meta
                        {
                            LastUpdated = DateTime.UtcNow
                        },
                        Subject = new ResourceReference($"Patient/{patientId}"),
                        RecordedDate = DateTime.UtcNow
                    };
                    
                    // Extract problem properties using dynamic object
                    dynamic problemDynamic = JObject.FromObject(problemObj);
                    
                    // Set the problem code
                    try
                    {
                        string problemName = problemDynamic.problemName ?? problemDynamic.name ?? problemDynamic.diagnosis ?? "Unknown";
                        condition.Code = new CodeableConcept
                        {
                            Text = problemName
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the onset date
                    try
                    {
                        if (problemDynamic.dateIdentified != null)
                        {
                            string dateStr = problemDynamic.dateIdentified.ToString();
                            if (DateTime.TryParse(dateStr, out DateTime date))
                            {
                                condition.OnsetDateTime = date;
                            }
                        }
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the status
                    try
                    {
                        string status = problemDynamic.status ?? "active";
                        condition.ClinicalStatus = new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new Coding
                                {
                                    System = "http://terminology.hl7.org/CodeSystem/condition-clinical",
                                    Code = status.ToLower() == "active" ? "active" : "inactive",
                                    Display = status.ToLower() == "active" ? "Active" : "Inactive"
                                }
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Add the problem to the bundle
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{condition.Id}",
                        Resource = condition
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding problems to FHIR Bundle");
            }
        }
        
        /// <summary>
        /// Add notes to the FHIR Bundle as DocumentReference resources
        /// </summary>
        private async System.Threading.Tasks.Task AddNotesToBundle(Bundle bundle, object notesData, int patientId)
        {
            try
            {
                // Convert notes data to a list of objects
                var notes = ConvertToList(notesData);
                
                foreach (var noteObj in notes)
                {
                    // Create a new DocumentReference resource
                    var documentReference = new DocumentReference
                    {
                        Id = Guid.NewGuid().ToString(),
                        Meta = new Meta
                        {
                            LastUpdated = DateTime.UtcNow
                        },
                        Subject = new ResourceReference($"Patient/{patientId}"),
                        Date = DateTime.UtcNow
                    };
                    
                    // Extract note properties using dynamic object
                    dynamic noteDynamic = JObject.FromObject(noteObj);
                    
                    // Set the note type
                    try
                    {
                        string noteSubject = noteDynamic.noteSubject ?? "Clinical Note";
                        documentReference.Type = new CodeableConcept
                        {
                            Text = noteSubject
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the note content
                    try
                    {
                        string noteBody = noteDynamic.noteBody ?? "";
                        documentReference.Content = new List<DocumentReference.ContentComponent>
                        {
                            new DocumentReference.ContentComponent
                            {
                                Attachment = new Attachment
                                {
                                    ContentType = "text/plain",
                                    Data = System.Text.Encoding.UTF8.GetBytes(noteBody)
                                }
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the author
                    try
                    {
                        string savedBy = noteDynamic.savedBy ?? "Unknown";
                        documentReference.Author = new List<ResourceReference>
                        {
                            new ResourceReference
                            {
                                Display = savedBy
                            }
                        };
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Set the date
                    try
                    {
                        if (noteDynamic.date != null)
                        {
                            string dateStr = noteDynamic.date.ToString();
                            if (DateTime.TryParse(dateStr, out DateTime date))
                            {
                                documentReference.Date = date;
                            }
                        }
                    }
                    catch { /* Ignore if property doesn't exist */ }
                    
                    // Add the note to the bundle
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        FullUrl = $"urn:uuid:{documentReference.Id}",
                        Resource = documentReference
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notes to FHIR Bundle");
            }
        }
        
        /// <summary>
        /// Helper method to convert an object to a list of objects
        /// </summary>
        private List<object> ConvertToList(object data)
        {
            if (data == null)
                return new List<object>();
                
            if (data is IEnumerable<object> enumerable)
                return enumerable.ToList();
                
            if (data is JArray jArray)
                return jArray.ToObject<List<object>>();
                
            // If it's a single object, wrap it in a list
            return new List<object> { data };
        }

        // Helper methods to get patient data
        private async System.Threading.Tasks.Task<DomainPatient> GetPatientFromDatabase(int? id)
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

        private async System.Threading.Tasks.Task<List<DomainPatient>> GetAllPatientsFromDatabase()
        {
            try
            {
                // Use the PatientDataService to get all patients from the database
                var patients = await _patientDataService.GetAllPatientsAsync();
                _logger.LogInformation($"PatientDataService returned {patients.Count()} patients");
                
                // If no patients were returned, log a warning
                if (!patients.Any())
                {
                    _logger.LogWarning("No patients returned from GetAllPatientsAsync");
                    return new List<DomainPatient>();
                }
                
                // Create a dictionary to track unique patients by ID
                var uniquePatients = new Dictionary<int, DomainPatient>();
                
                // Add patients from the main query
                foreach (var patient in patients)
                {
                    if (patient != null && patient.PatientID > 0 && !uniquePatients.ContainsKey(patient.PatientID))
                    {
                        uniquePatients[patient.PatientID] = patient;
                    }
                }
                
                _logger.LogInformation($"Returning {uniquePatients.Count} unique patients");
                return uniquePatients.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients from database");
                throw;
            }
        }
    }
}
