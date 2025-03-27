using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

namespace Phoenix_FHIR_API.Controllers
{
    [ApiController]
    [Route("api/fhir")]
    public class MetadataController : ControllerBase
    {
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(ILogger<MetadataController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get the FHIR CapabilityStatement for this server
        /// </summary>
        /// <returns>A FHIR CapabilityStatement resource</returns>
        [HttpGet("metadata")]
        [HttpGet(".well-known/smart-configuration")]
        [SwaggerOperation(
            Summary = "Get the FHIR CapabilityStatement for this server",
            Description = "Returns a FHIR CapabilityStatement resource describing the server's capabilities",
            OperationId = "GetMetadata",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "The FHIR CapabilityStatement resource", typeof(CapabilityStatement))]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public IActionResult GetMetadata()
        {
            try
            {
                var capabilityStatement = GenerateCapabilityStatement();
                return Ok(capabilityStatement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CapabilityStatement");
                return StatusCode(500, $"Error generating CapabilityStatement: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a FHIR CapabilityStatement resource
        /// </summary>
        /// <returns>A FHIR CapabilityStatement resource</returns>
        private CapabilityStatement GenerateCapabilityStatement()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0";

            var capabilityStatement = new CapabilityStatement
            {
                Status = PublicationStatus.Active,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Kind = CapabilityStatementKind.Instance,
                Software = new CapabilityStatement.SoftwareComponent
                {
                    Name = "Phoenix FHIR API",
                    Version = version,
                    ReleaseDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                },
                Implementation = new CapabilityStatement.ImplementationComponent
                {
                    Description = new Markdown("Phoenix FHIR API - FHIR R4 Implementation"),
                    Url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/fhir"
                },
                FhirVersion = FHIRVersion.N4_0_1,
                Format = new[] { "application/fhir+json" },
                Rest = new List<CapabilityStatement.RestComponent>
                {
                    new CapabilityStatement.RestComponent
                    {
                        Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                        Documentation = new Markdown("RESTful FHIR API for Phoenix Healthcare"),
                        Security = new CapabilityStatement.SecurityComponent
                        {
                            Cors = true,
                            Service = new List<CodeableConcept>
                            {
                                new CodeableConcept
                                {
                                    Coding = new List<Coding>
                                    {
                                        new Coding
                                        {
                                            System = "http://terminology.hl7.org/CodeSystem/restful-security-service",
                                            Code = "SMART-on-FHIR",
                                            Display = "SMART on FHIR"
                                        }
                                    },
                                    Text = "OAuth2 using SMART-on-FHIR profile (not yet implemented)"
                                }
                            },
                            Description = new Markdown("Authentication and authorization is handled using SMART on FHIR (not yet implemented)")
                        },
                        Resource = GetSupportedResources()
                    }
                }
            };

            return capabilityStatement;
        }

        /// <summary>
        /// Gets the supported resources for the CapabilityStatement
        /// This method should be updated as new resources are implemented
        /// </summary>
        /// <returns>A list of ResourceComponent objects</returns>
        private List<CapabilityStatement.ResourceComponent> GetSupportedResources()
        {
            var resources = new List<CapabilityStatement.ResourceComponent>();

            // Patient Resource
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Patient.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read a Patient resource by its ID")
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Create,
                        Documentation = new Markdown("Create a new Patient resource")
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Update,
                        Documentation = new Markdown("Update an existing Patient resource")
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Delete,
                        Documentation = new Markdown("Delete a Patient resource")
                    }
                },
                Operation = new List<CapabilityStatement.OperationComponent>
                {
                    new CapabilityStatement.OperationComponent
                    {
                        Name = "everything",
                        Definition = "http://hl7.org/fhir/OperationDefinition/Patient-everything",
                        Documentation = new Markdown("Get a Bundle of all resources related to the Patient")
                    }
                },
                ReadHistory = false,
                UpdateCreate = true,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "_id",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The ID of the resource")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "identifier",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("A patient identifier (MRN, etc.)")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the patient's name")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "family",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the patient's family name")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "given",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the patient's given name")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "birthdate",
                        Type = SearchParamType.Date,
                        Documentation = new Markdown("The patient's date of birth")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "gender",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The patient's gender (male | female | other | unknown)")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "phone",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The patient's phone number")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "email",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The patient's email address")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "address",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the patient's address")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "address-city",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("The patient's city")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "address-state",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("The patient's state")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "address-postalcode",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("The patient's postal code")
                    }
                }
            });

            // Practitioner Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Practitioner.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/Practitioner",
                Documentation = new Markdown("Practitioner resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read a Practitioner resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "_id",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The ID of the resource")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the practitioner's name")
                    }
                }
            });

            // Organization Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Organization.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/Organization",
                Documentation = new Markdown("Organization resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read an Organization resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "_id",
                        Type = SearchParamType.Token,
                        Documentation = new Markdown("The ID of the resource")
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = new Markdown("A portion of the organization's name")
                    }
                }
            });

            // Location Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Location.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/Location",
                Documentation = new Markdown("Location resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read a Location resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
            });

            // AllergyIntolerance Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.AllergyIntolerance.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/AllergyIntolerance",
                Documentation = new Markdown("AllergyIntolerance resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read an AllergyIntolerance resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "patient",
                        Type = SearchParamType.Reference,
                        Documentation = new Markdown("Who the sensitivity is for")
                    }
                }
            });

            // Condition Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Condition.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/Condition",
                Documentation = new Markdown("Condition resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read a Condition resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "patient",
                        Type = SearchParamType.Reference,
                        Documentation = new Markdown("Who has the condition")
                    }
                }
            });

            // MedicationStatement Resource (planned)
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.MedicationStatement.ToString(),
                Profile = "http://hl7.org/fhir/StructureDefinition/MedicationStatement",
                Documentation = new Markdown("MedicationStatement resource is planned for future implementation"),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read,
                        Documentation = new Markdown("Read a MedicationStatement resource by its ID (planned)")
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "patient",
                        Type = SearchParamType.Reference,
                        Documentation = new Markdown("Who is taking the medication")
                    }
                }
            });

            return resources;
        }
    }
}
