using Microsoft.AspNetCore.Mvc;
using VoxFundamentos.Application.DTOs;
using VoxFundamentos.Application.Interfaces;

namespace VoxFundamentos.Api.Controllers;

[ApiController]
[Route("api/fiis")]
public class FiisController : ControllerBase
{
    private readonly IFiiService _service;

    public FiisController(IFiiService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista FIIs (modo geral) com dados do Fundamentus e alguns cálculos extras.
    ///
    /// O que traz:
    /// - Lista de FIIs (atualmente com TAKE(10) no service)
    /// - Campos básicos: cotação, DY, P/VP, liquidez, valor de mercado, vacância etc.
    /// - Campos calculados: dividendo por cota (12m), provento mensal/diário, DY mensal,
    ///   número mágico de cotas e valor para atingir o “número mágico”.
    ///
    /// Bom:
    /// - Bom para “visão geral” e testar a API rapidamente.
    /// - Já retorna cálculos úteis para reinvestimento (número mágico).
    ///
    /// Ruim / cuidados:
    /// - Não é ranking: não garante os “melhores”, apenas uma amostra (por causa do Take(10)).
    /// - Pode ter chamadas concorrentes ao scraper (custo/latência).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var data = await _service.ObterFiisAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Busca um FII específico pelo código (papel) e retorna detalhes + cálculos.
    ///
    /// O que traz:
    /// - O FII do papel informado (ex: HGLG11)
    /// - Campos do Fundamentus + dividendo por cota (12m)
    /// - Cálculos: provento mensal/diário, DY mensal, número mágico e valor do número mágico.
    ///
    /// Bom:
    /// - Ideal para detalhar um fundo antes de investir.
    /// - Útil para simular reinvestimento com base no dividendo médio.
    ///
    /// Ruim / cuidados:
    /// - Depende do dado de dividendo por cota (scraping pode falhar/oscilar).
    /// - “Dividendo por cota 12m / 12” é uma média: não garante o pagamento futuro.
    /// </summary>
    [HttpGet("{papel}")]
    public async Task<IActionResult> GetByPapel(string papel, CancellationToken ct)
    {
        var fii = await _service.ObterPorPapelAsync(papel, ct);

        if (fii is null)
            return NotFound(new { message = $"FII '{papel}' não encontrado." });

        return Ok(fii);
    }

    /// <summary>
    /// Retorna FIIs filtrados por critérios “de oportunidade” com ranking interno (RankPvp + RankDy).
    ///
    /// O que traz:
    /// - Aplica filtros (DY, P/VP, liquidez, etc.) usando a SELIC como base (DY mínimo = SELIC - 3).
    /// - Faz ranking combinando:
    ///   - RankPvp (menor P/VP melhor)
    ///   - RankDy  (maior DY melhor)
    ///   - RankLevel = média dos ranks
    /// - Retorna TOP 10 após ranqueamento
    /// - Inclui cálculos extras (dividendo por cota 12m, proventos, número mágico…)
    ///
    /// Bom:
    /// - Excelente para achar “candidatos” com bom DY e P/VP.
    /// - Usa SELIC para adaptar o filtro ao cenário macro.
    ///
    /// Ruim / cuidados:
    /// - Pode puxar fundos com DY alto, mas risco alto (não analisa qualidade/contrato/crédito).
    /// - É um filtro “quantitativo”: serve para shortlist, não decisão final.
    /// </summary>
    [HttpGet("filtrados")]
    public async Task<IActionResult> GetFiltrados(CancellationToken ct)
    {
        var data = await _service.ObterFiisFiltradosAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Retorna FIIs de “ancoragem” (perfil mais conservador) com base em critérios rígidos.
    ///
    /// O que traz:
    /// - Lista de FIIs que passam nas regras:
    ///   - Liquidez >= 1.500.000
    ///   - Valor de mercado >= 1 bilhão
    ///   - P/VP >= 0,98
    ///   - Vacância <= 10
    ///   - Exclui Shopping (por regra atual do serviço)
    /// - Ordenado por papel
    ///
    /// Bom:
    /// - Bom para montar o “miolo” da carteira (mais estabilidade).
    /// - Evita fundos pequenos e com vacância alta.
    ///
    /// Ruim / cuidados:
    /// - Pode deixar de fora ótimos fundos por regra fixa (ex: P/VP < 0,98).
    /// - Excluir shopping é uma escolha: pode reduzir diversificação.
    /// </summary>
    [HttpGet("ancoragem")]
    public async Task<IActionResult> GetAncoragem(CancellationToken ct)
    {
        var data = await _service.ObterFiisAncoragemAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Ranking dos melhores FIIs de TIJOLO (Score 0–10) usando dados do Fundamentus (versão v1).
    ///
    /// O que traz:
    /// - Apenas FIIs classificados como “Tijolo”
    /// - Score 0–10 (explicável) + risco (Conservador/Moderado/Arrojado) + motivos
    /// - Ordenação por Score e liquidez
    ///
    /// Bom:
    /// - Ideal para escolher “tijolo âncora” (renda mais previsível + estabilidade).
    /// - Vacância, liquidez e valor de mercado pesam bastante (protege o dividendo).
    ///
    /// Ruim / cuidados:
    /// - Classificação “tijolo” via segmento pode errar em casos híbridos.
    /// - Sem WAULT/contratos ainda: é um ranking v1 (bom para triagem, não é laudo).
    /// </summary>
    [HttpGet("ranking/tijolo")]
    public async Task<IActionResult> GetRankingTijolo(CancellationToken ct)
    {
        var data = await _service.ObterMelhoresTijoloAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Ranking dos melhores FIIs de PAPEL (Score 0–10) usando dados do Fundamentus (versão v1).
    ///
    /// O que traz:
    /// - Apenas FIIs classificados como “Papel”
    /// - Score 0–10 + risco + motivos
    /// - Foco em DY, liquidez, valor de mercado e P/VP (proxy de qualidade no v1)
    ///
    /// Bom:
    /// - Bom para compor a parte “renda” da carteira (DY geralmente maior).
    /// - Ajuda a filtrar papel “bom” versus papel com sinais de exagero.
    ///
    /// Ruim / cuidados:
    /// - Como ainda não temos LTV/inadimplência/concentração, o score é “proxy”.
    /// - Papel pode esconder risco de crédito que não aparece só em DY/PVP.
    /// </summary>
    [HttpGet("ranking/papel")]
    public async Task<IActionResult> GetRankingPapel(CancellationToken ct)
    {
        var data = await _service.ObterMelhoresPapelAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Ranking de FIIs classificados como "Risco Confiável".
    /// 
    /// 🔹 O que traz:
    /// - FIIs com score consistente
    /// - Boa liquidez
    /// - Risco considerado controlado pelas regras do VoxFundamentos
    ///
    /// 🔹 O que tem de bom:
    /// - Menor volatilidade
    /// - Maior previsibilidade de renda
    /// - Ideal para compor a base da carteira
    ///
    /// 🔹 O que tem de ruim:
    /// - Menor potencial de upside
    /// - Pode perder performance em ciclos muito positivos
    /// </summary>
    [HttpGet("ranking/risco-confiavel")]
    public async Task<IActionResult> GetRiscoConfiavel(
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        var data = await _service.ObterRiscoConfiavelAsync(top, ct);
        return Ok(data);
    }


    /// <summary>
    /// Retorna os FIIs classificados como "Risco Elevado" (ordenados por score e liquidez).
    /// </summary>
    [HttpGet("risco-elevado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRiscoElevado(
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        var result = await _service.ObterRiscoElevadoAsync(top, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gera uma carteira sugerida automaticamente (alocação por tipo) usando rankings e score do VoxFundamentos.
    ///
    /// Regra:
    /// - 60% Tijolo (Top Score)
    /// - 35% Papel (Top Score)
    /// - 5%  Risco Confiável (janela de risco controlado)
    ///
    /// Bom:
    /// - Monta uma carteira base rápida e consistente.
    /// - Evita concentração excessiva (teto por ativo).
    ///
    /// Ruim / cuidados:
    /// - É uma carteira “v1” (sem WAULT/contratos/LTV/inadimplência ainda).
    /// - Serve como sugestão inicial; não substitui análise de RG.
    /// </summary>
    [HttpGet("carteira/sugerida")]
    public async Task<IActionResult> GetCarteiraSugerida(CancellationToken ct)
    {
        var data = await _service.ObterCarteiraSugeridaAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Gera uma carteira parametrizada informando percentuais e quantidades por tipo.
    ///
    /// Exemplo:
    /// GET /api/fiis/carteira/parametrizada?pesoTijolo=60&pesoPapel=35&pesoRisco=5&qtdTijolo=6&qtdPapel=5&qtdRisco=2
    ///
    /// Bom:
    /// - Permite testar várias estratégias rapidamente.
    /// - Você controla quanto quer em cada tipo e quantos ativos.
    ///
    /// Ruim / cuidados:
    /// - Se pedir muitos ativos pode acabar pegando fundos mais fracos do ranking.
    /// - Ainda é v1 (sem métricas avançadas como WAULT/LTV/inadimplência).
    /// </summary>
    [HttpGet("carteira/parametrizada")]
    public async Task<IActionResult> GetCarteiraParametrizada(
        [FromQuery] decimal pesoTijolo,
        [FromQuery] decimal pesoPapel,
        [FromQuery] decimal pesoRisco,
        [FromQuery] int qtdTijolo,
        [FromQuery] int qtdPapel,
        [FromQuery] int qtdRisco,
        CancellationToken ct)
    {
        var req = new CarteiraParamRequestDto(
            PesoTijoloPercentual: pesoTijolo,
            PesoPapelPercentual: pesoPapel,
            PesoRiscoPercentual: pesoRisco,
            QtdTijolo: qtdTijolo,
            QtdPapel: qtdPapel,
            QtdRisco: qtdRisco
        );

        var data = await _service.ObterCarteiraParametrizadaAsync(req, ct);
        return Ok(data);
    }

    [HttpGet("carteira/percentual")]
    public async Task<IActionResult> GetCarteiraPorPercentual(
    [FromQuery] decimal pesoTijolo,
    [FromQuery] decimal pesoPapel,
    [FromQuery] decimal pesoRisco,
    [FromQuery] int totalFiis,
    CancellationToken ct)
    {
        var req = new CarteiraPercentualRequestDto(
            PesoTijoloPercentual: pesoTijolo,
            PesoPapelPercentual: pesoPapel,
            PesoRiscoPercentual: pesoRisco,
            TotalFiis: totalFiis
        );

        var data = await _service.ObterCarteiraPorPercentualETotalAsync(req, ct);
        return Ok(data);
    }

    [HttpGet("carteira/perfis")]
    public async Task<IActionResult> GetCarteiraPorPerfis(
    [FromQuery] decimal ancoragem,
    [FromQuery] decimal potencial,
    [FromQuery] decimal riscoControlado,
    [FromQuery] decimal riscoElevado,
    [FromQuery] int totalFiis,
    CancellationToken ct)
    {
        var req = new CarteiraPerfisRequestDto(
            AncoragemPercentual: ancoragem,
            PotencialPercentual: potencial,
            RiscoControladoPercentual: riscoControlado,
            RiscoElevadoPercentual: riscoElevado,
            TotalFiis: totalFiis
        );

        var data = await _service.ObterCarteiraPorPerfisAsync(req, ct);
        return Ok(data);
    }

}
