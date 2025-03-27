using Hl7.Fhir.Model;
using Phoenix_FHIR_API.Models;
using Phoenix_FHIR_API.Services;
using Phoenix_FHIR_API.Validators;
using System.Collections.Generic;

namespace Phoenix_FHIR_API.Mappers
{
    /// <summary>
    /// Maps between legacy patient data and FHIR Patient resources
    /// </summary>
    public class PatientFhirMapper : IPatientFhirMapper
    {
        private readonly ILegacyApiService _legacyApiService;
        private readonly IFhirResourceValidator _validator;
        private readonly ILogger<PatientFhirMapper> _logger;

        public PatientFhirMapper(
            ILegacyApiService legacyApiService, 
            IFhirResourceValidator validator,
            ILogger<PatientFhirMapper> logger)
        {
            _legacyApiService = legacyApiService;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Maps a legacy demographics model to a FHIR Patient resource
        /// </summary>
        public Patient Map(DemographicsDomain source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var patient = new Patient
            {
                Id = source.patientID.ToString(),
                Meta = new Meta
                {
                    LastUpdated = source.dateLastTouched.HasValue 
                        ? new DateTimeOffset(source.dateLastTouched.Value) 
                        : DateTimeOffset.UtcNow,
                    VersionId = "1"
                },
                Identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        System = "http://example.org/fhir/identifier/mrn",
                        Value = source.chartID
                    }
                },
                Active = true
            };

            // Map name
            if (!string.IsNullOrEmpty(source.firstName) || !string.IsNullOrEmpty(source.lastName))
            {
                patient.Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Family = source.lastName,
                        Given = !string.IsNullOrEmpty(source.firstName) 
                            ? new List<string> { source.firstName } 
                            : null,
                        Suffix = !string.IsNullOrEmpty(source.suffix) 
                            ? new List<string> { source.suffix } 
                            : null,
                        Period = new Period
                        {
                            Start = source.dateRowAdded.HasValue 
                                ? new FhirDateTime(new DateTimeOffset(source.dateRowAdded.Value)).ToString()
                                : null
                        }
                    }
                };

                // Add preferred name if available
                if (!string.IsNullOrEmpty(source.preferredName))
                {
                    patient.Name.Add(new HumanName
                    {
                        Use = HumanName.NameUse.Usual,
                        Given = new List<string> { source.preferredName },
                        Period = new Period
                        {
                            Start = source.dateRowAdded.HasValue 
                                ? new FhirDateTime(new DateTimeOffset(source.dateRowAdded.Value)).ToString()
                                : null
                        }
                    });
                }
            }

            // Map telecom information
            var telecoms = new List<ContactPoint>();
            
            if (!string.IsNullOrEmpty(source.homePhone))
            {
                telecoms.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = source.homePhone,
                    Use = ContactPoint.ContactPointUse.Home,
                    Rank = 1
                });
            }
            
            if (!string.IsNullOrEmpty(source.cellPhone))
            {
                telecoms.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = source.cellPhone,
                    Use = ContactPoint.ContactPointUse.Mobile,
                    Rank = 2
                });
            }
            
            if (!string.IsNullOrEmpty(source.workPhone))
            {
                telecoms.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = source.workPhone,
                    Use = ContactPoint.ContactPointUse.Work,
                    Rank = 3
                });
            }
            
            if (!string.IsNullOrEmpty(source.email))
            {
                telecoms.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = source.email,
                    Use = ContactPoint.ContactPointUse.Home,
                    Rank = 1
                });
            }
            
            if (telecoms.Any())
            {
                patient.Telecom = telecoms;
            }

            // Map gender
            if (!string.IsNullOrEmpty(source.gender))
            {
                switch (source.gender.ToLower())
                {
                    case "male":
                    case "m":
                        patient.Gender = AdministrativeGender.Male;
                        break;
                    case "female":
                    case "f":
                        patient.Gender = AdministrativeGender.Female;
                        break;
                    case "other":
                    case "o":
                        patient.Gender = AdministrativeGender.Other;
                        break;
                    default:
                        patient.Gender = AdministrativeGender.Unknown;
                        break;
                }
            }

            // Map birth date
            if (source.birthDate.HasValue)
            {
                patient.BirthDate = source.birthDate.Value.ToString("yyyy-MM-dd");
            }

            // Map address
            if (!string.IsNullOrEmpty(source.address1) || !string.IsNullOrEmpty(source.city))
            {
                var address = new Address
                {
                    Use = Address.AddressUse.Home,
                    Type = Address.AddressType.Both,
                    Line = new List<string>(),
                    City = source.city,
                    State = source.state,
                    PostalCode = source.zip,
                    Country = "US"
                };

                if (!string.IsNullOrEmpty(source.address1))
                {
                    address.Line = address.Line.Concat(new[] { source.address1 }).ToList();
                }

                if (!string.IsNullOrEmpty(source.address2))
                {
                    address.Line = address.Line.Concat(new[] { source.address2 }).ToList();
                }

                patient.Address = new List<Address> { address };
            }

            // Map marital status
            if (!string.IsNullOrEmpty(source.maritalStatus))
            {
                patient.MaritalStatus = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus",
                            Code = MapMaritalStatus(source.maritalStatus),
                            Display = source.maritalStatus
                        }
                    },
                    Text = source.maritalStatus
                };
            }

            // Map contact (emergency contact)
            if (!string.IsNullOrEmpty(source.emergencyContactName))
            {
                var contact = new Patient.ContactComponent
                {
                    Relationship = new List<CodeableConcept>
                    {
                        new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new Coding
                                {
                                    System = "http://terminology.hl7.org/CodeSystem/v2-0131",
                                    Code = "C",
                                    Display = "Emergency Contact"
                                }
                            },
                            Text = source.emergencyContactRelationship
                        }
                    },
                    Name = new HumanName
                    {
                        Text = source.emergencyContactName
                    }
                };

                if (!string.IsNullOrEmpty(source.emergencyContactPhone))
                {
                    contact.Telecom = new List<ContactPoint>
                    {
                        new ContactPoint
                        {
                            System = ContactPoint.ContactPointSystem.Phone,
                            Value = source.emergencyContactPhone
                        }
                    };
                }

                patient.Contact = new List<Patient.ContactComponent> { contact };
            }

            // Map communication preferences
            if (!string.IsNullOrEmpty(source.language))
            {
                patient.Communication = new List<Patient.CommunicationComponent>
                {
                    new Patient.CommunicationComponent
                    {
                        Language = new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new Coding
                                {
                                    System = "urn:ietf:bcp:47",
                                    Code = MapLanguageCode(source.language),
                                    Display = source.language
                                }
                            },
                            Text = source.language
                        },
                        Preferred = true
                    }
                };
            }

            // Map race and ethnicity as extensions
            if (!string.IsNullOrEmpty(source.race))
            {
                var raceExtension = new Extension
                {
                    Url = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race",
                    Extension = new List<Extension>
                    {
                        new Extension
                        {
                            Url = "text",
                            Value = new FhirString(source.race)
                        }
                    }
                };
                
                patient.Extension = patient.Extension ?? new List<Extension>();
                patient.Extension.Add(raceExtension);
            }

            if (!string.IsNullOrEmpty(source.ethnicity))
            {
                var ethnicityExtension = new Extension
                {
                    Url = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity",
                    Extension = new List<Extension>
                    {
                        new Extension
                        {
                            Url = "text",
                            Value = new FhirString(source.ethnicity)
                        }
                    }
                };
                
                patient.Extension = patient.Extension ?? new List<Extension>();
                patient.Extension.Add(ethnicityExtension);
            }

            // Validate the patient resource
            _validator.Validate(patient);

            return patient;
        }

        /// <summary>
        /// Maps a FHIR Patient resource back to a legacy demographics model
        /// </summary>
        public DemographicsDomain MapBack(Patient source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var demographics = new DemographicsDomain
            {
                patientID = int.Parse(source.Id),
                dateLastTouched = source.Meta?.LastUpdated != null ? 
                    source.Meta.LastUpdated.Value.DateTime : 
                    DateTime.UtcNow,
                dateRowAdded = DateTime.UtcNow
            };

            // Map identifier (chart ID)
            var mrnIdentifier = source.Identifier?.FirstOrDefault(i => 
                i.System == "http://example.org/fhir/identifier/mrn");
            if (mrnIdentifier != null)
            {
                demographics.chartID = mrnIdentifier.Value;
            }

            // Map name
            var officialName = source.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Official) 
                ?? source.Name?.FirstOrDefault();
            if (officialName != null)
            {
                demographics.lastName = officialName.Family;
                demographics.firstName = officialName.Given?.FirstOrDefault();
                demographics.suffix = officialName.Suffix?.FirstOrDefault();
            }

            var preferredName = source.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Usual);
            if (preferredName != null)
            {
                demographics.preferredName = preferredName.Given?.FirstOrDefault();
            }

            // Map telecom
            var homePhone = source.Telecom?.FirstOrDefault(t => 
                t.System == ContactPoint.ContactPointSystem.Phone && t.Use == ContactPoint.ContactPointUse.Home);
            if (homePhone != null)
            {
                demographics.homePhone = homePhone.Value;
            }

            var mobilePhone = source.Telecom?.FirstOrDefault(t => 
                t.System == ContactPoint.ContactPointSystem.Phone && t.Use == ContactPoint.ContactPointUse.Mobile);
            if (mobilePhone != null)
            {
                demographics.cellPhone = mobilePhone.Value;
            }

            var workPhone = source.Telecom?.FirstOrDefault(t => 
                t.System == ContactPoint.ContactPointSystem.Phone && t.Use == ContactPoint.ContactPointUse.Work);
            if (workPhone != null)
            {
                demographics.workPhone = workPhone.Value;
            }

            var email = source.Telecom?.FirstOrDefault(t => 
                t.System == ContactPoint.ContactPointSystem.Email);
            if (email != null)
            {
                demographics.email = email.Value;
            }

            // Map gender
            switch (source.Gender)
            {
                case AdministrativeGender.Male:
                    demographics.gender = "Male";
                    break;
                case AdministrativeGender.Female:
                    demographics.gender = "Female";
                    break;
                case AdministrativeGender.Other:
                    demographics.gender = "Other";
                    break;
                default:
                    demographics.gender = "Unknown";
                    break;
            }

            // Map birth date
            if (!string.IsNullOrEmpty(source.BirthDate) && DateTime.TryParse(source.BirthDate, out var birthDate))
            {
                demographics.birthDate = birthDate;
            }

            // Map address
            var homeAddress = source.Address?.FirstOrDefault(a => a.Use == Address.AddressUse.Home) 
                ?? source.Address?.FirstOrDefault();
            if (homeAddress != null)
            {
                demographics.address1 = homeAddress.Line?.FirstOrDefault();
                demographics.address2 = homeAddress.Line?.Skip(1).FirstOrDefault();
                demographics.city = homeAddress.City;
                demographics.state = homeAddress.State;
                demographics.zip = homeAddress.PostalCode;
            }

            // Map marital status
            if (source.MaritalStatus?.Coding?.Any() == true)
            {
                demographics.maritalStatus = source.MaritalStatus.Text ?? 
                    source.MaritalStatus.Coding.First().Display;
            }

            // Map emergency contact
            var emergencyContact = source.Contact?.FirstOrDefault(c => 
                c.Relationship?.Any(r => r.Coding?.Any(coding => coding.Code == "C") == true) == true);
            if (emergencyContact != null)
            {
                demographics.emergencyContactName = emergencyContact.Name?.Text;
                demographics.emergencyContactRelationship = emergencyContact.Relationship?.FirstOrDefault()?.Text;
                demographics.emergencyContactPhone = emergencyContact.Telecom?.FirstOrDefault()?.Value;
            }

            // Map language
            var communicationPreference = source.Communication?.FirstOrDefault(c => c.Preferred == true) 
                ?? source.Communication?.FirstOrDefault();
            if (communicationPreference != null)
            {
                demographics.language = communicationPreference.Language?.Text ?? 
                    communicationPreference.Language?.Coding?.FirstOrDefault()?.Display;
            }

            // Map race and ethnicity from extensions
            var raceExtension = source.Extension?.FirstOrDefault(e => 
                e.Url == "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race");
            if (raceExtension != null)
            {
                var textExtension = raceExtension.Extension?.FirstOrDefault(e => e.Url == "text");
                if (textExtension?.Value is FhirString textValue)
                {
                    demographics.race = textValue.Value;
                }
            }

            var ethnicityExtension = source.Extension?.FirstOrDefault(e => 
                e.Url == "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity");
            if (ethnicityExtension != null)
            {
                var textExtension = ethnicityExtension.Extension?.FirstOrDefault(e => e.Url == "text");
                if (textExtension?.Value is FhirString textValue)
                {
                    demographics.ethnicity = textValue.Value;
                }
            }

            return demographics;
        }

        /// <summary>
        /// Creates a complete Patient resource with all available information
        /// </summary>
        public async Task<Patient> CreatePatientResourceAsync(DemographicsDomain demographics)
        {
            // Map the basic patient information
            var patient = Map(demographics);
            
            // Add any additional information or references that might be needed
            // This could include references to related resources like Conditions, AllergyIntolerances, etc.
            
            return patient;
        }

        #region Helper Methods

        /// <summary>
        /// Maps a legacy marital status to a FHIR marital status code
        /// </summary>
        private string MapMaritalStatus(string legacyMaritalStatus)
        {
            if (string.IsNullOrEmpty(legacyMaritalStatus))
            {
                return "UNK";
            }

            switch (legacyMaritalStatus.ToLower())
            {
                case "married":
                    return "M";
                case "single":
                    return "S";
                case "divorced":
                    return "D";
                case "widowed":
                    return "W";
                case "separated":
                    return "L";
                default:
                    return "UNK";
            }
        }

        /// <summary>
        /// Maps a legacy language to a BCP-47 language code
        /// </summary>
        private string MapLanguageCode(string legacyLanguage)
        {
            if (string.IsNullOrEmpty(legacyLanguage))
            {
                return "en-US";
            }

            switch (legacyLanguage.ToLower())
            {
                case "english":
                    return "en-US";
                case "spanish":
                    return "es-US";
                case "french":
                    return "fr-US";
                case "german":
                    return "de-US";
                case "italian":
                    return "it-US";
                case "chinese":
                    return "zh-US";
                case "japanese":
                    return "ja-US";
                case "korean":
                    return "ko-US";
                case "russian":
                    return "ru-US";
                case "arabic":
                    return "ar-US";
                default:
                    return "en-US";
            }
        }

        #endregion
    }
}
