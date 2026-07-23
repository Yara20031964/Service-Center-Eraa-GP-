using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.API.Swagger;

namespace KHDMA.API.Controllers
{
    /// <summary>
    /// Deploy smoke test. Anonymous so it can be called from the production Swagger
    /// page without a token - hitting it confirms the CI deploy pushed this build.
    /// </summary>
    [ApiController]
    [Route("api/ping")]
    [AllowAnonymous]
    [Tags(ApiTags.PublicCatalog)]
    public class PingController : ControllerBase
    {
        /// <summary>Returns "hello" plus the server time in UTC.</summary>
        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            message = "hello",
            serverTimeUtc = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }
}
