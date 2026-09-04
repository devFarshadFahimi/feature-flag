using Application.Features.FeatureEnvironments.Commands.AddFeatureStrategy;
using Application.Features.FeatureEnvironments.Commands.DisableFeatureEnvironment;
using Application.Features.FeatureEnvironments.Commands.EnableFeatureEnvironment;
using Application.Features.FeatureEnvironments.Commands.RemoveFeatureStrategy;
using Application.Features.FeatureEnvironments.Queries.GetFeatureEnvironmentById;

namespace WebApi.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/features/{featureId:guid}/environments")]
public class FeatureEnvironmentsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet("{environmentId:guid}")]
    public async Task<IActionResult> GetById(Guid featureId, Guid environmentId, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetFeatureEnvironmentByIdQuery(featureId, environmentId), cancellationToken);
    }

    [HttpPost("{environmentId:guid}/enable")]
    public async Task<IActionResult> Enable(Guid featureId, Guid environmentId, CancellationToken cancellationToken)
    {
        return await SendAsync(new EnableFeatureEnvironmentCommand(featureId, environmentId), cancellationToken);
    }

    [HttpPost("{environmentId:guid}/disable")]
    public async Task<IActionResult> Disable(Guid featureId, Guid environmentId, CancellationToken cancellationToken)
    {
        return await SendAsync(new DisableFeatureEnvironmentCommand(featureId, environmentId), cancellationToken);
    }

    [HttpPost("{environmentId:guid}/strategies")]
    public async Task<IActionResult> AddStrategy(Guid featureId, Guid environmentId, [FromBody] AddFeatureStrategyCommand command, CancellationToken cancellationToken)
    {
        return featureId != command.FeatureId || environmentId != command.EnvironmentId
            ? BadRequest(new { Message = "Feature or environment id mismatch" })
            : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{environmentId:guid}/strategies/{strategyId:guid}")]
    public async Task<IActionResult> RemoveStrategy(Guid featureId, Guid environmentId, Guid strategyId, CancellationToken cancellationToken)
    {
        return await SendAsync(new RemoveFeatureStrategyCommand(featureId, environmentId, strategyId), cancellationToken);
    }
}