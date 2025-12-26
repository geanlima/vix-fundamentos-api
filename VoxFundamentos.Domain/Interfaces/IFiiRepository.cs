using VoxFundamentos.Domain.Entities;

namespace VoxFundamentos.Domain.Interfaces;

public interface IFiiRepository
{
    Task<IReadOnlyList<Fii>> ObterTodosAsync(CancellationToken ct);
    Task<Fii?> ObterPorPapelAsync(string papel, CancellationToken ct);
}
