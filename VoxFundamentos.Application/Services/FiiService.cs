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

    public async Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct)
    {
        var fiis = (await _repo.ObterTodosAsync(ct))
            .Take(10)
            .ToList();

        const int maxConcorrencia = 4;
        using var sem = new SemaphoreSlim(maxConcorrencia);

        var tasks = fiis.Select(async f =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var divCota = await _repo.ObterDividendoPorCotaAsync(f.Papel, ct) ?? 0m;

                var proventoMensal = CalcularProventoMensalPeloDivCota(divCota);
                var proventoDiario = CalcularProventoDiario(proventoMensal);
                var dyMensal = CalcularDyMensalPeloProvento(f.Cotacao, proventoMensal);

                var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(f.Cotacao, proventoMensal);
                var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, f.Cotacao);

                return new FiiDto(
                    0, 0, 0,
                    f.Papel, f.Segmento, f.Cotacao, f.FfoYield, f.DividendYield, f.Pvp,
                    f.ValorMercado, f.Liquidez, f.QuantidadeImoveis, f.PrecoMetroQuadrado,
                    f.AluguelMetroQuadrado, f.CapRate, f.VacanciaMedia,
                    divCota, dyMensal, proventoMensal, proventoDiario,
                    qtdCotasNumeroMagico, valorParaNumeroMagico
                );
            }
            finally
            {
                sem.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<FiiAncoragemDto>> ObterFiisAncoragemAsync(CancellationToken ct)
    {
        var fiis = await _repo.ObterTodosAsync(ct);

        return fiis
            .Where(f =>
                f.Liquidez >= 1_500_000m &&
                f.ValorMercado >= 1_000_000_000m &&
                f.Pvp >= 0.98m &&
                f.VacanciaMedia <= 10m &&
                !IsShopping(f.Segmento)
            )
            .OrderBy(f => f.Papel, StringComparer.OrdinalIgnoreCase)
            .Select(f => new FiiAncoragemDto(
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
            ))
            .ToList();
    }

    private static bool IsShopping(string? segmento)
    {
        if (string.IsNullOrWhiteSpace(segmento)) return false;
        return segmento.Trim().ToLowerInvariant().Contains("shopping");
    }

    public async Task<FiiDto?> ObterPorPapelAsync(string papel, CancellationToken ct)
    {
        var fii = await _repo.ObterPorPapelAsync(papel, ct);
        if (fii is null) return null;

        var proventoMensal = CalcularProventoMensalPeloDivCota(fii.DividendoPorCota);
        var proventoDiario = CalcularProventoDiario(proventoMensal);
        var dyMensal = CalcularDyMensalPeloProvento(fii.Cotacao, proventoMensal);

        var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(fii.Cotacao, proventoMensal);
        var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, fii.Cotacao);

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
            VacanciaMedia: fii.VacanciaMedia,

            DividendoPorCota: fii.DividendoPorCota,
            DyMensalPercentual: dyMensal,
            ProventoMensalPorCota: proventoMensal,
            ProventoDiarioPorCota: proventoDiario,

            QtdCotasNumeroMagico: qtdCotasNumeroMagico,
            ValorParaNumeroMagico: valorParaNumeroMagico
        );
    }

    public async Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(CancellationToken ct)
    {
        var selic = await _indicadores.ObterSelicAtualAsync(ct);

        var dyMinimo = selic - 3m;
        var dyMaximo = 20m;

        var pvpMin = 0.50m;
        var pvpMax = 1.00m;

        var liquidezMin = 400000m;

        var fiis = await _repo.ObterTodosAsync(ct);

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

        if (baseList.Count == 0)
            return Array.Empty<FiiDto>();

        var rankPvpMap = baseList
            .OrderBy(f => f.Pvp)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        var rankDyMap = baseList
            .OrderByDescending(f => f.DividendYield)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        var top10Base = baseList
            .Select(f =>
            {
                var rankPvp = rankPvpMap[f.Papel];
                var rankDy = rankDyMap[f.Papel];
                var rankLevel = (rankPvp + rankDy) / 2m;

                return new { Fii = f, rankPvp, rankDy, rankLevel };
            })
            .OrderBy(x => x.rankLevel)
            .ThenBy(x => x.rankPvp)
            .ThenBy(x => x.rankDy)
            .Take(10)
            .ToList();

        const int maxConcorrencia = 4;
        using var sem = new SemaphoreSlim(maxConcorrencia);

        var tasks = top10Base.Select(async x =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var f = x.Fii;

                var divCota = await _repo.ObterDividendoPorCotaAsync(f.Papel, ct) ?? 0m;

                var proventoMensal = CalcularProventoMensalPeloDivCota(divCota);
                var proventoDiario = CalcularProventoDiario(proventoMensal);
                var dyMensal = CalcularDyMensalPeloProvento(f.Cotacao, proventoMensal);

                var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(f.Cotacao, proventoMensal);
                var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, f.Cotacao);

                return new FiiDto(
                    RankPvp: x.rankPvp,
                    RankDy: x.rankDy,
                    RankLevel: x.rankLevel,
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
                    VacanciaMedia: f.VacanciaMedia,

                    DividendoPorCota: divCota,
                    DyMensalPercentual: dyMensal,
                    ProventoMensalPorCota: proventoMensal,
                    ProventoDiarioPorCota: proventoDiario,

                    QtdCotasNumeroMagico: qtdCotasNumeroMagico,
                    ValorParaNumeroMagico: valorParaNumeroMagico
                );
            }
            finally
            {
                sem.Release();
            }
        });

        var top10 = await Task.WhenAll(tasks);

        return top10
            .OrderBy(x => x.RankLevel)
            .ThenBy(x => x.RankPvp)
            .ThenBy(x => x.RankDy)
            .ToList();
    }

    private static decimal CalcularProventoMensalPeloDivCota(decimal dividendoPorCota12m)
    {
        if (dividendoPorCota12m <= 0) return 0m;
        return Math.Round(dividendoPorCota12m / 12m, 2);
    }

    private static decimal CalcularDyMensalPeloProvento(decimal cotacao, decimal proventoMensal)
    {
        if (cotacao <= 0 || proventoMensal <= 0) return 0m;
        return Math.Round((proventoMensal / cotacao) * 100m, 2);
    }

    private static decimal CalcularProventoDiario(decimal proventoMensal)
    {
        var diasNoMes = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        if (diasNoMes <= 0 || proventoMensal <= 0) return 0m;
        return Math.Round(proventoMensal / diasNoMes, 6);
    }

    private static int CalcularQtdCotasNumeroMagico(decimal cotacao, decimal proventoMensal)
    {
        if (cotacao <= 0 || proventoMensal <= 0) return 0;
        return (int)Math.Ceiling(cotacao / proventoMensal);
    }

    private static decimal CalcularValorParaNumeroMagico(int qtdCotasNumeroMagico, decimal cotacao)
    {
        if (qtdCotasNumeroMagico <= 0 || cotacao <= 0) return 0m;
        return Math.Round(qtdCotasNumeroMagico * cotacao, 2);
    }
}
