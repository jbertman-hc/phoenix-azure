using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Phoenix_AzureAPI.Models;
using Phoenix_AzureAPI.Services;
using Phoenix_AzureAPI.Services.FHIR;
using Hl7.Fhir.Model;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PracticeController : ControllerBase
    {
        private readonly ILogger<PracticeController> _logger;
        private readonly IPracticeService _practiceService;
        private readonly IPracticeFhirMapper _practiceFhirMapper;
        private readonly IFhirService _fhirService;

        public PracticeController(
            ILogger<PracticeController> logger,
            IPracticeService practiceService,
            IPracticeFhirMapper practiceFhirMapper,
            IFhirService fhirService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _practiceService = practiceService ?? throw new ArgumentNullException(nameof(practiceService));
            _practiceFhirMapper = practiceFhirMapper ?? throw new ArgumentNullException(nameof(practiceFhirMapper));
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
        }

        // GET: api/Practice/fhir/5
        [HttpGet("fhir/{id}")]
        public async Task<ActionResult<string>> GetPracticeFhir(int id)
        {
            try
            {
                _logger.LogInformation($"Retrieving FHIR Organization for practice with ID {id}");
                var practice = await _practiceService.GetPracticeByIdAsync(id);

                if (practice == null)
                {
                    return NotFound();
                }

                // Map the practice to a FHIR Organization resource
                var organization = _practiceFhirMapper.Map(practice);
                
                // Serialize the FHIR resource to JSON
                var json = _fhirService.SerializeToJson(organization);
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving FHIR Organization for practice with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Practice/fhir
        [HttpGet("fhir")]
        public async Task<ActionResult<string>> GetAllPracticesFhir()
        {
            try
            {
                _logger.LogInformation("Retrieving FHIR Bundle of all practices as Organizations");
                var practices = await _practiceService.GetAllPracticesAsync();

                // Create a FHIR Bundle to hold all Organization resources
                var bundle = new Bundle
                {
                    Type = Bundle.BundleType.Searchset,
                    Total = practices.Count(),
                    Entry = new List<Bundle.EntryComponent>()
                };

                // Add each practice as an Organization resource to the bundle
                foreach (var practice in practices)
                {
                    var organization = _practiceFhirMapper.Map(practice);
                    
                    bundle.Entry.Add(new Bundle.EntryComponent
                    {
                        Resource = organization,
                        FullUrl = $"urn:uuid:{Guid.NewGuid()}"
                    });
                }

                // Serialize the FHIR Bundle to JSON
                var json = _fhirService.SerializeToJson(bundle);
                
                return Content(json, "application/fhir+json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FHIR Bundle of practices");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
