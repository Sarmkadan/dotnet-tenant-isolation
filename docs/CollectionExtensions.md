# CollectionExtensions

Provides a comprehensive set of static extension methods for collections, dictionaries, and enumerable sequences. These utilities simplify common operations such as null-safe access, conditional additions, filtering, partitioning, pagination, and set-based comparisons, reducing boilerplate code across the `dotnet-tenant-isolation` project.

## API

### `IsNullOrEmpty<T>(this IEnumerable<T>? source)`
Returns `true` if the source sequence is `null` or contains no elements.
- **Parameters:** `source` — the sequence to check.
- **Returns:** `bool` — `true` when `source` is `null` or empty; otherwise `false`.
- **Throws:** Never throws.

### `HasItems<T>(this IEnumerable<T>? source)`
Returns `true` if the source sequence is not `null` and contains at least one element.
- **Parameters:** `source` — the sequence to check.
- **Returns:** `bool` — `true` when `source` is non-null and non-empty; otherwise `false`.
- **Throws:** Never throws.

### `SafeGetAt<T>(this IList<T>? source, int index)`
Retrieves the element at the specified index, or returns the default value for `T` if the list is `null` or the index is out of range.
- **Parameters:** `source` — the list; `index` — zero-based position.
- **Returns:** `T?` — the element at `index`, or `default(T)`.
- **Throws:** Never throws.

### `AddIfNotExists<T>(this ICollection<T> source, T item)`
Adds `item` to the collection only if it is not already present, using the collection’s default equality comparison.
- **Parameters:** `source` — the target collection; `item` — the element to conditionally add.
- **Returns:** `void`
- **Throws:** `ArgumentNullException` if `source` is `null`.

### `AddRange<T>(this ICollection<T> source, IEnumerable<T> items)`
Adds all elements from `items` to the collection.
- **Parameters:** `source` — the target collection; `items` — the elements to add.
- **Returns:** `void`
- **Throws:** `ArgumentNullException` if `source` or `items` is `null`.

### `RemoveWhere<T>(this ICollection<T> source, Func<T, bool> predicate)`
Removes all elements that satisfy the predicate. Returns the count of removed items.
- **Parameters:** `source` — the collection to modify; `predicate` — the condition for removal.
- **Returns:** `int` — number of elements removed.
- **Throws:** `ArgumentNullException` if `source` or `predicate` is `null`.

### `DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
Returns distinct elements based on a key extracted by `keySelector`. The first occurrence of each key is retained.
- **Parameters:** `source` — the input sequence; `keySelector` — function to extract the comparison key.
- **Returns:** `IEnumerable<T>` — a sequence of distinct elements.
- **Throws:** `ArgumentNullException` if `source` or `keySelector` is `null`.

### `Chunk<T>(this IEnumerable<T> source, int size)`
Splits the source sequence into chunks of the specified `size`. The final chunk may be smaller.
- **Parameters:** `source` — the sequence to split; `size` — maximum number of elements per chunk.
- **Returns:** `IEnumerable<IEnumerable<T>>` — a sequence of chunks.
- **Throws:** `ArgumentNullException` if `source` is `null`; `ArgumentOutOfRangeException` if `size` is less than 1.

### `SafeGet<TKey, TValue>(this IDictionary<TKey, TValue>? dictionary, TKey key)`
Attempts to retrieve the value for `key`. Returns the default value for `TValue` if the dictionary is `null` or the key is not found.
- **Parameters:** `dictionary` — the dictionary; `key` — the lookup key.
- **Returns:** `TValue?` — the associated value, or `default(TValue)`.
- **Throws:** Never throws.

### `GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)`
Returns the value for `key` if present; otherwise returns `defaultValue`.
- **Parameters:** `dictionary` — the dictionary; `key` — the lookup key; `defaultValue` — fallback value.
- **Returns:** `TValue` — the stored value or `defaultValue`.
- **Throws:** `ArgumentNullException` if `dictionary` is `null`.

### `ToSafeDictionary<T, TKey, TValue>(this IEnumerable<T> source, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)`
Builds a dictionary from the source sequence. If duplicate keys are encountered, the first occurrence wins and subsequent duplicates are silently ignored.
- **Parameters:** `source` — the input sequence; `keySelector` — key extraction function; `valueSelector` — value extraction function.
- **Returns:** `Dictionary<TKey, TValue>` — a dictionary with unique keys.
- **Throws:** `ArgumentNullException` if `source`, `keySelector`, or `valueSelector` is `null`.

### `MaxBy<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)`
Returns the element with the maximum value as determined by `selector`, using the default comparer for `TValue`. Returns `default(T)` if the sequence is empty.
- **Parameters:** `source` — the sequence; `selector` — value extraction function.
- **Returns:** `T?` — the maximal element, or `default(T)`.
- **Throws:** `ArgumentNullException` if `source` or `selector` is `null`.

### `MinBy<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)`
Returns the element with the minimum value as determined by `selector`, using the default comparer for `TValue`. Returns `default(T)` if the sequence is empty.
- **Parameters:** `source` — the sequence; `selector` — value extraction function.
- **Returns:** `T?` — the minimal element, or `default(T)`.
- **Throws:** `ArgumentNullException` if `source` or `selector` is `null`.

### `Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)`
Splits the sequence into two groups: elements that satisfy the predicate (`Matching`) and those that do not (`NotMatching`).
- **Parameters:** `source` — the sequence; `predicate` — partitioning condition.
- **Returns:** `(IEnumerable<T> Matching, IEnumerable<T> NotMatching)` — a tuple of the two partitions.
- **Throws:** `ArgumentNullException` if `source` or `predicate` is `null`.

### `GetIntersection<T>(this IEnumerable<T> first, IEnumerable<T> second)`
Returns the set intersection of two sequences — elements that appear in both, using the default equality comparer.
- **Parameters:** `first` — first sequence; `second` — second sequence.
- **Returns:** `IEnumerable<T>` — distinct elements present in both sequences.
- **Throws:** `ArgumentNullException` if `first` or `second` is `null`.

### `GetDifference<T>(this IEnumerable<T> first, IEnumerable<T> second)`
Returns the set difference — elements that appear in `first` but not in `second`, using the default equality comparer.
- **Parameters:** `first` — the source sequence; `second` — the sequence to subtract.
- **Returns:** `IEnumerable<T>` — distinct elements unique to `first`.
- **Throws:** `ArgumentNullException` if `first` or `second` is `null`.

### `Flatten<T>(this IEnumerable<IEnumerable<T>> source)`
Flattens a sequence of sequences into a single, concatenated sequence.
- **Parameters:** `source` — the nested sequences.
- **Returns:** `IEnumerable<T>` — all elements from all inner sequences in order.
- **Throws:** `ArgumentNullException` if `source` is `null`.

### `Page<T>(this IEnumerable<T> source, int pageIndex, int pageSize)`
Returns a single page of results from the sequence. `pageIndex` is zero-based.
- **Parameters:** `source` — the sequence; `pageIndex` — zero-based page number; `pageSize` — maximum items per page.
- **Returns:** `IEnumerable<T>` — the elements belonging to the requested page.
- **Throws:** `ArgumentNullException` if `source` is `null`; `ArgumentOutOfRangeException` if `pageIndex` is negative or `pageSize` is less than 1.

## Usage

### Example 1: Safe dictionary building and pagination
```csharp
var tenants = new[] { "Alpha", "Beta", "Alpha", "Gamma", "Delta", "Epsilon" };

// Build a dictionary ignoring duplicate keys (first occurrence kept)
var tenantDict = tenants.ToSafeDictionary(
    keySelector: t => t,
    valueSelector: t => t.Length
);

// Paginate the distinct tenant names
var distinctTenants = tenantDict.Keys.DistinctBy(t => t).ToList();
var page = distinctTenants.Page(pageIndex: 1, pageSize: 2);

// page contains the second page of distinct tenant names
```

### Example 2: Partitioning and conditional removal
```csharp
var scores = new List<int> { 45, 72, 30, 88, 19, 60 };

// Partition into passing (>= 50) and failing
var (passing, failing) = scores.Partition(s => s >= 50);

// Remove all failing scores from the original list
scores.RemoveWhere(s => s < 50);

// scores now contains only passing values
```

## Notes

- **Null handling:** Methods prefixed with `Safe` (`SafeGetAt`, `SafeGet`) gracefully accept `null` inputs and return default values rather than throwing. All other methods throw `ArgumentNullException` when required arguments are `null`.
- **Empty sequences:** `MaxBy` and `MinBy` return `default(T)` for empty sequences, which may be `null` for reference types. Callers should check for empty sequences beforehand if `null` is ambiguous.
- **Deferred execution:** Methods returning `IEnumerable<T>` (e.g., `DistinctBy`, `Chunk`, `Partition`, `GetIntersection`, `GetDifference`, `Flatten`, `Page`) use deferred execution. Results are evaluated only when enumerated. Modifications to the underlying collection during enumeration may yield unexpected results.
- **Thread safety:** These methods are static utility functions with no shared mutable state. They are safe to call concurrently provided the source collections themselves are not modified during execution. `AddIfNotExists`, `AddRange`, and `RemoveWhere` mutate the target collection and are not thread-safe without external synchronization.
- **Duplicate handling in `ToSafeDictionary`:** When duplicate keys are encountered, the first key-value pair is retained and subsequent duplicates are silently discarded. This differs from `ToDictionary`, which throws on duplicates.
- **`Chunk` and `Page` sizing:** Both require a positive size/pageSize. `Chunk` returns all chunks covering the entire sequence; `Page` returns only the requested page, which may be empty if `pageIndex` exceeds the available pages.
