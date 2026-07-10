# Repository

A generic asynchronous repository abstraction for data access operations against an underlying data store. It provides common CRUD and query patterns while abstracting storage-specific details, enabling unit testing and loose coupling between domain logic and persistence concerns.

## API

### `public virtual async Task<TEntity?> GetByIdAsync(TKey id)`

Retrieves a single entity by its identifier asynchronously.
- **Parameters**: `id` – The identifier of the entity to retrieve.
- **Return value**: The entity if found; otherwise `null`.
- **Exceptions**: Throws if the identifier is invalid or the underlying query fails.

### `public virtual async Task<List<TEntity>> GetAllAsync()`

Retrieves all entities asynchronously.
- **Return value**: A list of all entities.
- **Exceptions**: Throws if the underlying query fails.

### `public virtual async Task<(List<TEntity> items, int total)> GetPagedAsync(int pageNumber, int pageSize)`

Retrieves a page of entities along with the total count asynchronously.
- **Parameters**:
  - `pageNumber` – The zero-based page index.
  - `pageSize` – The maximum number of items per page.
- **Return value**: A tuple containing the page items and the total entity count.
- **Exceptions**: Throws if `pageNumber` or `pageSize` are negative, or if the underlying query fails.

### `public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)`

Finds all entities matching the specified predicate asynchronously.
- **Parameters**: `predicate` – A function to test each entity for a condition.
- **Return value**: A list of entities that satisfy the predicate.
- **Exceptions**: Throws if `predicate` is `null` or the underlying query fails.

### `public virtual async Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate)`

Finds the first entity matching the specified predicate asynchronously.
- **Parameters**: `predicate` – A function to test each entity for a condition.
- **Return value**: The first matching entity if found; otherwise `null`.
- **Exceptions**: Throws if `predicate` is `null` or the underlying query fails.

### `public virtual async Task<TEntity> AddAsync(TEntity entity)`

Adds a new entity asynchronously.
- **Parameters**: `entity` – The entity to add.
- **Return value**: The added entity, typically with generated identifiers populated.
- **Exceptions**: Throws if `entity` is `null` or the underlying operation fails.

### `public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)`

Adds a collection of entities asynchronously.
- **Parameters**: `entities` – The entities to add.
- **Return value**: The added entities.
- **Exceptions**: Throws if `entities` is `null`, contains `null` elements, or the underlying operation fails.

### `public virtual async Task<TEntity> UpdateAsync(TEntity entity)`

Updates an existing entity asynchronously.
- **Parameters**: `entity` – The entity to update.
- **Return value**: The updated entity.
- **Exceptions**: Throws if `entity` is `null` or the underlying operation fails.

### `public virtual async Task<bool> DeleteAsync(TEntity entity)`

Deletes a single entity asynchronously.
- **Parameters**: `entity` – The entity to delete.
- **Return value**: `true` if the entity was found and deleted; otherwise `false`.
- **Exceptions**: Throws if `entity` is `null` or the underlying operation fails.

### `public virtual async Task<bool> DeleteAsync(TKey id)`

Deletes an entity by its identifier asynchronously.
- **Parameters**: `id` – The identifier of the entity to delete.
- **Return value**: `true` if the entity was found and deleted; otherwise `false`.
- **Exceptions**: Throws if the identifier is invalid or the underlying operation fails.

### `public virtual async Task<int> DeleteRangeAsync(IEnumerable<TKey> ids)`

Deletes multiple entities by their identifiers asynchronously.
- **Parameters**: `ids` – The identifiers of the entities to delete.
- **Return value**: The number of entities deleted.
- **Exceptions**: Throws if `ids` is `null`, contains invalid identifiers, or the underlying operation fails.

### `public virtual async Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities)`

Deletes a collection of entities asynchronously.
- **Parameters**: `entities` – The entities to delete.
- **Return value**: The number of entities deleted.
- **Exceptions**: Throws if `entities` is `null`, contains `null` elements, or the underlying operation fails.

### `public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)`

Determines whether any entity matches the specified predicate asynchronously.
- **Parameters**: `predicate` – A function to test each entity for a condition.
- **Return value**: `true` if at least one entity matches; otherwise `false`.
- **Exceptions**: Throws if `predicate` is `null` or the underlying query fails.

### `public virtual async Task<int> CountAsync()`

Counts all entities asynchronously.
- **Return value**: The total number of entities.
- **Exceptions**: Throws if the underlying query fails.

### `public virtual IQueryable<TEntity> AsQueryable()`

Returns an `IQueryable<TEntity>` for the underlying entity set, enabling further composition of queries.
- **Return value**: A queryable collection of entities.
- **Exceptions**: None.

### `public virtual async Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities)`

Performs a bulk update of multiple entities asynchronously.
- **Parameters**: `entities` – The entities containing updated values.
- **Return value**: The number of entities updated.
- **Exceptions**: Throws if `entities` is `null`, contains `null` elements, or the underlying operation fails.

### `public virtual async Task<int> BulkDeleteAsync(IEnumerable<TEntity> entities)`

Performs a bulk deletion of multiple entities asynchronously.
- **Parameters**: `entities` – The entities to delete.
- **Return value**: The number of entities deleted.
- **Exceptions**: Throws if `entities` is `null`, contains `null` elements, or the underlying operation fails.

## Usage

### Example 1: Basic CRUD Operations
