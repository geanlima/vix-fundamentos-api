namespace VoxFundamentos.Application.DTOs;

public record FiiTop10ItemDto(
    int Posicao,
    string Papel,
    string? Segmento,
    string Tipo,
    string Perfil,
    string Risco,
    decimal Score,
    decimal PrecoParaComprar,
    decimal DividendYield,
    decimal Pvp,
    decimal Liquidez,
    decimal ValorMercado,
    decimal VacanciaMedia,
    int QuantidadeImoveis,
    string[] Motivos // positivos + pontos de atenção (⚠) explicando a posição no ranking
);

public record FiiTop10ResponseDto(
    IReadOnlyList<FiiTop10ItemDto> Itens,
    int TotalAnalisados,
    DateTimeOffset GeradoEm
);
