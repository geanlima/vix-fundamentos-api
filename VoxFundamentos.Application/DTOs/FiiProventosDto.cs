namespace VoxFundamentos.Application.DTOs;

public readonly record struct FiiProventosDto(
    decimal ProventoMensalPorCota,
    decimal ProventoDiarioPorCota,
    decimal ReceberPorMes,
    decimal ReceberPorDia,
    int QtdCotasNumeroMagico,
    decimal InvestirParaNumeroMagico
);
