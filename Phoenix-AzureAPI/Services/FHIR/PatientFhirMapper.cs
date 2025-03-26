using System;
using System.Collections.Generic;
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
                
                // Add identifiers
                Identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        System = "http://phoenix-azure.org/fhir/identifier/patient-id",
                        Value = source.PatientID.ToString()
                    }
                },
                
                // Add name
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
                            : null
                    }
                },
                
                // Set gender
                Gender = MapGender(source.Gender),
                
                // Set birth date
                BirthDate = source.BirthDate?.ToString("yyyy-MM-dd"),
                
                // Set contact information
                Telecom = new List<ContactPoint>(),
                
                // Set address
                Address = new List<Address>()
            };
            
            // Add phone if available
            if (!string.IsNullOrEmpty(source.Phone))
            {
                fhirPatient.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = source.Phone,
                    Use = ContactPoint.ContactPointUse.Home
                });
            }
            
            // Add email if available
            if (!string.IsNullOrEmpty(source.Email))
            {
                fhirPatient.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = source.Email
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
                    Line = !string.IsNullOrEmpty(source.PatientAddress) 
                        ? new string[] { source.PatientAddress } 
                        : null,
                    City = source.City,
                    State = source.State,
                    PostalCode = source.Zip,
                    Country = "US" // Assuming US as default
                });
            }
            
            // Add SSN as an identifier if available
            if (!string.IsNullOrEmpty(source.SS))
            {
                fhirPatient.Identifier.Add(new Identifier
                {
                    System = "http://hl7.org/fhir/sid/us-ssn",
                    Value = source.SS
                });
            }
            
            // Add chart ID as an identifier if available
            if (!string.IsNullOrEmpty(source.ChartID))
            {
                fhirPatient.Identifier.Add(new Identifier
                {
                    System = "http://phoenix-azure.org/fhir/identifier/chart-id",
                    Value = source.ChartID
                });
            }
            
            // Set active status
            fhirPatient.Active = !source.Inactive;
            
            // Add general practitioner reference if available
            if (!string.IsNullOrEmpty(source.PrimaryCareProvider))
            {
                fhirPatient.GeneralPractitioner = new List<ResourceReference>
                {
                    new ResourceReference
                    {
                        Display = source.PrimaryCareProvider
                        // Note: We would ideally have a proper reference to a Practitioner resource
                        // Reference = $"Practitioner/{practitionerId}"
                    }
                };
            }
            
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
    }
}
