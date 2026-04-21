using FirebaseAdmin;
using KHDMA.Application.DTOs;
using KHDMA.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
            InitializeFirebase();
        }

        private static void InitializeFirebase()
        {
            if (FirebaseApp.DefaultInstance is not null) return;

            var serviceAccountJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON");
        }

        public async Task<bool> SendToDeviceAsync(string deviceToken, FCMPayloadDto payload, CancellationToken ct = default)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public async Task<bool> SendToTopicAsync(string topic, FCMPayloadDto payload, CancellationToken ct = default)
        {
            // TODO: implement
            throw new NotImplementedException();
        }
    }
}
