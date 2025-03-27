using Hl7.Fhir.Model;
using Phoenix_AzureAPI.Models;
using DomainPractice = Phoenix_AzureAPI.Models.Practice;
using FhirOrganization = Hl7.Fhir.Model.Organization;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Interface for mapping between the application's Practice model and FHIR Organization resource
    /// </summary>
    public interface IPracticeFhirMapper
    {
        /// <summary>
        /// Maps a domain Practice model to a FHIR Organization resource
        /// </summary>
        FhirOrganization Map(DomainPractice source);
        
        /// <summary>
        /// Maps a FHIR Organization resource to a domain Practice model
        /// </summary>
        DomainPractice Map(FhirOrganization source);
    }
}
