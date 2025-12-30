public record PerfilAporteDto(
    string Perfil,
    decimal Percentual,
    decimal ValorAlocado,
    decimal ValorInvestido,
    decimal SobraCaixa,
    decimal RendaMensal,
    decimal RendaDiaria,
    List<ItemAporteDto> Itens
);