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
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Check if collection has items
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    public static bool HasItems<T>(this IEnumerable<T>? collection)
    {
        return collection != null && collection.Any();
    }

    /// <summary>
    /// Get item at index safely, returns default if out of bounds
    /// Prevents IndexOutOfRangeException when accessing items
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The collection to access</param>
    /// <param name="index">The zero-based index of the item to retrieve</param>
    /// <returns>The item at the specified index, or <see langword="null"/> if out of bounds or collection is null</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    public static T? SafeGetAt<T>(this IEnumerable<T> collection, int index) where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (index < 0)
            return null;

        // Use ElementAtOrDefault which is more efficient than Skip(index).FirstOrDefault()
        return collection.ElementAtOrDefault(index);
    }

    /// <summary>
    /// Add item to list if not already present
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="list">The list to modify</param>
    /// <param name="item">The item to add</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/></exception>
    public static void AddIfNotExists<T>(this IList<T> list, T item)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (!list.Contains(item))
            list.Add(item);
    }

    /// <summary>
    /// Add multiple items to list
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The collection to modify</param>
    /// <param name="items">The items to add</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="items"/> is <see langword="null"/></exception>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? items)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (items != null)
        {
            foreach (var item in items)
                collection.Add(item);
        }
    }

    /// <summary>
    /// Remove items matching predicate
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="list">The list to modify</param>
    /// <param name="predicate">The predicate to match items for removal</param>
    /// <returns>The number of items removed</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> or <paramref name="predicate"/> is <see langword="null"/></exception>
    public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(predicate);

        var itemsToRemove = list.Where(predicate).ToList();
        foreach (var item in itemsToRemove)
            list.Remove(item);

        return itemsToRemove.Count;
    }

    /// <summary>
    /// Get distinct items based on key selector
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <typeparam name="TKey">The type of key to distinguish by</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="keySelector">Function to extract key from each element</param>
    /// <returns>Collection containing only distinct elements based on key</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> collection,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(keySelector);

        var seen = new HashSet<TKey>();
        return collection.Where(item => seen.Add(keySelector(item)));
    }

    /// <summary>
    /// Chunk collection into smaller collections of specified size
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The source collection to chunk</param>
    /// <param name="chunkSize">The maximum size of each chunk</param>
    /// <returns>Collection of chunks, each containing up to <paramref name="chunkSize"/> items</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chunkSize"/> is less than 1</exception>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(
        this IEnumerable<T> collection,
        int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive");

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
    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
    /// <param name="dictionary">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The value associated with the key, or <see langword="null"/> if not found or dictionary is null</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/></exception>
    public static TValue? SafeGet<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key) where TValue : class
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(key);

        return dictionary.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Get value from dictionary or return default
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
    /// <param name="dictionary">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">The default value to return if key is not found</param>
    /// <returns>The value associated with the key, or <paramref name="defaultValue"/> if not found</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/></exception>
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(key);

        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Convert collection to dictionary with duplicate key handling
    /// When duplicates occur, the last value wins
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <typeparam name="TKey">The type of keys in the resulting dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the resulting dictionary</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="keySelector">Function to extract key from each element</param>
    /// <param name="valueSelector">Function to extract value from each element</param>
    /// <returns>A dictionary with keys mapped to values from the collection</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/>, <paramref name="keySelector"/>, or <paramref name="valueSelector"/> is <see langword="null"/></exception>
    public static Dictionary<TKey, TValue> ToSafeDictionary<T, TKey, TValue>(
        this IEnumerable<T> collection,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);

        var result = new Dictionary<TKey, TValue>();
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
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <typeparam name="TValue">The type of comparable value</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="selector">Function to extract comparable value from each element</param>
    /// <returns>The item with the maximum value, or <see langword="null"/> if collection is empty</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static T? MaxBy<T, TValue>(
        this IEnumerable<T> collection,
        Func<T, TValue> selector) where TValue : IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

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
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <typeparam name="TValue">The type of comparable value</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="selector">Function to extract comparable value from each element</param>
    /// <returns>The item with the minimum value, or <see langword="null"/> if collection is empty</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static T? MinBy<T, TValue>(
        this IEnumerable<T> collection,
        Func<T, TValue> selector) where TValue : IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

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
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="predicate">The predicate to partition by</param>
    /// <returns>A tuple containing matching and non-matching items</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="predicate"/> is <see langword="null"/></exception>
    public static (IEnumerable<T> Matching, IEnumerable<T> NotMatching) Partition<T>(
        this IEnumerable<T> collection,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

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
    /// <typeparam name="T">The type of elements in the collections</typeparam>
    /// <param name="collection1">The first collection</param>
    /// <param name="collection2">The second collection</param>
    /// <returns>Collection containing items present in both collections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection1"/> or <paramref name="collection2"/> is <see langword="null"/></exception>
    public static IEnumerable<T> GetIntersection<T>(
        this IEnumerable<T> collection1,
        IEnumerable<T> collection2)
    {
        ArgumentNullException.ThrowIfNull(collection1);
        ArgumentNullException.ThrowIfNull(collection2);

        var set2 = new HashSet<T>(collection2);
        return collection1.Where(set2.Contains).Distinct();
    }

    /// <summary>
    /// Get difference of two collections (items in first but not in second)
    /// </summary>
    /// <typeparam name="T">The type of elements in the collections</typeparam>
    /// <param name="collection1">The first collection</param>
    /// <param name="collection2">The second collection</param>
    /// <returns>Collection containing items present in first but not in second</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection1"/> or <paramref name="collection2"/> is <see langword="null"/></exception>
    public static IEnumerable<T> GetDifference<T>(
        this IEnumerable<T> collection1,
        IEnumerable<T> collection2)
    {
        ArgumentNullException.ThrowIfNull(collection1);
        ArgumentNullException.ThrowIfNull(collection2);

        var set2 = new HashSet<T>(collection2);
        return collection1.Where(item => !set2.Contains(item)).Distinct();
    }

    /// <summary>
    /// Flatten nested collections
    /// </summary>
    /// <typeparam name="T">The type of elements in the collections</typeparam>
    /// <param name="collection">The collection of collections to flatten</param>
    /// <returns>Flattened collection containing all elements</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return collection.SelectMany(x => x ?? Enumerable.Empty<T>());
    }

    /// <summary>
    /// Create a batched/paged version of collection for pagination
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="pageNumber">The one-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>Collection containing items for the specified page</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1</exception>
    public static IEnumerable<T> Page<T>(
        this IEnumerable<T> collection,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be positive");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive");

        var skip = (pageNumber - 1) * pageSize;
        return collection.Skip(skip).Take(pageSize);
    }
}