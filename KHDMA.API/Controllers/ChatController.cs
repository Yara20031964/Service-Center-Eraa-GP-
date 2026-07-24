using System.Security.Claims;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    /// <summary>
    /// REST side of chat. Shares <see cref="IChatService"/> with <c>ChatHub</c>, so
    /// the PII filter, the completed-booking lock and the membership check apply
    /// identically whichever transport the client uses.
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    [Tags(ApiTags.CommonChat)]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chat;
        private readonly IBlobStorageService _storage;

        public ChatController(IChatService chat, IBlobStorageService storage)
        {
            _chat = chat;
            _storage = storage;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>The caller's conversation list.</summary>
        [HttpGet("threads")]
        [ProducesResponseType<ApiResponse<List<ChatThreadDto>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetThreads()
        {
            if (UserId is null) return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _chat.GetThreadsAsync(UserId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Message history for one booking. This is the offline fallback: a client
        /// that missed messages while disconnected replays them from here.
        /// </summary>
        [HttpGet("{bookingId:guid}/history")]
        [ProducesResponseType<PagedResponse<ChatMessageDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<PagedResponse<ChatMessageDto>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetHistory(
            Guid bookingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (UserId is null) return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _chat.GetHistoryAsync(bookingId, UserId, page, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Send a message. Same path the hub uses - available so a client with a
        /// broken WebSocket can still participate.
        /// </summary>
        [HttpPost("{bookingId:guid}/messages")]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<ChatMessageDto>>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Send(Guid bookingId, [FromBody] SendMessageDto dto)
        {
            if (UserId is null) return Unauthorized(ApiResponse<ChatMessageDto>.Unauthorized());

            var result = await _chat.SendMessageAsync(bookingId, UserId, dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Upload an image for a chat message, then send it as an Image message
        /// with the returned url.
        /// </summary>
        [HttpPost("{bookingId:guid}/attachments")]
        [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<PagedResponse<ChatMessageDto>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadAttachment(Guid bookingId, IFormFile file)
        {
            if (UserId is null) return Unauthorized(ApiResponse<string>.Unauthorized());
            if (file is null || file.Length == 0)
                return BadRequest(ApiResponse<string>.Fail("No file was uploaded"));

            // Upload is gated on membership too, so a stranger cannot use the
            // endpoint as free file hosting.
            var history = await _chat.GetHistoryAsync(bookingId, UserId, 1, 1);
            if (!history.Success) return StatusCode(history.StatusCode, history);

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _storage.UploadFileAsync(stream, file.FileName, file.ContentType);
                return Ok(ApiResponse<string>.Ok(url, "Uploaded"));
            }
            catch (InvalidOperationException ex)
            {
                // Whitelist or size-cap rejection - the message is meant for the user.
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpPost("{bookingId:guid}/read")]
        [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkRead(Guid bookingId)
        {
            if (UserId is null) return Unauthorized(ApiResponse<int>.Unauthorized());

            var result = await _chat.MarkReadAsync(bookingId, UserId);
            return StatusCode(result.StatusCode, result);
        }
    }

    /// <summary>Dispute review. Admins can read any conversation (SRS 7.3).</summary>
    [ApiController]
    [Route("api/admin/chat")]
    [Authorize(Roles = "Admin")]
    [Tags(ApiTags.AdminChat)]
    public class AdminChatController : ControllerBase
    {
        private readonly IChatService _chat;

        public AdminChatController(IChatService chat) => _chat = chat;

        [HttpGet("{bookingId:guid}/transcript")]
        [ProducesResponseType<ApiResponse<List<ChatTranscriptDto>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<List<ChatTranscriptDto>>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTranscript(Guid bookingId)
        {
            var result = await _chat.GetTranscriptAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
