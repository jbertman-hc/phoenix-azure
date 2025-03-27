namespace Phoenix_FHIR_API.Models
{
    /// <summary>
    /// Domain model for patient index data from the legacy API
    /// </summary>
    public class PatientIndexDomain
    {
        public int? patID { get; set; }
        public string? chartID { get; set; }
        public string? lastName { get; set; }
        public string? firstName { get; set; }
        public string? DOB { get; set; }
        public string? gender { get; set; }
    }
}
