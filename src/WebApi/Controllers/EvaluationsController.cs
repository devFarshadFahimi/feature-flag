using Application.Features.Evaluations;

namespace WebApi.Controllers;

[Authorize]
[RequestSizeLimit(65536)]
public sealed class EvaluationsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    public Task<IActionResult> Evaluate([FromBody] EvaluateFlagsCommand command, CancellationToken cancellationToken) => SendAsync(command with { Preview = false }, cancellationToken);

    [HttpPost("preview")]
    public Task<IActionResult> Preview([FromBody] EvaluateFlagsCommand command, CancellationToken cancellationToken) => SendAsync(command with { Preview = true }, cancellationToken);
}