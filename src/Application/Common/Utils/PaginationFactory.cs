using System.Linq.Expressions;

namespace Application.Common.Utils;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MapToEntityAttribute : Attribute
{
    public string EntityPropertyPath { get; }

    public MapToEntityAttribute(string entityPropertyPath)
    {
        EntityPropertyPath = entityPropertyPath;
    }
}

public class FilterCriteria
{
    public string? Column { get; set; }

    public string? Query { get; set; }
    public FilterCondition Condition { get; set; }
}

public enum FilterCondition
{
    Equals,
    Contains,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}

public class PaginationParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortColumn { get; set; }
    public bool SortDescending { get; set; } = false;
    public List<FilterCriteria> Filters { get; set; } = [];
}

public class PaginationResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int TotalRecords { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}

public static class PaginationFactory
{
    public async static Task<PaginationResult<TModel>> GetPaginatedDataAsync<TModel, TEntity>(
        this IQueryable<TEntity> query,
        PaginationParams paginationParams)
        where TModel : new()
    {
        // Apply filtering
        if (paginationParams.Filters.Count != 0)
        {
            query = ApplyFilters<TModel, TEntity>(query, paginationParams.Filters);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(paginationParams.SortColumn))
        {
            query = ApplySort(query, paginationParams.SortColumn, paginationParams.SortDescending);
        }

        // Get total records count before pagination
        var totalRecords = await query.CountAsync();

        // Apply pagination
        var data = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ProjectToDto<TEntity, TModel>()
            .ToListAsync();

        // Return paginated result
        return new PaginationResult<TModel>
        {
            Data = data,
            TotalRecords = totalRecords,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize
        };
    }

    public static IQueryable<TModel> ProjectToDto<TEntity, TModel>(this IQueryable<TEntity> query)
        where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var bindings = typeof(TModel).GetProperties().Select(dtoProperty =>
        {
            var entityPropertyPath = GetEntityPropertyPath<TModel>(dtoProperty.Name);
            var entityProperty = GetNestedPropertyExpression(parameter, entityPropertyPath);
            return Expression.Bind(dtoProperty, entityProperty);
        });

        var body = Expression.MemberInit(Expression.New(typeof(TModel)), bindings);
        var selector = Expression.Lambda<Func<TEntity, TModel>>(body, parameter);
        return query.Select(selector);
    }

    private static IQueryable<T> ApplySort<T>(IQueryable<T> query, string sortColumn, bool sortDescending)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, sortColumn);
        var lambda = Expression.Lambda(property, parameter);
        var methodName = sortDescending ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(typeof(Queryable), methodName, [query.ElementType, property.Type],
            query.Expression, Expression.Quote(lambda));
        return query.Provider.CreateQuery<T>(resultExpression);
    }
    private static IQueryable<T> ApplyFilters<TModel, T>(IQueryable<T> query, List<FilterCriteria> filters)
    {
        foreach (var filter in filters)
        {
            var entityPropertyPath = GetEntityPropertyPath<TModel>(filter.Column);
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = GetNestedPropertyExpression(parameter, entityPropertyPath);
            var constant = ConvertFilterValue(filter.Query, property.Type);
            var filterExpression = CreateFilterExpression(property, constant, filter.Condition);
            var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static string GetEntityPropertyPath<T>(string dtoPropertyName)
    {
        var dtoProperty = typeof(T).GetProperty(dtoPropertyName);
        if (dtoProperty != null)
        {
            var attribute = dtoProperty.GetCustomAttributes(typeof(MapToEntityAttribute), false)
                .Cast<MapToEntityAttribute>()
                .FirstOrDefault();
            if (attribute != null)
            {
                return attribute.EntityPropertyPath;
            }
        }

        return dtoPropertyName; // Default to the same name if no attribute is found
    }

    private static MemberExpression GetNestedPropertyExpression(Expression parameter, string propertyPath)
    {
        var properties = propertyPath.Split('.');
        Expression property = parameter;
        Type propertyType = parameter.Type;

        foreach (var propName in properties)
        {
            var propertyInfo = propertyType.GetProperty(propName);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"Property '{propName}' not found on type '{propertyType.FullName}'");
            }

            property = Expression.Property(property, propertyInfo);
            propertyType = propertyInfo.PropertyType;
        }

        return (MemberExpression)property;
    }

    private static Expression ConvertFilterValue(string query, Type propertyType)
    {
        object value;
        if (propertyType == typeof(string))
        {
            value = query;
        }
        else if (propertyType == typeof(int))
        {
            value = int.Parse(query);
        }
        else if (propertyType == typeof(long))
        {
            value = long.Parse(query);
        }
        else if (propertyType == typeof(bool))
        {
            value = bool.Parse(query);
        }
        else if (propertyType == typeof(DateTime))
        {
            value = DateTime.Parse(query);
        }
        else if (propertyType.IsEnum)
        {
            value = Enum.Parse(propertyType, query);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported filter type: {propertyType}");
        }

        return Expression.Constant(value, propertyType);
    }

    private static Expression CreateFilterExpression(MemberExpression property, Expression constant,
        FilterCondition condition)
    {
        Expression filterExpression;
        if (property.Type == typeof(string))
        {
            filterExpression = condition switch
            {
                FilterCondition.Contains => Expression.Call(
                    property,
                    typeof(string).GetMethod("Contains", [typeof(string)])!, constant),
                FilterCondition.StartsWith => Expression.Call(
                    property,
                    typeof(string).GetMethod("StartsWith", [typeof(string)])!, constant),
                FilterCondition.EndsWith => Expression.Call(
                    property,
                    typeof(string).GetMethod("EndsWith", [typeof(string)])!, constant),
                FilterCondition.Equals => Expression.Equal(property, constant),
                _ => throw new InvalidOperationException($"Unsupported condition for type string: {condition}")
            };
        }
        else if (property.Type == typeof(int) || property.Type == typeof(long) || property.Type == typeof(DateTime))
        {
            filterExpression = condition switch
            {
                FilterCondition.Equals => Expression.Equal(property, constant),
                FilterCondition.GreaterThan => Expression.GreaterThan(property, constant),
                FilterCondition.LessThan => Expression.LessThan(property, constant),
                FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
                FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
                _ => throw new InvalidOperationException($"Unsupported condition for type {property.Type}: {condition}")
            };
        }
        else if (property.Type == typeof(bool) || property.Type.IsEnum)
        {
            if (condition != FilterCondition.Equals)
            {
                throw new InvalidOperationException($"Unsupported condition for type bool: {condition}");
            }

            filterExpression = Expression.Equal(property, constant);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported filter type: {property.Type.Name}");
        }

        return filterExpression;
    }
}