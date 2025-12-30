public record CarteiraAporteResponseDto(
    decimal ValorTotal,
    decimal AncoragemPercentual,
    decimal PotencialPercentual,
    decimal RiscoControladoPercentual,
    decimal RiscoElevadoPercentual,
    PerfilAporteDto Ancoragem,
    PerfilAporteDto Potencial,
    PerfilAporteDto RiscoControlado,
    PerfilAporteDto RiscoElevado,
    TotaisAporteDto TotaisGerais
);