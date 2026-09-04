using Application.Features.Projects.Queries.GetProjectById;

namespace Application.Common.Mappers;

public static class ProjectMappingConfig
{
    public static void Configure()
    {
        // ProjectMember → ProjectMemberResponse
        _ = TypeAdapterConfig<ProjectMember, ProjectMemberResponse>
            .NewConfig()
            .Map(dest => dest.Role, src => src.Role.ToString());

        // Project → ProjectResponse
        _ = TypeAdapterConfig<Project, ProjectResponse>
            .NewConfig();
    }
}
