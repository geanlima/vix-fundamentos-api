namespace VoxFundamentos.Application.DTOs;

public record AporteFiltradosResponseDto(
    decimal ValorTotal,
    int Top,
    decimal ValorInvestido,
    decimal SobraCaixa,
    decimal RendaMensal,
    decimal RendaDiaria,
    List<ItemAporteFiltradoDto> Itens
);

public record ItemAporteFiltradoDto(
    string Papel,
    string Segmento,
    decimal Cotacao,
    decimal ProventoMensalPorCota,
    int Cotas,
    decimal ValorInvestido,
    decimal RendaMensal,
    decimal RendaDiaria,

    // extras do "filtrados"
    int RankPvp,
    int RankDy,
    decimal RankLevel,
    decimal DividendYield,
    decimal Pvp
);
