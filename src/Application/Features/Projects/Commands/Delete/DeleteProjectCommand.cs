using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Projects.Commands.Delete;

public record DeleteProjectCommand(Guid Id) : ICommandRequest;

internal class DeleteProjectCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DeleteProjectCommand>
{
    public override async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.Id);

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}