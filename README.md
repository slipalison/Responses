# Responses

> A modern .NET 10 library implementing the Result Pattern with Railway-Oriented Programming and Flurl HTTP integration.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Version](https://img.shields.io/badge/version-2.0.0-green)
![License](https://img.shields.io/badge/license-MIT-blue)
[![CI](https://github.com/slipalison/Responses/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/slipalison/Responses/actions/workflows/dotnetcore.yml)
[![codecov](https://codecov.io/gh/slipalison/Responses/branch/master/graph/badge.svg)](https://codecov.io/gh/slipalison/Responses)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=slipalison_Responses&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=slipalison_Responses)
---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core API](#core-api)
  - [Result Types](#result-types)
  - [Factory Methods](#factory-methods)
  - [Accessing Values](#accessing-values)
- [Railway-Oriented Programming](#railway-oriented-programming)
  - [Map](#map)
  - [Bind](#bind)
  - [Tap](#tap)
  - [Ensure](#ensure)
- [Pattern Matching](#pattern-matching)
  - [Match](#match)
  - [Else](#else)
- [LINQ Query Syntax](#linq-query-syntax)
- [Error Model](#error-model)
  - [ErrorType](#errortype)
  - [Metadata](#metadata)
  - [Factory Methods](#error-factory-methods)
- [Multi-Error Support](#multi-error-support)
- [JSON Serialization](#json-serialization)
  - [Using DTOs](#using-dtos)
- [HTTP Extensions (Flurl)](#http-extensions-flurl)
  - [Basic Usage](#basic-http-usage)
  - [HTTP Status Code Mapping](#http-status-code-mapping)
  - [RFC 9457 ProblemDetails](#rfc-9457-problemdetails)
- [Async Support](#async-support)
- [Null Safety](#null-safety)
- [Benchmarking](#benchmarking)
- [Development](#development)

---

## Overview

Responses provides:

- **Immutable `readonly struct`** types for zero-allocation hot paths
- **Railway-Oriented Programming** with Map, Bind, Tap, and Ensure
- **Pattern Matching** with Match and Else
- **LINQ Query Syntax** via SelectMany
- **Typed Errors** with ErrorType enum and metadata support
- **Multi-Error Collections** for validation scenarios
- **HTTP Integration** via Flurl with ProblemDetails parsing
- **JSON Serialization** via System.Text.Json with DTO pattern

---

## Installation

```bash
dotnet add package Responses --version 2.0.0
dotnet add package Responses.Http --version 2.0.0
```

**Requirements:** .NET 10.0+

---

## Quick Start

```csharp
using Responses;

// Success
var result = Result.Ok();
var resultWithValue = Result.Ok(42);

// Failure
var fail = Result.Fail("ERR001", "Something went wrong");
var validationFail = Result.Fail<int>(Error.Validation("VAL", "Invalid input"));

// Check outcome
if (result.IsSuccess) { /* ... */ }
if (result.IsFailed) { /* ... */ }
```

---

## Core API

### Result Types

| Type | Description |
|------|-------------|
| `Result` | Void success/failure |
| `Result<T>` | Success with value |
| `Result<TValue, TError>` | Success with typed error |

All types are immutable `readonly struct` with `[StructLayout(LayoutKind.Auto)]` for optimal memory layout.

### Factory Methods

```csharp
// Basic
Result.Ok()                                    // → Result
Result.Ok(42)                                  // → Result<int>
Result.Fail("ERR001", "message")              // → Result
Result.Fail<int>("ERR001", "message")         // → Result<int>

// Conditional
Result.OkIf(age >= 18, age, "ERR", "Must be 18+")
Result.FailIf(string.IsNullOrEmpty(email), email, "ERR", "Required")

// With Error object
var error = Error.Validation("VAL", "Invalid email");
Result.Fail(error)
Result.Fail<int>(error)
```

### Accessing Values

```csharp
var result = Result.Ok(42);

// Value — throws InvalidOperationException when failed
int value = result.Value;

// ValueOrDefault — safe access, returns default(T) when failed
int safeValue = result.ValueOrDefault;

// Error — throws InvalidOperationException when success
Error error = result.Error;

// Errors — collection (safe, never throws)
ErrorCollection allErrors = result.Errors;
```

---

## Railway-Oriented Programming

### Map

Transforms the value on success, propagates error on failure:

```csharp
var result = Result.Ok("hello")
    .Map(s => s.ToUpper())
    .Map(s => s.Length);
// → Result<int> with value 5

var failed = Result.Fail<int>("ERR", "msg")
    .Map(x => x * 2);
// → Still failed, func was NOT called
```

### Bind

Chains fallible operations — stops at first failure:

```csharp
Result<int> ParseAndValidate(string input) =>
    int.TryParse(input, out var n) && n > 0
        ? Result.Ok(n)
        : Result.Fail<int>("PARSE", "Invalid number");

var result = Result.Ok("42")
    .Bind(ParseAndValidate)
    .Bind(x => x > 10 ? Result.Ok(x) : Result.Fail<int>("RANGE", "Too small"));
```

### Tap

Executes a side-effect without modifying the Result:

```csharp
var result = Result.Ok(42)
    .Tap(x => Console.WriteLine($"Value: {x}"))
    .Tap(x => _logger.LogInfo($"Processed: {x}"));
// → Result<int> unchanged
```

### Ensure

Validates a condition, returns failure if false:

```csharp
var result = Result.Ok("user@example.com")
    .Ensure(e => e.Contains("@"), Error.Validation("FMT", "Invalid email"))
    .Ensure(e => e.Length >= 5, Error.Validation("LEN", "Too short"));
```

---

## Pattern Matching

### Match

```csharp
var message = result.Match(
    v => $"Success: {v} items processed",
    e => $"Error {e.Code}: {e.Message}"
);

// Void Match
result.Match(
    v => Console.WriteLine($"Got: {v}"),
    e => Console.WriteLine($"Failed: {e.Code}")
);
```

### Else

```csharp
int value = result.Else(0);                    // Fallback value
int computed = result.Else(e => e.Code == "NOT_FOUND" ? -1 : 0);  // Fallback function
```

---

## LINQ Query Syntax

```csharp
var sum = from x in Result.Ok(5)
          from y in Result.Ok(10)
          from z in Result.Ok(3)
          select x + y + z;
// → Result<int> with value 18

// Short-circuits on first failure
var fail = from x in Result.Fail<int>("ERR", "msg")
           from y in Result.Ok(10)    // NOT executed
           select x + y;
// → Failed Result
```

---

## Error Model

### ErrorType

Values match their corresponding HTTP status codes where applicable (RFC 9110, RFC 6585, RFC 4918, RFC 7725):

```csharp
public enum ErrorType
{
    Unknown = 0,

    // 4xx client errors
    Validation = 400,
    Unauthorized = 401,
    PaymentRequired = 402,
    Forbidden = 403,
    NotFound = 404,
    Timeout = 408,
    Conflict = 409,
    Gone = 410,
    UnprocessableEntity = 422,
    Locked = 423,
    FailedDependency = 424,
    UpgradeRequired = 426,
    PreconditionRequired = 428,
    TooManyRequests = 429,
    UnavailableForLegal = 451,
    ClientClosed = 499,

    // 5xx server errors
    ServerError = 500,
    BadGateway = 502,
    ServiceUnavailable = 503,
    GatewayTimeout = 504,

    // Non-HTTP
    InternalError = 998,
    Cancelled = 999,
}
```

### Metadata

```csharp
var metadata = new Dictionary<string, string>
{
    { "field", "email" },
    { "value", "invalid-input" }
};

var error = new Error("VAL001", "Invalid email", ErrorType.Validation, metadata);
string field = error.Metadata["field"];  // "email"
```

### Error Factory Methods

```csharp
Error.Validation("VAL", "Invalid input")
Error.NotFound("NF", "Resource not found")
Error.Conflict("CON", "Duplicate resource")
Error.Unauthorized("UA", "Authentication required")
Error.Forbidden("FB", "Access denied")
Error.Server("SVR", "Internal server error")
Error.Timeout("TO", "Request timed out")
Error.Cancelled("CAN", "Operation cancelled")
Error.TooManyRequests("RATE", "Rate limit exceeded")
Error.UnprocessableEntity("UNP", "Semantically invalid")
Error.BadGateway("BGW", "Invalid upstream response")
Error.ServiceUnavailable("SU", "Temporarily unavailable")
Error.GatewayTimeout("GTO", "Upstream did not respond")
```

Code and message are required: `null` throws `ArgumentNullException`, empty throws `ArgumentException`.

---

## Multi-Error Support

```csharp
var errors = new IError[]
{
    Error.Validation("NAME", "Name is required"),
    Error.Validation("EMAIL", "Invalid email format"),
    Error.Validation("AGE", "Must be 18 or older")
};

var result = Result.Fail<int>(errors);

// Access all errors
foreach (var error in result.Errors)
    Console.WriteLine($"[{error.Type}] {error.Code}: {error.Message}");

// Or via LINQ
var validationErrors = result.Errors
    .Where(e => e.Type == ErrorType.Validation)
    .ToList();
```

---

## JSON Serialization

### Using DTOs

Responses uses the DTO pattern for reliable System.Text.Json serialization:

```csharp
using Responses.Serialization;

// Serialize
var result = Result.Ok(42);
var dto = ResultDto<int>.FromResult(result);
string json = JsonSerializer.Serialize(dto);

// Deserialize
var dtoBack = JsonSerializer.Deserialize<ResultDto<int>>(json);
var resultBack = dtoBack.ToResult();
```

`ResultJsonContext.DefaultOptions` exposes read-only, source-generated (zero-reflection) options for the DTOs. Serialize and deserialize through the DTO — it has a stable shape and round-trips:

```csharp
var dto = ResultDto<int>.FromResult(Result.Ok(42));
string json = JsonSerializer.Serialize(dto, ResultJsonContext.DefaultOptions);

var back = JsonSerializer.Deserialize<ResultDto<int>>(json, ResultJsonContext.DefaultOptions).ToResult();
```

Result structs are intentionally **not** registered for direct serialization: a Result cannot be reconstructed from its own serialized form (the constructors are internal), so serializing one directly through `DefaultOptions` throws `NotSupportedException` rather than emitting JSON that will not round-trip. Always go through the DTO.

**JSON format:**

```json
{
    "isSuccessful": true,
    "value": 42,
    "errors": []
}
```

```json
{
    "isSuccessful": false,
    "value": null,
    "errors": [
        {
            "code": "VAL001",
            "message": "Invalid email",
            "type": "Validation",
            "layer": "Responses",
            "applicationName": "MyApp",
            "metadata": { "field": "email" }
        }
    ]
}
```

---

## HTTP Extensions (Flurl)

### Basic Usage

```csharp
using Responses.Http;
using Flurl.Http;

// GET with typed result
var result = await "https://api.example.com/users/1"
    .GetAsync()
    .ReceiveResult<User>();

if (result.IsSuccess)
    Console.WriteLine(result.Value.Name);

// POST
var created = await "https://api.example.com/users"
    .PostJsonAsync(newUser)
    .ReceiveResult<User>();
```

### HTTP Status Code Mapping

`StatusCodeMapping.ToErrorType` maps every error status directly to its `ErrorType` (values match the status codes): 400, 401, 403, 404, 408, 409, 410, 422, 423, 424, 426, 428, 429, 451, 499, 500, 502, 503, 504.

| Status Code | ErrorType |
|-------------|-----------|
| Mapped codes above | Matching `ErrorType` (e.g. 404 → `NotFound`) |
| Other 4xx | `Validation` |
| Other 5xx | `ServerError` |
| 1xx/2xx/3xx | `Unknown` (not errors) |

### Error contents on failure

For non-2xx responses the resulting `Error` carries:

- **Code**: the problem-details `title` when present, otherwise the numeric status code (`"404"`)
- **Message**: the problem-details `detail` when present, otherwise the raw body; an empty body falls back to `"HTTP 404 NotFound"`
- **Type**: mapped from the status code (table above)
- **Metadata**: `problemType`, `detail`, and `instance` when problem details are present

Cancellation surfaces as `Error.Cancelled("HttpCancelled", ...)` — including for the `IFlurlResponse` overloads — and network faults as `Error.Server("HttpNetworkError", ...)`. No expected HTTP outcome throws.

### RFC 9457 ProblemDetails

When the server returns `application/problem+json`:

```json
{
    "type": "https://example.com/errors/not-found",
    "title": "User Not Found",
    "status": 404,
    "detail": "The requested user does not exist",
    "instance": "/api/users/999"
}
```

Responses automatically parses it:

```csharp
var result = await "https://api.example.com/users/999"
    .GetAsync()
    .ReceiveResult<User>();

// Error.Code    = "User Not Found"
// Error.Message = "The requested user does not exist"
// Error.Type    = ErrorType.NotFound
// Error.Metadata["problemType"] = "https://example.com/errors/not-found"
```

### Graceful Error Handling

```csharp
// Serialization error — doesn't throw
var result = await "https://api.example.com/broken"
    .GetAsync()
    .ReceiveResult<User>();

// Returns Result with error containing raw body
if (result.IsFailed)
    Console.WriteLine(result.Errors[0].Message); // Raw response body
```

---

## Async Support

All composition methods have async variants:

```csharp
var result = await Result.Ok("user@example.com")
    .MapAsync(async email => await ValidateEmailAsync(email))
    .BindAsync(async id => await FetchUserAsync(id))
    .TapAsync(async user => await LogAsync(user));
```

---

## Null Safety

Every composition method — including the async variants and LINQ operators, on all three result types — validates its delegate arguments:

```csharp
result.Map(null!);             // ArgumentNullException
result.Bind(null!);            // ArgumentNullException
result.Tap(null!);             // ArgumentNullException
result.Ensure(null!, e);       // ArgumentNullException
result.Match(null!, f);        // ArgumentNullException
await result.MapAsync<int>(null!);  // ArgumentNullException (from the returned Task)
```

`Error` construction requires non-empty code and message: `null` throws `ArgumentNullException`, empty throws `ArgumentException`.

---

## Benchmarking

Run the BenchmarkDotNet suite to verify zero-allocation claims:

```bash
dotnet run -c Release --project benchmarks/Responses.Benchmarks
```

Benchmarks cover:
- `Result.Ok()` / `Result.Ok(42)` — allocation verification
- `Map` / `Bind` — success and failure paths
- `ValueOrDefault` — success and failure paths
- Error creation — with and without metadata

---

## Development

```bash
dotnet tool restore                                   # restores dotnet-sonarscanner
dotnet build Responses.sln -c Release                 # warnings are errors; SonarAnalyzer.CSharp runs at build time
dotnet test test/Responses.Tests/Responses.Tests.csproj -c Release
```

Standards enforced by the build and CI:

- **Zero warnings** — `TreatWarningsAsErrors` with SonarAnalyzer.CSharp in every project
- **Coverage floor** — 80% line coverage repo-wide (source-generated code excluded), enforced by coverlet in CI
- **SonarQube** — CI runs a SonarCloud analysis when `SONAR_TOKEN` is configured; fork PRs build and test normally without it
- **XML docs** — every public member documents itself (`GenerateDocumentationFile`)
- **Zero allocations on success paths** — see `AllocationTests` and the benchmark suite

Development rules live in [`.claude/rules/`](.claude/rules/) (`csharp.md` for the hot-path C# standard, `public-api.md` for the definition of done). Commits follow Conventional Commits.

---

## Version History

| Version | Changes |
|---------|---------|
| 2.0.0 | .NET 10, readonly struct, railway-oriented programming, STJ, Flurl 4.x, multi-error, ProblemDetails |
| 1.2.0 | Legacy Newtonsoft.Json-based Result pattern with Flurl 3.x extensions |

---

## License

MIT
