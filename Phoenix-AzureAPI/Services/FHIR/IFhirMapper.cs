using System;
using Hl7.Fhir.Model;
using Phoenix_AzureAPI.Models;
using DomainPatient = Phoenix_AzureAPI.Models.Patient;
using FhirPatient = Hl7.Fhir.Model.Patient;

namespace Phoenix_AzureAPI.Services.FHIR
{
    /// <summary>
    /// Interface for mapping domain models to FHIR resources
    /// </summary>
    /// <typeparam name="TSource">The source domain model type</typeparam>
    /// <typeparam name="TResource">The target FHIR resource type</typeparam>
    public interface IFhirMapper<TSource, TResource> where TResource : Resource
    {
        /// <summary>
        /// Maps a domain model to a FHIR resource
        /// </summary>
        /// <param name="source">The source domain model</param>
        /// <returns>The mapped FHIR resource</returns>
        TResource Map(TSource source);

        /// <summary>
        /// Maps a FHIR resource to a domain model
        /// </summary>
        /// <param name="resource">The FHIR resource</param>
        /// <returns>The mapped domain model</returns>
        TSource MapBack(TResource resource);
    }

    /// <summary>
    /// Interface for mapping between Patient domain model and FHIR Patient resource
    /// </summary>
    public interface IPatientFhirMapper : IFhirMapper<DomainPatient, FhirPatient>
    {
        /// <summary>
        /// Maps a domain Patient model to a FHIR Patient resource
        /// </summary>
        FhirPatient MapToFhir(DomainPatient source);
    }
}
