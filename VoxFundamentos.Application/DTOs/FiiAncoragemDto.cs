namespace VoxFundamentos.Application.DTOs;

public record FiiAncoragemDto(
    string Papel,
    string Segmento,
    decimal Cotacao,
    decimal FfoYield,
    decimal DividendYield,
    decimal Pvp,
    decimal ValorMercado,
    decimal Liquidez,
    int QuantidadeImoveis,
    decimal PrecoMetroQuadrado,
    decimal AluguelMetroQuadrado,
    decimal CapRate,
    decimal VacanciaMedia
);
