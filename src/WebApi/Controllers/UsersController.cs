using Application.Features.Users.Commands.ActivateUser;
using Application.Features.Users.Commands.CreateUser;
using Application.Features.Users.Commands.DeactivateUser;
using Application.Features.Users.Commands.PromoteUser;
using Application.Features.Users.Commands.UpdateUser;
using Application.Features.Users.Queries.GetAllUsers;
using Application.Features.Users.Queries.GetUserById;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
public class UsersController(IMediator mediator) : BusinessApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllUsersQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetUserByIdQuery(id), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "Id mismatch" });
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new ActivateUserCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeactivateUserCommand(id), cancellationToken);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> Promote(Guid id, [FromBody] PromoteUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "Id mismatch" });
        return await SendAsync(command, cancellationToken);
    }
}