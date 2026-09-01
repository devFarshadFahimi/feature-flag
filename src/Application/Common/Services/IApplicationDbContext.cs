namespace Application.Common.Services;

public interface IApplicationDbContext
{
    Task SaveChangeAsync(CancellationToken cancellationToken = default);
}
