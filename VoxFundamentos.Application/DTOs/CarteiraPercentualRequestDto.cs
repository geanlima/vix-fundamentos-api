namespace VoxFundamentos.Application.DTOs;

public record CarteiraPercentualRequestDto(
    decimal PesoTijoloPercentual,
    decimal PesoPapelPercentual,
    decimal PesoRiscoPercentual,
    int TotalFiis
);
