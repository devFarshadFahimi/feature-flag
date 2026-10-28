using Application.Common.Services;
using Microsoft.EntityFrameworkCore;
using Application.Features.Projects.Commands.AddProjectMember;
using Application.Features.Projects.Commands.Create;
using Application.Features.Projects.Commands.Delete;
using Application.Features.Projects.Commands.RemoveProjectMember;
using Application.Features.Projects.Commands.SetProjectFeatureLimit;
using Application.Features.Projects.Commands.Update;
using Application.Features.Projects.Queries.GetAllProjects;
using Application.Features.Projects.Queries.GetProjectById;
using Application.Features.Projects.Queries.GetProjectsSelective;

namespace WebApi.Controllers;

[Authorize]
public class ProjectsController(IMediator mediator, IApplicationDbContext db) : ApiControllerBase(mediator)
{
    [HttpGet("{id:guid}/environments/{environmentId:guid}/approvals")]
    public async Task<IActionResult> GetApprovals(Guid id, Guid environmentId, CancellationToken cancellationToken)
    {
        var policy = await db.ProjectEnvironmentPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProjectId == id && p.EnvironmentId == environmentId, cancellationToken);

        return Ok(policy ?? new Domain.Aggregates.ProjectAggregate.ProjectEnvironmentPolicy { ProjectId = id, EnvironmentId = environmentId });
    }

    [HttpPut("{id:guid}/environments/{environmentId:guid}/approvals")]
    public Task<IActionResult> Approvals(Guid id, Guid environmentId, [FromBody] Application.Features.ChangeRequests.SetApprovalPolicyCommand command, CancellationToken cancellationToken) => id != command.ProjectId || environmentId != command.EnvironmentId ? Task.FromResult<IActionResult>(BadRequest(new { Message = "شناسه پروژه یا محیط یکسان نیست." })) : SendAsync(command, cancellationToken);

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

    [HttpGet("selective")]
    public async Task<IActionResult> Selective(CancellationToken cancellationToken)
    {
        return await SendAsync(new GetProjectsSelectiveQuery(), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new
        {
            Message = "شناسه مسیر و درخواست یکسان نیست."
        }) : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeleteProjectCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddProjectMemberCommand command, CancellationToken cancellationToken)
    {
        return id != command.ProjectId ? BadRequest(new
        {
            Message = "شناسه پروژه مسیر و درخواست یکسان نیست."
        }) : await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await SendAsync(new RemoveProjectMemberCommand(id, userId), cancellationToken);
    }

    [HttpPut("{id:guid}/feature-limit")]
    public async Task<IActionResult> SetFeatureLimit(Guid id, [FromBody] SetProjectFeatureLimitCommand command, CancellationToken cancellationToken)
    {
        return id != command.ProjectId ? BadRequest(new
        {
            Message = "شناسه پروژه مسیر و درخواست یکسان نیست."
        }) : await SendAsync(command, cancellationToken);
    }
}
