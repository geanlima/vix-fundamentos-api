using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Domain.Entities;

namespace VoxFundamentos.Application.Mappers;

public static class FiiMapper
{
    public static FiiDto ToDto(this Fii f)
    {
        var dyMensal = CalcularDyMensal(f.DividendYield);
        var proventoMensal = CalcularProventoMensal(f.Cotacao, f.DividendYield);
        var proventoDiario = CalcularProventoDiario(proventoMensal);

        return new FiiDto(
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
            VacanciaMedia: f.VacanciaMedia,

            // ✅ novos campos
            DyMensalPercentual: dyMensal,
            ProventoMensalPorCota: proventoMensal,
            ProventoDiarioPorCota: proventoDiario
        );
    }

    private static decimal CalcularDyMensal(decimal dyAnual)
        => Math.Round(dyAnual / 12m, 4);

    private static decimal CalcularProventoMensal(decimal cotacao, decimal dyAnual)
    {
        if (cotacao <= 0 || dyAnual <= 0) return 0m;
        return Math.Round(cotacao * (dyAnual / 100m) / 12m, 4);
    }

    private static decimal CalcularProventoDiario(decimal proventoMensal)
    {
        var diasNoMes = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        if (diasNoMes <= 0) return 0m;
        return Math.Round(proventoMensal / diasNoMes, 6);
    }
}
