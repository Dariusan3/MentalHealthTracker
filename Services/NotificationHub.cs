using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MentalHealthTracker.Services
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string message)
        {
            // Trimite doar către clientul curent
            await Clients.Caller.SendAsync("ReceiveNotification", message);
        }
    }
} 