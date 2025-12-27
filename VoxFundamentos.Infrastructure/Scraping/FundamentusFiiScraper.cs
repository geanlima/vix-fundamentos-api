using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace VoxFundamentos.Infrastructure.Scraping;

public class FundamentusFiiScraper
{
    private const string Url = "https://www.fundamentus.com.br/fii_resultado.php";
    private const string UrlDetalhesBase = "https://www.fundamentus.com.br/detalhes.php?papel=";

    private readonly HttpClient _http;
    private readonly Random _rng = new();

    public FundamentusFiiScraper(HttpClient http)
    {
        _http = http;

        // Headers "humanos"
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36");
        }

        if (_http.DefaultRequestHeaders.AcceptLanguage.Count == 0)
            _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en;q=0.8");

        _http.DefaultRequestHeaders.Referrer ??= new Uri("https://www.fundamentus.com.br/");
    }

    // =========================================================
    // DOWNLOAD: LISTA
    // =========================================================

    public async Task<string> DownloadHtmlAsync(CancellationToken ct)
        => await DownloadWithRetryAsync(Url, ct);

    // =========================================================
    // DOWNLOAD: DETALHES
    // =========================================================

    public async Task<string> DownloadDetalhesHtmlAsync(string papel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(papel))
            throw new ArgumentException("papel inválido", nameof(papel));

        var normalized = papel.Trim().ToUpperInvariant();
        var url = UrlDetalhesBase + WebUtility.UrlEncode(normalized);

        return await DownloadWithRetryAsync(url, ct);
    }

    // Download genérico com retry/backoff (reuso do seu padrão)
    private async Task<string> DownloadWithRetryAsync(string url, CancellationToken ct)
    {
        Exception? last = null;

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                // polidez
                await Task.Delay(TimeSpan.FromMilliseconds(_rng.Next(900, 2200)), ct);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.Clear();
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (resp.StatusCode == HttpStatusCode.OK)
                    return await resp.Content.ReadAsStringAsync(ct);

                // 429 / 5xx => retry com backoff
                if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
                {
                    var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) + _rng.NextDouble());
                    await Task.Delay(backoff, ct);
                    continue;
                }

                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (attempt < 4)
            {
                last = ex;
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) + _rng.NextDouble());
                await Task.Delay(backoff, ct);
            }
        }

        throw new InvalidOperationException($"Falha ao baixar HTML do Fundamentus ({url}).", last);
    }

    // =========================================================
    // PARSE: LISTA (tabela geral)
    // =========================================================

    public IReadOnlyList<Dictionary<string, string>> ParseTableRows(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Em vez de pegar a primeira tabela com <th>,
        // pega a tabela que contém header "Papel" (mais estável)
        var table = doc.DocumentNode.SelectSingleNode(
            "//table[.//th[contains(translate(normalize-space(.)," +
            "'abcdefghijklmnopqrstuvwxyzáàâãäéèêëíìîïóòôõöúùûüç'," +
            "'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ'), 'PAPEL')]]"
        );

        // fallback
        table ??= doc.DocumentNode.SelectSingleNode("//table[.//th]");

        if (table is null)
            throw new InvalidOperationException("Tabela não encontrada. O layout pode ter mudado.");

        var headerNodes = table.SelectNodes(".//tr/th");
        if (headerNodes is null || headerNodes.Count == 0)
            throw new InvalidOperationException("Cabeçalhos da tabela não encontrados.");

        var headers = headerNodes
            .Select(h => WebUtility.HtmlDecode(h.InnerText).Trim())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(NormalizeHeader)
            .ToList();

        var rows = table.SelectNodes(".//tr[td]");
        if (rows is null || rows.Count == 0)
            return Array.Empty<Dictionary<string, string>>();

        var result = new List<Dictionary<string, string>>(rows.Count);

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count == 0)
                continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < Math.Min(headers.Count, cells.Count); i++)
            {
                var text = WebUtility.HtmlDecode(cells[i].InnerText).Trim();
                dict[headers[i]] = text;
            }

            result.Add(dict);
        }

        return result;
    }

    // =========================================================
    // PARSE: DETALHES (campo "Dividendo/cota")
    // =========================================================

    /// <summary>
    /// Extrai o valor do campo "Dividendo/cota" da página detalhes.php.
    /// (No print: 11,17)
    /// </summary>
    public decimal? ParseDividendoPorCota(string htmlDetalhes)
    {
        if (string.IsNullOrWhiteSpace(htmlDetalhes))
            return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlDetalhes);

        // Acha a célula do label "Dividendo/cota" e pega o td ao lado
        var labelNode = doc.DocumentNode.SelectSingleNode(
            "//*[self::td or self::th][contains(" +
            "translate(normalize-space(.), " +
            "'abcdefghijklmnopqrstuvwxyzáàâãäéèêëíìîïóòôõöúùûüç', " +
            "'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ'), " +
            "'DIVIDENDO/COTA')]");

        if (labelNode is null)
            return null;

        var valueTd = labelNode.SelectSingleNode("./following-sibling::td[1]");
        if (valueTd is null)
            return null;

        var raw = WebUtility.HtmlDecode(valueTd.InnerText).Trim();

        // Ex.: "R$ 11,17" -> "11,17"
        raw = Regex.Replace(raw, @"[^\d,\.\-]", "");

        // seu ParseDecimalBr retorna 0 quando falha;
        // aqui preferimos null quando não der pra parsear
        var parsed = TryParseDecimalBr(raw);
        return parsed;
    }

    private static decimal? TryParseDecimalBr(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var s = input.Trim().Replace("%", "").Trim();
        if (s is "-" or "—" or "--" or "N/A")
            return null;

        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("pt-BR"), out var v))
            return v;

        // fallback: "1.234,56" -> "1234.56"
        s = s.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
            return v;

        return null;
    }

    // =========================================================
    // NORMALIZAÇÃO (headers)
    // =========================================================

    // Normaliza o texto do cabeçalho para chave estável
    private static string NormalizeHeader(string h)
    {
        h = h.Trim();

        // preserva o caso especial "P/VP" (pra bater com o seu repo)
        if (h.Contains("P/VP", StringComparison.OrdinalIgnoreCase))
            return "P/VP";

        h = h.ToUpperInvariant();

        h = h.Replace("Ç", "C");
        h = h.Replace("Ã", "A").Replace("Á", "A").Replace("À", "A").Replace("Â", "A");
        h = h.Replace("É", "E").Replace("Ê", "E");
        h = h.Replace("Í", "I");
        h = h.Replace("Ó", "O").Replace("Ô", "O").Replace("Õ", "O");
        h = h.Replace("Ú", "U");
        h = h.Replace("²", "2");

        // alguns headers vêm com espaços e quebras
        h = Regex.Replace(h, @"\s+", " ").Trim();
        h = h.Replace(" ", "_");

        return h;
    }

    // =========================================================
    // PARSERS (mantidos como você já usa)
    // =========================================================

    public static decimal ParseDecimalBr(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0m;

        var s = input.Trim();

        // remove percent e separadores comuns
        s = s.Replace("%", "").Trim();

        // alguns campos podem vir com "-" ou vazio
        if (s == "-" || s == "—") return 0m;

        // pt-BR parse (1.234,56)
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("pt-BR"), out var v))
            return v;

        // fallback: tenta trocar manualmente
        s = s.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
            return v;

        return 0m;
    }

    public static int ParseIntBr(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;

        var s = input.Trim();
        if (s == "-" || s == "—") return 0;

        if (int.TryParse(s, NumberStyles.Any, new CultureInfo("pt-BR"), out var v))
            return v;

        s = s.Replace(".", "").Replace(",", ".");
        if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
            return v;

        return 0;
    }
}
