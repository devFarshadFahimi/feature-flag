namespace WebApi.Controllers;

[Authorize]
public class ApiTokensController(IMediator mediator) : ApiControllerBase(mediator)
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