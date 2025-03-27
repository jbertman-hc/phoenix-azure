using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Phoenix_AzureAPI.Models;
using DomainPractice = Phoenix_AzureAPI.Models.Practice;
using FhirOrganization = Hl7.Fhir.Model.Organization;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Maps between the application's Practice model and FHIR Organization resource
    /// </summary>
    public class PracticeFhirMapper : IPracticeFhirMapper
    {
        /// <summary>
        /// Maps a domain Practice model to a FHIR Organization resource
        /// </summary>
        public FhirOrganization Map(DomainPractice source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var fhirOrganization = new FhirOrganization
            {
                // Set the logical ID based on the practice ID
                Id = source.PracticeID.ToString(),
                
                // Set the metadata required by FHIR
                Meta = new Meta
                {
                    // Use the standard Organization profile
                    Profile = new string[] { "http://hl7.org/fhir/StructureDefinition/Organization" },
                    // Set the last updated timestamp
                    LastUpdated = DateTime.UtcNow
                },
                
                // Add identifiers - at least one identifier is required for valid FHIR resources
                Identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        System = "http://phoenix-azure.org/fhir/identifier/practice-id",
                        Value = source.PracticeID.ToString(),
                        Use = Identifier.IdentifierUse.Official
                    }
                },
                
                // Add name - required for valid FHIR Organization resources
                Name = source.Name ?? "Unknown Practice",
                
                // Set active status
                Active = source.Active,
                
                // Set contact information
                Telecom = BuildTelecomList(source),
                
                // Set address
                Address = BuildAddressList(source),
                
                // Set the narrative text summary (required for valid FHIR resources)
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Organization: {source.Name}</p></div>"
                }
            };
            
            // Add NPI identifier if available
            if (!string.IsNullOrEmpty(source.NPI))
            {
                fhirOrganization.Identifier.Add(new Identifier
                {
                    System = "http://hl7.org/fhir/sid/us-npi",
                    Value = source.NPI,
                    Use = Identifier.IdentifierUse.Official
                });
            }
            
            // Add Tax ID if available
            if (!string.IsNullOrEmpty(source.TaxID))
            {
                fhirOrganization.Identifier.Add(new Identifier
                {
                    System = "http://hl7.org/fhir/sid/us-ein",
                    Value = source.TaxID,
                    Use = Identifier.IdentifierUse.Official
                });
            }
            
            // Set organization type as a healthcare provider
            fhirOrganization.Type = new List<CodeableConcept>
            {
                new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = "http://terminology.hl7.org/CodeSystem/organization-type",
                            Code = "prov",
                            Display = "Healthcare Provider"
                        }
                    },
                    Text = "Healthcare Provider"
                }
            };
            
            return fhirOrganization;
        }
        
        /// <summary>
        /// Builds a list of telecom contact points from the practice data
        /// </summary>
        private List<ContactPoint> BuildTelecomList(DomainPractice practice)
        {
            var telecom = new List<ContactPoint>();
            
            // Add phone if available
            if (!string.IsNullOrEmpty(practice.Phone))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Phone,
                    Value = practice.Phone,
                    Use = ContactPoint.ContactPointUse.Work,
                    Rank = 1
                });
            }
            
            // Add fax if available
            if (!string.IsNullOrEmpty(practice.Fax))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Fax,
                    Value = practice.Fax,
                    Use = ContactPoint.ContactPointUse.Work
                });
            }
            
            // Add email if available
            if (!string.IsNullOrEmpty(practice.Email))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = practice.Email,
                    Use = ContactPoint.ContactPointUse.Work
                });
            }
            
            // Add website if available
            if (!string.IsNullOrEmpty(practice.Website))
            {
                telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Url,
                    Value = practice.Website,
                    Use = ContactPoint.ContactPointUse.Work
                });
            }
            
            return telecom;
        }
        
        /// <summary>
        /// Builds a list of addresses from the practice data
        /// </summary>
        private List<Address> BuildAddressList(DomainPractice practice)
        {
            var addresses = new List<Address>();
            
            // Only add an address if we have at least some address data
            if (!string.IsNullOrEmpty(practice.Address) || 
                !string.IsNullOrEmpty(practice.City) || 
                !string.IsNullOrEmpty(practice.State) || 
                !string.IsNullOrEmpty(practice.Zip))
            {
                addresses.Add(new Address
                {
                    Use = Address.AddressUse.Work,
                    Type = Address.AddressType.Both,
                    Line = !string.IsNullOrEmpty(practice.Address) 
                        ? new string[] { practice.Address } 
                        : new string[] { },
                    City = practice.City,
                    State = practice.State,
                    PostalCode = practice.Zip,
                    Country = "USA" // Assuming USA for this implementation
                });
            }
            
            return addresses;
        }
        
        /// <summary>
        /// Maps a FHIR Organization resource to a domain Practice model
        /// </summary>
        public DomainPractice Map(FhirOrganization source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
                
            var practice = new DomainPractice
            {
                // Try to parse the ID as an integer
                PracticeID = int.TryParse(source.Id, out var id) ? id : 0,
                
                // Set the name
                Name = source.Name,
                
                // Set active status
                Active = source.Active ?? true
            };
            
            // Extract NPI from identifiers if available
            var npiIdentifier = source.Identifier?.FirstOrDefault(i => 
                i.System == "http://hl7.org/fhir/sid/us-npi");
            if (npiIdentifier != null)
            {
                practice.NPI = npiIdentifier.Value;
            }
            
            // Extract Tax ID from identifiers if available
            var taxIdIdentifier = source.Identifier?.FirstOrDefault(i => 
                i.System == "http://hl7.org/fhir/sid/us-ein");
            if (taxIdIdentifier != null)
            {
                practice.TaxID = taxIdIdentifier.Value;
            }
            
            // Extract contact information
            if (source.Telecom != null)
            {
                // Extract phone
                var phone = source.Telecom.FirstOrDefault(t => 
                    t.System == ContactPoint.ContactPointSystem.Phone);
                if (phone != null)
                {
                    practice.Phone = phone.Value;
                }
                
                // Extract fax
                var fax = source.Telecom.FirstOrDefault(t => 
                    t.System == ContactPoint.ContactPointSystem.Fax);
                if (fax != null)
                {
                    practice.Fax = fax.Value;
                }
                
                // Extract email
                var email = source.Telecom.FirstOrDefault(t => 
                    t.System == ContactPoint.ContactPointSystem.Email);
                if (email != null)
                {
                    practice.Email = email.Value;
                }
                
                // Extract website
                var website = source.Telecom.FirstOrDefault(t => 
                    t.System == ContactPoint.ContactPointSystem.Url);
                if (website != null)
                {
                    practice.Website = website.Value;
                }
            }
            
            // Extract address information
            if (source.Address != null && source.Address.Count > 0)
            {
                var address = source.Address.FirstOrDefault();
                if (address != null)
                {
                    practice.Address = address.Line?.FirstOrDefault();
                    practice.City = address.City;
                    practice.State = address.State;
                    practice.Zip = address.PostalCode;
                }
            }
            
            return practice;
        }
    }
}
