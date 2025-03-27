using Hl7.Fhir.Model;

namespace Phoenix_FHIR_API.Mappers
{
    /// <summary>
    /// Base interface for all FHIR resource mappers
    /// </summary>
    /// <typeparam name="TFhirResource">The FHIR resource type</typeparam>
    /// <typeparam name="TLegacyModel">The legacy model type</typeparam>
    public interface IFhirMapper<TFhirResource, TLegacyModel> where TFhirResource : Resource
    {
        /// <summary>
        /// Maps a legacy model to a FHIR resource
        /// </summary>
        /// <param name="source">The legacy model</param>
        /// <returns>The FHIR resource</returns>
        TFhirResource Map(TLegacyModel source);
        
        /// <summary>
        /// Maps a FHIR resource to a legacy model
        /// </summary>
        /// <param name="source">The FHIR resource</param>
        /// <returns>The legacy model</returns>
        TLegacyModel MapBack(TFhirResource source);
    }
}
