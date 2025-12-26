using VoxFundamentos.Application.Interfaces;
using VoxFundamentos.Domain.Interfaces;

namespace VoxFundamentos.Application.Services;

public class IndicadorEconomicoService : IIndicadorEconomicoService
{
    private readonly IIndicadorEconomicoRepository _repo;

    public IndicadorEconomicoService(IIndicadorEconomicoRepository repo)
    {
        _repo = repo;
    }

    public Task<decimal> ObterSelicAtualAsync(CancellationToken ct)
        => _repo.ObterSelicAtualAsync(ct);
}
