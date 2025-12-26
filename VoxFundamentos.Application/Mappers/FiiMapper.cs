using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Domain.Entities;

namespace VoxFundamentos.Application.Mappers;

public static class FiiMapper
{
    // Mapper "default" (sem ranking)
    public static FiiDto ToDto(this Fii f)
        => new(
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
        );
}
