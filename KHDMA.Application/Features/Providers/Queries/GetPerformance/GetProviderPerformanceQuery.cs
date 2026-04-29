using MediatR;
using KHDMA.Application.DTOs.Admin;
using Domain.Common;

namespace KHDMA.Application.Features.Providers.Queries.GetPerformance
{
    public class GetProviderPerformanceQuery : IRequest<ApiResponse<ProviderPerformanceDto>>
    {
        public string ProviderId { get; set; }
    }
}
