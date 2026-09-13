using System.Linq.Dynamic.Core;
using System.Reflection;

namespace Assignment;

public static class Extensions
{
    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers.XRequestedWith == "XMLHttpRequest";
    }

    // Dynamic search powered by System.Linq.Dynamic.Core
    // Fully supports direct properties, nested navigation paths (e.g. "Staff.Account.AccountDetail.Username"), and "-- All --" search
    public static IQueryable<T> SearchBy<T>(this IQueryable<T> query, string? search, string? searchBy) where T : class
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.Trim();

        try
        {
            if (!string.IsNullOrWhiteSpace(searchBy))
            {
                // 1. Search specific field / path using System.Linq.Dynamic.Core
                var resolved = ResolvePropertyPath(typeof(T), searchBy);

                if (resolved.HasValue)
                {
                    var actualPath = resolved.Value.ResolvedPath;
                    var targetType = resolved.Value.PropertyType;
                    var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

                    if (underlying == typeof(string))
                    {
                        return query.Where($"{actualPath} != null && {actualPath}.Contains(@0)", search);
                    }
                    else if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short))
                    {
                        if (long.TryParse(search, out long num))
                        {
                            return query.Where($"{actualPath} == @0", Convert.ChangeType(num, underlying));
                        }
                        return query.Where("1 == 0");
                    }
                    else if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
                    {
                        if (decimal.TryParse(search, out decimal dec))
                        {
                            return query.Where($"{actualPath} == @0", Convert.ChangeType(dec, underlying));
                        }
                        return query.Where("1 == 0");
                    }
                    else if (underlying.IsEnum)
                    {
                        if (Enum.TryParse(underlying, search, true, out object? enumVal) && enumVal != null)
                        {
                            return query.Where($"{actualPath} == @0", enumVal);
                        }
                        return query.Where("1 == 0");
                    }
                }

                // If path could not be resolved, fall back to -- All -- search
                return ExecuteAllSearch(query, search);
            }
            else
            {
                // 2. "-- All --" search: collect string property paths and build dynamic OR predicate
                return ExecuteAllSearch(query, search);
            }
        }
        catch
        {
            // Fail-safe: if any dynamic expression parsing fails, return original query safely
            return query;
        }
    }

    // ------- private helpers -------

    private static IQueryable<T> ExecuteAllSearch<T>(IQueryable<T> query, string search) where T : class
    {
        var stringPaths = CollectStringPropertyPaths(typeof(T), "", new HashSet<Type> { typeof(T) }, 0);
        if (stringPaths.Count > 0)
        {
            var clauses = stringPaths.Select(p => $"({p} != null && {p}.Contains(@0))");
            var combinedPredicate = string.Join(" || ", clauses);
            return query.Where(combinedPredicate, search);
        }
        return query;
    }

    private static (string ResolvedPath, Type PropertyType)? ResolvePropertyPath(Type rootType, string path)
    {
        var directType = GetPropertyType(rootType, path);
        if (directType != null)
        {
            return (path, directType);
        }

        // Auto-resolve leaf property name if only leaf name was provided (e.g. "PokemonName" -> "BookingDetail.PokemonName")
        var allPaths = CollectAllPropertyPaths(rootType, "", new HashSet<Type> { rootType }, 0);
        var match = allPaths.FirstOrDefault(p =>
            string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase) ||
            p.Path.EndsWith("." + path, StringComparison.OrdinalIgnoreCase));

        if (match.Path != null && match.Type != null)
        {
            return (match.Path, match.Type);
        }

        return null;
    }

    private static Type? GetPropertyType(Type rootType, string path)
    {
        var current = rootType;
        foreach (var part in path.Split('.'))
        {
            var prop = current.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) return null;
            current = prop.PropertyType;
        }
        return current;
    }

    private static List<(string Path, Type Type)> CollectAllPropertyPaths(Type type, string prefix, HashSet<Type> visited, int depth)
    {
        var list = new List<(string Path, Type Type)>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            list.Add((propPath, prop.PropertyType));

            if (depth < 3 && prop.PropertyType.IsClass && prop.PropertyType.Namespace == "Assignment.Models" && !prop.PropertyType.IsGenericType)
            {
                if (!visited.Contains(prop.PropertyType))
                {
                    var newVisited = new HashSet<Type>(visited) { prop.PropertyType };
                    list.AddRange(CollectAllPropertyPaths(prop.PropertyType, propPath, newVisited, depth + 1));
                }
            }
        }
        return list;
    }

    private static List<string> CollectStringPropertyPaths(Type type, string prefix, HashSet<Type> visited, int depth)
    {
        var list = new List<string>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            if (prop.PropertyType == typeof(string))
            {
                list.Add(propPath);
            }
            else if (depth < 3 && prop.PropertyType.IsClass && prop.PropertyType.Namespace == "Assignment.Models" && !prop.PropertyType.IsGenericType)
            {
                if (!visited.Contains(prop.PropertyType))
                {
                    var newVisited = new HashSet<Type>(visited) { prop.PropertyType };
                    list.AddRange(CollectStringPropertyPaths(prop.PropertyType, propPath, newVisited, depth + 1));
                }
            }
        }
        return list;
    }
}
