namespace VoxFundamentos.Application.DTOs;

public record FiiDto(
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
