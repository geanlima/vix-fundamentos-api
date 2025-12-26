using Microsoft.Extensions.Caching.Memory;
using VoxFundamentos.Domain.Entities;
using VoxFundamentos.Domain.Interfaces;
using VoxFundamentos.Infrastructure.Scraping;

namespace VoxFundamentos.Infrastructure.Repositories;

public class FiiRepository : IFiiRepository
{
    private readonly FundamentusFiiScraper _scraper;
    private readonly IMemoryCache _cache;

    public FiiRepository(FundamentusFiiScraper scraper, IMemoryCache cache)
    {
        _scraper = scraper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Fii>> ObterTodosAsync(CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync("fiis_fundamentus_all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

            var html = await _scraper.DownloadHtmlAsync(ct);
            var rows = _scraper.ParseTableRows(html);

            var list = new List<Fii>(rows.Count);

            foreach (var r in rows)
            {
                // Chaves normalizadas (ex.: PAPEL, SEGMENTO, COTACAO, FFO_YIELD, DIVIDEND_YIELD...)
                var papel = Get(r, "PAPEL");
                if (string.IsNullOrWhiteSpace(papel)) continue;

                var fii = new Fii(
                    papel: papel,
                    segmento: Get(r, "SEGMENTO"),
                    cotacao: FundamentusFiiScraper.ParseDecimalBr(Get(r, "COTACAO")),
                    ffoYield: FundamentusFiiScraper.ParseDecimalBr(Get(r, "FFO_YIELD")),
                    dividendYield: FundamentusFiiScraper.ParseDecimalBr(Get(r, "DIVIDEND_YIELD")),
                    pvp: FundamentusFiiScraper.ParseDecimalBr(Get(r, "P/VP")), // pode vir como "P/VP"
                    valorMercado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "VALOR_DE_MERCADO")),
                    liquidez: FundamentusFiiScraper.ParseDecimalBr(Get(r, "LIQUIDEZ")),
                    quantidadeImoveis: FundamentusFiiScraper.ParseIntBr(Get(r, "QTD_DE_IMOVEIS")),
                    precoMetroQuadrado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "PRECO_DO_M2")),
                    aluguelMetroQuadrado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "ALUGUEL_POR_M2")),
                    capRate: FundamentusFiiScraper.ParseDecimalBr(Get(r, "CAP_RATE")),
                    vacanciaMedia: FundamentusFiiScraper.ParseDecimalBr(Get(r, "VACANCIA_MEDIA"))
                );

                list.Add(fii);
            }

            return (IReadOnlyList<Fii>)list;
        }) ?? Array.Empty<Fii>();
    }

    private static string Get(Dictionary<string, string> r, string key)
    {
        // tenta a chave exata
        if (r.TryGetValue(key, out var v)) return v;

        // tratamento especial para "P/VP" que vira chave esquisita dependendo do HTML
        if (key == "P/VP")
        {
            foreach (var k in r.Keys)
            {
                if (k.Contains("P/VP", StringComparison.OrdinalIgnoreCase) || k == "PVP")
                    return r[k];
            }
        }

        return "";
    }

    public async Task<Fii?> ObterPorPapelAsync(string papel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(papel))
            return null;

        var all = await ObterTodosAsync(ct);

        return all.FirstOrDefault(f =>
            string.Equals(f.Papel, papel.Trim(), StringComparison.OrdinalIgnoreCase));
    }

}
