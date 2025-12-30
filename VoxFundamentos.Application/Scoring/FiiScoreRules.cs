using System.Globalization;
using System.Text;
using VoxFundamentos.Application.DTOs;

public static class FiiScoreRules
{
    public static string ClassificarPerfil(FiiRankingDto f)
    {
        if (f is null)
            return "Risco Elevado";

        // 🟡 RISCO CONTROLADO: sua janela “boa” (prioridade)
        if (FiiScoreRules.IsRiscoConfiavel(f))
            return "Risco Controlado";

        // 🟢 ANCORAGEM: score alto + perfil conservador
        if (f.Score >= 8.0m &&
            string.Equals(f.Risco, "Conservador", StringComparison.OrdinalIgnoreCase))
            return "Ancoragem";

        // 🟠 POTENCIAL: bom score, mas não atende ancoragem nem risco controlado
        if (f.Score >= 7.0m && f.Score < 8.0m)
            return "Potencial";

        // 🔴 RISCO ELEVADO: o restante
        return "Risco Elevado";
    }

    public static string DetectarTipo(string? segmento)
    {
        var s = Normalizar(segmento);

        // PAPEL: CRI / recebíveis / papel (heurística)
        if (s.Contains("cri") || s.Contains("receb") || s.Contains("papel"))
            return "PAPEL";

        // Se quiser: considerar "hibrido" fora por enquanto
        if (s.Contains("hibr"))
            return "HIBRIDO";

        // Tudo que sobrar, tratamos como TIJOLO
        return "TIJOLO";
    }

    public static (decimal score, string risco, string[] motivos) ScoreTijolo(FiiAncoragemDto f)
    {
        var motivos = new List<string>();

        // Pré-filtro (investível)
        if (f.Liquidez < 500_000m) return (0, "N/A", new[] { "Liquidez < 500k (fora do ranking)." });
        if (f.DividendYield < 6m) return (0, "N/A", new[] { "DY < 6% (fora do ranking)." });
        if (f.VacanciaMedia > 15m) return (0, "N/A", new[] { "Vacância > 15% (fora do ranking)." });

        // Sub-notas 0..10 (baseado no que você já tem)
        var sVac = NotaVacanciaTijolo(f.VacanciaMedia);
        var sLiq = NotaLiquidez(f.Liquidez);
        var sVm = NotaValorMercado(f.ValorMercado);
        var sDy = NotaDyTijolo(f.DividendYield);
        var sPvp = NotaPvp(f.Pvp);
        var sImo = NotaQtdImoveis(f.QuantidadeImoveis);
        var sSeg = BonusSegmentoTijolo(f.Segmento); // -0.3..+0.3

        // Pesos (v1 com Fundamentus)
        // Vacância 25%, Liquidez 20%, VM 15%, DY 20%, P/VP 15%, Imóveis 5%
        decimal score =
            sVac * 0.25m +
            sLiq * 0.20m +
            sVm * 0.15m +
            sDy * 0.20m +
            sPvp * 0.15m +
            sImo * 0.05m;

        score = score + sSeg; // ajuste leve por segmento

        score = Clamp(score, 0m, 10m);

        // Motivos
        if (f.VacanciaMedia > 10m) motivos.Add("Vacância acima de 10%.");
        if (f.Pvp > 1.15m) motivos.Add("P/VP esticado.");
        if (f.ValorMercado < 1_000_000_000m) motivos.Add("Valor de mercado menor (mais volátil).");
        if (f.Liquidez < 1_000_000m) motivos.Add("Liquidez moderada/baixa.");

        var risco = ClassificarRisco(score);

        return (Math.Round(score, 2), risco, motivos.ToArray());
    }

    public static (decimal score, string risco, string[] motivos) ScorePapel(FiiAncoragemDto f)
    {
        var motivos = new List<string>();

        // Pré-filtro (investível) - papel tende a DY > tijolo
        if (f.Liquidez < 400_000m) return (0, "N/A", new[] { "Liquidez < 400k (fora do ranking)." });
        if (f.DividendYield < 8m) return (0, "N/A", new[] { "DY < 8% (fora do ranking de papel)." });

        // Como você ainda não tem LTV/inadimplência/concentração, v1 é “proxy”
        var sLiq = NotaLiquidez(f.Liquidez);
        var sVm = NotaValorMercado(f.ValorMercado);
        var sDy = NotaDyPapel(f.DividendYield);
        var sPvp = NotaPvp(f.Pvp);

        // Penalidade se vacância vier muito alta (pode indicar que não é papel ou dado ruim)
        var penVac = (f.VacanciaMedia > 30m) ? -0.3m : 0m;

        // Pesos v1 papel (proxy)
        // DY 35%, Liquidez 25%, VM 20%, P/VP 20%
        decimal score =
            sDy * 0.35m +
            sLiq * 0.25m +
            sVm * 0.20m +
            sPvp * 0.20m +
            penVac;

        score = Clamp(score, 0m, 10m);

        if (f.Pvp > 1.15m) motivos.Add("P/VP esticado.");
        if (f.DividendYield > 16m) motivos.Add("DY muito alto (pode ser risco).");
        if (f.Liquidez < 800_000m) motivos.Add("Liquidez moderada/baixa.");

        var risco = ClassificarRisco(score);

        return (Math.Round(score, 2), risco, motivos.ToArray());
    }

    // ======= Notas (0..10) =======

    private static decimal NotaVacanciaTijolo(decimal vac)
        => vac switch
        {
            <= 5m => 10m,
            <= 10m => 8m,
            <= 15m => 6m,
            _ => 0m
        };

    private static decimal NotaLiquidez(decimal liq)
        => liq switch
        {
            >= 2_000_000m => 10m,
            >= 1_000_000m => 8m,
            >= 500_000m => 6m,
            _ => 0m
        };

    private static decimal NotaValorMercado(decimal vm)
        => vm switch
        {
            >= 2_000_000_000m => 10m,
            >= 1_000_000_000m => 8m,
            >= 500_000_000m => 6m,
            _ => 4m
        };

    private static decimal NotaDyTijolo(decimal dy)
        => dy switch
        {
            >= 8m and <= 11m => 10m,
            >= 7m and < 8m => 8m,
            > 11m and <= 13m => 7m,
            >= 6m and < 7m => 6m,
            _ => 4m
        };

    private static decimal NotaDyPapel(decimal dy)
        => dy switch
        {
            >= 9m and <= 13.5m => 10m,
            >= 8m and < 9m => 8m,
            > 13.5m and <= 16m => 7m,
            _ => 4m
        };

    private static decimal NotaPvp(decimal pvp)
        => pvp switch
        {
            >= 0.95m and <= 1.05m => 10m,
            >= 0.90m and < 0.95m => 8m,
            > 1.05m and <= 1.10m => 8m,
            >= 0.85m and < 0.90m => 6m,
            > 1.10m and <= 1.20m => 6m,
            _ => 4m
        };

    private static decimal NotaQtdImoveis(int qtd)
        => qtd switch
        {
            >= 10 => 10m,
            >= 5 => 8m,
            >= 3 => 6m,
            _ => 4m
        };

    private static decimal BonusSegmentoTijolo(string? segmento)
    {
        var s = Normalizar(segmento);
        if (s.Contains("log")) return 0.3m;       // logística
        if (s.Contains("hosp") || s.Contains("saud")) return 0.3m; // saúde/hospital
        if (s.Contains("laje") || s.Contains("escr")) return 0.1m; // lajes/escritórios
        if (s.Contains("shop")) return 0.0m;      // shopping neutro (ou -0.1 se quiser)
        return 0.0m;
    }

    private static string ClassificarRisco(decimal score)
        => score switch
        {
            >= 8.0m => "Conservador",
            >= 6.5m => "Moderado",
            _ => "Arrojado"
        };

    private static decimal Clamp(decimal v, decimal min, decimal max)
        => Math.Max(min, Math.Min(max, v));

    private static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim().ToLowerInvariant();

        // remove acentos simples
        var normalized = s.Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray());
    }

    public static bool IsRiscoConfiavel(FiiRankingDto f)
    {
        // Janela de risco “bom”: nem conservador demais, nem bomba
        return
            f.Score >= 6.5m &&
            f.Score <= 7.7m &&
            f.Liquidez >= 800_000m &&
            f.ValorMercado >= 600_000_000m &&
            f.DividendYield >= 9m &&
            f.Pvp <= 1.10m;
    }
}
