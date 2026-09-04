using Application.Features.Environments.Commands.CreateApiToken;
using Application.Features.Environments.Commands.CreateEnvironment;
using Application.Features.Environments.Commands.DeleteEnvironment;
using Application.Features.Environments.Commands.DisableEnvironment;
using Application.Features.Environments.Commands.EnableEnvironment;
using Application.Features.Environments.Commands.RevokeApiToken;
using Application.Features.Environments.Commands.UpdateEnvironment;
using Application.Features.Environments.Queries.GetAllEnvironments;
using Application.Features.Environments.Queries.GetEnvironmentById;

namespace WebApi.Controllers;

[Authorize]
public class EnvironmentsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnvironmentCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllEnvironmentsQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetEnvironmentByIdQuery(id), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEnvironmentCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new { Message = "Id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeleteEnvironmentCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new EnableEnvironmentCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DisableEnvironmentCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/tokens")]
    public async Task<IActionResult> CreateToken(Guid id, [FromBody] CreateApiTokenCommand command, CancellationToken cancellationToken)
    {
        return id != command.EnvironmentId
            ? BadRequest(new { Message = "Environment id mismatch" })
            : await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/tokens/{tokenId:guid}/revoke")]
    public async Task<IActionResult> RevokeToken(Guid id, Guid tokenId, CancellationToken cancellationToken)
    {
        return await SendAsync(new RevokeApiTokenCommand(id, tokenId), cancellationToken);
    }
}