using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using VoxFundamentos.Domain.Entities;
using VoxFundamentos.Domain.Interfaces;
using VoxFundamentos.Infrastructure.Scraping;

namespace VoxFundamentos.Infrastructure.Repositories;

public class FiiRepository : IFiiRepository
{
    private readonly FundamentusFiiScraper _scraper;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheAllTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan CacheByPapelTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan CacheDetalhesTtl = TimeSpan.FromHours(6);

    // ✅ anti-stampede: 1 request por papel quando estoura concorrência
    private static readonly ConcurrentDictionary<string, Lazy<Task<decimal>>> _inflightDivCota =
        new(StringComparer.OrdinalIgnoreCase);

    public FiiRepository(FundamentusFiiScraper scraper, IMemoryCache cache)
    {
        _scraper = scraper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Fii>> ObterTodosAsync(CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync("fiis_fundamentus_all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheAllTtl;

            var html = await _scraper.DownloadHtmlAsync(ct);
            var rows = _scraper.ParseTableRows(html);

            var list = new List<Fii>(rows.Count);

            foreach (var r in rows)
            {
                var papel = Get(r, "PAPEL");
                if (string.IsNullOrWhiteSpace(papel))
                    continue;

                var normalized = NormalizePapel(papel);

                var fii = new Fii(
                    papel: normalized,
                    segmento: Get(r, "SEGMENTO"),
                    cotacao: FundamentusFiiScraper.ParseDecimalBr(Get(r, "COTACAO")),
                    ffoYield: FundamentusFiiScraper.ParseDecimalBr(Get(r, "FFO_YIELD")),
                    dividendYield: FundamentusFiiScraper.ParseDecimalBr(Get(r, "DIVIDEND_YIELD")),
                    pvp: FundamentusFiiScraper.ParseDecimalBr(Get(r, "P/VP")),
                    valorMercado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "VALOR_DE_MERCADO")),
                    liquidez: FundamentusFiiScraper.ParseDecimalBr(Get(r, "LIQUIDEZ")),
                    quantidadeImoveis: FundamentusFiiScraper.ParseIntBr(Get(r, "QTD_DE_IMOVEIS")),
                    precoMetroQuadrado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "PRECO_DO_M2")),
                    aluguelMetroQuadrado: FundamentusFiiScraper.ParseDecimalBr(Get(r, "ALUGUEL_POR_M2")),
                    capRate: FundamentusFiiScraper.ParseDecimalBr(Get(r, "CAP_RATE")),
                    vacanciaMedia: FundamentusFiiScraper.ParseDecimalBr(Get(r, "VACANCIA_MEDIA")),
                    dividendoPorCota: 0m // ✅ agora vem sob demanda
                );

                list.Add(fii);
            }

            return (IReadOnlyList<Fii>)list;
        }) ?? Array.Empty<Fii>();
    }

    public async Task<Fii?> ObterPorPapelAsync(string papel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(papel))
            return null;

        var normalized = NormalizePapel(papel);
        var cacheKey = $"fii_fundamentus_{normalized}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheByPapelTtl;

            var all = await ObterTodosAsync(ct);
            var fii = all.FirstOrDefault(f =>
                string.Equals(f.Papel, normalized, StringComparison.OrdinalIgnoreCase));

            if (fii is null) return null;

            // ✅ já devolve com DivCota preenchido (cacheado)
            var divCota = await ObterDividendoPorCotaAsync(normalized, ct) ?? 0m;

            return new Fii(
                papel: fii.Papel,
                segmento: fii.Segmento,
                cotacao: fii.Cotacao,
                ffoYield: fii.FfoYield,
                dividendYield: fii.DividendYield,
                pvp: fii.Pvp,
                valorMercado: fii.ValorMercado,
                liquidez: fii.Liquidez,
                quantidadeImoveis: fii.QuantidadeImoveis,
                precoMetroQuadrado: fii.PrecoMetroQuadrado,
                aluguelMetroQuadrado: fii.AluguelMetroQuadrado,
                capRate: fii.CapRate,
                vacanciaMedia: fii.VacanciaMedia,
                dividendoPorCota: divCota
            );
        });
    }

    public async Task<decimal?> ObterDividendoPorCotaAsync(string papel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(papel))
            return null;

        var normalized = NormalizePapel(papel);
        var cacheKey = $"fii_fundamentus_divcota_{normalized}";

        // ✅ 1) tenta cache direto
        if (_cache.TryGetValue(cacheKey, out decimal cached))
            return cached;

        // ✅ 2) anti-stampede: uma task por papel
        var lazy = _inflightDivCota.GetOrAdd(normalized, _ =>
            new Lazy<Task<decimal>>(async () =>
            {
                if (_cache.TryGetValue(cacheKey, out decimal cached2))
                    return cached2;

                var html = await _scraper.DownloadDetalhesHtmlAsync(normalized, ct);
                var parsed = _scraper.ParseDividendoPorCota(html) ?? 0m;

                _cache.Set(cacheKey, parsed, CacheDetalhesTtl);
                return parsed;
            })
        );

        try
        {
            return await lazy.Value;
        }
        finally
        {
            _inflightDivCota.TryRemove(normalized, out _);
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static string NormalizePapel(string papel)
        => papel.Trim().ToUpperInvariant();

    private static string Get(Dictionary<string, string> r, string key)
    {
        if (r == null || r.Count == 0 || string.IsNullOrWhiteSpace(key))
            return "";

        if (r.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v;

        var target = NormalizeKey(key);

        foreach (var (k, value) in r)
        {
            if (NormalizeKey(k) == target)
                return value ?? "";
        }

        if (key.Equals("P/VP", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (k, value) in r)
            {
                var nk = NormalizeKey(k);
                if (nk is "PVP" or "PVP_" or "P_VP" or "PVPATIO" || k.Contains("P/VP", StringComparison.OrdinalIgnoreCase))
                    return value ?? "";
            }
        }

        return "";
    }

    private static string NormalizeKey(string key)
    {
        key = key.Trim().ToUpperInvariant();

        key = key.Replace(" ", "", StringComparison.Ordinal)
                 .Replace("_", "", StringComparison.Ordinal);

        key = key.Replace("/", "", StringComparison.Ordinal)
                 .Replace(".", "", StringComparison.Ordinal)
                 .Replace("-", "", StringComparison.Ordinal);

        return key;
    }
}
