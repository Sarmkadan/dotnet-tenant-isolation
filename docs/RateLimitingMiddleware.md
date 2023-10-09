# RateLimitingMiddleware

A middleware component that enforces rate limiting on HTTP requests based on a configurable token bucket algorithm. It tracks request consumption per minute and calculates reset times to prevent abuse while allowing graceful degradation under load.

## API

### `public RateLimitingMiddleware`

Initializes a new instance of the `RateLimitingMiddleware` with default settings (100 requests per minute, 60-second retry window, and enabled state).

### `public async Task InvokeAsync(HttpContext context)`

Invokes the middleware to process the HTTP request.

- **Parameters**
  - `context`: The `HttpContext` containing the incoming request and response.
- **Return Value**
  - A `Task` representing the asynchronous operation.
- **Throws**
  - `ArgumentNullException`: If `context` is `null`.

### `public DateTime ResetTime`

Gets the timestamp when the current rate limit window resets.

- **Remarks**
  - Calculated as the next whole minute boundary from the first request in the current window.

### `public RateLimitBucket PublicRateLimitBucket { get; }`

Gets the current rate limit bucket tracking token consumption.

- **Remarks**
  - The bucket is initialized on first use and reset at each window boundary.

### `public bool TryConsumeToken()`

Attempts to consume a token from the rate limit bucket.

- **Return Value**
  - `true` if a token was consumed; otherwise, `false`.
- **Remarks**
  - Returns `false` if the bucket is empty or disabled.

### `public int RequestsPerMinute { get; set; }`

Gets or sets the maximum number of requests allowed per minute.

- **Default**
  - `100`
- **Remarks**
  - Changing this value resets the current bucket.

### `public int RetryAfterSeconds { get; set; }`

Gets or sets the number of seconds to wait before retrying after a rate limit is exceeded.

- **Default**
  - `60`
- **Remarks**
  - Affects the `Retry-After` header in responses.

### `public bool Enabled { get; set; }`

Gets or sets a value indicating whether rate limiting is active.

- **Default**
  - `true`
- **Remarks**
  - When `false`, all requests bypass rate limiting.

### `public static IApplicationBuilder UseRateLimiting(IApplicationBuilder app)`

Adds the `RateLimitingMiddleware` to the request pipeline.

- **Parameters**
  - `app`: The `IApplicationBuilder` instance.
- **Return Value**
  - The `IApplicationBuilder` for method chaining.
- **Throws**
  - `ArgumentNullException`: If `app` is `null`.

## Usage

### Basic Setup
