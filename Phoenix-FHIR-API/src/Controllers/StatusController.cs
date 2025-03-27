using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Swashbuckle.AspNetCore.Annotations;

namespace Phoenix_FHIR_API.Controllers
{
    [ApiController]
    [Route("api/status")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;

        public StatusController(ILogger<StatusController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get the implementation status of FHIR resources
        /// </summary>
        /// <returns>The implementation status</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get the implementation status of FHIR resources",
            Description = "Returns the implementation status of all FHIR resources",
            OperationId = "GetStatus",
            Tags = new[] { "Status" }
        )]
        [SwaggerResponse(200, "The implementation status")]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("application/json")]
        public IActionResult GetStatus()
        {
            try
            {
                var status = new
                {
                    Project = "Phoenix FHIR API",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    LastUpdated = DateTime.UtcNow,
                    Resources = GetResourceStatus()
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating status");
                return StatusCode(500, $"Error generating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the implementation status of FHIR resources
        /// This method should be updated as new resources are implemented
        /// </summary>
        /// <returns>A list of resource status objects</returns>
        private List<object> GetResourceStatus()
        {
            var resources = new List<object>
            {
                new
                {
                    ResourceType = "Patient",
                    Status = "Implemented",
                    Completeness = 100,
                    Operations = new[] { "read", "create", "update", "delete", "$everything" },
                    LastUpdated = "2025-03-27",
                    Notes = "Complete implementation with all required fields and extensions"
                },
                new
                {
                    ResourceType = "Practitioner",
                    Status = "In Progress",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for next implementation phase"
                },
                new
                {
                    ResourceType = "Organization",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after Practitioner"
                },
                new
                {
                    ResourceType = "Location",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after Organization"
                },
                new
                {
                    ResourceType = "AllergyIntolerance",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after Location"
                },
                new
                {
                    ResourceType = "Condition",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after AllergyIntolerance"
                },
                new
                {
                    ResourceType = "MedicationStatement",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after Condition"
                },
                new
                {
                    ResourceType = "DocumentReference",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for implementation after MedicationStatement"
                },
                new
                {
                    ResourceType = "Observation",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for later implementation phase"
                },
                new
                {
                    ResourceType = "Appointment",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for later implementation phase"
                },
                new
                {
                    ResourceType = "PractitionerRole",
                    Status = "Planned",
                    Completeness = 0,
                    Operations = new string[] { },
                    LastUpdated = string.Empty,
                    Notes = "Planned for later implementation phase"
                },
                new
                {
                    ResourceType = "Bundle",
                    Status = "Partially Implemented",
                    Completeness = 50,
                    Operations = new[] { "$everything" },
                    LastUpdated = "2025-03-27",
                    Notes = "Basic implementation for Patient $everything operation"
                }
            };

            return resources;
        }

        /// <summary>
        /// Get a simple HTML dashboard showing the implementation status
        /// </summary>
        /// <returns>HTML dashboard</returns>
        [HttpGet("dashboard")]
        [SwaggerOperation(
            Summary = "Get a simple HTML dashboard showing the implementation status",
            Description = "Returns an HTML dashboard showing the implementation status of all FHIR resources",
            OperationId = "GetDashboard",
            Tags = new[] { "Status" }
        )]
        [SwaggerResponse(200, "The HTML dashboard")]
        [SwaggerResponse(500, "Internal server error")]
        [Produces("text/html")]
        public IActionResult GetDashboard()
        {
            try
            {
                var resources = GetResourceStatus();
                var html = GenerateDashboardHtml(resources);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard");
                return StatusCode(500, $"Error generating dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates HTML for the status dashboard
        /// </summary>
        /// <param name="resources">The resource status objects</param>
        /// <returns>HTML string</returns>
        private string GenerateDashboardHtml(List<object> resources)
        {
            var html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Phoenix FHIR API - Implementation Status</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }
        h1, h2 {
            color: #2c3e50;
        }
        .dashboard {
            margin-top: 20px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        th, td {
            padding: 12px 15px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }
        th {
            background-color: #f8f9fa;
            font-weight: bold;
        }
        tr:hover {
            background-color: #f5f5f5;
        }
        .status-implemented {
            color: #28a745;
            font-weight: bold;
        }
        .status-partial {
            color: #fd7e14;
            font-weight: bold;
        }
        .status-progress {
            color: #007bff;
            font-weight: bold;
        }
        .status-planned {
            color: #6c757d;
        }
        .progress-bar-container {
            width: 100%;
            background-color: #e9ecef;
            border-radius: 4px;
            height: 20px;
        }
        .progress-bar {
            height: 100%;
            border-radius: 4px;
            text-align: center;
            line-height: 20px;
            color: white;
            font-size: 12px;
        }
        .progress-implemented {
            background-color: #28a745;
        }
        .progress-partial {
            background-color: #fd7e14;
        }
        .progress-progress {
            background-color: #007bff;
        }
        .progress-planned {
            background-color: #6c757d;
        }
        .summary {
            display: flex;
            justify-content: space-between;
            margin-bottom: 20px;
        }
        .summary-card {
            background-color: #f8f9fa;
            border-radius: 8px;
            padding: 15px;
            width: 23%;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .summary-card h3 {
            margin-top: 0;
            color: #2c3e50;
        }
        .summary-card p {
            font-size: 24px;
            font-weight: bold;
            margin: 10px 0 0;
        }
        .last-updated {
            text-align: right;
            font-style: italic;
            color: #6c757d;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <h1>Phoenix FHIR API - Implementation Status</h1>
    <p>This dashboard shows the current implementation status of FHIR resources in the Phoenix FHIR API.</p>
    
    <div class='summary'>
        <div class='summary-card'>
            <h3>Implemented</h3>
            <p>" + resources.Count(r => ((dynamic)r).Status == "Implemented") + @"</p>
        </div>
        <div class='summary-card'>
            <h3>Partially Implemented</h3>
            <p>" + resources.Count(r => ((dynamic)r).Status == "Partially Implemented") + @"</p>
        </div>
        <div class='summary-card'>
            <h3>In Progress</h3>
            <p>" + resources.Count(r => ((dynamic)r).Status == "In Progress") + @"</p>
        </div>
        <div class='summary-card'>
            <h3>Planned</h3>
            <p>" + resources.Count(r => ((dynamic)r).Status == "Planned") + @"</p>
        </div>
    </div>
    
    <div class='dashboard'>
        <table>
            <thead>
                <tr>
                    <th>Resource Type</th>
                    <th>Status</th>
                    <th>Completeness</th>
                    <th>Operations</th>
                    <th>Last Updated</th>
                    <th>Notes</th>
                </tr>
            </thead>
            <tbody>";

            foreach (dynamic resource in resources)
            {
                string statusClass = "";
                string progressClass = "";
                
                switch (resource.Status)
                {
                    case "Implemented":
                        statusClass = "status-implemented";
                        progressClass = "progress-implemented";
                        break;
                    case "Partially Implemented":
                        statusClass = "status-partial";
                        progressClass = "progress-partial";
                        break;
                    case "In Progress":
                        statusClass = "status-progress";
                        progressClass = "progress-progress";
                        break;
                    case "Planned":
                        statusClass = "status-planned";
                        progressClass = "progress-planned";
                        break;
                }
                
                html += $@"
                <tr>
                    <td>{resource.ResourceType}</td>
                    <td class='{statusClass}'>{resource.Status}</td>
                    <td>
                        <div class='progress-bar-container'>
                            <div class='progress-bar {progressClass}' style='width: {resource.Completeness}%'>{resource.Completeness}%</div>
                        </div>
                    </td>
                    <td>{string.Join(", ", resource.Operations)}</td>
                    <td>{(resource.LastUpdated != string.Empty ? resource.LastUpdated : "-")}</td>
                    <td>{resource.Notes}</td>
                </tr>";
            }

            html += @"
            </tbody>
        </table>
    </div>
    
    <div class='last-updated'>
        Last updated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + @" UTC
    </div>
</body>
</html>";

            return html;
        }
    }
}
