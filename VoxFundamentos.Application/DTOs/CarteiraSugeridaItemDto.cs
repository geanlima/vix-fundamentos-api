namespace VoxFundamentos.Application.DTOs;

public record CarteiraSugeridaItemDto(
    string Papel,
    string Tipo,          // TIJOLO | PAPEL | RISCO
    decimal Score,        // 0..10
    string Risco,         // Conservador | Moderado | Arrojado
    decimal PesoPercentual,
    decimal Cotacao,
    decimal DividendYield,
    decimal Pvp,
    decimal Liquidez,
    decimal ValorMercado,
    string? Segmento,
    string[] Motivos
);


