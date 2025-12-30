using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Application.Interfaces;
using VoxFundamentos.Domain.Entities;
using VoxFundamentos.Domain.Interfaces;

namespace VoxFundamentos.Application.Services;

public class FiiService : IFiiService
{
    private readonly IFiiRepository _repo;
    private readonly IIndicadorEconomicoService _indicadores;

    // Defaults (evita "Take(10)" espalhado)
    private const int DefaultTop = 10;
    private const int DefaultMaxConcorrencia = 4;

    public FiiService(
        IFiiRepository repo,
        IIndicadorEconomicoService indicadores)
    {
        _repo = repo;
        _indicadores = indicadores;
    }

    // =========================================================
    // 1) LISTAGEM BÁSICA (com campos calculados) - TOP parametrizável
    // =========================================================

    public async Task<IEnumerable<FiiDto>> ObterFiisAsync(CancellationToken ct)
        => await ObterFiisAsync(top: DefaultTop, ct);

    // ✅ overload interno (se você quiser expor no IFiiService, pode)
    private async Task<IEnumerable<FiiDto>> ObterFiisAsync(int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        // Pega todos do Fundamentus e só então aplica Top (como você pediu)
        var fiis = (await _repo.ObterTodosAsync(ct))
            .OrderBy(f => f.Papel, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();

        return await MapearFiiDtoComCamposCalculadosAsync(fiis, DefaultMaxConcorrencia, ct);
    }

    public async Task<FiiDto?> ObterPorPapelAsync(string papel, CancellationToken ct)
    {
        var fii = await _repo.ObterPorPapelAsync(papel, ct);
        if (fii is null) return null;

        var dto = await MapearFiiDtoComCamposCalculadosAsync(fii, ct);
        return dto;
    }

    // =========================================================
    // 2) ANCORAGEM
    // =========================================================

    public async Task<IEnumerable<FiiAncoragemDto>> ObterFiisAncoragemAsync(CancellationToken ct)
    {
        var fiis = await _repo.ObterTodosAsync(ct);

        return fiis
            .Where(f =>
                f.Liquidez >= 1_500_000m &&
                f.ValorMercado >= 1_000_000_000m &&
                f.Pvp >= 0.98m &&
                f.VacanciaMedia <= 10m &&
                !IsShopping(f.Segmento)
            )
            .OrderBy(f => f.Papel, StringComparer.OrdinalIgnoreCase)
            .Select(ToAncoragemDto)
            .ToList();
    }

    // =========================================================
    // 3) RANKINGS (TIJOLO / PAPEL) - BASE ÚNICA + TOP parametrizável
    // =========================================================

    public async Task<IEnumerable<FiiRankingDto>> ObterMelhoresTijoloAsync(CancellationToken ct)
        => await ObterRankingPorTipoAsync("TIJOLO", DefaultTop, ct);

    public async Task<IEnumerable<FiiRankingDto>> ObterMelhoresPapelAsync(CancellationToken ct)
        => await ObterRankingPorTipoAsync("PAPEL", DefaultTop, ct);

    // =========================================================
    // 4) RISCO (confiável / elevado) - TOP parametrizável
    // =========================================================

    public async Task<IEnumerable<FiiRankingDto>> ObterRiscoConfiavelAsync(CancellationToken ct)
        => await ObterRiscoConfiavelAsync(DefaultTop, ct);

    public async Task<IEnumerable<FiiRankingDto>> ObterRiscoConfiavelAsync(int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        // base grande para não faltar depois do filtro
        var topBase = Math.Max(120, top * 8);

        var todos = await ObterRankingMistoAsync(topBase, ct);

        return todos
            .Where(FiiScoreRules.IsRiscoConfiavel)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Liquidez)
            .Take(top)
            .ToList();
    }

    public async Task<IEnumerable<FiiRankingDto>> ObterRiscoElevadoAsync(int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        var topBase = Math.Max(120, top * 8);

        var todos = await ObterRankingMistoAsync(topBase, ct);

        return todos
            .Where(f => FiiScoreRules.ClassificarPerfil(f) == "Risco Elevado")
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Liquidez)
            .Take(top)
            .ToList();
    }

    // =========================================================
    // 5) CARTEIRAS
    // =========================================================

    public async Task<CarteiraSugeridaDto> ObterCarteiraSugeridaAsync(CancellationToken ct)
    {
        // Regras de alocação
        const decimal pesoTijolo = 60m;
        const decimal pesoPapel = 35m;
        const decimal pesoRisco = 5m;

        // Regras de construção
        const int qtdTijolo = 6;
        const int qtdPapel = 5;
        const int qtdRisco = 2;

        const decimal maxPorAtivo = 15m;
        const decimal minLiquidezGeral = 800_000m;

        // Base grande para seleção
        var baseTijolo = (await ObterRankingPorTipoAsync("TIJOLO", top: 120, ct))
            .Where(x => x.Liquidez >= minLiquidezGeral)
            .ToList();

        var basePapel = (await ObterRankingPorTipoAsync("PAPEL", top: 120, ct))
            .Where(x => x.Liquidez >= minLiquidezGeral)
            .ToList();

        var baseRisco = (await ObterRiscoConfiavelAsync(top: 120, ct))
            .ToList();

        var escolhidosTijolo = baseTijolo.Take(qtdTijolo).ToList();
        var escolhidosPapel = basePapel.Take(qtdPapel).ToList();
        var escolhidosRisco = baseRisco.Take(qtdRisco).ToList();

        var itens = new List<CarteiraSugeridaItemDto>();
        itens.AddRange(DistribuirPeso(escolhidosTijolo, "TIJOLO", pesoTijolo, maxPorAtivo));
        itens.AddRange(DistribuirPeso(escolhidosPapel, "PAPEL", pesoPapel, maxPorAtivo));
        itens.AddRange(DistribuirPeso(escolhidosRisco, "RISCO", pesoRisco, maxPorAtivo));

        NormalizarParaCem(itens);

        return new CarteiraSugeridaDto(
            PesoTijoloPercentual: pesoTijolo,
            PesoPapelPercentual: pesoPapel,
            PesoRiscoPercentual: pesoRisco,
            TotalAtivos: itens.Count,
            Itens: itens
        );
    }

    public async Task<CarteiraSugeridaDto> ObterCarteiraParametrizadaAsync(CarteiraParamRequestDto req, CancellationToken ct)
    {
        if (req.PesoTijoloPercentual < 0 || req.PesoPapelPercentual < 0 || req.PesoRiscoPercentual < 0)
            throw new ArgumentException("Pesos não podem ser negativos.");

        var somaPesos = Math.Round(req.PesoTijoloPercentual + req.PesoPapelPercentual + req.PesoRiscoPercentual, 2);
        if (somaPesos != 100m)
            throw new ArgumentException($"A soma dos percentuais deve ser 100. Atual: {somaPesos}");

        if (req.QtdTijolo < 0 || req.QtdPapel < 0 || req.QtdRisco < 0)
            throw new ArgumentException("Quantidades não podem ser negativas.");

        if (req.QtdTijolo + req.QtdPapel + req.QtdRisco <= 0)
            throw new ArgumentException("Informe pelo menos 1 ativo no total.");

        const decimal maxPorAtivo = 15m;
        const decimal minLiquidezGeral = 800_000m;

        // base grande para filtrar e ainda ter quantidade
        var baseSize = Math.Max(120, (req.QtdTijolo + req.QtdPapel + req.QtdRisco) * 10);

        var rankingTijolo = (await ObterRankingPorTipoAsync("TIJOLO", baseSize, ct))
            .Where(x => x.Liquidez >= minLiquidezGeral)
            .ToList();

        var rankingPapel = (await ObterRankingPorTipoAsync("PAPEL", baseSize, ct))
            .Where(x => x.Liquidez >= minLiquidezGeral)
            .ToList();

        var rankingRisco = (await ObterRiscoConfiavelAsync(baseSize, ct)).ToList();

        var escolhidosTijolo = rankingTijolo.Take(req.QtdTijolo).ToList();
        var escolhidosPapel = rankingPapel.Take(req.QtdPapel).ToList();
        var escolhidosRisco = rankingRisco.Take(req.QtdRisco).ToList();

        var itens = new List<CarteiraSugeridaItemDto>();
        itens.AddRange(DistribuirPeso(escolhidosTijolo, "TIJOLO", req.PesoTijoloPercentual, maxPorAtivo));
        itens.AddRange(DistribuirPeso(escolhidosPapel, "PAPEL", req.PesoPapelPercentual, maxPorAtivo));
        itens.AddRange(DistribuirPeso(escolhidosRisco, "RISCO", req.PesoRiscoPercentual, maxPorAtivo));

        NormalizarParaCem(itens);

        return new CarteiraSugeridaDto(
            PesoTijoloPercentual: req.PesoTijoloPercentual,
            PesoPapelPercentual: req.PesoPapelPercentual,
            PesoRiscoPercentual: req.PesoRiscoPercentual,
            TotalAtivos: itens.Count,
            Itens: itens
        );
    }

    public async Task<CarteiraSugeridaDto> ObterCarteiraPorPercentualETotalAsync(
        CarteiraPercentualRequestDto req,
        CancellationToken ct)
    {
        if (req.TotalFiis <= 0)
            throw new ArgumentException("TotalFiis deve ser maior que zero.");

        if (req.PesoTijoloPercentual < 0 || req.PesoPapelPercentual < 0 || req.PesoRiscoPercentual < 0)
            throw new ArgumentException("Pesos não podem ser negativos.");

        var somaPesos = Math.Round(req.PesoTijoloPercentual + req.PesoPapelPercentual + req.PesoRiscoPercentual, 2);
        if (somaPesos != 100m)
            throw new ArgumentException($"A soma dos percentuais deve ser 100. Atual: {somaPesos}");

        var (qtdTijolo, qtdPapel, qtdRisco) = CalcularQuantidades(
            req.TotalFiis,
            req.PesoTijoloPercentual,
            req.PesoPapelPercentual,
            req.PesoRiscoPercentual);

        var param = new CarteiraParamRequestDto(
            PesoTijoloPercentual: req.PesoTijoloPercentual,
            PesoPapelPercentual: req.PesoPapelPercentual,
            PesoRiscoPercentual: req.PesoRiscoPercentual,
            QtdTijolo: qtdTijolo,
            QtdPapel: qtdPapel,
            QtdRisco: qtdRisco
        );

        return await ObterCarteiraParametrizadaAsync(param, ct);
    }

    public async Task<CarteiraSugeridaDto> ObterCarteiraPorPerfisAsync(CarteiraPerfisRequestDto req, CancellationToken ct)
    {
        if (req.TotalFiis <= 0)
            throw new ArgumentException("TotalFiis deve ser > 0.");

        var soma = Math.Round(
            req.AncoragemPercentual +
            req.PotencialPercentual +
            req.RiscoControladoPercentual +
            req.RiscoElevadoPercentual, 2);

        if (soma != 100m)
            throw new ArgumentException($"A soma dos percentuais deve ser 100. Atual: {soma}");

        var (qAnc, qPot, qRC, qRE) = CalcularQuantidades4(
            req.TotalFiis,
            req.AncoragemPercentual,
            req.PotencialPercentual,
            req.RiscoControladoPercentual,
            req.RiscoElevadoPercentual);

        // base grande, senão falta papel ao separar por perfil
        var topBase = Math.Max(150, req.TotalFiis * 12);
        var todos = await ObterRankingMistoAsync(topBase, ct);

        var ancoragem = todos.Where(f => FiiScoreRules.ClassificarPerfil(f) == "Ancoragem").ToList();
        var potencial = todos.Where(f => FiiScoreRules.ClassificarPerfil(f) == "Potencial").ToList();
        var riscoControlado = todos.Where(f => FiiScoreRules.ClassificarPerfil(f) == "Risco Controlado").ToList();
        var riscoElevado = todos.Where(f => FiiScoreRules.ClassificarPerfil(f) == "Risco Elevado").ToList();

        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var selAnc = Pick(ancoragem, qAnc, usados);
        var selPot = Pick(potencial, qPot, usados);
        var selRC = Pick(riscoControlado, qRC, usados);
        var selRE = Pick(riscoElevado, qRE, usados);

        const decimal maxPorAtivo = 15m;

        var itens = new List<CarteiraSugeridaItemDto>();
        itens.AddRange(DistribuirPeso(selAnc, "Ancoragem", req.AncoragemPercentual, maxPorAtivo));
        itens.AddRange(DistribuirPeso(selPot, "Potencial", req.PotencialPercentual, maxPorAtivo));
        itens.AddRange(DistribuirPeso(selRC, "Risco Controlado", req.RiscoControladoPercentual, maxPorAtivo));
        itens.AddRange(DistribuirPeso(selRE, "Risco Elevado", req.RiscoElevadoPercentual, maxPorAtivo));

        NormalizarParaCem(itens);

        return new CarteiraSugeridaDto(
            PesoTijoloPercentual: 0,
            PesoPapelPercentual: 0,
            PesoRiscoPercentual: 0,
            TotalAtivos: itens.Count,
            Itens: itens
        );
    }

<<<<<<< Updated upstream
    // =========================================================
    // 6) FILTRADOS (ranking PVP + DY) - TOP parametrizável
    // =========================================================
=======
    public async Task<CarteiraAporteResponseDto> SimularAportePorPerfisAsync(
        decimal valorTotal,
        CarteiraPerfisRequestDto req,
        CancellationToken ct
    )
    {
        if (valorTotal <= 0) throw new ArgumentException("Valor deve ser > 0.");

        // Reaproveita seu método existente (gera os 15 FIIs com tipo e proventoMensalPorCota etc.)
        var carteira = await ObterCarteiraPorPerfisAsync(req, ct);

        // Agrupa por perfil (Tipo)
        var grupos = carteira.Itens
            .GroupBy(x => x.Tipo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        PerfilAporteDto MontarPerfil(string perfil, decimal percentual)
        {
            grupos.TryGetValue(perfil, out var fiis);
            fiis ??= new List<FiiDto>();

            var valorAlocado = Math.Round(valorTotal * (percentual / 100m), 2);

            if (fiis.Count == 0)
            {
                return new PerfilAporteDto(
                    Perfil: perfil,
                    Percentual: percentual,
                    ValorAlocado: valorAlocado,
                    ValorInvestido: 0m,
                    SobraCaixa: valorAlocado,
                    RendaMensal: 0m,
                    RendaDiaria: 0m,
                    Itens: new List<ItemAporteDto>());
            }

            var verbaPorFii = valorAlocado / fiis.Count;

            var itens = new List<ItemAporteDto>();
            foreach (var f in fiis)
            {
                var cotacao = f.Cotacao;
                var proventoMensal = f.ProventoMensalPorCota;

                var cotas = (cotacao > 0m)
                    ? (int)Math.Floor(verbaPorFii / cotacao)
                    : 0;

                var investido = Math.Round(cotas * cotacao, 2);
                var rendaMensal = Math.Round(cotas * proventoMensal, 4);
                var rendaDiaria = Math.Round(rendaMensal / 30.4375m, 6); // média mês

                itens.Add(new ItemAporteDto(
                    Papel: f.Papel,
                    Segmento: f.Segmento,
                    Cotacao: cotacao,
                    ProventoMensalPorCota: proventoMensal,
                    Cotas: cotas,
                    ValorInvestido: investido,
                    RendaMensal: rendaMensal,
                    RendaDiaria: rendaDiaria
                ));
            }

            var valorInvestidoPerfil = Math.Round(itens.Sum(x => x.ValorInvestido), 2);
            var sobra = Math.Round(valorAlocado - valorInvestidoPerfil, 2);
            var rendaMensalPerfil = Math.Round(itens.Sum(x => x.RendaMensal), 4);
            var rendaDiariaPerfil = Math.Round(itens.Sum(x => x.RendaDiaria), 6);

            return new PerfilAporteDto(
                Perfil: perfil,
                Percentual: percentual,
                ValorAlocado: Math.Round(valorAlocado, 2),
                ValorInvestido: valorInvestidoPerfil,
                SobraCaixa: sobra,
                RendaMensal: rendaMensalPerfil,
                RendaDiaria: rendaDiariaPerfil,
                Itens: itens
            );
        }

        var anc = MontarPerfil("Ancoragem", req.AncoragemPercentual);
        var pot = MontarPerfil("Potencial", req.PotencialPercentual);
        var rc = MontarPerfil("Risco Controlado", req.RiscoControladoPercentual);
        var re = MontarPerfil("Risco Elevado", req.RiscoElevadoPercentual);

        var totalInvestido = Math.Round(anc.ValorInvestido + pot.ValorInvestido + rc.ValorInvestido + re.ValorInvestido, 2);
        var sobraGeral = Math.Round(anc.SobraCaixa + pot.SobraCaixa + rc.SobraCaixa + re.SobraCaixa, 2);
        var rendaMensalGeral = Math.Round(anc.RendaMensal + pot.RendaMensal + rc.RendaMensal + re.RendaMensal, 4);
        var rendaDiariaGeral = Math.Round(anc.RendaDiaria + pot.RendaDiaria + rc.RendaDiaria + re.RendaDiaria, 6);

        return new CarteiraAporteResponseDto(
            ValorTotal: Math.Round(valorTotal, 2),
            AncoragemPercentual: req.AncoragemPercentual,
            PotencialPercentual: req.PotencialPercentual,
            RiscoControladoPercentual: req.RiscoControladoPercentual,
            RiscoElevadoPercentual: req.RiscoElevadoPercentual,
            Ancoragem: anc,
            Potencial: pot,
            RiscoControlado: rc,
            RiscoElevado: re,
            TotaisGerais: new TotaisAporteDto(
                ValorInvestido: totalInvestido,
                SobraCaixa: sobraGeral,
                RendaMensal: rendaMensalGeral,
                RendaDiaria: rendaDiariaGeral
            )
        );
    }



    private static (string tipoPerfil, string[] motivos) CalcularTipoEMotivos(Fii f, string? perfilForcado = null)
    {
        var a = ToAncoragemDto(f);
        var tipoFundo = FiiScoreRules.DetectarTipo(f.Segmento);

        var (score, risco, motivosBase) = tipoFundo == "TIJOLO"
            ? FiiScoreRules.ScoreTijolo(a)
            : FiiScoreRules.ScorePapel(a);

        // ✅ se FiiRankingDto.Motivos for List<string>
        var rankingTmp = new FiiRankingDto(
            Papel: f.Papel,
            Segmento: f.Segmento,
            Cotacao: f.Cotacao,
            DividendYield: f.DividendYield,
            Pvp: f.Pvp,
            ValorMercado: f.ValorMercado,
            Liquidez: f.Liquidez,
            VacanciaMedia: f.VacanciaMedia,
            QuantidadeImoveis: f.QuantidadeImoveis,
            Score: score,
            Tipo: tipoFundo,
            Risco: risco,
            Motivos: motivosBase ?? Array.Empty<string>()
        );

        var perfilCalculado = FiiScoreRules.ClassificarPerfil(rankingTmp);
        var perfilFinal = string.IsNullOrWhiteSpace(perfilForcado) ? perfilCalculado : perfilForcado;

        var motivosPerfil = MotivosPerfil(a, perfilFinal, tipoFundo, risco, score);

        var final = new List<string>();

        if (motivosBase is { Length: > 0 })
            final.AddRange(motivosBase);

        if (motivosPerfil is { Length: > 0 })
            final.AddRange(motivosPerfil);

        var arr = final
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (arr.Length == 0)
            arr = new[] { $"Classificado como '{perfilFinal}' pelas regras." };

        return (perfilFinal, arr);
    }

>>>>>>> Stashed changes

    public async Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(CancellationToken ct)
        => await ObterFiisFiltradosAsync(top: DefaultTop, ct);

    private async Task<IEnumerable<FiiDto>> ObterFiisFiltradosAsync(int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        var selic = await _indicadores.ObterSelicAtualAsync(ct);

        var dyMinimo = selic - 3m;
        var dyMaximo = 20m;

        var pvpMin = 0.50m;
        var pvpMax = 1.00m;

        var liquidezMin = 400000m;

        var fiis = await _repo.ObterTodosAsync(ct);

        var baseList = fiis
            .Where(f =>
                f.DividendYield >= dyMinimo &&
                f.DividendYield <= dyMaximo &&
                f.Pvp >= pvpMin &&
                f.Pvp <= pvpMax &&
                f.Liquidez >= liquidezMin &&
                f.VacanciaMedia <= 100
            )
            .ToList();

        if (baseList.Count == 0)
            return Array.Empty<FiiDto>();

        var rankPvpMap = baseList
            .OrderBy(f => f.Pvp)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        var rankDyMap = baseList
            .OrderByDescending(f => f.DividendYield)
            .Select((f, index) => new { f.Papel, Rank = index + 1 })
            .ToDictionary(x => x.Papel, x => x.Rank, StringComparer.OrdinalIgnoreCase);

        var topBase = baseList
            .Select(f =>
            {
                var rankPvp = rankPvpMap[f.Papel];
                var rankDy = rankDyMap[f.Papel];
                var rankLevel = (rankPvp + rankDy) / 2m;

                return new { Fii = f, rankPvp, rankDy, rankLevel };
            })
            .OrderBy(x => x.rankLevel)
            .ThenBy(x => x.rankPvp)
            .ThenBy(x => x.rankDy)
            .Take(top)
            .ToList();

        // Mapeia com campos calculados (div/cota, etc)
        var dtos = await MapearFiiDtoComCamposCalculadosAsync(topBase.Select(x => x.Fii).ToList(), DefaultMaxConcorrencia, ct);

        // reintroduz ranks
        var dtoMap = dtos.ToDictionary(x => x.Papel, x => x, StringComparer.OrdinalIgnoreCase);

        var result = topBase.Select(x =>
        {
            var d = dtoMap[x.Fii.Papel];

            return d with
            {
                RankPvp = x.rankPvp,
                RankDy = x.rankDy,
                RankLevel = x.rankLevel
            };
        });

        return result
            .OrderBy(x => x.RankLevel)
            .ThenBy(x => x.RankPvp)
            .ThenBy(x => x.RankDy)
            .ToList();
    }

    public async Task<AporteFiltradosResponseDto> SimularAporteFiisFiltradosAsync(
    decimal valorTotal,
    int top,
    CancellationToken ct)
    {
        if (valorTotal <= 0) throw new ArgumentException("Valor deve ser > 0.");
        if (top <= 0) top = DefaultTop;

        // usa seu método existente (ele já calcula ranks + campos calculados)
        var fiis = (await ObterFiisFiltradosAsync(top, ct)).ToList();

        if (fiis.Count == 0)
        {
            return new AporteFiltradosResponseDto(
                ValorTotal: Math.Round(valorTotal, 2),
                Top: top,
                ValorInvestido: 0m,
                SobraCaixa: Math.Round(valorTotal, 2),
                RendaMensal: 0m,
                RendaDiaria: 0m,
                Itens: new List<ItemAporteFiltradoDto>()
            );
        }

        var verbaPorFii = valorTotal / fiis.Count;

        var itens = new List<ItemAporteFiltradoDto>();
        foreach (var f in fiis)
        {
            var cotacao = f.Cotacao;
            var proventoMensal = f.ProventoMensalPorCota;

            var cotas = (cotacao > 0m)
                ? (int)Math.Floor(verbaPorFii / cotacao)
                : 0;

            var investido = Math.Round(cotas * cotacao, 2);
            var rendaMensal = Math.Round(cotas * proventoMensal, 4);
            var rendaDiaria = Math.Round(rendaMensal / 30.4375m, 6); // média mês

            itens.Add(new ItemAporteFiltradoDto(
                Papel: f.Papel,
                Segmento: f.Segmento,
                Cotacao: cotacao,
                ProventoMensalPorCota: proventoMensal,
                Cotas: cotas,
                ValorInvestido: investido,
                RendaMensal: rendaMensal,
                RendaDiaria: rendaDiaria,
                RankPvp: f.RankPvp,
                RankDy: f.RankDy,
                RankLevel: f.RankLevel,
                DividendYield: f.DividendYield,
                Pvp: f.Pvp
            ));
        }

        var totalInvestido = Math.Round(itens.Sum(x => x.ValorInvestido), 2);
        var sobra = Math.Round(valorTotal - totalInvestido, 2);
        var rendaMensalTotal = Math.Round(itens.Sum(x => x.RendaMensal), 4);
        var rendaDiariaTotal = Math.Round(itens.Sum(x => x.RendaDiaria), 6);

        return new AporteFiltradosResponseDto(
            ValorTotal: Math.Round(valorTotal, 2),
            Top: top,
            ValorInvestido: totalInvestido,
            SobraCaixa: sobra,
            RendaMensal: rendaMensalTotal,
            RendaDiaria: rendaDiariaTotal,
            Itens: itens
                .OrderBy(x => x.RankLevel)
                .ThenBy(x => x.RankPvp)
                .ThenBy(x => x.RankDy)
                .ToList()
        );
    }


    // =========================================================
    // PRIVADOS: ranking por tipo, ranking misto, mapeamentos
    // =========================================================

    private async Task<List<FiiRankingDto>> ObterRankingPorTipoAsync(string tipo, int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        var fiis = await _repo.ObterTodosAsync(ct);

        var baseList = fiis
            .Where(f => FiiScoreRules.DetectarTipo(f.Segmento) == tipo)
            .Select(ToAncoragemDto)
            .ToList();

        return baseList
            .Select(f =>
            {
                var (score, risco, motivos) = tipo == "TIJOLO"
                    ? FiiScoreRules.ScoreTijolo(f)
                    : FiiScoreRules.ScorePapel(f);

                return new FiiRankingDto(
                    Papel: f.Papel,
                    Segmento: f.Segmento,
                    Cotacao: f.Cotacao,
                    DividendYield: f.DividendYield,
                    Pvp: f.Pvp,
                    ValorMercado: f.ValorMercado,
                    Liquidez: f.Liquidez,
                    VacanciaMedia: f.VacanciaMedia,
                    QuantidadeImoveis: f.QuantidadeImoveis,
                    Score: score,
                    Tipo: tipo,
                    Risco: risco,
                    Motivos: motivos
                );
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Liquidez)
            .Take(top)
            .ToList();
    }

    private async Task<List<FiiRankingDto>> ObterRankingMistoAsync(int top, CancellationToken ct)
    {
        if (top <= 0) top = DefaultTop;

        // Para o misto, pegamos bastante de cada e depois cortamos
        var perType = Math.Max(80, top);

        var t = await ObterRankingPorTipoAsync("TIJOLO", perType, ct);
        var p = await ObterRankingPorTipoAsync("PAPEL", perType, ct);

        return t.Concat(p)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Liquidez)
            .Take(top)
            .ToList();
    }

    private static FiiAncoragemDto ToAncoragemDto(Fii f)
    => new FiiAncoragemDto(
        Papel: f.Papel,
        Segmento: f.Segmento,
        Cotacao: f.Cotacao,
        FfoYield: f.FfoYield,
        DividendYield: f.DividendYield,
        Pvp: f.Pvp,
        ValorMercado: f.ValorMercado,
        Liquidez: f.Liquidez,
        QuantidadeImoveis: f.QuantidadeImoveis,
        PrecoMetroQuadrado: f.PrecoMetroQuadrado,
        AluguelMetroQuadrado: f.AluguelMetroQuadrado,
        CapRate: f.CapRate,
        VacanciaMedia: f.VacanciaMedia
    );


    private async Task<List<FiiDto>> MapearFiiDtoComCamposCalculadosAsync(
        List<Fii> fiis,
        int maxConcorrencia,
        CancellationToken ct)
    {
        using var sem = new SemaphoreSlim(maxConcorrencia);

        var tasks = fiis.Select(async f =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var divCota12m = await _repo.ObterDividendoPorCotaAsync((string)f.Papel, ct) ?? 0m;

                var proventoMensal = CalcularProventoMensalPeloDivCota(divCota12m);
                var proventoDiario = CalcularProventoDiario(proventoMensal);
                var dyMensal = CalcularDyMensalPeloProvento((decimal)f.Cotacao, proventoMensal);

                var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico((decimal)f.Cotacao, proventoMensal);
                var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, (decimal)f.Cotacao);

                return new FiiDto(
                    RankPvp: 0,
                    RankDy: 0,
                    RankLevel: 0,
                    Papel: f.Papel,
                    Segmento: f.Segmento,
                    Cotacao: f.Cotacao,
                    FfoYield: f.FfoYield,
                    DividendYield: f.DividendYield,
                    Pvp: f.Pvp,
                    ValorMercado: f.ValorMercado,
                    Liquidez: f.Liquidez,
                    QuantidadeImoveis: f.QuantidadeImoveis,
                    PrecoMetroQuadrado: f.PrecoMetroQuadrado,
                    AluguelMetroQuadrado: f.AluguelMetroQuadrado,
                    CapRate: f.CapRate,
                    VacanciaMedia: f.VacanciaMedia,
                    DividendoPorCota: divCota12m,
                    DyMensalPercentual: dyMensal,
                    ProventoMensalPorCota: proventoMensal,
                    ProventoDiarioPorCota: proventoDiario,
                    QtdCotasNumeroMagico: qtdCotasNumeroMagico,
                    ValorParaNumeroMagico: valorParaNumeroMagico
                );
            }
            finally
            {
                sem.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<FiiDto> MapearFiiDtoComCamposCalculadosAsync(Fii f, CancellationToken ct)
    {
        var divCota12m = await _repo.ObterDividendoPorCotaAsync(f.Papel, ct) ?? 0m;

        var proventoMensal = CalcularProventoMensalPeloDivCota(divCota12m);
        var proventoDiario = CalcularProventoDiario(proventoMensal);
        var dyMensal = CalcularDyMensalPeloProvento(f.Cotacao, proventoMensal);

        var qtdCotasNumeroMagico = CalcularQtdCotasNumeroMagico(f.Cotacao, proventoMensal);
        var valorParaNumeroMagico = CalcularValorParaNumeroMagico(qtdCotasNumeroMagico, f.Cotacao);

        return new FiiDto(
            RankPvp: 0,
            RankDy: 0,
            RankLevel: 0,
            Papel: f.Papel,
            Segmento: f.Segmento,
            Cotacao: f.Cotacao,
            FfoYield: f.FfoYield,
            DividendYield: f.DividendYield,
            Pvp: f.Pvp,
            ValorMercado: f.ValorMercado,
            Liquidez: f.Liquidez,
            QuantidadeImoveis: f.QuantidadeImoveis,
            PrecoMetroQuadrado: f.PrecoMetroQuadrado,
            AluguelMetroQuadrado: f.AluguelMetroQuadrado,
            CapRate: f.CapRate,
            VacanciaMedia: f.VacanciaMedia,
            DividendoPorCota: divCota12m,
            DyMensalPercentual: dyMensal,
            ProventoMensalPorCota: proventoMensal,
            ProventoDiarioPorCota: proventoDiario,
            QtdCotasNumeroMagico: qtdCotasNumeroMagico,
            ValorParaNumeroMagico: valorParaNumeroMagico
        );
    }


    // =========================================================
    // HELPERS: picks, quantidades, distribuição, etc
    // =========================================================

    private static List<FiiRankingDto> Pick(List<FiiRankingDto> fonte, int qtd, HashSet<string> usados)
    {
        var result = new List<FiiRankingDto>();
        foreach (var f in fonte)
        {
            if (result.Count >= qtd) break;
            if (usados.Add(f.Papel))
                result.Add(f);
        }
        return result;
    }

    private static (int qtdTijolo, int qtdPapel, int qtdRisco) CalcularQuantidades(
        int totalFiis,
        decimal pesoTijolo,
        decimal pesoPapel,
        decimal pesoRisco)
    {
        var exT = (pesoTijolo / 100m) * totalFiis;
        var exP = (pesoPapel / 100m) * totalFiis;
        var exR = (pesoRisco / 100m) * totalFiis;

        var t = (int)Math.Floor(exT);
        var p = (int)Math.Floor(exP);
        var r = (int)Math.Floor(exR);

        var soma = t + p + r;
        var faltam = totalFiis - soma;

        var restos = new List<(string key, decimal resto)>
        {
            ("T", exT - t),
            ("P", exP - p),
            ("R", exR - r)
        }
        .OrderByDescending(x => x.resto)
        .ToList();

        var i = 0;
        while (faltam > 0)
        {
            var k = restos[i % restos.Count].key;
            if (k == "T") t++;
            else if (k == "P") p++;
            else r++;

            faltam--;
            i++;
        }

        return (t, p, r);
    }

    private static (int a, int p, int rc, int re) CalcularQuantidades4(
        int total,
        decimal pa, decimal pp, decimal prc, decimal pre)
    {
        var exA = (pa / 100m) * total;
        var exP = (pp / 100m) * total;
        var exRC = (prc / 100m) * total;
        var exRE = (pre / 100m) * total;

        var a = (int)Math.Floor(exA);
        var p = (int)Math.Floor(exP);
        var rc = (int)Math.Floor(exRC);
        var re = (int)Math.Floor(exRE);

        var soma = a + p + rc + re;
        var faltam = total - soma;

        var restos = new List<(string k, decimal r)>
        {
            ("A", exA - a),
            ("P", exP - p),
            ("RC", exRC - rc),
            ("RE", exRE - re),
        }
        .OrderByDescending(x => x.r)
        .ToList();

        var i = 0;
        while (faltam > 0)
        {
            var k = restos[i % restos.Count].k;
            if (k == "A") a++;
            else if (k == "P") p++;
            else if (k == "RC") rc++;
            else re++;

            faltam--;
            i++;
        }

        return (a, p, rc, re);
    }

    private static List<CarteiraSugeridaItemDto> DistribuirPeso(
        List<FiiRankingDto> selecionados,
        string tipoCarteira,
        decimal pesoBucket,
        decimal maxPorAtivo)
    {
        if (selecionados.Count == 0) return new List<CarteiraSugeridaItemDto>();

        var pesoBase = Math.Round(pesoBucket / selecionados.Count, 2);
        if (pesoBase > maxPorAtivo) pesoBase = maxPorAtivo;

        return selecionados.Select(f => new CarteiraSugeridaItemDto(
            Papel: f.Papel,
            Tipo: tipoCarteira,
            Score: f.Score,
            Risco: f.Risco,
            PesoPercentual: pesoBase,
            Cotacao: f.Cotacao,
            DividendYield: f.DividendYield,
            Pvp: f.Pvp,
            Liquidez: f.Liquidez,
            ValorMercado: f.ValorMercado,
            Segmento: f.Segmento,
            Motivos: f.Motivos
        )).ToList();
    }

    private static void NormalizarParaCem(List<CarteiraSugeridaItemDto> itens)
    {
        if (itens.Count == 0) return;

        var soma = itens.Sum(i => i.PesoPercentual);
        var diff = Math.Round(100m - soma, 2);
        if (diff == 0m) return;

        var maxScore = itens.Max(i => i.Score);
        var best = itens.FirstOrDefault(i => i.Score == maxScore) ?? itens[0];
        var idx = itens.IndexOf(best);

        itens[idx] = itens[idx] with
        {
            PesoPercentual = Math.Round(itens[idx].PesoPercentual + diff, 2)
        };
    }

    private static bool IsShopping(string? segmento)
    {
        if (string.IsNullOrWhiteSpace(segmento)) return false;
        return segmento.Trim().ToLowerInvariant().Contains("shopping");
    }

    // =========================================================
    // CÁLCULOS (mantidos)
    // =========================================================

    private static decimal CalcularProventoMensalPeloDivCota(decimal dividendoPorCota12m)
    {
        if (dividendoPorCota12m <= 0) return 0m;
        return Math.Round(dividendoPorCota12m / 12m, 2);
    }

    private static decimal CalcularDyMensalPeloProvento(decimal cotacao, decimal proventoMensal)
    {
        if (cotacao <= 0 || proventoMensal <= 0) return 0m;
        return Math.Round((proventoMensal / cotacao) * 100m, 2);
    }

    private static decimal CalcularProventoDiario(decimal proventoMensal)
    {
        var diasNoMes = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        if (diasNoMes <= 0 || proventoMensal <= 0) return 0m;
        return Math.Round(proventoMensal / diasNoMes, 6);
    }

    private static int CalcularQtdCotasNumeroMagico(decimal cotacao, decimal proventoMensal)
    {
        if (cotacao <= 0 || proventoMensal <= 0) return 0;
        return (int)Math.Ceiling(cotacao / proventoMensal);
    }

    private static decimal CalcularValorParaNumeroMagico(int qtdCotasNumeroMagico, decimal cotacao)
    {
        if (qtdCotasNumeroMagico <= 0 || cotacao <= 0) return 0m;
        return Math.Round(qtdCotasNumeroMagico * cotacao, 2);
    }
}
