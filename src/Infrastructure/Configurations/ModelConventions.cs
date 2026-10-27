using Domain.Aggregates.FeatureAggregate;

namespace Infrastructure.Configurations;

internal static class ModelConventions
{
    /// <summary>Applies shared revision and mutable JSON tracking conventions after entity mappings.</summary>
    internal static void ApplyConfigurationConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.GetProperty(nameof(Feature.Revision)) != null))
        {
            modelBuilder
                .Entity(entity.ClrType)
                .Property<long>(nameof(Feature.Revision))
                .IsConcurrencyToken();
        }

        foreach (var property in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.GetValueConverter() != null && p.ClrType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ClrType)))
        {
            property.SetValueComparer((Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer)Activator.CreateInstance(typeof(JsonValueComparer<>).MakeGenericType(property.ClrType))!);
        }
    }
}