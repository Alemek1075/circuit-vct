using Microsoft.AspNetCore.SignalR;

namespace Circuit.Realtime;

public sealed class ScorePublisher(IHubContext<ScoreHub> hub)
{
    public Task ChangedAsync(int tournamentId, int matchId) =>
        hub.Clients.Group($"tournament-{tournamentId}").SendAsync("matchChanged", matchId);
}
