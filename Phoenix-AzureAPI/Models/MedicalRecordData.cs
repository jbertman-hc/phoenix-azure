using System;
using System.Text.Json.Serialization;

namespace Phoenix_AzureAPI.Models
{
    /// <summary>
    /// Represents a medical record data item with its type and content
    /// </summary>
    public class MedicalRecordData
    {
        /// <summary>
        /// The type of medical record (e.g., "Allergies", "Medications", "Problems", "Notes")
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// The data content of the medical record
        /// </summary>
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }
}
