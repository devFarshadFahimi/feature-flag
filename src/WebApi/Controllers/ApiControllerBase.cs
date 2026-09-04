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

[ApiVersion(2.0, Deprecated = true)]
public class YTApiController : BasicController
{
    [HttpGet]
    public IActionResult GetUsers()
    {
        throw new NotImplementedException();
    }
}

[ApiVersion(2.2)]
public class TestxxxxController : BasicController
{
    [HttpGet]
    public IActionResult GetUsers()
    {
        throw new NotImplementedException();
    }
}

[ApiVersion(2.1, status: "workinprogress")]
public class ProductsController : BasicController
{
    [HttpGet]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetProductById()
    {
        return Ok();
    }
}