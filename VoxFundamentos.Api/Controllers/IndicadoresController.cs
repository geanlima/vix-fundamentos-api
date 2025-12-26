using Microsoft.AspNetCore.Mvc;
using VoxFundamentos.Application.Interfaces;

namespace VoxFundamentos.Api.Controllers;

[ApiController]
[Route("api/indicadores")]
public class IndicadoresController : ControllerBase
{
    private readonly IIndicadorEconomicoService _service;

    public IndicadoresController(IIndicadorEconomicoService service)
    {
        _service = service;
    }

    [HttpGet("selic")]
    public async Task<IActionResult> GetSelic(CancellationToken ct)
    {
        var selic = await _service.ObterSelicAtualAsync(ct);

        return Ok(new
        {
            taxa = selic,
            unidade = "% a.a.",
            fonte = "Banco Central do Brasil (SGS)",
            serie = 11
        });
    }
}
