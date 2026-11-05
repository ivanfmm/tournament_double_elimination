using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests;

// Pasos HTTP comunes para armar y jugar un torneo de doble eliminacion.
// Los usan BracketRoutesTests (InMemory) y PostgresBracketTests.
public static class BracketApi
{
    // Crea los equipos, un torneo DOUBLE_ELIMINATION y un grupo con todos inscritos
    public static async Task<string> CreateTournamentWithTeamsAsync(HttpClient client, JsonSerializerOptions json, int teamCount)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var teamIds = new List<string>();
        for (var i = 1; i <= teamCount; i++)
        {
            var team = await ReadCreatedAsync<TeamDto>(
                await client.PostAsJsonAsync("/teams", new CreateTeamDto($"Equipo {i} {suffix}"), json), json);
            teamIds.Add(team.Id);
        }

        var tournament = await ReadCreatedAsync<TournamentDto>(await client.PostAsJsonAsync("/tournaments",
            new CreateTournamentDto($"Copa {suffix}", new FormatInputDto(teamCount, 1, TournamentType.DoubleElimination)), json), json);

        var group = await ReadCreatedAsync<GroupDto>(await client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups", new CreateGroupDto("Inscritos"), json), json);

        var assign = await client.PatchAsJsonAsync($"/tournaments/{tournament.Id}/groups/{group.Id}/teams", teamIds, json);
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        return tournament.Id;
    }

    public static async Task<List<MatchDto>> PendingMatchesAsync(HttpClient client, JsonSerializerOptions json, string tournamentId)
    {
        var all = await client.GetFromJsonAsync<List<MatchDto>>($"/tournaments/{tournamentId}/matches", json);
        return all!.Where(m => !m.IsCompleted).ToList();
    }

    public static Task<HttpResponseMessage> ReportAsync(HttpClient client, JsonSerializerOptions json,
        string tournamentId, string matchId, int home, int visitor)
        => client.PatchAsJsonAsync($"/tournaments/{tournamentId}/bracket/matches/{matchId}/score",
            new ScoreDto(home, visitor), json);

    // Reporta el primer pendiente hasta que no quede ninguno; regresa cuantos se jugaron
    public static async Task<int> PlayUntilChampionAsync(HttpClient client, JsonSerializerOptions json,
        string tournamentId, int home, int visitor)
    {
        var played = 0;
        while (true)
        {
            var pending = await PendingMatchesAsync(client, json, tournamentId);
            if (pending.Count == 0)
            {
                return played;
            }

            var response = await ReportAsync(client, json, tournamentId, pending[0].Id, home, visitor);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            played++;
        }
    }

    private static async Task<T> ReadCreatedAsync<T>(HttpResponseMessage response, JsonSerializerOptions json)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(json))!;
    }
}
