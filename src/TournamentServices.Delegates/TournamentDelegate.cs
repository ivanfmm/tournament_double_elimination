using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class TournamentDelegate : ITournamentDelegate
{
    private readonly ITournamentRepository _repository;

    public TournamentDelegate(ITournamentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Tournament> GetByIdAsync(string id)
    {
        var tournament = await _repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException($"No existe un torneo con id '{id}'.");
        }

        return tournament;
    }

    public Task<IReadOnlyList<Tournament>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<Tournament> CreateAsync(Tournament tournament)
    {
        return _repository.AddAsync(tournament);
    }

    public async Task<Tournament> UpdateAsync(Tournament tournament)
    {
        var existing = await _repository.GetByIdAsync(tournament.Id);

        if (existing is null)
        {
            throw new NotFoundException($"No existe un torneo con id '{tournament.Id}'.");
        }

        await _repository.UpdateAsync(tournament);
        return tournament;
    }

    public async Task<Tournament> PatchAsync(string id, TournamentPatch patch)
    {
        var tournament = await _repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException($"No existe un torneo con id '{id}'.");
        }

        if (patch.Name is not null)
        {
            tournament.Name = patch.Name;
        }

        if (patch.Format is not null)
        {
            tournament.Format = patch.Format;
        }

        await _repository.UpdateAsync(tournament);
        return tournament;
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            throw new NotFoundException($"No existe un torneo con id '{id}'.");
        }
    }
}