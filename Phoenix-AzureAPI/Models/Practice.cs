using System;

namespace Phoenix_AzureAPI.Models
{
    public class Practice
    {
        public int PracticeID { get; set; }
        public string? Name { get; set; }
        public string? TaxID { get; set; }
        public string? NPI { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool Active { get; set; }
        
        // Display-friendly properties
        public string FullAddress => $"{Address ?? ""}, {City ?? ""}, {State ?? ""} {Zip ?? ""}";
        public string DisplayName => $"{Name ?? ""} ({NPI ?? "No NPI"})";
    }
}
