namespace VoxFundamentos.Application.DTOs;

public record CarteiraParamRequestDto(
    decimal PesoTijoloPercentual,
    decimal PesoPapelPercentual,
    decimal PesoRiscoPercentual,
    int QtdTijolo,
    int QtdPapel,
    int QtdRisco
);
