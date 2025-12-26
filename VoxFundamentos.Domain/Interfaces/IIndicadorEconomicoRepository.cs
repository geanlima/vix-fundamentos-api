namespace VoxFundamentos.Domain.Interfaces;

public interface IIndicadorEconomicoRepository
{
    Task<decimal> ObterSelicAtualAsync(CancellationToken ct);
}
