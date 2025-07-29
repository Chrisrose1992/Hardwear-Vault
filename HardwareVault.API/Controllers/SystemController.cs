using Microsoft.AspNetCore.Mvc;
using HardwareVault.Core.Services;
using HardwareVault.Core.Models;

namespace HardwareVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        /// <summary>
        /// Get comprehensive system information with dataset enhancement
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<SystemResponse>> GetSystemInfo()
        {
            try
            {
                var systemCollector = new SystemCollectorService();
                var systemInfo = await systemCollector.GetCompleteSystemInfoAsync();

                var systemResponse = new SystemResponse
                {
                    System = systemInfo
                };

                return Ok(systemResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get system summary with key information
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<Dictionary<string, object>>> GetSystemSummary()
        {
            try
            {
                var systemCollector = new SystemCollectorService();
                var summary = await systemCollector.GetSystemSummaryAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}