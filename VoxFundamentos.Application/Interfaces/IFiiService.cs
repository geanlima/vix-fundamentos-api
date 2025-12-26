using VoxFundamentos.Application.DTOs;

namespace VoxFundamentos.Application.Interfaces;

public interface IFiiService
{
    Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct);
}
