using System.Net.Http.Json;
using VoxFundamentos.Domain.Interfaces;

namespace VoxFundamentos.Infrastructure.Integrations;

public class BancoCentralSgsClient : IIndicadorEconomicoRepository
{
    private readonly HttpClient _http;

    // Série 11 = Selic Meta (% a.a.)
    private const string Url =
        "https://api.bcb.gov.br/dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json";

    public BancoCentralSgsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal> ObterSelicAtualAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<List<SgsResponse>>(Url, ct);

        var valorStr = response?.FirstOrDefault()?.valor;
        if (string.IsNullOrWhiteSpace(valorStr))
            throw new InvalidOperationException("Não foi possível obter a Selic.");

        // API retorna com ponto como separador
        return decimal.Parse(valorStr, System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class SgsResponse
    {
        public string data { get; set; } = "";
        public string valor { get; set; } = "";
    }
}
