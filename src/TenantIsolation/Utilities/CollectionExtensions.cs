#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Utilities;

/// <summary>
/// Extension methods for collections including lists, dictionaries, and enumerables
/// Provides safe and convenient collection manipulation patterns
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Check if collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Check if collection has items
    /// </summary>
    public static bool HasItems<T>(this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Get safe item at index, returns default if out of bounds
    /// Prevents IndexOutOfRangeException when accessing items
    /// </summary>
    public static T? SafeGetAt<T>(this IEnumerable<T> collection, int index) where T : class
    {
        if (collection == null || index < 0)
            return null;

        return collection.Skip(index).FirstOrDefault();
    }

    /// <summary>
    /// Add item to list if not already present
    /// </summary>
    public static void AddIfNotExists<T>(this IList<T> list, T item)
    {
        if (list != null && !list.Contains(item))
            list.Add(item);
    }

    /// <summary>
    /// Add multiple items to list
    /// </summary>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        if (collection != null && items != null)
        {
            foreach (var item in items)
                collection.Add(item);
        }
    }

    /// <summary>
    /// Remove items matching predicate
    /// </summary>
    public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
    {
        if (list == null)
            return 0;

        var itemsToRemove = list.Where(predicate).ToList();
        foreach (var item in itemsToRemove)
            list.Remove(item);

        return itemsToRemove.Count;
    }

    /// <summary>
    /// Get distinct items based on key selector
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector)
    {
        if (collection == null)
            return Enumerable.Empty<T>();

        var seen = new HashSet<TKey>();
        return collection.Where(item => seen.Add(keySelector(item)));
    }

    /// <summary>
    /// Chunk collection into smaller collections of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> collection, int chunkSize)
    {
        if (collection == null || chunkSize <= 0)
            yield break;

        var chunk = new List<T>(chunkSize);
        foreach (var item in collection)
        {
            chunk.Add(item);
            if (chunk.Count == chunkSize)
            {
                yield return chunk;
                chunk = new List<T>(chunkSize);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>
    /// Safely get value from dictionary
    /// </summary>
    public static TValue? SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
    {
        if (dictionary == null || key == null)
            return null;

        return dictionary.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Get value from dictionary or return default
    /// </summary>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        if (dictionary == null || key == null)
            return defaultValue;

        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Convert collection to dictionary with duplicate key handling
    /// When duplicates occur, the last value wins
    /// </summary>
    public static Dictionary<TKey, TValue> ToSafeDictionary<T, TKey, TValue>(
        this IEnumerable<T> collection,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        if (collection == null)
            return result;

        foreach (var item in collection)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Get the item with maximum value
    /// </summary>
    public static T? MaxBy<T, TValue>(this IEnumerable<T> collection, Func<T, TValue> selector) where TValue : IComparable<TValue>
    {
        if (collection.IsNullOrEmpty())
            return default;

        T? maxItem = default;
        var maxValue = default(TValue);

        foreach (var item in collection)
        {
            var value = selector(item);
            if (maxItem == null || value.CompareTo(maxValue) > 0)
            {
                maxItem = item;
                maxValue = value;
            }
        }

        return maxItem;
    }

    /// <summary>
    /// Get the item with minimum value
    /// </summary>
    public static T? MinBy<T, TValue>(this IEnumerable<T> collection, Func<T, TValue> selector) where TValue : IComparable<TValue>
    {
        if (collection.IsNullOrEmpty())
            return default;

        T? minItem = default;
        var minValue = default(TValue);

        foreach (var item in collection)
        {
            var value = selector(item);
            if (minItem == null || value.CompareTo(minValue) < 0)
            {
                minItem = item;
                minValue = value;
            }
        }

        return minItem;
    }

    /// <summary>
    /// Partition collection into two based on predicate
    /// Returns matching and non-matching items
    /// </summary>
    public static (IEnumerable<T> Matching, IEnumerable<T> NotMatching) Partition<T>(
        this IEnumerable<T> collection,
        Func<T, bool> predicate)
    {
        if (collection == null)
            return (Enumerable.Empty<T>(), Enumerable.Empty<T>());

        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in collection)
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }

    /// <summary>
    /// Get intersection of two collections
    /// </summary>
    public static IEnumerable<T> GetIntersection<T>(this IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        if (collection1.IsNullOrEmpty() || collection2.IsNullOrEmpty())
            return Enumerable.Empty<T>();

        var set2 = new HashSet<T>(collection2);
        return collection1.Where(set2.Contains).Distinct();
    }

    /// <summary>
    /// Get difference of two collections (items in first but not in second)
    /// </summary>
    public static IEnumerable<T> GetDifference<T>(this IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        if (collection1.IsNullOrEmpty())
            return Enumerable.Empty<T>();

        if (collection2.IsNullOrEmpty())
            return collection1;

        var set2 = new HashSet<T>(collection2);
        return collection1.Where(item => !set2.Contains(item)).Distinct();
    }

    /// <summary>
    /// Flatten nested collections
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
    {
        if (collection == null)
            return Enumerable.Empty<T>();

        return collection.SelectMany(x => x ?? Enumerable.Empty<T>());
    }

    /// <summary>
    /// Create a batched/paged version of collection for pagination
    /// </summary>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> collection, int pageNumber, int pageSize)
    {
        if (collection.IsNullOrEmpty() || pageNumber < 1 || pageSize < 1)
            return Enumerable.Empty<T>();

        var skip = (pageNumber - 1) * pageSize;
        return collection.Skip(skip).Take(pageSize);
    }
}
