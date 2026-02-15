using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController(AdsPortalContext db, ILogger<HealthController> logger) : ControllerBase
    {
        private readonly AdsPortalContext _db = db;
        private readonly ILogger<HealthController> _logger = logger;

        [HttpGet]
        [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            var (dbConnected, dbError) = await CheckDatabaseAsync(cancellationToken);
            if (!dbConnected)
            {
                var fail = new HealthStatusDto { Backend = true, Db = false, Error = dbError };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, fail);
            }

            var ok = new HealthStatusDto { Backend = true, Db = true };
            return Ok(ok);
        }

        [HttpGet("db")]
        [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Db(CancellationToken cancellationToken = default)
        {
            var (dbConnected, dbError) = await CheckDatabaseAsync(cancellationToken);
            if (!dbConnected)
            {
                var fail = new HealthStatusDto { Backend = true, Db = false, Error = dbError };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, fail);
            }

            var ok = new HealthStatusDto { Backend = true, Db = true };
            return Ok(ok);
        }

        private async Task<(bool connected, string? error)> CheckDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
                return (canConnect, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB health check failed");
                return (false, ex.Message);
            }
        }
    }
}