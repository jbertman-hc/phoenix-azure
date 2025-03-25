using System;

namespace Phoenix_AzureAPI.Models
{
    public class Patient
    {
        public int PatientID { get; set; }
        public string? ChartID { get; set; }
        public string? Salutation { get; set; }
        public string? First { get; set; }
        public string? Middle { get; set; }
        public string? Last { get; set; }
        public string? Suffix { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? SS { get; set; }
        public string? PatientAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool Inactive { get; set; }
        public DateTime? LastVisit { get; set; }
        public string? PrimaryCareProvider { get; set; }
        
        // Display-friendly properties
        public string FullName => $"{Last ?? ""}, {First ?? ""} {Middle ?? ""}".Trim();
        public string FullNameWithSuffix => string.IsNullOrEmpty(Suffix) ? FullName : $"{FullName} {Suffix}";
        public string DisplayName => $"{FullNameWithSuffix} (DOB: {(BirthDate.HasValue ? BirthDate.Value.ToString("M/d/yyyy") : "Unknown")})";
        public string Address => $"{PatientAddress ?? ""}, {City ?? ""}, {State ?? ""} {Zip ?? ""}";
    }
}
