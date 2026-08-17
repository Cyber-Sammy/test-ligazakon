namespace UserService.Application.Common.Extensions;

public static class CollectionExtensions
{
    public static bool IsNullOrEmpty<T>(
        this IReadOnlyCollection<T>? collection)
    {
        return collection is null || collection.Count == 0;
    }

    public static bool IsNotNullOrEmpty<T>(
        this IReadOnlyCollection<T>? collection)
    {
        return collection is { Count: > 0 };
    }
}
