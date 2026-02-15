// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet("status")]
        public IActionResult Status() => Ok(new { service = "AdsPortal_V2", status = "ok" });
    }
}
