public record FiiDto(
    int RankPvp,
    int RankDy,
    decimal RankLevel,
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
    decimal VacanciaMedia,

    // 🔹 Dividendos
    decimal DividendoPorCota,
    decimal DyMensalPercentual,
    decimal ProventoMensalPorCota,
    decimal ProventoDiarioPorCota,

    // 🔥 NOVAS COLUNAS (Número Mágico)
    int QtdCotasNumeroMagico,
    decimal ValorParaNumeroMagico
);
