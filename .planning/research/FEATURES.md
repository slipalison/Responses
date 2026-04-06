# FEATURES — Research: Mature Result Pattern Libraries (2025/2026)

> Research compiled from: Ardalis.Result, CSharpFunctionalExtensions, error-or, FluentResults, ResultCrafter, OneOf, DotNext, and broader ecosystem analysis.
> Downstream consumer: `/gsd:define-requirements`

---

## 1. TABLE STAKES — Must Have or Developers Leave

These are non-negotiable. Every mature Result library in 2025/2026 ships these. If Responses lacks any of them, developers will pick a competitor.

### 1.1 Core Result Types

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| `Result` (void success/failure) | Low | None | **Implemented** (needs refactoring — mutable props, Newtonsoft.Json) |
| `Result<T>` (typed value) | Low | None | **Implemented** (needs refactoring — mutable props, Newtonsoft.Json) |
| `Result<TValue, TError>` (custom error type) | Low | `IError` constraint | **Implemented** (needs refactoring — mutable props) |
| `IsSuccess` / `IsFailed` properties | Low | None | **Implemented** |
| `Value` accessor (throws on failure) | Low | None | **Implemented** |
| `Error` accessor (throws on success) | Low | None | **Implemented** |
| Immutability (init-only / `readonly struct`) | Low | Core types | **Needs Refactoring** — current code uses mutable properties with private setters |
| `ValueOrDefault` (safe access without throw) | Low | `Result<T>` | **Not Implemented** |

### 1.2 Factory Methods

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| `Ok()` / `Ok<T>(value)` | Low | Core types | **Implemented** |
| `Fail(code, message)` / `Fail<T>(code, message)` | Low | Core types, Error | **Implemented** |
| `Fail(Error)` / `Fail<T>(Error)` | Low | Core types, Error | **Implemented** |
| `OkIf(condition, value)` | Low | Core types | **Not Implemented** |
| `FailIf(condition, error)` | Low | Core types | **Not Implemented** |
| `Try(Func<T>)` — exception capture | Medium | Core types, exception handler config | **Not Implemented** |

### 1.3 Error Model

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| Error with Code, Message | Low | `IError` | **Implemented** |
| Error with Metadata (key/value dict) | Low | `Error` | **Not Implemented** — `Error.Errors` exists but is `IEnumerable<KeyValuePair<string,string>>`, not a proper metadata dict |
| Multiple error collection | Medium | Error list, Merge | **Not Implemented** — current model stores only a single Error |
| Error types/enums (Validation, NotFound, Conflict, Unauthorized, Forbidden, etc.) | Low | Error type system | **Not Implemented** — no typed error categories |
| Custom error types | Medium | `IError` constraint | **Partially Implemented** — `IError` exists but no factory helpers |

### 1.4 Functional Composition (Railway-Oriented Programming)

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| `Map(Func<T, TOut>)` — transform success value | Low | `Result<T>` | **Not Implemented** |
| `Bind(Func<T, Result<TOut>>)` — chain fallible operations | Medium | `Result<T>`, `Map` | **Not Implemented** |
| `Tap(Action<T>)` — side-effect without changing value | Low | `Result<T>` | **Not Implemented** |
| `Ensure(Predicate, Error)` — conditional guard | Low | `Result<T>`, `FailIf` | **Not Implemented** |
| Async variants (`MapAsync`, `BindAsync`, `TapAsync`, `EnsureAsync`) | Medium | Async/Task support | **Not Implemented** |
| LINQ query syntax support (`from...in...select`) | Medium | `SelectMany` impl | **Not Implemented** |

### 1.5 Serialization

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| JSON serialization (success) | Low | JSON library | **Implemented** (Newtonsoft.Json — needs migration to System.Text.Json) |
| JSON serialization (failure) | Low | JSON Library, Error model | **Implemented** (Newtonsoft.Json — needs migration) |
| JSON round-trip fidelity | Medium | Both above | **Partially Implemented** — tests exist but cross-type round-trip (`Result` -> `Result<T>`) is broken |
| System.Text.Json support | Medium | .NET 10 STJ APIs | **Not Implemented** — still using Newtonsoft.Json |

### 1.6 Testing Support

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| xUnit test project | Low | xUnit | **Implemented** |
| 90%+ code coverage | Medium | Coverlet, tests | **Not yet measured** — coverage likely < 60% |
| FluentAssertions extensions | Low | FluentAssertions pkg | **Not Implemented** |

---

## 2. DIFFERENTIATORS — Competitive Advantage

These features separate top-tier libraries from the rest. Implementing even a subset gives Responses a strong market position.

### 2.1 HTTP Integration (Flurl Extensions) — PRIMARY DIFFERENTIATOR

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| HTTP status code in Result metadata | Medium | HTTP metadata model | **Not Implemented** — status code is lost during parsing |
| Response headers captured | Medium | HTTP metadata model | **Not Implemented** |
| Raw response body (string/Memory) | Low | HTTP metadata model | **Not Implemented** |
| Reason phrase captured | Low | HTTP metadata model | **Not Implemented** |
| `Result<T>.WithHttpInfo(statusCode, headers, body)` extension | High | All above, immutable struct design | **Not Implemented** — planned via extension pattern |
| Pagination header parsing (Link, X-Total-Count, etc.) | Medium | Headers captured | **Not Implemented** |
| Retry/resilience hooks (Polly integration points) | High | Polly, async pipeline | **Not Implemented** — out of scope for v1 but design for it |
| Typed error mapping from HTTP status -> Error type | Medium | Error type enums | **Not Implemented** |
| RFC 9457 ProblemDetails serialization | Medium | STJ, Error model | **Not Implemented** |
| ASP.NET Core `IResult` / `ActionResult` mapping | Medium | AspNetCore pkg | **Not Implemented** |
| Minimal API extensions (`.ToOkResult()`, `.ToProblemResult()`) | Medium | AspNetCore pkg | **Not Implemented** |
| Swagger/OpenAPI metadata conventions | Medium | AspNetCore pkg | **Not Implemented** |

### 2.2 Performance — Zero-Allocation Design

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| `readonly struct` for Result (zero heap alloc on success path) | Medium | Immutability | **Not Implemented** — current `struct` is mutable |
| `Span<T>` / `Memory<T>` for error message handling | Medium | .NET 10 APIs | **Not Implemented** |
| `ArrayPool<T>` for serialization buffers | Medium | STJ, ArrayPool | **Not Implemented** |
| No-reflection serialization | Medium | STJ source-gen mode | **Not Implemented** |
| `[StructLayout(LayoutKind.Auto)]` optimization | Low | .NET 10 | **Not Implemented** |
| Benchmark suite (BenchmarkDotNet) | Low | BDN pkg | **Not Implemented** |

### 2.3 Compile-Time Validation (Source Generators + Analyzers)

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| Roslyn analyzer: warn when Result return value is discarded | High | Roslyn SDK | **Not Implemented** |
| Roslyn analyzer: warn when `.Value` accessed without `.IsSuccess` check | High | Roslyn SDK | **Not Implemented** |
| Source generator: auto-generate HTTP result mappings from controller signatures | High | Roslyn SDK, AspNetCore | **Not Implemented** |
| Source generator: auto-generate OpenAPI `[ProducesResponseType]` from Result-returning endpoints | High | Roslyn SDK, AspNetCore | **Not Implemented** |

### 2.4 Advanced Error Handling

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| Multiple error aggregation (list of errors per Result) | Medium | Error collection, Merge | **Not Implemented** |
| Error cause chain (`.CausedBy()`) — hierarchical error tracking | Medium | Error list, recursion | **Not Implemented** |
| `Merge()` — combine multiple Results into one | Medium | Multiple error support | **Not Implemented** |
| `Match()` / `Switch()` — pattern matching delegates | Low | Core types | **Not Implemented** |
| `Else()` / `ElseAsync()` — fallback value on failure | Low | `Result<T>` | **Not Implemented** |
| Error type discrimination (switch on Error.Type enum) | Low | Error type enums | **Not Implemented** |

### 2.5 Ecosystem Integration

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| FluentValidation integration (`ValidateToResultAsync`) | Medium | FluentValidation pkg | **Not Implemented** |
| MediatR pipeline behavior (auto-validate + short-circuit) | High | MediatR pkg, FluentValidation | **Not Implemented** |
| Entity Framework Core concurrency handling (DbUpdateConcurrencyException -> 409) | Medium | EFCore pkg | **Not Implemented** |
| Structured logging (`[LoggerMessage]` on failure) | Medium | Microsoft.Extensions.Logging | **Not Implemented** |
| Correlation ID / distributed tracing support | Medium | Activity/HttpContext | **Not Implemented** |

### 2.6 Developer Experience

| Feature | Complexity | Dependencies | Status |
|---------|-----------|-------------|--------|
| `ToString()` with meaningful output | Low | Core types | **Partially Implemented** — Error.ToString() exists, Result.ToString() missing |
| C# 11+ pattern matching support (`result is { IsSuccess: true, Value: var v }`) | Low | Core types | **Works** (by virtue of properties) |
| Deconstruction (`var (isSuccess, value, error) = result`) | Low | Core types | **Not Implemented** |
| Implicit conversions (`Result<T>` from `T`) | Low | Core types | **Not Implemented** |
| Global factory customization (`Result.Setup(...)`) | Medium | Static config | **Not Implemented** |

---

## 3. ANTI-FEATURES — Deliberately NOT Build

These are features that mature libraries explicitly avoid. Responses should too.

| Anti-Feature | Rationale |
|-------------|-----------|
| **XML serialization** | Dead weight. STJ is the standard. Anyone who needs XML can map themselves. |
| **UI/CLI/Example apps in the library repo** | Bloats the package. Examples belong in a separate samples repo or wiki. |
| **Support for .NET < 10** | Target framework is net10.0. Back-porting to netstandard2.1 or net8.0 fragments the codebase and blocks Span/Memory APIs. |
| **Other HTTP clients (Refit, RestSharp, HttpClient raw)** | Flurl is the chosen HTTP abstraction. Adding adapters for every client violates SRP. Consumers can layer their own adapters on top. |
| **Exception replacement (Result as a full exception substitute)** | Result is for expected failures, not exceptional conditions. `Try()` captures exceptions, but the library should not encourage swallowing `OutOfMemoryException` or `StackOverflowException`. |
| **Full monad tutorial / FP evangelism** | The library should be self-documenting through API design, not require a philosophy course. |
| **Dynamic/runtime error code generation** | Error codes should be compile-time constants. Dynamic codes break serialization contracts and make API contracts unpredictable. |
| **Built-in retry/resilience logic** | Polly owns this space. Responses should expose extension points, not re-implement retry/circuit-breaker. |
| **Database/ORM integration beyond EF Core concurrency** | The Result pattern is transport-agnostic. Database-specific error mapping belongs in a separate package. |

---

## 4. FEATURE MATRIX — How Competitors Compare

| Feature | Ardalis.Result | CSharpFunctionalExtensions | error-or | FluentResults | ResultCrafter | **Responses (current)** |
|---------|:-:|:-:|:-:|:-:|:-:|:-:|
| Result / Result<T> | Yes | Yes | Yes (ErrorOr<T>) | Yes | Yes | Yes |
| Custom error type | No | No | Yes | Yes | Yes | Yes (`TError`) |
| Multiple errors | No | No | Yes | Yes | No | No |
| Error metadata | No | No | Yes | Yes | Yes | Partial |
| Map / Bind | Yes | Yes | Yes (.Then) | Yes (Bind) | Yes | **No** |
| Try/capture | No | No | No | Yes | No | **No** |
| Merge | No | No | No | Yes | No | **No** |
| OkIf / FailIf | Yes | Yes (SuccessIf) | Yes (.FailIf) | Yes | No | **No** |
| Async variants | Yes | Yes | Yes | Partial | Yes | **No** |
| HTTP status mapping | Yes | Yes | Yes | No | Yes | Partial (lost) |
| ProblemDetails (RFC 9457) | No | No | No | No | Yes | **No** |
| FluentValidation | Yes | No | Via MediatR | No | Yes | **No** |
| MediatR pipeline | Yes | No | Via behavior | No | Yes | **No** |
| Source generator / analyzer | No | Yes (analyzer) | No | No | No | **No** |
| Zero-allocation struct | No | No | Yes (struct) | No | Yes (readonly struct) | **No** |
| System.Text.Json | Yes | Yes | Yes | Yes | Yes | **No** (Newtonsoft) |
| Deconstruction | No | No | No | Yes | No | **No** |
| Cause chain | No | No | No | Yes | No | **No** |
| Logging integration | No | No | No | Yes | Yes | **No** |

---

## 5. PRIORITY MATRIX — What to Build First

### Tier 1 — Must ship in v2.0 (table stakes + primary differentiator)

1. Migrate Newtonsoft.Json → System.Text.Json
2. Immutable `readonly struct` with init-only properties
3. `ValueOrDefault` property
4. `Map` / `Bind` / `Tap` / `Ensure` (sync + async)
5. `OkIf` / `FailIf` factories
6. HTTP metadata (status code, headers, body) in Result
7. `Result<T>.WithHttpInfo()` extension
8. Multiple error collection (`IReadOnlyList<Error>`)
9. Error type enumeration (Validation, NotFound, Conflict, etc.)
10. `ToString()` on Result types

### Tier 2 — Competitive differentiators (v2.1)

11. RFC 9457 ProblemDetails serialization
12. Zero-allocation benchmarks (BenchmarkDotNet)
13. `Span<T>` / `ArrayPool<T>` for serialization
14. `Match` / `Switch` pattern matching delegates
15. `Merge` for combining Results
16. `Try(Func<T>)` exception capture
17. Deconstruction support
18. Implicit conversions
19. FluentAssertions test extensions

### Tier 3 — Ecosystem plays (v3.0+)

20. FluentValidation integration package
21. MediatR pipeline behavior package
22. ASP.NET Core `IResult` / `ActionResult` mapping
23. Roslyn analyzer (discarded Result, unsafe `.Value` access)
24. Structured logging (`[LoggerMessage]`)
25. Correlation ID support
26. EF Core concurrency package
27. Source generator for OpenAPI metadata

---

## 6. DEPENDENCY GRAPH

```
Tier 0 (Foundation)
  └─ Immutable readonly struct
  └─ System.Text.Json migration
  └─ Error type enumeration

Tier 1 (Core + HTTP)
  └─ ValueOrDefault          ← depends on Tier 0
  └─ Map/Bind/Tap/Ensure     ← depends on Tier 0
  └─ OkIf/FailIf             ← depends on Tier 0
  └─ Multiple error collection ← depends on Error type enumeration
  └─ HTTP metadata            ← depends on Tier 0 (immutable struct)
  └─ WithHttpInfo()          ← depends on HTTP metadata

Tier 2 (Performance + DX)
  └─ RFC 9457 ProblemDetails  ← depends on STJ + Error types
  └─ Zero-allocation          ← depends on readonly struct + Span
  └─ Match/Switch             ← depends on Map/Bind
  └─ Merge                    ← depends on Multiple errors
  └─ Try                      ← depends on Error types
  └─ Deconstruction           ← depends on Tier 0
  └─ Implicit conversions     ← depends on Tier 0

Tier 3 (Ecosystem)
  └─ FluentValidation         ← depends on Error types + Map
  └─ MediatR                  ← depends on FluentValidation
  └─ ASP.NET Core mapping     ← depends on RFC 9457 + HTTP metadata
  └─ Roslyn analyzer          ← independent (parallel track)
  └─ Structured logging       ← depends on Error types
  └─ EF Core                  ← depends on ASP.NET Core mapping
```

---

## 7. BROWNFIELD STATUS SUMMARY

| Category | Files Affected | Effort |
|----------|---------------|--------|
| Newtonsoft.Json → STJ | `Result.cs`, `Error.cs`, `HttpResponseMessageExtensions.cs`, all tests | Medium |
| Immutable structs | `Result.cs`, `Result<T>`, `Result<TValue,TError>`, `Error.cs` | Medium |
| HTTP metadata capture | `HttpResponseMessageExtensions.cs` (rewrite) | High |
| Missing functional methods | New file: `ResultExtensions.cs` or methods on existing types | Medium |
| Missing error types | `Error.cs` refactor + new `ErrorType` enum | Low |
| Missing tests | New test classes for every new feature | Ongoing |
| Newtonsoft.Json dependency | `Responses.csproj` package reference | Low |
| Target framework | `Responses.csproj` netstandard2.1 → net10.0 | Low (but breaks compat) |

---

## 8. KEY INSIGHTS FROM RESEARCH

1. **The market has standardized on `readonly struct` for the success path.** error-or, ResultCrafter, and DotNext all use this. Mutable structs are a red flag for performance-conscious developers.

2. **Multiple error collection is the #1 feature that separates modern libraries from legacy ones.** Ardalis.Result explicitly does NOT support it. FluentResults and error-or both lead with it.

3. **RFC 9457 ProblemDetails is becoming table stakes for HTTP-aware Result libraries.** ResultCrafter ships it by default. Any library targeting .NET 10 API developers needs this.

4. **Source generators/analyzers are the new frontier.** CSharpFunctionalExtensions already ships an analyzer. The next wave of libraries will compete on compile-time safety, not runtime features.

5. **The HTTP + Result integration is Responses' unique positioning.** No major library currently offers Flurl-specific extensions with full metadata capture. This is the white space.

6. **Performance is a first-class feature now.** Zero-allocation success paths are no longer optional — they are expected by the performance-sensitive segment. BenchmarkDotNet suites are standard.

7. **MediatR + FluentValidation integration is the dominant architecture pattern.** Any Result library that doesn't play well with this stack is at a disadvantage for enterprise adoption.
