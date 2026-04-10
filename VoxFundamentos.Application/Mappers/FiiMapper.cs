using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Domain.Entities;

namespace VoxFundamentos.Application.Mappers;

public static class FiiMapper
{
    public static FiiDto ToDto(
    this Fii f,
    decimal dividendoPorCota12m,
    string? tipo = null,
    string[]? motivos = null,
    int rankPvp = 0,
    int rankDy = 0,
    decimal rankLevel = 0m)
    {
        var proventoMensal = CalcularProventoMensalPeloDivCota(dividendoPorCota12m);
        var proventoDiario = CalcularProventoDiario(proventoMensal);
        var dyMensal = CalcularDyMensalPeloProvento(f.Cotacao, proventoMensal);

        var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(f.Cotacao, proventoMensal);
        var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, f.Cotacao);

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
            VacanciaMedia: f.VacanciaMedia,
            DividendoPorCota: dividendoPorCota12m,
            DyMensal: dyMensal,
            ProventoMensalPorCota: proventoMensal,
            ProventoDiarioPorCota: proventoDiario,
            QtdCotasNumeroMagico: qtdCotasNumeroMagico,
            ValorParaNumeroMagico: valorParaNumeroMagico,
            Tipo: tipo ?? string.Empty,
            Motivos: motivos ?? Array.Empty<string>()
        );
    }




    private static decimal CalcularProventoMensalPeloDivCota(decimal dividendoPorCota12m)
    {
        if (dividendoPorCota12m <= 0) return 0m;
        return Math.Round(dividendoPorCota12m / 12m, 4);
    }

    private static decimal CalcularDyMensalPeloProvento(decimal cotacao, decimal proventoMensal)
    {
        if (cotacao <= 0 || proventoMensal <= 0) return 0m;
        return Math.Round((proventoMensal / cotacao) * 100m, 4);
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
