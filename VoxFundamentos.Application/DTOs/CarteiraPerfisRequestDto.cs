namespace VoxFundamentos.Application.DTOs;

public record CarteiraPerfisRequestDto(
    decimal AncoragemPercentual,
    decimal PotencialPercentual,
    decimal RiscoControladoPercentual,
    decimal RiscoElevadoPercentual,
    int TotalFiis
);
