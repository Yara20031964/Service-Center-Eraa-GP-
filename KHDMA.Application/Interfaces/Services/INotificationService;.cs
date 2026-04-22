using KHDMA.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<bool> SendToDeviceAsync(string deviceToken, FCMPayloadDto payload, CancellationToken ct = default);
        Task<bool> SendToTopicAsync(string topic, FCMPayloadDto payload, CancellationToken ct = default);
         Task TriggerAsync(NotificationEventDto notificationEvent);
        
            // TODO: implement FCM + SignalR
           
        
    }
}
