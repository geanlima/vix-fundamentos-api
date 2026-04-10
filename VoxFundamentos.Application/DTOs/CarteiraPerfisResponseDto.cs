using VoxFundamentos.Application.DTOs;

namespace VoxFundamentos.Application.DTOs;

public record CarteiraPerfisFiiResponseDto(
    decimal Ancoragem,
    decimal Potencial,
    decimal RiscoControlado,
    decimal RiscoElevado,
    int TotalFii,
    List<FiiDto> Itens
);
