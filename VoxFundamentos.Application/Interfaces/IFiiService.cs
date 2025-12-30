using VoxFundamentos.Application.DTOs;

namespace VoxFundamentos.Application.Interfaces;

public interface IFiiService
{
    Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct);
    Task<FiiDto?> ObterPorPapelAsync(string papel, CancellationToken ct);
    Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(CancellationToken ct);
    Task<IEnumerable<FiiAncoragemDto>> ObterFiisAncoragemAsync(CancellationToken ct);
    Task<IEnumerable<FiiRankingDto>> ObterMelhoresTijoloAsync(CancellationToken ct);
    Task<IEnumerable<FiiRankingDto>> ObterMelhoresPapelAsync(CancellationToken ct);
    Task<IEnumerable<FiiRankingDto>> ObterRiscoConfiavelAsync(int top, CancellationToken ct);
    Task<IEnumerable<FiiRankingDto>> ObterRiscoElevadoAsync(int top, CancellationToken ct);
    Task<CarteiraSugeridaDto> ObterCarteiraSugeridaAsync(CancellationToken ct);
    Task<CarteiraSugeridaDto> ObterCarteiraParametrizadaAsync(CarteiraParamRequestDto req, CancellationToken ct);
    Task<CarteiraSugeridaDto> ObterCarteiraPorPercentualETotalAsync(CarteiraPercentualRequestDto req, CancellationToken ct);
    Task<CarteiraPerfisFiiResponseDto> ObterCarteiraPorPerfisAsync(CarteiraPerfisRequestDto req, CancellationToken ct);

}
