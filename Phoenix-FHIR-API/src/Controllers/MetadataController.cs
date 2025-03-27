using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Swashbuckle.AspNetCore.Annotations;

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
        /// Get the FHIR server's capability statement (metadata)
        /// </summary>
        /// <returns>The FHIR capability statement</returns>
        [HttpGet("metadata")]
        [HttpGet(".well-known/smart-configuration")]
        [SwaggerOperation(
            Summary = "Get the FHIR server's capability statement",
            Description = "Returns a FHIR CapabilityStatement resource describing the server's capabilities",
            OperationId = "GetMetadata",
            Tags = new[] { "FHIR" }
        )]
        [SwaggerResponse(200, "The FHIR capability statement", typeof(CapabilityStatement))]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/fhir+json")]
        public IActionResult GetMetadata()
        {
            try
            {
                var capabilityStatement = CreateCapabilityStatement();
                return Ok(capabilityStatement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating capability statement");
                return StatusCode(500, $"Error generating capability statement: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a FHIR CapabilityStatement resource describing the server's capabilities
        /// </summary>
        /// <returns>A FHIR CapabilityStatement resource</returns>
        private CapabilityStatement CreateCapabilityStatement()
        {
            var capabilityStatement = new CapabilityStatement
            {
                Status = PublicationStatus.Active,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Kind = CapabilityStatement.CapabilityStatementKind.Instance,
                Software = new CapabilityStatement.SoftwareComponent
                {
                    Name = "Phoenix FHIR API",
                    Version = "1.0.0",
                    ReleaseDate = "2025-03-27"
                },
                Implementation = new CapabilityStatement.ImplementationComponent
                {
                    Description = "Phoenix FHIR API Transformation Layer",
                    Url = "https://api.example.org/fhir"
                },
                FhirVersion = FHIRVersion.N4_0_1,
                Format = new string[] { "application/fhir+json" },
                Rest = new List<CapabilityStatement.RestComponent>
                {
                    new CapabilityStatement.RestComponent
                    {
                        Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                        Documentation = "RESTful FHIR Server for Phoenix FHIR API",
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
                            }
                        },
                        Resource = GetSupportedResources()
                    }
                }
            };

            return capabilityStatement;
        }

        /// <summary>
        /// Gets a list of supported resources and their capabilities
        /// This method should be updated as new resources are implemented
        /// </summary>
        /// <returns>A list of ResourceComponents describing supported resources</returns>
        private List<CapabilityStatement.ResourceComponent> GetSupportedResources()
        {
            var resources = new List<CapabilityStatement.ResourceComponent>();

            // Patient resource - IMPLEMENTED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Patient,
                Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Create
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Update
                    },
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Delete
                    }
                },
                Operation = new List<CapabilityStatement.OperationComponent>
                {
                    new CapabilityStatement.OperationComponent
                    {
                        Name = "everything",
                        Definition = "http://hl7.org/fhir/OperationDefinition/Patient-everything"
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported,
                SearchInclude = new string[] { "Patient:organization" },
                SearchRevInclude = new string[] { "AllergyIntolerance:patient", "Condition:patient", "MedicationStatement:patient" },
                SearchParam = new List<CapabilityStatement.SearchParamComponent>
                {
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "_id",
                        Type = SearchParamType.Token,
                        Documentation = "The ID of the resource"
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = "A portion of the family or given name of the patient"
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "identifier",
                        Type = SearchParamType.Token,
                        Documentation = "A patient identifier (MRN, etc.)"
                    }
                }
            });

            // Practitioner resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Practitioner,
                Profile = "http://hl7.org/fhir/StructureDefinition/Practitioner",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "The ID of the resource"
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = "A portion of the family or given name of the practitioner"
                    }
                }
            });

            // Organization resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Organization,
                Profile = "http://hl7.org/fhir/StructureDefinition/Organization",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "The ID of the resource"
                    },
                    new CapabilityStatement.SearchParamComponent
                    {
                        Name = "name",
                        Type = SearchParamType.String,
                        Documentation = "A portion of the name of the organization"
                    }
                }
            });

            // Location resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Location,
                Profile = "http://hl7.org/fhir/StructureDefinition/Location",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
                    }
                },
                ReadHistory = false,
                UpdateCreate = false,
                ConditionalCreate = false,
                ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                ConditionalUpdate = false,
                ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
            });

            // AllergyIntolerance resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.AllergyIntolerance,
                Profile = "http://hl7.org/fhir/StructureDefinition/AllergyIntolerance",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "Who the sensitivity is for"
                    }
                }
            });

            // Condition resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.Condition,
                Profile = "http://hl7.org/fhir/StructureDefinition/Condition",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "Who has the condition"
                    }
                }
            });

            // MedicationStatement resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.MedicationStatement,
                Profile = "http://hl7.org/fhir/StructureDefinition/MedicationStatement",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "Who is taking the medication"
                    }
                }
            });

            // DocumentReference resource - PLANNED
            resources.Add(new CapabilityStatement.ResourceComponent
            {
                Type = ResourceType.DocumentReference,
                Profile = "http://hl7.org/fhir/StructureDefinition/DocumentReference",
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                {
                    new CapabilityStatement.ResourceInteractionComponent
                    {
                        Code = CapabilityStatement.TypeRestfulInteraction.Read
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
                        Documentation = "Who/what is the subject of the document"
                    }
                }
            });

            return resources;
        }
    }
}
