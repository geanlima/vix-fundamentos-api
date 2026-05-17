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
    decimal rankLevel = 0m,
    int qtdCotas = 1)
    {
        var proventos = CalcularProventos(f.Cotacao, dividendoPorCota12m, qtdCotas);
        var dyMensal = CalcularDyMensalPeloProvento(f.Cotacao, proventos.ProventoMensalPorCota);

        return new FiiDto(
            RankPvp: rankPvp,
            RankDy: rankDy,
            RankLevel: rankLevel,
            Papel: f.Papel,
            Segmento: f.Segmento,
            PrecoParaComprar: f.Cotacao,
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
            ProventoMensalPorCota: proventos.ProventoMensalPorCota,
            ProventoDiarioPorCota: proventos.ProventoDiarioPorCota,
            ReceberPorMes: proventos.ReceberPorMes,
            ReceberPorDia: proventos.ReceberPorDia,
            QtdCotasNumeroMagico: proventos.QtdCotasNumeroMagico,
            ValorParaNumeroMagico: proventos.InvestirParaNumeroMagico,
            InvestirParaNumeroMagico: proventos.InvestirParaNumeroMagico,
            Tipo: tipo ?? string.Empty,
            Motivos: motivos ?? Array.Empty<string>()
        );
    }




    public static FiiProventosDto CalcularProventos(decimal cotacao, decimal dividendoPorCota12m, int qtdCotas)
    {
        if (qtdCotas < 0)
            throw new ArgumentOutOfRangeException(nameof(qtdCotas), "A quantidade de cotas não pode ser negativa.");

        var proventoMensal = CalcularProventoMensalPeloDivCota(dividendoPorCota12m);
        var proventoDiario = CalcularProventoDiario(proventoMensal);
        var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(cotacao, proventoMensal);
        var investirParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, cotacao);

        return new FiiProventosDto(
            ProventoMensalPorCota: proventoMensal,
            ProventoDiarioPorCota: proventoDiario,
            ReceberPorMes: CalcularReceberPorMes(proventoMensal, qtdCotas),
            ReceberPorDia: CalcularReceberPorDia(proventoDiario, qtdCotas),
            QtdCotasNumeroMagico: qtdCotasNumeroMagico,
            InvestirParaNumeroMagico: investirParaNumeroMagico
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

    private static decimal CalcularReceberPorMes(decimal proventoMensalPorCota, int qtdCotas)
    {
        if (qtdCotas <= 0 || proventoMensalPorCota <= 0) return 0m;
        return Math.Round(proventoMensalPorCota * qtdCotas, 2);
    }

    private static decimal CalcularReceberPorDia(decimal proventoDiarioPorCota, int qtdCotas)
    {
        if (qtdCotas <= 0 || proventoDiarioPorCota <= 0) return 0m;
        return Math.Round(proventoDiarioPorCota * qtdCotas, 4);
    }
}
