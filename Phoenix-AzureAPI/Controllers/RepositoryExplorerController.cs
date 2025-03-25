using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Phoenix_AzureAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Reflection;
using System.Text.Json;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepositoryExplorerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public RepositoryExplorerController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _apiBaseUrl = _configuration["RemoteApiBaseUrl"];
        }

        [HttpGet]
        [Route("available")]
        public async Task<IActionResult> GetAvailableRepositories()
        {
            try
            {
                // This is a mock implementation - in a real scenario, we would discover repositories dynamically
                var repositories = GetAvailableRepositoriesInternal();
                
                return Ok(repositories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{repositoryName}")]
        public async Task<IActionResult> GetRepositoryData(string repositoryName)
        {
            try
            {
                // Find the repository by name
                var repositories = GetAvailableRepositoriesInternal();
                var repository = repositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (repository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                return Ok(repository);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{repositoryName}/methods")]
        public async Task<IActionResult> GetRepositoryMethods(string repositoryName)
        {
            try
            {
                // Find the repository by name
                var repositories = GetAvailableRepositoriesInternal();
                var repository = repositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (repository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                return Ok(repository.Methods);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{repositoryName}/execute/{methodName}")]
        public async Task<IActionResult> ExecuteRepositoryMethodDirect(string repositoryName, string methodName, string parameter = "")
        {
            try
            {
                // Find the repository by name
                var repositories = GetAvailableRepositoriesInternal();
                var repository = repositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (repository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                // Check if the method exists
                if (!repository.Methods.Contains(methodName))
                {
                    return NotFound($"Method '{methodName}' not found in repository '{repositoryName}'");
                }
                
                // Mock execution result
                var result = new 
                {
                    RepositoryName = repositoryName,
                    MethodName = methodName,
                    Parameter = parameter,
                    ExecutionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Result = $"Executed {methodName} on {repositoryName}" + (!string.IsNullOrEmpty(parameter) ? $" with parameter: {parameter}" : ""),
                    SampleData = GenerateMockDataForRepository(repositoryName)
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("explore/{repositoryName}")]
        public async Task<IActionResult> ExploreRepository(string repositoryName)
        {
            try
            {
                // Find the repository by name
                var repositories = GetAvailableRepositoriesInternal();
                var repository = repositories.FirstOrDefault(r => r.Name == repositoryName);
                
                if (repository == null)
                {
                    return NotFound($"Repository '{repositoryName}' not found");
                }
                
                // Return repository data
                var result = GenerateMockDataForRepository(repositoryName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("explore/{repositoryName}/{methodName}")]
        public async Task<IActionResult> ExecuteRepositoryMethod(string repositoryName, string methodName, [FromQuery] string parameters = "")
        {
            try
            {
                // Mock implementation - in a real scenario, we would execute the method on the repository
                var result = new
                {
                    Repository = repositoryName,
                    Method = methodName,
                    Parameters = parameters,
                    Result = $"Executed {methodName} on {repositoryName}" + (!string.IsNullOrEmpty(parameters) ? $" with parameters: {parameters}" : ""),
                    Timestamp = DateTime.Now
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to get available repositories
        private List<RepositoryInfo> GetAvailableRepositoriesInternal()
        {
            return new List<RepositoryInfo>
            {
                new RepositoryInfo { Name = "DemographicsRepository", Description = "Patient demographic information", Methods = new List<string> { "GetDemographics", "InsertDemographics", "UpdateDemographics", "DeleteDemographics" } },
                new RepositoryInfo { Name = "PatientIndexRepository", Description = "Patient index and search", Methods = new List<string> { "GetPatientIndex", "SearchPatients" } },
                new RepositoryInfo { Name = "AddendumRepository", Description = "Patient addendum data", Methods = new List<string> { "GetAddendum", "AddAddendum" } },
                new RepositoryInfo { Name = "PatientChargesRepository", Description = "Patient billing charges", Methods = new List<string> { "GetCharges", "AddCharge", "UpdateCharge" } },
                new RepositoryInfo { Name = "PatientMessagesRepository", Description = "Patient messages and communication", Methods = new List<string> { "GetMessages", "SendMessage", "MarkAsRead" } },
                new RepositoryInfo { Name = "PatientOptionsRepository", Description = "Patient preferences and options", Methods = new List<string> { "GetOptions", "UpdateOptions" } },
                new RepositoryInfo { Name = "GuardianPatientsRepository", Description = "Patient guardian information", Methods = new List<string> { "GetGuardians", "AddGuardian", "RemoveGuardian" } },
                new RepositoryInfo { Name = "ListPatientRacesRepository", Description = "Patient race information", Methods = new List<string> { "GetRaces", "AddRace" } },
                new RepositoryInfo { Name = "ListPatientLanguagesRepository", Description = "Patient language preferences", Methods = new List<string> { "GetLanguages", "SetPreferredLanguage" } },
                // Additional repositories based on Phoenix architecture
                new RepositoryInfo { Name = "SqlPatientRepository", Description = "SQL-based patient data access", Methods = new List<string> { "GetPatient", "GetAllPatients", "CreatePatient", "UpdatePatient", "DeletePatient" } },
                new RepositoryInfo { Name = "FhirPatientRepository", Description = "FHIR-based patient data access", Methods = new List<string> { "GetPatient", "GetAllPatients", "CreatePatient", "UpdatePatient", "DeletePatient" } },
                new RepositoryInfo { Name = "PatientMedicationRepository", Description = "Patient medication information", Methods = new List<string> { "GetMedications", "AddMedication", "UpdateMedication", "DiscontinueMedication" } },
                new RepositoryInfo { Name = "PatientAllergiesRepository", Description = "Patient allergy information", Methods = new List<string> { "GetAllergies", "AddAllergy", "UpdateAllergy", "RemoveAllergy" } },
                new RepositoryInfo { Name = "PatientImmunizationRepository", Description = "Patient immunization records", Methods = new List<string> { "GetImmunizations", "AddImmunization", "UpdateImmunization" } },
                new RepositoryInfo { Name = "PatientVitalSignsRepository", Description = "Patient vital signs data", Methods = new List<string> { "GetVitalSigns", "AddVitalSign", "GetVitalSignHistory" } }
            };
        }

        // Helper method to get sample data for a method
        private object GetSampleDataForMethod(string repositoryName, string methodName, string parameter)
        {
            // Return sample data based on repository and method
            switch (repositoryName)
            {
                case "SqlPatientRepository":
                case "FhirPatientRepository":
                    if (methodName == "GetAllPatients")
                    {
                        return new[]
                        {
                            new { Id = 1, FirstName = "John", LastName = "Doe", DateOfBirth = "1980-05-15", Gender = "Male" },
                            new { Id = 2, FirstName = "Jane", LastName = "Smith", DateOfBirth = "1975-08-22", Gender = "Female" },
                            new { Id = 3, FirstName = "Robert", LastName = "Johnson", DateOfBirth = "1990-03-10", Gender = "Male" }
                        };
                    }
                    else if (methodName == "GetPatient")
                    {
                        int patientId = 1;
                        if (!string.IsNullOrEmpty(parameter) && int.TryParse(parameter, out int id))
                        {
                            patientId = id;
                        }
                        
                        return new 
                        { 
                            Id = patientId, 
                            FirstName = "John", 
                            LastName = "Doe", 
                            DateOfBirth = "1980-05-15", 
                            Gender = "Male",
                            Address = "123 Main St, Anytown, USA",
                            Phone = "555-123-4567",
                            Email = "john.doe@example.com"
                        };
                    }
                    break;
                    
                case "DemographicsRepository":
                    if (methodName == "GetDemographics")
                    {
                        return new
                        {
                            PatientId = 1,
                            FirstName = "John",
                            LastName = "Doe",
                            DateOfBirth = "1980-05-15",
                            Gender = "Male",
                            Race = "White",
                            Ethnicity = "Non-Hispanic",
                            Language = "English",
                            Address = "123 Main St, Anytown, USA",
                            Phone = "555-123-4567",
                            Email = "john.doe@example.com",
                            InsuranceProvider = "Blue Cross Blue Shield",
                            InsuranceNumber = "BC123456789"
                        };
                    }
                    break;
                    
                case "PatientMedicationRepository":
                    if (methodName == "GetMedications")
                    {
                        return new[]
                        {
                            new { Id = 1, Name = "Lisinopril", Dosage = "10mg", Frequency = "Once daily", StartDate = "2023-01-15" },
                            new { Id = 2, Name = "Metformin", Dosage = "500mg", Frequency = "Twice daily", StartDate = "2022-11-05" },
                            new { Id = 3, Name = "Atorvastatin", Dosage = "20mg", Frequency = "Once daily", StartDate = "2023-03-22" }
                        };
                    }
                    break;
                    
                case "PatientAllergiesRepository":
                    if (methodName == "GetAllergies")
                    {
                        return new[]
                        {
                            new { Id = 1, Allergen = "Penicillin", Severity = "Severe", Reaction = "Hives, Difficulty breathing", OnsetDate = "2010-05-12" },
                            new { Id = 2, Allergen = "Peanuts", Severity = "Moderate", Reaction = "Swelling, Itching", OnsetDate = "2005-08-30" }
                        };
                    }
                    break;
                    
                case "PatientVitalSignsRepository":
                    if (methodName == "GetVitalSigns")
                    {
                        return new
                        {
                            PatientId = 1,
                            Date = DateTime.Now.ToString("yyyy-MM-dd"),
                            BloodPressure = "120/80",
                            HeartRate = 72,
                            RespiratoryRate = 16,
                            Temperature = 98.6,
                            Height = 70,
                            Weight = 180,
                            BMI = 25.8
                        };
                    }
                    break;
            }
            
            // Default sample data
            return new { Message = "Sample data not available for this method" };
        }

        private object GenerateMockDataForRepository(string repositoryName)
        {
            switch (repositoryName.ToLower())
            {
                case "demographicsrepository":
                    return new
                    {
                        Schema = new
                        {
                            PatientId = "int",
                            ChartID = "string",
                            Salutation = "string",
                            First = "string",
                            Middle = "string",
                            Last = "string",
                            Suffix = "string",
                            Gender = "string",
                            BirthDate = "DateTime",
                            SS = "string",
                            PatientAddress = "string",
                            City = "string",
                            State = "string",
                            Zip = "string",
                            Phone = "string",
                            WorkPhone = "string",
                            Email = "string",
                            EmployerName = "string",
                            EmergencyContactName = "string",
                            EmergencyContactPhone = "string",
                            SpouseName = "string",
                            Inactive = "bool",
                            PreferredPhysician = "string"
                        },
                        SampleData = new List<object>
                        {
                            new
                            {
                                PatientId = 1036,
                                ChartID = "CH10036",
                                Salutation = "Ms.",
                                First = "Dianna",
                                Middle = "",
                                Last = "Almond",
                                Suffix = "",
                                Gender = "Female",
                                BirthDate = "1985-06-15",
                                SS = "***-**-1234",
                                PatientAddress = "123 Main St",
                                City = "Springfield",
                                State = "IL",
                                Zip = "62701",
                                Phone = "(555) 123-4567",
                                WorkPhone = "(555) 987-6543",
                                Email = "dianna.almond@example.com",
                                EmployerName = "ABC Corporation",
                                EmergencyContactName = "John Almond",
                                EmergencyContactPhone = "(555) 234-5678",
                                SpouseName = "",
                                Inactive = false,
                                PreferredPhysician = "Dr. Smith"
                            },
                            new
                            {
                                PatientId = 2518,
                                ChartID = "CH2518",
                                Salutation = "Mrs.",
                                First = "Maryjo",
                                Middle = "L",
                                Last = "Bach",
                                Suffix = "",
                                Gender = "Female",
                                BirthDate = "1972-03-22",
                                SS = "***-**-5678",
                                PatientAddress = "456 Oak Ave",
                                City = "Springfield",
                                State = "IL",
                                Zip = "62704",
                                Phone = "(555) 345-6789",
                                WorkPhone = "(555) 456-7890",
                                Email = "maryjo.bach@example.com",
                                EmployerName = "XYZ Industries",
                                EmergencyContactName = "Robert Bach",
                                EmergencyContactPhone = "(555) 567-8901",
                                SpouseName = "Robert Bach",
                                Inactive = false,
                                PreferredPhysician = "Dr. Johnson"
                            }
                        }
                    };

                case "patientindexrepository":
                    return new
                    {
                        Schema = new
                        {
                            PatientId = "int",
                            FullName = "string",
                            BirthDate = "DateTime",
                            Gender = "string",
                            LastVisitDate = "DateTime"
                        },
                        SampleData = new List<object>
                        {
                            new
                            {
                                PatientId = 1036,
                                FullName = "Almond, Dianna",
                                BirthDate = "1985-06-15",
                                Gender = "Female",
                                LastVisitDate = "2024-02-10"
                            },
                            new
                            {
                                PatientId = 2518,
                                FullName = "Bach, Maryjo L",
                                BirthDate = "1972-03-22",
                                Gender = "Female",
                                LastVisitDate = "2024-01-05"
                            },
                            new
                            {
                                PatientId = 2521,
                                FullName = "Cross, David",
                                BirthDate = "1968-11-30",
                                Gender = "Male",
                                LastVisitDate = "2024-03-01"
                            }
                        }
                    };

                case "addendumrepository":
                    return new
                    {
                        Schema = new
                        {
                            AddendumId = "int",
                            PatientId = "int",
                            AddendumType = "string",
                            AddendumText = "string",
                            CreatedDate = "DateTime",
                            CreatedBy = "string"
                        },
                        SampleData = new List<object>
                        {
                            new
                            {
                                AddendumId = 12345,
                                PatientId = 1036,
                                AddendumType = "Note",
                                AddendumText = "Patient reported allergies to penicillin",
                                CreatedDate = "2024-01-15",
                                CreatedBy = "Dr. Smith"
                            },
                            new
                            {
                                AddendumId = 12346,
                                PatientId = 1036,
                                AddendumType = "Lab",
                                AddendumText = "Blood work results normal",
                                CreatedDate = "2024-02-10",
                                CreatedBy = "Dr. Johnson"
                            }
                        }
                    };

                case "patientchargesrepository":
                    return new
                    {
                        Schema = new
                        {
                            ChargeId = "int",
                            PatientId = "int",
                            ServiceDate = "DateTime",
                            CPTCode = "string",
                            Description = "string",
                            Amount = "decimal",
                            PaidAmount = "decimal",
                            Balance = "decimal"
                        },
                        SampleData = new List<object>
                        {
                            new
                            {
                                ChargeId = 45678,
                                PatientId = 1036,
                                ServiceDate = "2024-02-10",
                                CPTCode = "99213",
                                Description = "Office visit, established patient",
                                Amount = 125.00,
                                PaidAmount = 100.00,
                                Balance = 25.00
                            },
                            new
                            {
                                ChargeId = 45679,
                                PatientId = 1036,
                                ServiceDate = "2024-02-10",
                                CPTCode = "85025",
                                Description = "Complete blood count",
                                Amount = 45.00,
                                PaidAmount = 45.00,
                                Balance = 0.00
                            }
                        }
                    };

                default:
                    return new
                    {
                        Message = $"Repository '{repositoryName}' not found or not implemented in the explorer"
                    };
            }
        }

        public class RepositoryInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Methods { get; set; }
        }
    }
}
