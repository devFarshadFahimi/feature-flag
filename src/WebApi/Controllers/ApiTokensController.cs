using Application.Features.ApiTokens.Queries.GetAllApiTokens;
using Application.Features.ApiTokens.Queries.GetApiTokenById;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[Route("api/tokens")]
public class ApiTokensController(IMediator mediator) : BusinessApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? environmentId, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllApiTokensQuery(environmentId), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetApiTokenByIdQuery(id), cancellationToken);
    }
}