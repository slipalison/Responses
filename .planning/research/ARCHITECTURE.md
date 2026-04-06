# ARCHITECTURE — Research: Result Pattern System Structure

> Research compiled from: Ardalis.Result, CSharpFunctionalExtensions, error-or architecture, and Righting Software principles.
> Downstream consumer: `/gsd:create-roadmap`

---

## 1. Component Boundaries

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Consumer Code                         │
│                 (Application/Service Layer)                 │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
               ▼                              ▼
┌──────────────────────────┐    ┌──────────────────────────────┐
│      Responses.Core      │    │      Responses.Http          │
│  (Pure, no dependencies) │    │  (Depends on Responses)      │
│                          │    │                              │
│  ┌────────────────────┐  │    │  ┌────────────────────────┐  │
│  │   Result Types     │  │    │  │   HTTP Extensions      │  │
│  │   Result           │  │    │  │   HttpResponseMessage  │  │
│  │   Result<T>        │  │    │  │   IFlurlResponse       │  │
│  │   Result<T, E>     │  │    │  │                        │  │
│  └────────────────────┘  │    │  └────────────────────────┘  │
│  ┌────────────────────┐  │    │  ┌────────────────────────┐  │
│  │   Error Types      │  │    │  │   HTTP Info Types      │  │
│  │   IError           │  │    │  │   HttpResponseInfo     │  │
│  │   Error            │  │    │  │   (StatusCode, Headers │  │
│  │   ErrorCollection  │  │    │  │    RawBody, Reason)    │  │
│  └────────────────────┘  │    │  └────────────────────────┘  │
│  ┌────────────────────┐  │    │  ┌────────────────────────┐  │
│  │   Serialization    │  │    │  │   HTTP Error Parser    │  │
│  │   ResultConverter  │  │    │  │   HttpErrorParser      │  │
│  │   ErrorConverter   │  │    │  │   (Span<T> optimized)  │  │
│  └────────────────────┘  │    │  └────────────────────────┘  │
│  ┌────────────────────┐  │    └──────────────────────────────┘
│  │   Context          │  │
│  │   ResultContext    │  │
│  │   (Layer, AppName) │  │
│  └────────────────────┘  │
└──────────────────────────┘
```

### 1.2 Dependency Flow

```
Consumer Code
    │
    ├──► Responses.Core (stable, no dependencies)
    │       │
    │       ├──► .NET 10 BCL only
    │       └──► System.Text.Json (built-in)
    │
    └──► Responses.Http (depends on Responses.Core)
            │
            ├──► Flurl.Http 4.x
            └──► Responses.Core (Result types)
```

**Stability Principle:** Dependencies flow toward stability. `Responses.Core` é o componente mais estável — não depende de nada externo. `Responses.Http` é menos estável — depende de Flurl (externo).

---

## 2. Component Details

### 2.1 Responses.Core — Result Types

```csharp
namespace Responses.Core;

/// <summary>Base immutable result type — no value, no typed error.</summary>
public readonly struct Result : IEquatable<Result>
{
    public bool IsSuccess { get; }
    public bool IsFailed => !IsSuccess;
    public IReadOnlyList<IError> Errors { get; }
    public ErrorType ErrorType { get; } // Validation, Business, System, NotFound, etc.

    internal Result(bool isSuccess, IReadOnlyList<IError> errors, ErrorType errorType) { ... }

    public static Result Ok() => new(true, Array.Empty<IError>(), ErrorType.None);
    public static Result Fail(IError error) => new(false, new[] { error }, ErrorType.Business);
    public static Result Fail(IEnumerable<IError> errors, ErrorType type) => ...;

    public Result<T> WithValue<T>(T value) => ...;
    public T Match<T>(Func<T> onSuccess, Func<IReadOnlyList<IError>, T> onFailure) => ...;
}

/// <summary>Immutable result with a typed value.</summary>
public readonly struct Result<TValue> : IEquatable<Result<TValue>>
{
    public bool IsSuccess { get; }
    public bool IsFailed => !IsSuccess;
    public TValue Value { get; }      // Throws if IsFailed
    public TValue ValueOrDefault { get; }  // Safe access
    public IReadOnlyList<IError> Errors { get; }
    public HttpResponseInfo? HttpInfo { get; } // Optional, set via extension

    internal Result(bool isSuccess, TValue value, IReadOnlyList<IError> errors, HttpResponseInfo? httpInfo) { ... }

    public static Result<TValue> Ok(TValue value) => ...;
    public static Result<TValue> Fail(IError error) => ...;

    // Functional composition
    public Result<TNewValue> Map<TNewValue>(Func<TValue, TNewValue> selector) => ...;
    public Result<TNewValue> Bind<TNewValue>(Func<TValue, Result<TNewValue>> selector) => ...;
    public Result<TValue> Tap(Action<TValue> action) => ...;
    public Result<TValue> Ensure(Func<TValue, bool> predicate, IError error) => ...;
    public T Match<T>(Func<TValue, T> onSuccess, Func<IReadOnlyList<IError>, T> onFailure) => ...;
}

/// <summary>Immutable result with typed value and typed error.</summary>
public readonly struct Result<TValue, TError> 
    where TError : IError
{
    // Same shape as Result<TValue> but with TError instead of IError
}
```

### 2.2 Responses.Core — Error Types

```csharp
namespace Responses.Error;

/// <summary>Contract for custom error types.</summary>
public interface IError
{
    string Code { get; }
    string Message { get; }
    string? Layer { get; }
    string? ApplicationName { get; }
    IReadOnlyDictionary<string, string>? Metadata { get; }
}

/// <summary>Standard immutable error implementation.</summary>
public readonly struct Error : IError, IEquatable<Error>
{
    public string Code { get; }
    public string Message { get; }
    public string? Layer { get; }
    public string? ApplicationName { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    internal Error(string code, string message, string? layer, string? appName, IReadOnlyDictionary<string, string>? metadata) { ... }

    public static Error Create(string code, string message) => ...;
    public static Error Create(string code, string message, string layer) => ...;
}

/// <summary>Collection of multiple errors (aggregate).</summary>
public readonly struct ErrorCollection : IError
{
    public string Code => "AGGREGATE_ERROR";
    public string Message => $"{Errors.Count} errors occurred";
    public IReadOnlyList<IError> Errors { get; }
    // ... delegates to IError for first error compatibility
}
```

### 2.3 Responses.Core — Serialization

```csharp
namespace Responses.Serialization;

/// <summary>System.Text.Json converter for Result types. Zero-reflection, AOT-friendly.</summary>
public class ResultJsonConverter : JsonConverter<Result> { ... }
public class ResultOfTConverter<TValue> : JsonConverter<Result<TValue>> { ... }
public class ErrorJsonConverter : JsonConverter<Error> { ... }
public class ErrorCollectionJsonConverter : JsonConverter<ErrorCollection> { ... }
```

### 2.4 Responses.Core — Context

```csharp
namespace Responses.Context;

/// <summary>Immutable context for Result operations (Layer, AppName). Thread-safe, set once.</summary>
public sealed class ResultContext
{
    public string Layer { get; }
    public string ApplicationName { get; }

    private ResultContext(string layer, string appName) { ... }

    public static ResultContext Create(string layer, string appName) => new(layer, appName);
}
```

### 2.5 Responses.Http — HTTP Extensions

```csharp
namespace Responses.Http;

/// <summary>Immutable struct with HTTP response metadata.</summary>
public readonly struct HttpResponseInfo : IEquatable<HttpResponseInfo>
{
    public HttpStatusCode StatusCode { get; }
    public HttpResponseHeaders Headers { get; }
    public string? RawBody { get; }
    public string? ReasonPhrase { get; }

    internal HttpResponseInfo(HttpStatusCode statusCode, HttpResponseHeaders headers, string? rawBody, string? reasonPhrase) { ... }
}

/// <summary>Extension methods for HttpResponseMessage → Result conversion.</summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Converts HttpResponseMessage to Result. Reads body ONCE (no duplicate reads).
    /// Uses ArrayPool<T> for buffer reuse. Captures StatusCode, Headers, RawBody, ReasonPhrase.
    /// </summary>
    public static async Task<Result<T>> ReceiveResultAsync<T>(
        this HttpResponseMessage response,
        CancellationToken ct = default) where T : class
    {
        // Single body read into pooled buffer
        var body = await response.Content.ReadAsStringAsync(ct);
        
        if (response.IsSuccessStatusCode)
        {
            var value = JsonSerializer.Deserialize<T>(body, ...);
            var result = Result<T>.Ok(value!);
            return result.WithHttpInfo(new HttpResponseInfo(
                response.StatusCode, response.Headers, body, response.ReasonPhrase));
        }
        
        var error = HttpErrorParser.Parse(response, body);
        return Result<T>.Fail(error).WithHttpInfo(new HttpResponseInfo(...));
    }
}

/// <summary>Extension methods for IFlurlResponse → Result conversion.</summary>
public static class FlurlExtensions
{
    public static async Task<Result<T>> ReceiveResultAsync<T>(
        this Task<IFlurlResponse> flurlResponse,
        CancellationToken ct = default) where T : class
    {
        using var response = await flurlResponse;
        return await response.ResponseMessage.ReceiveResultAsync<T>(ct);
    }
}
```

### 2.6 Responses.Http — HTTP Error Parser

```csharp
namespace Responses.Http;

/// <summary>Optimized HTTP error parser. Uses Span<T> for string parsing where applicable.</summary>
internal static class HttpErrorParser
{
    /// <summary>Parses HTTP error into IError. Attempts to deserialize body as ProblemDetails (RFC 9457), falls back to generic error.</summary>
    public static IError Parse(HttpResponseMessage response, string body)
    {
        // Try RFC 9457 ProblemDetails format first
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, ...);
        if (problemDetails != null)
        {
            return Error.Create(
                problemDetails.Type ?? $"HTTP_{(int)response.StatusCode}",
                problemDetails.Title ?? response.ReasonPhrase ?? "Unknown error",
                layer: "HTTP",
                metadata: problemDetails.Extensions);
        }
        
        // Fallback: generic error with HTTP context
        return Error.Create(
            $"HTTP_{(int)response.StatusCode}",
            body.Length > 0 ? body : response.ReasonPhrase ?? "Unknown error",
            layer: "HTTP");
    }
}
```

---

## 3. Data Flow

### 3.1 HTTP Request → Result Flow

```
IFlurlResponse
    │
    ▼
FlurlExtensions.ReceiveResultAsync<T>()
    │ (unwrap IFlurlResponse → HttpResponseMessage)
    ▼
HttpResponseMessageExtensions.ReceiveResultAsync<T>()
    │ (single body read via ReadAsStringAsync)
    ▼
HttpErrorParser.Parse(response, body)
    │ (try ProblemDetails → fallback generic)
    ▼
Result<T>.Fail(error) or Result<T>.Ok(value)
    │
    ▼
Result<T>.WithHttpInfo(httpResponseInfo)
    │
    ▼
Result<T> with complete HTTP metadata
    │
    ▼
Consumer code (Match/Value/IsSuccess)
```

### 3.2 Serialization Flow

```
Result<T>
    │
    ▼
ResultJsonConverter.Write(jsonWriter, result, options)
    │ (writes { "isSuccess": true, "value": ..., "errors": [...] })
    ▼
UTF8 JSON output (zero intermediate string via IBufferWriter<byte>)
```

---

## 4. Build Order & Dependencies

### Phase 1: Core Foundation (Week 1-2)

**Components:** `Result`, `Result<T>`, `Result<T, E>`, `IError`, `Error`

**Rationale:** Sem esses tipos, nada mais existe. São puros — sem dependências externas. Podem ser testados isoladamente.

**Dependencies:** Nenhum (apenas .NET 10 BCL)

### Phase 2: Error Handling & Serialization (Week 2-3)

**Components:** `ErrorCollection`, `ErrorBuilder`, `ResultJsonConverter`, `ErrorJsonConverter`, `ResultContext`

**Rationale:** Depende dos tipos core do Phase 1. Adiciona serialização e contexto.

**Dependencies:** Phase 1 (Result, Error types)

### Phase 3: HTTP Extensions (Week 3-4)

**Components:** `HttpResponseInfo`, `HttpResponseMessageExtensions`, `FlurlExtensions`, `HttpErrorParser`

**Rationale:** Depende dos tipos core (Phase 1) e pode ser desenvolvido em paralelo com Phase 2. HTTP é o ponto de integração mais instável.

**Dependencies:** Phase 1 (Result types), Flurl.Http 4.x

### Phase 4: Performance Optimization (Week 4-5)

**Components:** `ArrayPool<T>` buffers, `Span<T>` parsing, `IBufferWriter<T>` serialization, benchmarks

**Rationale:** Otimizações são aplicadas depois que a funcionalidade básica funciona. "Make it work, make it right, make it fast."

**Dependencies:** Phases 1-3 (funcionalidade completa)

### Phase 5: Source Generators & Analyzers (Week 5-6) [Optional v2.1+]

**Components:** Result usage analyzer, JSON context generator

**Rationale:** Nice-to-have. Pode ser adiado para v2.1 sem bloquear release.

**Dependencies:** Phases 1-3 (tipos estáveis para análise)

---

## 5. Brownfield Migration Strategy

### 5.1 What to Keep

| Current | Keep? | Rationale |
|---------|-------|-----------|
| `Result`, `Result<T>`, `Result<T, E>` names | ✅ | Manter familiaridade para consumidores |
| `Ok()`, `Fail()` factory names | ✅ | API surface familiar |
| `IError` interface | ✅ Refatorar | Manter contrato, adicionar propriedades se necessário |
| Test structure (xUnit) | ✅ | Já usa xUnit; expandir cobertura |
| Project separation (Responses / Responses.Http) | ✅ | SRP already respected |

### 5.2 What to Rewrite

| Current | Rewrite? | Rationale |
|---------|----------|-----------|
| Mutable properties → `readonly struct` + `init` | ✅ | Imutabilidade é breaking change interna |
| Newtonsoft.Json → System.Text.Json | ✅ | API diferente; reescrever serialização |
| `HttpResponseMessageExtensions` (duplicate body reads) | ✅ | Bug crítico; reescrever com single-read pattern |
| `AssemblyContext` (mutable `Func<string>`) | ✅ | Thread safety; usar `ResultContext` imutável |
| `Error` (public setters) | ✅ | Quebra imutabilidade |

### 5.3 Migration Approach

**Strangler Pattern:**
1. Criar novos tipos em namespaces paralelos (`Responses.Core.V2`)
2. Manter tipos atuais compilando
3. Testar novos tipos em paralelo
4. Quando novos tipos passam em todos os testes (+ novos), swap namespaces
5. Remover tipos antigos

**Alternative (preferred for this project):**
1. Criar branch de refatoração
2. Reescrever cada tipo do zero com TDD
3. Manter testes originais + novos testes
4. Quando todos os testes passam, mergear
5. Rationale: Projeto pequeno (2 projetos + tests); strangler overhead não vale

---

## 6. Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Component boundaries | HIGH | Padrão bem estabelecido em Result libraries |
| Data flow | HIGH | HTTP → Parser → Result é padrão simples |
| Build order | HIGH | Core primeiro, depois extensões, depois otimização |
| Brownfield strategy | MEDIUM | Strangler vs rewrite depende do tamanho do projeto |
| Source generators | MEDIUM | Complexidade alta; pode ser adiado |

---

*Research completed: 6 de abril de 2026*
