using System.Text.Json.Serialization;

namespace Phoenix_FHIR_API.Models
{
    /// <summary>
    /// Demographics domain model from legacy API
    /// </summary>
    public class DemographicsDomain
    {
        public int patientID { get; set; }
        public string? chartID { get; set; }
        public string? lastName { get; set; }
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? suffix { get; set; }
        public string? preferredName { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? zip { get; set; }
        public string? homePhone { get; set; }
        public string? workPhone { get; set; }
        public string? cellPhone { get; set; }
        public string? email { get; set; }
        public string? gender { get; set; }
        public DateTime? birthDate { get; set; }
        public string? ssn { get; set; }
        public string? race { get; set; }
        public string? ethnicity { get; set; }
        public string? language { get; set; }
        public string? maritalStatus { get; set; }
        public string? employmentStatus { get; set; }
        public string? employer { get; set; }
        public string? emergencyContactName { get; set; }
        public string? emergencyContactPhone { get; set; }
        public string? emergencyContactRelationship { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Allergies domain model from legacy API
    /// </summary>
    public class ListAllergiesDomain
    {
        public int id { get; set; }
        public int patientId { get; set; }
        public string? allergyName { get; set; }
        public string? allergyType { get; set; }
        public string? reaction { get; set; }
        public string? severity { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? notes { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Medications domain model from legacy API
    /// </summary>
    public class ListMedsDomain
    {
        public int scriptID { get; set; }
        public int patientID { get; set; }
        public string? drugName { get; set; }
        public string? strength { get; set; }
        public string? directions { get; set; }
        public string? quantity { get; set; }
        public string? refills { get; set; }
        public string? daw { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? status { get; set; }
        public string? notes { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Problems domain model from legacy API
    /// </summary>
    public class ListProblemDomain
    {
        public int listProblemID { get; set; }
        public int patientID { get; set; }
        public string? problemName { get; set; }
        public string? icd10Code { get; set; }
        public string? icd9Code { get; set; }
        public string? status { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? notes { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// SOAP notes domain model from legacy API
    /// </summary>
    public class SOAPDomain
    {
        public int visitNumber { get; set; }
        public int patientID { get; set; }
        public DateTime? visitDate { get; set; }
        public string? subjective { get; set; }
        public string? objective { get; set; }
        public string? assessment { get; set; }
        public string? plan { get; set; }
        public string? providerCode { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Provider domain model from legacy API
    /// </summary>
    public class ProviderIndexDomain
    {
        public int providerIndexId { get; set; }
        public string? providerCode { get; set; }
        public string? externalProviderName { get; set; }
        public string? externalProviderPassword { get; set; }
        public string? source { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
        public string? externalProviderID { get; set; }
    }

    /// <summary>
    /// Practice information domain model from legacy API
    /// </summary>
    public class PracticeInfoDomain
    {
        public int id { get; set; }
        public string? practiceName { get; set; }
        public string? streetAddress1 { get; set; }
        public string? streetAddress2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? zip { get; set; }
        public string? phone1 { get; set; }
        public string? fax { get; set; }
        public string? ein { get; set; }
        public string? npi { get; set; }
        public string? clia { get; set; }
        public string? email { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Locations domain model from legacy API
    /// </summary>
    public class LocationsDomain
    {
        public string? locationsID { get; set; }
        public string? locations { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? stateOrRegion { get; set; }
        public string? stateOrRegionText { get; set; }
        public string? postalCode { get; set; }
        public string? country { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
        public bool isDefault { get; set; }
    }

    /// <summary>
    /// Facilities domain model from legacy API
    /// </summary>
    public class FacilitiesDomain
    {
        public string? facilitiesID { get; set; }
        public string? facilities { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? city { get; set; }
        public string? stateOrRegion { get; set; }
        public string? stateOrRegionText { get; set; }
        public string? postalCode { get; set; }
        public string? country { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
    }

    /// <summary>
    /// Scheduling domain model from legacy API
    /// </summary>
    public class SchedulingDomain
    {
        public int visitID { get; set; }
        public DateTime? date { get; set; }
        public int patientID { get; set; }
        public string? name { get; set; }
        public string? phone { get; set; }
        public string? visitType { get; set; }
        public string? comments { get; set; }
        public string? booker { get; set; }
        public DateTime? dateBooked { get; set; }
        public int providerID { get; set; }
        public int duration { get; set; }
        public string? xLinkProviderID { get; set; }
        public string? visitIdExternal { get; set; }
        public DateTime? dateLastTouched { get; set; }
        public string? lastTouchedBy { get; set; }
        public DateTime? dateRowAdded { get; set; }
        public bool isEditable { get; set; }
        public string? sourceSystemId { get; set; }
    }
}
