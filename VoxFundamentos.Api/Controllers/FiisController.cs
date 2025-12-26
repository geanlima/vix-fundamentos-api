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
}
