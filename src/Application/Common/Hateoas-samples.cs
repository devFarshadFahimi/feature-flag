using BusinessMakerFramework.Infrastructure.Common.HateoasSetup.Abstractions;
using BusinessMakerFramework.Infrastructure.Common.HateoasSetup.Models;
using BusinessMakerFramework.SourceGenerator.Shared.Contracts;

namespace Application.Common;

// 1. The DTO strictly implements IIdentifiable
public class UserDto : IIdentifiable<long>
{
    public long Id { get; set; } // Compiler enforces this!
    public string Name { get; set; }
}

// 2. The Query explicitly returns a HATEOAS Resource
public class GetUserQuery : IResourceQueryRequest<UserDto>
{
    public int Id { get; set; }
}

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Resource<UserDto>>
{
    public async Task<Resource<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = UserData.Users.FirstOrDefault(p => p.Id.Equals(request.Id));
        return new Resource<UserDto>(user);
    }
}

// 3. Pagination Query
public class GetUsersQuery : IRequest<PagedResource<UserDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResource<UserDto>>
{
    public async Task<PagedResource<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = UserData.Users.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        // DEVELOPER WORK: Just wrap the collection and provide pagination metadata.
        // The Pipeline Behavior will automatically inject First, Prev, Next, and Last links.
        return new PagedResource<UserDto>(users, request.Page, request.PageSize, totalCount: UserData.Users.Count);
    }
}


public static class UserData
{
    public static List<UserDto> Users => Enumerable.Range(1, 100).Select(p => new UserDto()
    {
        Id = p,
        Name = "John " + p
    }).ToList();
}

//// Register the link generator
//builder.Services.AddScoped<IHateoasLinkGenerator, AspNetCoreHateoasLinkGenerator>();
//builder.Services.AddHttpContextAccessor(); // Required for the link generator

//// Register the MediatR Pipeline Behavior
//// Because of 'where TResponse : IResource', it will ONLY run for HATEOAS responses.
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HateoasPipelineBehavior<,>));