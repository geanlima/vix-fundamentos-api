using VoxFundamentos.Domain.Entities;
using VoxFundamentos.Application.DTOs;

namespace VoxFundamentos.Application.Mappers;

public static class FiiMapper
{
    public static FiiDto ToDto(this Fii fii)
        => new(
            fii.Papel,
            fii.Segmento,
            fii.Cotacao,
            fii.FfoYield,
            fii.DividendYield,
            fii.Pvp,
            fii.ValorMercado,
            fii.Liquidez,
            fii.QuantidadeImoveis,
            fii.PrecoMetroQuadrado,
            fii.AluguelMetroQuadrado,
            fii.CapRate,
            fii.VacanciaMedia
        );
}
