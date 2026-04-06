# Responses

> A modern .NET 10 library implementing the Result Pattern with Railway-Oriented Programming and Flurl HTTP integration.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Version](https://img.shields.io/badge/version-2.0.0-green)
![License](https://img.shields.io/badge/license-MIT-blue)

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
IError[] allErrors = result.Errors;
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

```csharp
public enum ErrorType
{
    Unknown = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    ServerError = 6,
    Timeout = 7,
    Cancelled = 8,
    InternalError = 9,
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
```

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

| Status Code | ErrorType |
|-------------|-----------|
| 400 | Validation |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | NotFound |
| 409 | Conflict |
| 5xx | ServerError |

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

// Error.Message = "The requested user does not exist"
// Error.Type = ErrorType.NotFound
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

All methods throw `ArgumentNullException` for null arguments:

```csharp
result.Map(null!);       // ArgumentNullException
result.Bind(null!);      // ArgumentNullException
result.Tap(null!);       // ArgumentNullException
result.Ensure(null!, e); // ArgumentNullException
result.Match(null!, f);  // ArgumentNullException
```

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

## Version History

| Version | Changes |
|---------|---------|
| 2.0.0 | .NET 10, readonly struct, railway-oriented programming, STJ, Flurl 4.x, multi-error, ProblemDetails |
| 1.2.0 | Legacy Newtonsoft.Json-based Result pattern with Flurl 3.x extensions |

---

## License

MIT
