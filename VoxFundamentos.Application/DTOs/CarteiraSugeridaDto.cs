namespace VoxFundamentos.Application.DTOs;
public record CarteiraSugeridaDto(
    decimal PesoTijoloPercentual,
    decimal PesoPapelPercentual,
    decimal PesoRiscoPercentual,
    int TotalAtivos,
    List<CarteiraSugeridaItemDto> Itens
);
