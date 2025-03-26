using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Phoenix_AzureAPI.Models;
using DomainPatient = Phoenix_AzureAPI.Models.Patient;
using FhirPatient = Hl7.Fhir.Model.Patient;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Maps between the application's Patient model and FHIR Patient resource
    /// </summary>
    public class PatientFhirMapper : IPatientFhirMapper
    {
        /// <summary>
        /// Maps a domain Patient model to a FHIR Patient resource
        /// </summary>
        public FhirPatient Map(DomainPatient source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var fhirPatient = new FhirPatient
            {
                // Set the logical ID based on the patient ID
                Id = source.PatientID.ToString(),
                
                // Set the metadata required by FHIR
                Meta = new Meta
                {
                    // Use the standard Patient profile
                    Profile = new string[] { "http://hl7.org/fhir/StructureDefinition/Patient" },
                    // Set the last updated timestamp
                    LastUpdated = DateTime.UtcNow
                },
                
                // Add identifiers - at least one identifier is required for valid FHIR resources
                Identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        System = "http://phoenix-azure.org/fhir/identifier/patient-id",
                        Value = source.PatientID.ToString(),
                        Use = Identifier.IdentifierUse.Official
                    }
                },
                
                // Add name - at least one name is required for valid FHIR resources
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Prefix = !string.IsNullOrEmpty(source.Salutation) 
                            ? new string[] { source.Salutation } 
                            : null,
                        Given = new string[] 
                        { 
                            source.First ?? string.Empty,
                            !string.IsNullOrEmpty(source.Middle) ? source.Middle : null
                        }.Where(n => !string.IsNullOrEmpty(n)).ToArray(),
                        Family = source.Last ?? string.Empty,
                        Suffix = !string.IsNullOrEmpty(source.Suffix) 
                            ? new string[] { source.Suffix } 
                            : null,
                        // Add period to indicate when this name is/was used
                        Period = new Period
                        {
                            Start = DateTime.UtcNow.AddYears(-10).ToString("yyyy-MM-dd") // Arbitrary start date
                        }
                    }
                },
                
                // Set gender - required for valid FHIR Patient resources
                Gender = MapGender(source.Gender ?? string.Empty) ?? AdministrativeGender.Unknown,
                
                // Set birth date with proper format
                BirthDate = source.BirthDate?.ToString("yyyy-MM-dd"),
                
                // Set contact information
                Telecom = new List<ContactPoint>(),
                
                // Set address
                Address = new List<Address>(),
                
                // Set the narrative text summary (required for valid FHIR resources)
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Patient: {source.First} {source.Last}</p></div>"
                },
                
                // Set active status
                Active = !source.Inactive,
                
                // Add communication information if language is available
                // Communication information is commented out as Language property is not available in the Patient model
                /*
                Communication = !string.IsNullOrEmpty(source.Language) 
                    ? new List<FhirPatient.CommunicationComponent>
                    {
                        new FhirPatient.CommunicationComponent
                        {
                            Language = new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new Coding
                                    {
                                        System = "urn:ietf:bcp:47",
                                        Code = MapLanguageToCode(source.Language),
                                        Display = source.Language
                                    }
                                },
                                Text = source.Language
                            },
                            Preferred = true
                        }
                    }
                    : null
                */
            };
            
            // Add phone if available
            if (!string.IsNullOrEmpty(source.Phone))
            {
                fhirPatient.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = source.Phone,
                    Use = ContactPoint.ContactPointUse.Home,
                    Rank = 1
                });
            }
            
            // Add email if available
            if (!string.IsNullOrEmpty(source.Email))
            {
                fhirPatient.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = source.Email,
                    Rank = 2
                });
            }
            
            // Add address if available
            if (!string.IsNullOrEmpty(source.PatientAddress) || 
                !string.IsNullOrEmpty(source.City) || 
                !string.IsNullOrEmpty(source.State) || 
                !string.IsNullOrEmpty(source.Zip))
            {
                fhirPatient.Address.Add(new Address
                {
                    Use = Address.AddressUse.Home,
                    Type = Address.AddressType.Both,
                    Line = !string.IsNullOrEmpty(source.PatientAddress) 
                        ? new string[] { source.PatientAddress } 
                        : new string[] { "Unknown" }, // Provide a default value for required field
                    City = !string.IsNullOrEmpty(source.City) ? source.City : "Unknown", // Provide a default value for required field
                    State = !string.IsNullOrEmpty(source.State) ? source.State : "Unknown", // Provide a default value for required field
                    PostalCode = !string.IsNullOrEmpty(source.Zip) ? source.Zip : "00000", // Provide a default value for required field
                    Country = "US", // Assuming US as default
                    Period = new Period
                    {
                        Start = DateTime.UtcNow.AddYears(-5).ToString("yyyy-MM-dd") // Arbitrary start date
                    }
                });
            }
            
            // Add SSN as an identifier if available
            if (!string.IsNullOrEmpty(source.SS))
            {
                fhirPatient.Identifier.Add(new Identifier
                {
                    System = "http://hl7.org/fhir/sid/us-ssn",
                    Value = source.SS,
                    Use = Identifier.IdentifierUse.Official,
                    Type = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = "http://terminology.hl7.org/CodeSystem/v2-0203",
                                Code = "SS",
                                Display = "Social Security Number"
                            }
                        },
                        Text = "Social Security Number"
                    }
                });
            }
            
            // Add chart ID as an identifier if available
            if (!string.IsNullOrEmpty(source.ChartID))
            {
                fhirPatient.Identifier.Add(new Identifier
                {
                    System = "http://phoenix-azure.org/fhir/identifier/chart-id",
                    Value = source.ChartID,
                    Use = Identifier.IdentifierUse.Secondary,
                    Type = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = "http://terminology.hl7.org/CodeSystem/v2-0203",
                                Code = "MR",
                                Display = "Medical Record Number"
                            }
                        },
                        Text = "Chart ID"
                    }
                });
            }
            
            // Add general practitioner reference if available
            if (!string.IsNullOrEmpty(source.PrimaryCareProvider))
            {
                fhirPatient.GeneralPractitioner = new List<ResourceReference>
                {
                    new ResourceReference
                    {
                        Display = source.PrimaryCareProvider,
                        // Create a proper reference with a logical ID
                        Reference = $"Practitioner/{CreatePractitionerId(source.PrimaryCareProvider)}",
                        // Add identifier for the practitioner
                        Identifier = new Identifier
                        {
                            System = "http://phoenix-azure.org/fhir/identifier/practitioner-id",
                            Value = CreatePractitionerId(source.PrimaryCareProvider)
                        }
                    }
                };
            }
            
            // Add marital status if available
            // Note: Our domain model doesn't have MaritalStatus property, so we're commenting this out
            /*
            if (!string.IsNullOrEmpty(source.MaritalStatus))
            {
                fhirPatient.MaritalStatus = new Hl7.Fhir.Model.CodeableConcept
                {
                    Coding = new List<Hl7.Fhir.Model.Coding>
                    {
                        new Hl7.Fhir.Model.Coding
                        {
                            System = "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus",
                            Code = MapMaritalStatusToCode(source.MaritalStatus),
                            Display = source.MaritalStatus
                        }
                    },
                    Text = source.MaritalStatus
                };
            }
            */
            
            return fhirPatient;
        }

        /// <summary>
        /// Maps a domain Patient model to a FHIR Patient resource
        /// </summary>
        public FhirPatient MapToFhir(DomainPatient source)
        {
            return Map(source);
        }

        /// <summary>
        /// Maps a FHIR Patient resource back to a domain Patient model
        /// </summary>
        public DomainPatient MapBack(FhirPatient resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var patient = new DomainPatient();
            
            // Try to parse the ID
            if (int.TryParse(resource.Id, out int patientId))
            {
                patient.PatientID = patientId;
            }
            
            // Get the name components from the official name
            var officialName = resource.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Official) 
                ?? resource.Name?.FirstOrDefault();
            
            if (officialName != null)
            {
                patient.Salutation = officialName.Prefix?.FirstOrDefault();
                patient.First = officialName.Given?.FirstOrDefault();
                patient.Middle = officialName.Given?.Skip(1).FirstOrDefault();
                patient.Last = officialName.Family;
                patient.Suffix = officialName.Suffix?.FirstOrDefault();
            }
            
            // Map gender
            patient.Gender = MapGenderBack(resource.Gender);
            
            // Map birth date
            if (DateTime.TryParse(resource.BirthDate, out DateTime birthDate))
            {
                patient.BirthDate = birthDate;
            }
            
            // Map phone
            var phone = resource.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Phone);
            if (phone != null)
            {
                patient.Phone = phone.Value;
            }
            
            // Map email
            var email = resource.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Email);
            if (email != null)
            {
                patient.Email = email.Value;
            }
            
            // Map address
            var address = resource.Address?.FirstOrDefault();
            if (address != null)
            {
                patient.PatientAddress = address.Line?.FirstOrDefault();
                patient.City = address.City;
                patient.State = address.State;
                patient.Zip = address.PostalCode;
            }
            
            // Map SSN
            var ssn = resource.Identifier?.FirstOrDefault(i => i.System == "http://hl7.org/fhir/sid/us-ssn");
            if (ssn != null)
            {
                patient.SS = ssn.Value;
            }
            
            // Map chart ID
            var chartId = resource.Identifier?.FirstOrDefault(i => i.System == "http://phoenix-azure.org/fhir/identifier/chart-id");
            if (chartId != null)
            {
                patient.ChartID = chartId.Value;
            }
            
            // Map active status
            patient.Inactive = !resource.Active.GetValueOrDefault(true);
            
            // Map primary care provider
            var practitioner = resource.GeneralPractitioner?.FirstOrDefault();
            if (practitioner != null)
            {
                patient.PrimaryCareProvider = practitioner.Display;
            }
            
            // Map marital status
            if (resource.MaritalStatus?.Text != null)
            {
                // Note: Our domain Patient model doesn't have a MaritalStatus property
                // This would be implemented if the model is extended
            }
            
            // Map language
            var communication = resource.Communication?.FirstOrDefault(c => c.Preferred.GetValueOrDefault());
            if (communication?.Language?.Text != null)
            {
                // Note: Our domain Patient model doesn't have a Language property
                // This would be implemented if the model is extended
            }
            
            // Set last modified from meta
            if (resource.Meta?.LastUpdated.HasValue == true)
            {
                // Note: Our domain Patient model doesn't have a LastModified property
                // This would be implemented if the model is extended
            }
            
            return patient;
        }

        /// <summary>
        /// Maps a string gender to FHIR AdministrativeGender
        /// </summary>
        private AdministrativeGender? MapGender(string gender)
        {
            if (string.IsNullOrEmpty(gender))
                return null;
                
            return gender.ToLower() switch
            {
                "male" or "m" => AdministrativeGender.Male,
                "female" or "f" => AdministrativeGender.Female,
                "other" or "o" => AdministrativeGender.Other,
                _ => AdministrativeGender.Unknown
            };
        }

        /// <summary>
        /// Maps a FHIR AdministrativeGender back to a string
        /// </summary>
        private string MapGenderBack(AdministrativeGender? gender)
        {
            return gender switch
            {
                AdministrativeGender.Male => "Male",
                AdministrativeGender.Female => "Female",
                AdministrativeGender.Other => "Other",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Maps a language name to a BCP-47 language code
        /// </summary>
        private string MapLanguageToCode(string language)
        {
            return language?.ToLower() switch
            {
                "english" => "en",
                "spanish" => "es",
                "french" => "fr",
                "german" => "de",
                "italian" => "it",
                "chinese" => "zh",
                "japanese" => "ja",
                "korean" => "ko",
                "russian" => "ru",
                "arabic" => "ar",
                "hindi" => "hi",
                "portuguese" => "pt",
                _ => "en" // Default to English
            };
        }
        
        /// <summary>
        /// Maps a marital status to a FHIR marital status code
        /// </summary>
        private string MapMaritalStatusToCode(string maritalStatus)
        {
            return maritalStatus?.ToLower() switch
            {
                "married" => "M",
                "single" => "S",
                "divorced" => "D",
                "widowed" => "W",
                "separated" => "L",
                "domestic partner" => "P",
                _ => "UNK" // Unknown
            };
        }
        
        /// <summary>
        /// Creates a deterministic practitioner ID from a name
        /// </summary>
        private string CreatePractitionerId(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown";
                
            // Remove spaces and special characters, convert to lowercase
            var sanitized = new string(name.ToLower().Where(c => char.IsLetterOrDigit(c)).ToArray());
            
            // Take the first 20 characters or pad if shorter
            if (sanitized.Length > 20)
                return sanitized.Substring(0, 20);
                
            return sanitized.PadRight(20, 'x');
        }
    }
}
