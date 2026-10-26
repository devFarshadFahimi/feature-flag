# Coding conventions and source-based examples

Read [AGENTS.md](../AGENTS.md) and [PRINCIPLES.md](../docs/PRINCIPLES.md). Match nearby feature namespaces, file-scoped namespaces, records, constructor injection, and four-space indentation. Prefer descriptive names and short cohesive handlers. Requests and handlers normally share one use-case file; do not split them mechanically.

## Small command

This follows `src/Application/Features/Projects/Commands/Create/CreateProjectCommand.cs`. It uses the existing database abstraction and framework handler; no new project service or repository is needed.

```csharp
public record CreateProjectCommand(
    string Name,
    string Description,
    string DefaultStickiness = "default") : ICommandRequest<Guid>;

internal class CreateProjectCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CreateProjectCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(
        CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description, request.DefaultStickiness);
        _ = dbContext.Projects.Add(project);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(project.Id);
    }
}
```

Use `CommandRequestHandler<TCommand>` and `Task<Result>` for status-only commands, `CommandRequestHandler<TCommand,T>` and `Task<Result<T>>` for typed results. `Ok(...)` is inherited; do not invent `IsSuccess`/`Value` members from another result library. The inspected framework calls them `Succeded` and `Body`.

## Query projection

Adapted from `GetAllProjectsQuery.cs`. New read-only queries may add `AsNoTracking()` when tracking is unnecessary; verify Mapster/EF translation for nested collections.

```csharp
public record GetAllProjectsQuery : IQueryRequest<List<ProjectResponse>>;

internal class GetAllProjectsQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllProjectsQuery, List<ProjectResponse>>
{
    public override Task<List<ProjectResponse>> Handle(
        GetAllProjectsQuery request, CancellationToken cancellationToken) =>
        dbContext.Projects.AsNoTracking()
            .ProjectToType<ProjectResponse>()
            .ToListAsync(cancellationToken);
}
```

Use DTOs rather than serializing aggregates. Update `Common/Mappers` and `MappingInitializer` only when needed. Persistence string conversions for enums do not imply string enum inputs over HTTP.

## Controller boundary and Persian errors

Adapted from the existing Features update action, with the required Persian message. Keep route/body ID checks in the HTTP layer and pass cancellation through.

```csharp
[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdateFeatureCommand command, CancellationToken cancellationToken)
{
    if (id != command.Id)
    {
        return BadRequest(new { Message = "شناسه مسیر با شناسه درخواست مطابقت ندارد." });
    }

    return await SendAsync(command, cancellationToken);
}
```

For a missing entity, use a supported exception/result API with explicit Persian text. For example, after the existing EF lookup:

```csharp
if (feature is null)
{
    throw new InvalidEntityStateException("ویژگی موردنظر یافت نشد.");
}
```

When returning framework errors explicitly, pass `ProblemDetails` so title/detail are controlled and overload/default-message ambiguity is avoided:

```csharp
return Result<Guid>.Error(new Microsoft.AspNetCore.Mvc.ProblemDetails
{
    Title = "درخواست نامعتبر",
    Detail = "نام پروژه الزامی است.",
    Status = 400
});
```

This is an error-construction example, not an instruction to duplicate validation in every handler. Use an existing validator mechanism only after checking the installed package contract and generated discovery. Do not mix FluentValidation and BusinessMaker's custom validator API by assumption. Every explicit validation message must also be Persian.

## Domain changes and persistence

Load the state used by aggregate invariants, invoke the domain operation, then persist once. For example, a member addition needs `Include(p => p.Members)` before `project.AddMember(...)`; the current `FindAsync`-only handler is not a complete relationship-loading example. Database uniqueness constraints remain necessary under concurrent writes.

Do not add a new service for an ordinary EF lookup, projection, or aggregate transition. Search both upstream repositories before a genuinely new integration service is introduced. Keep IO behind existing Application interfaces; verify service lifetimes against scoped DbContext use.
