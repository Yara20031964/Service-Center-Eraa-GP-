using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IBlobStorageService _blobService;

        public FilesController(IBlobStorageService blobService)
        {
            _blobService = blobService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        [HttpGet("presigned-url")]
        public IActionResult GetPresignedUrl([FromQuery] string fileName)
        {
            // TODO: implement
            throw new NotImplementedException();
        }
    }
}
