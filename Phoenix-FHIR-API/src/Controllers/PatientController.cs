using Microsoft.AspNetCore.Mvc;
using Phoenix_FHIR_API.Services;
using Phoenix_FHIR_API.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System.Linq;

namespace Phoenix_FHIR_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly ILegacyApiService _legacyApiService;
        private readonly ILogger<PatientController> _logger;
        private readonly FhirJsonSerializer _serializer;

        public PatientController(ILegacyApiService legacyApiService, ILogger<PatientController> logger)
        {
            _legacyApiService = legacyApiService;
            _logger = logger;
            _serializer = new FhirJsonSerializer(new SerializerSettings
            {
                Pretty = true
            });
        }

        /// <summary>
        /// Get all patients
        /// </summary>
        /// <returns>A list of all patients</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all patients",
            Description = "Returns a list of all patients as a FHIR Bundle",
            OperationId = "GetAllPatients",
            Tags = new[] { "Patient" }
        )]
        [SwaggerResponse(200, "A list of all patients as a FHIR Bundle", typeof(Bundle))]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                _logger.LogInformation("Getting all patients");
                var patients = await _legacyApiService.GetAllPatientsAsync();
                
                if (patients == null || patients.Count == 0)
                {
                    _logger.LogWarning("No patients found");
                    // Return an empty FHIR Bundle
                    var emptyBundle = new Bundle
                    {
                        Type = Bundle.BundleType.Searchset,
                        Total = 0,
                        Entry = new List<Bundle.EntryComponent>()
                    };
                    
                    return Ok(emptyBundle);
                }
                
                _logger.LogInformation($"Found {patients.Count} patients");
                
                // Convert to FHIR Bundle
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Searchset,
                    Total = patients.Count,
                    Entry = new List<Bundle.EntryComponent>()
                };
                
                foreach (var patient in patients)
                {
                    var fhirPatient = ConvertToFhirPatient(patient);
                    
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        Resource = fhirPatient,
                        FullUrl = $"{Request.Scheme}://{Request.Host}/api/Patient/{fhirPatient.Id}"
                    });
                }
                
                return Ok(bundle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patients");
                return StatusCode(500, $"Error getting all patients: {ex.Message}");
            }
        }

        /// <summary>
        /// Get patient by ID
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <returns>Patient details</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get patient by ID",
            Description = "Returns patient details for the specified ID as a FHIR Patient resource",
            OperationId = "GetPatientById",
            Tags = new[] { "Patient" }
        )]
        [SwaggerResponse(200, "Patient details as a FHIR Patient resource", typeof(Patient))]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            try
            {
                _logger.LogInformation($"Getting patient with ID {id}");
                var patient = await _legacyApiService.GetPatientByIdAsync(id);
                
                if (patient == null)
                {
                    _logger.LogWarning($"Patient with ID {id} not found");
                    return NotFound($"Patient with ID {id} not found");
                }
                
                _logger.LogInformation($"Found patient with ID {id}");
                
                // Convert to FHIR Patient
                var fhirPatient = ConvertToFhirPatient(patient);
                
                return Ok(fhirPatient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting patient with ID {id}");
                return StatusCode(500, $"Error getting patient with ID {id}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert DemographicsDomain to FHIR Patient
        /// </summary>
        private Patient ConvertToFhirPatient(DemographicsDomain demographics)
        {
            var fhirPatient = new Patient
            {
                Id = demographics.patientID.ToString(),
                Meta = new Meta
                {
                    LastUpdated = DateTimeOffset.UtcNow,
                    Profile = new List<string> { "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient" }
                }
            };
            
            // Add name - handle special cases where name contains DOB and ID
            if (!string.IsNullOrEmpty(demographics.firstName) || !string.IsNullOrEmpty(demographics.lastName))
            {
                var name = new HumanName();
                name.Use = HumanName.NameUse.Official;
                
                if (!string.IsNullOrEmpty(demographics.firstName))
                {
                    // Check if firstName contains DOB and ID info
                    if (demographics.firstName.Contains("DOB:") || demographics.firstName.Contains("ID:"))
                    {
                        // This is likely a misformatted name, extract just the name part
                        name.Given = new List<string> { demographics.firstName };
                    }
                    else
                    {
                        name.Given = new List<string> { demographics.firstName };
                    }
                }
                
                if (!string.IsNullOrEmpty(demographics.lastName))
                {
                    // Check if lastName contains DOB and ID info
                    if (demographics.lastName.Contains("DOB:") || demographics.lastName.Contains("ID:"))
                    {
                        // Try to extract just the name part
                        var namePart = demographics.lastName.Split(new[] { "(DOB:" }, StringSplitOptions.None)[0].Trim();
                        name.Family = namePart;
                    }
                    else
                    {
                        name.Family = demographics.lastName;
                    }
                }
                
                if (!string.IsNullOrEmpty(demographics.middleName))
                {
                    if (name.Given == null)
                    {
                        name.Given = new List<string>();
                    }
                    
                    // Create a new list with existing items plus the new item
                    var newGiven = name.Given.ToList();
                    newGiven.Add(demographics.middleName);
                    name.Given = newGiven;
                }
                
                if (!string.IsNullOrEmpty(demographics.suffix))
                {
                    name.Suffix = new List<string> { demographics.suffix };
                }
                
                fhirPatient.Name = new List<HumanName> { name };
            }
            
            // Add gender
            if (!string.IsNullOrEmpty(demographics.gender))
            {
                // Map gender to FHIR AdministrativeGender enum
                switch (demographics.gender.ToLower())
                {
                    case "male":
                    case "m":
                        fhirPatient.Gender = AdministrativeGender.Male;
                        break;
                    case "female":
                    case "f":
                        fhirPatient.Gender = AdministrativeGender.Female;
                        break;
                    case "other":
                    case "o":
                        fhirPatient.Gender = AdministrativeGender.Other;
                        break;
                    default:
                        fhirPatient.Gender = AdministrativeGender.Unknown;
                        break;
                }
            }
            
            // Add birth date
            if (demographics.birthDate.HasValue)
            {
                fhirPatient.BirthDate = demographics.birthDate.Value.ToString("yyyy-MM-dd");
            }
            
            // Add address
            if (!string.IsNullOrEmpty(demographics.address1) || 
                !string.IsNullOrEmpty(demographics.city) || 
                !string.IsNullOrEmpty(demographics.state) || 
                !string.IsNullOrEmpty(demographics.zip))
            {
                var address = new Address
                {
                    Use = Address.AddressUse.Home
                };
                
                if (!string.IsNullOrEmpty(demographics.address1))
                {
                    address.Line = new List<string> { demographics.address1 };
                    
                    if (!string.IsNullOrEmpty(demographics.address2))
                    {
                        // Create a new list with existing items plus the new item
                        var newLines = address.Line.ToList();
                        newLines.Add(demographics.address2);
                        address.Line = newLines;
                    }
                }
                
                if (!string.IsNullOrEmpty(demographics.city))
                {
                    address.City = demographics.city;
                }
                
                if (!string.IsNullOrEmpty(demographics.state))
                {
                    address.State = demographics.state;
                }
                
                if (!string.IsNullOrEmpty(demographics.zip))
                {
                    address.PostalCode = demographics.zip;
                }
                
                fhirPatient.Address = new List<Address> { address };
            }
            
            // Add telecom (phone, email)
            var telecom = new List<ContactPoint>();
            
            if (!string.IsNullOrEmpty(demographics.homePhone))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Use = ContactPoint.ContactPointUse.Home,
                    Value = demographics.homePhone
                });
            }
            
            if (!string.IsNullOrEmpty(demographics.cellPhone))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Use = ContactPoint.ContactPointUse.Mobile,
                    Value = demographics.cellPhone
                });
            }
            
            if (!string.IsNullOrEmpty(demographics.workPhone))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Use = ContactPoint.ContactPointUse.Work,
                    Value = demographics.workPhone
                });
            }
            
            if (!string.IsNullOrEmpty(demographics.email))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Use = ContactPoint.ContactPointUse.Home,
                    Value = demographics.email
                });
            }
            
            if (telecom.Count > 0)
            {
                fhirPatient.Telecom = telecom;
            }
            
            // Add identifiers
            fhirPatient.Identifier = new List<Identifier>
            {
                new Identifier
                {
                    System = "http://example.org/fhir/identifier/mrn",
                    Value = demographics.patientID.ToString(),
                    Use = Identifier.IdentifierUse.Official
                }
            };
            
            return fhirPatient;
        }
    }
}
