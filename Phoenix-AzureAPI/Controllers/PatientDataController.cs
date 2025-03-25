using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Phoenix_AzureAPI.Services;

namespace Phoenix_AzureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientDataController : ControllerBase
    {
        private readonly PatientDataService _patientDataService;

        public PatientDataController(PatientDataService patientDataService)
        {
            _patientDataService = patientDataService ?? throw new ArgumentNullException(nameof(patientDataService));
        }

        /// <summary>
        /// Get comprehensive patient data from all repositories
        /// </summary>
        /// <param name="patientId">The patient identifier</param>
        /// <returns>Comprehensive patient data</returns>
        [HttpGet("comprehensive/{patientId}")]
        public async Task<IActionResult> GetComprehensivePatientData(int patientId)
        {
            try
            {
                var patientData = await _patientDataService.GetComprehensivePatientData(patientId);
                return Ok(patientData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get a map of all repositories and methods needed for patient data
        /// </summary>
        /// <returns>Dictionary of repositories and their methods</returns>
        [HttpGet("repositories-map")]
        public async Task<IActionResult> GetPatientDataRepositoriesMap()
        {
            try
            {
                var repositoriesMap = await _patientDataService.GetPatientDataRepositoriesMap();
                return Ok(repositoriesMap);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
