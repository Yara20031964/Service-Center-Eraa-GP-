using Domain.Common;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    [Tags(ApiTags.CommonFiles)]
    public class FilesController : ControllerBase
    {
        private readonly IBlobStorageService _blobService;

        public FilesController(IBlobStorageService blobService)
        {
            _blobService = blobService;
        }

        // Both endpoints are placeholders for the Azure Blob path, which this
        // deployment does not use - uploads currently go through the profile and
        // chat-attachment endpoints backed by LocalFileStorageService. They answer
        // 501 in the standard envelope rather than throwing, so a client that calls
        // them gets a readable body instead of a bodiless 500.
        [HttpPost("upload")]
        [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status501NotImplemented)]
        public Task<IActionResult> Upload(IFormFile file)
            => Task.FromResult<IActionResult>(StatusCode(501,
                ApiResponse<string>.Fail("Direct blob upload is not enabled; use POST /api/profile/portfolio or /api/chat/{bookingId}/attachments", 501)));

        [HttpGet("presigned-url")]
        [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status501NotImplemented)]
        public IActionResult GetPresignedUrl([FromQuery] string fileName)
            => StatusCode(501, ApiResponse<string>.Fail("Presigned URLs are not enabled on this deployment", 501));
    }
}
