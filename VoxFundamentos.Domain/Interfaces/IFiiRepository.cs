using VoxFundamentos.Domain.Entities;

namespace VoxFundamentos.Domain.Interfaces;

public interface IFiiRepository
{
    Task<IReadOnlyList<Fii>> ObterTodosAsync(CancellationToken ct);
}
