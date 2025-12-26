namespace VoxFundamentos.Application.DTOs;

public record FiiFiltroRegraDto(
    decimal Selic,
    decimal DyMinimo,
    decimal DyMaximo,
    decimal PvpMinimo,
    decimal PvpMaximo,
    decimal LiquidezMinima
);
