namespace VoxFundamentos.Application.Interfaces;

public interface IIndicadorEconomicoService
{
    Task<decimal> ObterSelicAtualAsync(CancellationToken ct);
}
