using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.DTOs.RealTime;

namespace KHDMA.Application.Interfaces.Services
{
    /// <summary>
    /// Chat behaviour shared by the REST controller and the SignalR hub.
    /// </summary>
    /// <remarks>
    /// Both entry points call the same methods so the PII filter, the auto-lock
    /// and the membership check cannot be bypassed by choosing one transport over
    /// the other.
    /// </remarks>
    public interface IChatService
    {
        /// <summary>
        /// Persists first, then returns the message for broadcast. That order is
        /// what makes the offline fallback work: a message is never delivered to
        /// an online peer without also being replayable from history.
        /// </summary>
        Task<ApiResponse<ChatMessageDto>> SendMessageAsync(Guid bookingId, string senderId, SendMessageDto dto);

        Task<PagedResponse<ChatMessageDto>> GetHistoryAsync(Guid bookingId, string userId, int page, int pageSize);

        Task<ApiResponse<List<ChatThreadDto>>> GetThreadsAsync(string userId);

        Task<ApiResponse<int>> MarkReadAsync(Guid bookingId, string userId);

        /// <summary>Admin dispute review - no membership check, admin-only at the controller.</summary>
        Task<ApiResponse<List<ChatTranscriptDto>>> GetTranscriptAsync(Guid bookingId);
    }
}
