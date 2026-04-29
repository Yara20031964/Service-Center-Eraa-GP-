using MediatR;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Application.Interfaces.Repositories;

namespace KHDMA.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDispatchService _dispatchService;

        public CreateBookingCommandHandler(IUnitOfWork unitOfWork, IDispatchService dispatchService)
        {
            _unitOfWork = unitOfWork;
            _dispatchService = dispatchService;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var customerRepository = _unitOfWork.Repository<Customer>();
            var providerRepository = _unitOfWork.Repository<Provider>();
            var serviceRepository = _unitOfWork.Repository<Service>();
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var customer = await customerRepository.GetOneAsync(c => c.ApplicationUserId == request.CustomerId);
            if (customer == null) throw new Exception("Customer not found");

            var provider = await providerRepository.GetOneAsync(p => p.ApplicationUserId == request.ProviderId);
            if (provider == null) throw new Exception("Provider not found");

            var service = await serviceRepository.GetOneAsync(s => s.id == request.ServiceId);
            if (service == null) throw new Exception("Service not found");

            if (request.BookingType == KHDMA.Domain.Enums.BookingType.Scheduled)
            {
                if (!request.ScheduledTime.HasValue) throw new Exception("Scheduled time is required for scheduled bookings");
                if (request.ScheduledTime.Value < DateTime.UtcNow.AddHours(2)) 
                    throw new Exception("Scheduled bookings must be at least 2 hours in advance");
            }

            var booking = new Booking
            {
                CustomerId = request.CustomerId,
                ProviderId = request.ProviderId,
                ServiceId = request.ServiceId,
                BookingType = request.BookingType,
                ScheduledTime = request.ScheduledTime,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                TotalPrice = request.TotalPrice,
                Notes = request.Notes,
                Status = KHDMA.Domain.Enums.BookingStatus.Pending
            };

            await bookingRepository.CreateAsync(booking);
            await _unitOfWork.CommitAsync();

            await _dispatchService.DispatchAsync(booking);
            await _unitOfWork.CommitAsync();

            return booking.Id;
        }
    }
}
