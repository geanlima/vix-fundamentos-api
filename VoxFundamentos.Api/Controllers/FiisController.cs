using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var data = await _service.ObterFiisAsync(ct);
        return Ok(data);
    }

    [HttpGet("{papel}")]
    public async Task<IActionResult> GetByPapel(string papel, CancellationToken ct)
    {
        var fii = await _service.ObterPorPapelAsync(papel, ct);

        if (fii is null)
            return NotFound(new { message = $"FII '{papel}' não encontrado." });

        return Ok(fii);
    }

    [HttpGet("filtrados")]
    public async Task<IActionResult> GetFiltrados(CancellationToken ct)
    {
        var data = await _service.ObterFiisFiltradosAsync(ct);
        return Ok(data);
    }

    [HttpGet("ancoragem")]
    public async Task<IActionResult> GetAncoragem(CancellationToken ct)
    {
        var data = await _service.ObterFiisAncoragemAsync(ct);
        return Ok(data);
    }
}
