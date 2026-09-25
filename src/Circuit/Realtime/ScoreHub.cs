using Microsoft.AspNetCore.SignalR;

namespace Circuit.Realtime;

public sealed class ScoreHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.GetHttpContext()?.Request.Query["tournamentId"], out var tournamentId) && tournamentId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        await base.OnConnectedAsync();
    }
}
