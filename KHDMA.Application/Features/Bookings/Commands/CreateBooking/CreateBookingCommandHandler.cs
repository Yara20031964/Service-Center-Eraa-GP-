using Domain.Common;
using KHDMA.Application.Common;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandHandler
        : IRequestHandler<CreateBookingCommand, ApiResponse<CreateBookingResultDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDispatchService _dispatchService;
        private readonly IPricingService _pricing;

        public CreateBookingCommandHandler(
            IUnitOfWork unitOfWork,
            IDispatchService dispatchService,
            IPricingService pricing)
        {
            _unitOfWork = unitOfWork;
            _dispatchService = dispatchService;
            _pricing = pricing;
        }

        public async Task<ApiResponse<CreateBookingResultDto>> Handle(
            CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var customers = _unitOfWork.Repository<Customer>();
            var providers = _unitOfWork.Repository<Provider>();
            var services = _unitOfWork.Repository<Service>();
            var bookings = _unitOfWork.Repository<Booking>();
            var addresses = _unitOfWork.Repository<Address>();
            var payments = _unitOfWork.Repository<Payment>();

            var customer = await customers.GetOneAsync(c => c.ApplicationUserId == request.CustomerId);
            if (customer is null)
                return ApiResponse<CreateBookingResultDto>.Fail("Customer profile not found", 403);

            var service = await services.GetOneAsync(s => s.id == request.ServiceId);
            if (service is null)
                return ApiResponse<CreateBookingResultDto>.NotFound("Service not found");

            if (!service.IsActive)
                return ApiResponse<CreateBookingResultDto>.Fail("This service is not currently available");

            if (request.ProviderId is not null)
            {
                var provider = await providers.GetOneAsync(p => p.ApplicationUserId == request.ProviderId);
                if (provider is null)
                    return ApiResponse<CreateBookingResultDto>.NotFound("Provider not found");

                if (provider.State != ProviderState.Active)
                    return ApiResponse<CreateBookingResultDto>.Fail("This provider is not accepting bookings");
            }

            if (request.BookingType == BookingType.Scheduled)
            {
                if (!request.ScheduledTime.HasValue)
                    return ApiResponse<CreateBookingResultDto>.Fail("Scheduled time is required for scheduled bookings");

                if (request.ScheduledTime.Value < DateTime.UtcNow.AddHours(2))
                    return ApiResponse<CreateBookingResultDto>.Fail("Scheduled bookings must be at least 2 hours in advance");
            }

            // Resolve the destination. A saved address wins over inline values so a
            // stale client copy cannot send a provider to the wrong place.
            var address = request.Address;
            var latitude = request.Latitude;
            var longitude = request.Longitude;

            if (request.AddressId is not null)
            {
                var saved = await addresses.GetOneAsync(
                    a => a.Id == request.AddressId && a.UserId == request.CustomerId);

                if (saved is null)
                    return ApiResponse<CreateBookingResultDto>.Fail("Saved address not found");

                address = saved.AddresssLine;
                latitude = saved.Latitude;
                longitude = saved.Longitude;
            }

            // Dispatch is geospatial - with no coordinates there is nothing to search around.
            if (latitude is null || longitude is null)
                return ApiResponse<CreateBookingResultDto>.Fail("A location (latitude and longitude) is required");

            // Server-authoritative price. The client never supplies an amount.
            var price = await _pricing.ForServiceAsync(request.ServiceId);
            if (price is null)
                return ApiResponse<CreateBookingResultDto>.Fail(
                    "This service has no price configured; please contact support", 409);

            var booking = new Booking
            {
                CustomerId = request.CustomerId,
                ProviderId = null,           // assigned only when a provider accepts
                ServiceId = request.ServiceId,
                BookingType = request.BookingType,
                ScheduledTime = request.ScheduledTime,
                Address = address,
                Latitude = latitude,
                Longitude = longitude,
                TotalPrice = price.Total,
                Notes = request.Notes,
                Status = BookingStatus.Pending,
            };

            await bookings.CreateAsync(booking);

            await payments.CreateAsync(new Payment
            {
                BookingId = booking.Id,
                Amount = price.Total,
                ServiceFee = price.ServiceFee,
                VatAmount = price.VatAmount,
                CommissionAmount = price.CommissionAmount,
                ProviderEarning = price.ProviderEarning,
                PaymentStatus = PaymentStatus.Pending,
            });

            await _unitOfWork.CommitAsync();

            // Immediate bookings dispatch now. Scheduled ones are left Pending and
            // picked up by ScheduledBookingWorker at T-30m, so a provider is not
            // tied up hours early.
            var dispatchResult = request.BookingType == BookingType.Scheduled
                ? new DispatchRoundResult(DispatchOutcome.NotDispatchable, 0, 0, 0)
                : request.ProviderId is not null
                    ? await _dispatchService.DispatchToProviderAsync(booking.Id, request.ProviderId, cancellationToken)
                    : await _dispatchService.DispatchAsync(booking.Id, cancellationToken);

            var status = dispatchResult.Outcome switch
            {
                DispatchOutcome.Broadcast => BookingStatus.Dispatching,
                DispatchOutcome.Exhausted => BookingStatus.NoProviderFound,
                _ => BookingStatus.Pending,
            };

            return ApiResponse<CreateBookingResultDto>.Created(new CreateBookingResultDto
            {
                BookingId = booking.Id,
                Status = status.ToString(),
                StatusLabelEn = BookingStatusLabels.En(status),
                StatusLabelAr = BookingStatusLabels.Ar(status),
                ProvidersNotified = dispatchResult.ProvidersNotified,
                DispatchStarted = dispatchResult.Outcome == DispatchOutcome.Broadcast,
                PriceBreakdown = new PriceBreakdownDto
                {
                    ServiceFee = price.ServiceFee,
                    VatRate = price.VatRate,
                    VatAmount = price.VatAmount,
                    Total = price.Total,
                    Currency = price.Currency,
                },
            }, "Booking created");
        }
    }
}
