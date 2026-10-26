using Asp.Versioning;

namespace WebApi.Controllers;

[ApiVersion(1.0)]
[ApiController]
//[ApiVersion(1.0, Deprecated = true)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase(IMediator mediator) : BusinessApiControllerBase(mediator)
{
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BasicController : ControllerBase
{
}