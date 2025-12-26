using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace VoxFundamentos.Infrastructure.Scraping;

public class FundamentusFiiScraper
{
    private const string Url = "https://www.fundamentus.com.br/fii_resultado.php";
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

        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en;q=0.8");
        _http.DefaultRequestHeaders.Referrer = new Uri("https://www.fundamentus.com.br/");
    }

    public async Task<string> DownloadHtmlAsync(CancellationToken ct)
    {
        Exception? last = null;

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                // polidez
                await Task.Delay(TimeSpan.FromMilliseconds(_rng.Next(900, 2200)), ct);

                using var req = new HttpRequestMessage(HttpMethod.Get, Url);
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

        throw new InvalidOperationException("Falha ao baixar HTML do Fundamentus.", last);
    }

    public IReadOnlyList<Dictionary<string, string>> ParseTableRows(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // primeira tabela com <th>
        var table = doc.DocumentNode.SelectSingleNode("//table[.//th]");
        if (table is null)
            throw new InvalidOperationException("Tabela não encontrada. O layout pode ter mudado.");

        var headers = table.SelectNodes(".//tr/th")
            ?.Select(h => WebUtility.HtmlDecode(h.InnerText).Trim())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();

        if (headers is null || headers.Count == 0)
            throw new InvalidOperationException("Cabeçalhos da tabela não encontrados.");

        var rows = table.SelectNodes(".//tr[td]");
        if (rows is null || rows.Count == 0) return Array.Empty<Dictionary<string, string>>();

        var result = new List<Dictionary<string, string>>();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells is null) continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < Math.Min(headers.Count, cells.Count); i++)
            {
                var text = WebUtility.HtmlDecode(cells[i].InnerText).Trim();
                dict[NormalizeHeader(headers[i])] = text;
            }

            result.Add(dict);
        }

        return result;
    }

    // Normaliza o texto do cabeçalho para chave estável
    private static string NormalizeHeader(string h)
    {
        h = h.Trim().ToUpperInvariant();
        h = h.Replace("Ç", "C");
        h = h.Replace("Ã", "A").Replace("Á", "A").Replace("À", "A").Replace("Â", "A");
        h = h.Replace("É", "E").Replace("Ê", "E");
        h = h.Replace("Í", "I");
        h = h.Replace("Ó", "O").Replace("Ô", "O").Replace("Õ", "O");
        h = h.Replace("Ú", "U");
        h = h.Replace("²", "2");
        h = h.Replace(" ", "_");
        return h;
    }

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
