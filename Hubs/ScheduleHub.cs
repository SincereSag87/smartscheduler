using Microsoft.AspNetCore.SignalR;

namespace smartscheduler.Hubs;

public sealed class ScheduleHub : Hub
{
    public async Task BroadcastScheduleChanged(string message)
    {
        await Clients.All.SendAsync("ScheduleChanged", message);
    }
}
