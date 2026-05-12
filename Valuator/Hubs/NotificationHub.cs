using Microsoft.AspNetCore.SignalR;

namespace Valuator.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"CONNECTED: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public async Task SubscribeToText(string textId)
        {
            Console.WriteLine($"{Context.ConnectionId} → SubscribeToText({textId})");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"text-{textId}");
        }
    }
}