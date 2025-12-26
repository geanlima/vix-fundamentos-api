using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Application.Interfaces;
using VoxFundamentos.Domain.Interfaces;

namespace VoxFundamentos.Application.Services;

public class FiiService : IFiiService
{
    private readonly IFiiRepository _repo;
    private readonly IIndicadorEconomicoService _indicadores;

    public FiiService(
        IFiiRepository repo,
        IIndicadorEconomicoService indicadores)
    {
        _repo = repo;
        _indicadores = indicadores;
    }

    // 🔹 Lista completa (sem ranking)
    public async Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct)
    {
        var fiis = await _repo.ObterTodosAsync(ct);

        return fiis.Select(f => new FiiDto(
            RankPvp: 0,
            RankDy: 0,
            RankLevel: 0,
            Papel: f.Papel,
            Segmento: f.Segmento,
            Cotacao: f.Cotacao,
            FfoYield: f.FfoYield,
            DividendYield: f.DividendYield,
            Pvp: f.Pvp,
            ValorMercado: f.ValorMercado,
            Liquidez: f.Liquidez,
            QuantidadeImoveis: f.QuantidadeImoveis,
            PrecoMetroQuadrado: f.PrecoMetroQuadrado,
            AluguelMetroQuadrado: f.AluguelMetroQuadrado,
            CapRate: f.CapRate,
            VacanciaMedia: f.VacanciaMedia
        ));
    }

    // 🔹 Buscar FII por papel
    public async Task<FiiDto?> ObterPorPapelAsync(string papel, CancellationToken ct)
    {
        var fii = await _repo.ObterPorPapelAsync(papel, ct);
        if (fii is null) return null;

        return new FiiDto(
            RankPvp: 0,
            RankDy: 0,
            RankLevel: 0,
            Papel: fii.Papel,
            Segmento: fii.Segmento,
            Cotacao: fii.Cotacao,
            FfoYield: fii.FfoYield,
            DividendYield: fii.DividendYield,
            Pvp: fii.Pvp,
            ValorMercado: fii.ValorMercado,
            Liquidez: fii.Liquidez,
            QuantidadeImoveis: fii.QuantidadeImoveis,
            PrecoMetroQuadrado: fii.PrecoMetroQuadrado,
            AluguelMetroQuadrado: fii.AluguelMetroQuadrado,
            CapRate: fii.CapRate,
            VacanciaMedia: fii.VacanciaMedia
        );
    }

    // 🔹 Lista filtrada + ranking completo
    public async Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(CancellationToken ct)
    {
        var selic = await _indicadores.ObterSelicAtualAsync(ct);

        var dyMinimo = selic - 3m;
        var dyMaximo = 20m;

        var pvpMin = 0.50m;
        var pvpMax = 1.00m;

        var liquidezMin = 400000m;

        var fiis = await _repo.ObterTodosAsync(ct);

        // 1️⃣ Aplicar filtros
        var baseList = fiis
            .Where(f =>
                f.DividendYield >= dyMinimo &&
                f.DividendYield <= dyMaximo &&
                f.Pvp >= pvpMin &&
                f.Pvp <= pvpMax &&
                f.Liquidez >= liquidezMin &&
                f.VacanciaMedia <= 100
            )
            .ToList();

        // 2️⃣ Rank P/VP (menor → maior)
        var rankPvpMap = baseList
            .OrderBy(f => f.Pvp)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        // 3️⃣ Rank DY (maior → menor)
        var rankDyMap = baseList
            .OrderByDescending(f => f.DividendYield)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        // 4️⃣ Montar DTO + RankLevel
        var resultado = baseList
            .Select(f =>
            {
                var rankPvp = rankPvpMap[f.Papel];
                var rankDy = rankDyMap[f.Papel];
                var rankLevel = (rankPvp + rankDy) / 2m;

                return new FiiDto(
                    RankPvp: rankPvp,
                    RankDy: rankDy,
                    RankLevel: rankLevel,
                    Papel: f.Papel,
                    Segmento: f.Segmento,
                    Cotacao: f.Cotacao,
                    FfoYield: f.FfoYield,
                    DividendYield: f.DividendYield,
                    Pvp: f.Pvp,
                    ValorMercado: f.ValorMercado,
                    Liquidez: f.Liquidez,
                    QuantidadeImoveis: f.QuantidadeImoveis,
                    PrecoMetroQuadrado: f.PrecoMetroQuadrado,
                    AluguelMetroQuadrado: f.AluguelMetroQuadrado,
                    CapRate: f.CapRate,
                    VacanciaMedia: f.VacanciaMedia
                );
            })
            // 5️⃣ Ordenação final pelo RankLevel (menor é melhor)
            .OrderBy(x => x.RankLevel)
            .ThenBy(x => x.RankPvp)
            .ThenBy(x => x.RankDy)
            .ToList();

        return resultado;
    }
}
