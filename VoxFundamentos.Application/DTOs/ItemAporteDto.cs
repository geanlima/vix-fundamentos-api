public record ItemAporteDto(
    string Papel,
    string Segmento,
    decimal Cotacao,
    decimal ProventoMensalPorCota,
    int Cotas,
    decimal ValorInvestido,
    decimal RendaMensal,
    decimal RendaDiaria
);