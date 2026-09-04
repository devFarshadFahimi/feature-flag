using Application.Features.ChangeRequests.Queries.GetChangeRequestById;

namespace Application.Common.Mappers;

public static class ChangeRequestMappingConfig
{
    public static void Configure()
    {
        // ChangeRequestItem → ChangeRequestItemResponse
        _ = TypeAdapterConfig<ChangeRequestItem, ChangeRequestItemResponse>
            .NewConfig();

        // ChangeRequest → ChangeRequestResponse
        _ = TypeAdapterConfig<ChangeRequest, ChangeRequestResponse>
            .NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Items, src => src.Items.Adapt<List<ChangeRequestItemResponse>>());
    }
}
