using Microsoft.AspNetCore.SignalR;

namespace RVM.HealthGuard.API.Hubs;

public class HealthStatusHub : Hub
{
    public async Task JoinServiceGroup(string serviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, serviceId);
    }

    public async Task LeaveServiceGroup(string serviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, serviceId);
    }
}
