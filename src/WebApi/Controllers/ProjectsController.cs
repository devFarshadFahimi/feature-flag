using Application.Features.Projects.Commands.AddProjectMember;
using Application.Features.Projects.Commands.Create;
using Application.Features.Projects.Commands.Delete;
using Application.Features.Projects.Commands.RemoveProjectMember;
using Application.Features.Projects.Commands.SetProjectFeatureLimit;
using Application.Features.Projects.Commands.Update;
using Application.Features.Projects.Queries.GetAllProjects;
using Application.Features.Projects.Queries.GetProjectById;

namespace WebApi.Controllers;

[Authorize]
public class ProjectsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllProjectsQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetProjectByIdQuery(id), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new { Message = "Id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeleteProjectCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddProjectMemberCommand command, CancellationToken cancellationToken)
    {
        return id != command.ProjectId ? BadRequest(new { Message = "Project id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await SendAsync(new RemoveProjectMemberCommand(id, userId), cancellationToken);
    }

    [HttpPut("{id:guid}/feature-limit")]
    public async Task<IActionResult> SetFeatureLimit(Guid id, [FromBody] SetProjectFeatureLimitCommand command, CancellationToken cancellationToken)
    {
        return id != command.ProjectId ? BadRequest(new { Message = "Project id mismatch" }) : await SendAsync(command, cancellationToken);
    }
}