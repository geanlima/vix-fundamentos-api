namespace VoxFundamentos.Application.DTOs;

public record CarteiraSugeridaItemDto(
    string Papel,
    string Tipo,          // TIJOLO | PAPEL | RISCO
    decimal Score,        // 0..10
    string Risco,         // Conservador | Moderado | Arrojado
    decimal PesoPercentual,
    decimal PrecoParaComprar,
    decimal DividendYield,
    decimal Pvp,
    decimal Liquidez,
    decimal ValorMercado,
    string? Segmento,
    string[] Motivos,
    decimal ReceberPorMes,
    decimal ReceberPorDia,
    int QtdCotasNumeroMagico,
    decimal InvestirParaNumeroMagico
);


