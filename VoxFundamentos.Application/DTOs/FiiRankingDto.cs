namespace VoxFundamentos.Application.DTOs;

public record FiiRankingDto(
    string Papel,
    string? Segmento,
    decimal Cotacao,
    decimal DividendYield,
    decimal Pvp,
    decimal ValorMercado,
    decimal Liquidez,
    decimal VacanciaMedia,
    int QuantidadeImoveis,

    decimal Score,          // 0..10
    string Tipo,            // "TIJOLO" | "PAPEL"
    string Risco,           // "Conservador" | "Moderado" | "Arrojado"
    string[] Motivos
);
