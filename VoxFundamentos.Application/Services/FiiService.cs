using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Application.Interfaces;
using VoxFundamentos.Domain.Interfaces;

namespace VoxFundamentos.Application.Services;

public class FiiService : IFiiService
{
    private readonly IFiiRepository _repo;

    public FiiService(IFiiRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct)
    {
        var fiis = await _repo.ObterTodosAsync(ct);

        return fiis.Select(f => new FiiDto(
            f.Papel,
            f.Segmento,
            f.Cotacao,
            f.FfoYield,
            f.DividendYield,
            f.Pvp,
            f.ValorMercado,
            f.Liquidez,
            f.QuantidadeImoveis,
            f.PrecoMetroQuadrado,
            f.AluguelMetroQuadrado,
            f.CapRate,
            f.VacanciaMedia
        ));
    }
}
