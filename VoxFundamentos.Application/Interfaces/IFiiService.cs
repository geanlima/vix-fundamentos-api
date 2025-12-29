using VoxFundamentos.Application.DTOs;

namespace VoxFundamentos.Application.Interfaces;

public interface IFiiService
{
    Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct);
    Task<FiiDto?> ObterPorPapelAsync(string papel, CancellationToken ct);
    Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(CancellationToken ct);
    Task<IEnumerable<FiiAncoragemDto>> ObterFiisAncoragemAsync(CancellationToken ct);
}
