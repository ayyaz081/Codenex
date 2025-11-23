using Microsoft.AspNetCore.Mvc;

namespace Neelsol.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/config/recaptcha-site-key
        [HttpGet("recaptcha-site-key")]
        public ActionResult<object> GetRecaptchaSiteKey()
        {
            var siteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITE_KEY") ??
                         _configuration["Recaptcha:SiteKey"] ?? string.Empty;

            return Ok(new { siteKey });
        }
    }
}
