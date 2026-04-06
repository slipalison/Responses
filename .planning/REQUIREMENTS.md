# REQUIREMENTS — Responses v2.0

> Refatoração completa da biblioteca Responses (Result Pattern + extensão Flurl)
> Target: .NET 10, Flurl 4.x, System.Text.Json, TDD obrigatório, princípios "Righting Software"
> Date: 6 de abril de 2026

---

## v1 Scope — Core Foundation + HTTP + Performance

### 1. Core Result Types

| # | Requirement | Checkable |
|---|------------|-----------|
| R1 | `Result` como `readonly struct` imutável (void success/failure) | Compila como `readonly struct`; propriedades são `init`-only; tentativa de mutação gera erro de compilação |
| R2 | `Result<T>` como `readonly struct` imutável com `Value` accessor | Compila como `readonly struct`; `Value` lança se `IsFailed`; `ValueOrDefault` retorna default sem lançar |
| R3 | `Result<TValue, TError>` como `readonly struct` imutável com erro tipado | Compila como `readonly struct`; `TError` constrained por `IError`; acesso seguro a valor e erro |
| R4 | `IsSuccess` / `IsFailed` properties em todos os Result types | `result.IsSuccess` e `result.IsFailed` compilam e retornam valores opostos corretos |
| R5 | `ValueOrDefault` property em `Result<T>` — acesso seguro sem throw | `result.ValueOrDefault` retorna `default(T)` quando failed, valor quando success, sem exceção |
| R6 | `ToString()` significativo em todos os Result types | `result.ToString()` inclui IsSuccess, Value/Error info legível |
| R7 | Factory methods `Ok()`, `Ok<T>(value)`, `Fail(code, message)`, `Fail(Error)` mantidos (retrocompatibilidade) | `Result.Ok()`, `Result.Ok<T>(value)`, `Result.Fail("code", "msg")`, `Result.Fail(error)` compilam e funcionam |
| R8 | Factory methods `OkIf(condition, value)` e `FailIf(condition, error)` adicionados | `Result.OkIf(true, value)` retorna Ok; `Result.OkIf(false, value)` retorna Fail; `Result.FailIf(true, error)` retorna Fail |
| R9 | `[StructLayout(LayoutKind.Auto)]` aplicado aos structs | Atributo presente no código; layout otimizado para .NET 10 |

### 2. Error Model

| # | Requirement | Checkable |
|---|------------|-----------|
| R10 | `Error` como `readonly struct` imutável com Code, Message, Layer, ApplicationName | Compila como `readonly struct`; todas as propriedades são `init`-only |
| R11 | `Errors` collection — múltiplos erros por Result (`IReadOnlyList<IError>`) | `result.Errors` retorna lista imutável; `result.Fail()` pode receber múltiplos erros |
| R12 | Error type enumeration (Validation, NotFound, Conflict, Unauthorized, Forbidden, etc.) | Enum `ErrorType` existe com valores tipados; factory methods criam erros com tipo correto |
| R13 | `IError` interface para erros customizados com `Code`, `Message`, `Type`, `Metadata` | Interface compilada; `Error` implementa `IError`; custom errors podem implementar `IError` |
| R14 | Metadata dictionary no Error (`IReadOnlyDictionary<string, string>`) | `error.Metadata["key"]` funciona; metadata é serializada em JSON |
| R15 | `Match(success, failure)` e `MatchAsync(success, failure)` delegates | `result.Match(v => ..., e => ...)` compila e executa branches corretamente |
| R16 | `Else(fallbackValue)` e `ElseAsync(fallbackFunc)` — fallback on failure | `result.Else(defaultValue)` retorna valor quando failed; retorna Value quando success |

### 3. Functional Composition (Railway-Oriented Programming)

| # | Requirement | Checkable |
|---|------------|-----------|
| R17 | `Map(Func<T, TOut>)` — transforma valor de sucesso | `result.Map(x => x * 2)` retorna `Result<TOut>` com valor transformado; passa erro se failed |
| R18 | `Bind(Func<T, Result<TOut>>)` — encadeia operações fallible | `result.Bind(x => SomeFallibleOp(x))` encadeiaResults; short-circuit no primeiro failure |
| R19 | `Tap(Action<T>)` — side-effect sem alterar valor | `result.LogConsole(x => ...)` executa ação se success; retorna mesmo Result; não executa se failed |
| R20 | `Ensure(Predicate, Error)` — guard condicional | `result.Ensure(x => x > 0, error)` retorna Fail se predicate é falso |
| R21 | `MapAsync`, `BindAsync`, `TapAsync`, `EnsureAsync` — variantes assíncronas | Todas as 4 variantes async compilam e funcionam com `Task<T>` / `ValueTask<T>` |
| R22 | LINQ query syntax (`from...in...select`) via `SelectMany` | Query expression com Result compila e retorna Result correto |

### 4. Serialization

| # | Requirement | Checkable |
|---|------------|-----------|
| R23 | `System.Text.Json` como único serializer (Newtonsoft.Json removido) | Package reference a Newtonsoft.Json removida; `System.Text.Json` é o único serializer |
| R24 | `ResultJsonConverter` — STJ converter para Result types | Serialização e deserialização de `Result`, `Result<T>`, `Result<TValue, TError>` round-trip fiel |
| R25 | `ErrorJsonConverter` — STJ converter para Error types | Serialização e deserialização de `Error` e `IError` customizados round-trip fiel |
| R26 | JSON round-trip fidelity entre tipos | `Result<T>` serializado e desserializado como `Result<T>` preserva valor/erro; `Result` -> `Result<T>` não é suportado (documentado) |
| R27 | `JsonSerializerContext` source generator para zero-reflection | `ResultJsonContext` gerado; serialização usa source-gen mode, não reflection |

### 5. HTTP Integration (Flurl Extensions) — Primary Differentiator

| # | Requirement | Checkable |
|---|------------|-----------|
| R28 | `HttpResponseInfo` struct — StatusCode, Headers, RawBody, ReasonPhrase | Struct imutável com 4 campos; headers como `IReadOnlyDictionary<string, IEnumerable<string>>` |
| R29 | `ReceiveResult<T>()` — extensão Flurl que retorna `Result<T>` com metadata HTTP | `await url.GetJsonAsync().ReceiveResult<T>()` retorna `Result<T>` com `HttpResponseInfo` anexado |
| R30 | Single body read — zero leituras duplicadas do body | Implementação lê body uma vez; reutiliza para parsing de sucesso e erro; sem bug de double-read |
| R31 | HTTP status code mapeado para Error.Type automaticamente | 4xx -> Validation/Unauthorized/Forbidden/NotFound/Conflict; 5xx -> ServerError; mapeamento testado |
| R32 | `Result<T>.WithHttpInfo()` — acesso a metadata HTTP | `result.WithHttpInfo()` retorna `(Result<T>, HttpResponseInfo)` ou accessor; compila e funciona |
| R33 | RFC 9457 ProblemDetails parsing — erro HTTP com ProblemDetails | Response com `application/problem+json` é parsed para Error com campos RFC 9457 (type, title, detail, instance) |
| R34 | `ReceiveResult()` sem tipo — retorna `Result` void | `await response.ReceiveResult()` retorna `Result` (sem valor), com HTTP info |
| R35 | Error de serialização tratado graciosamente | Falha ao deserializar body -> `Result<T>` com Error contendo raw body e mensagem de erro de parsing |
| R36 | Timeout e cancellation propagados como Error | `CancellationToken` cancelado -> `Result<T>` com Error tipo "Timeout" ou "Cancelled" |
| R37 | Network error (exceção de rede) capturado como Error | Exceção de rede (DNS fail, connection refused) -> `Result<T>` com Error, sem lançar exceção |

### 6. Performance — Zero-Allocation Design

| # | Requirement | Checkable |
|---|------------|-----------|
| R38 | `readonly struct` em todos os Result/Error types — zero heap alloc no success path | BenchmarkDotNet mostra 0 bytes alocados no hot path de `Result.Ok(value)` |
| R39 | `Span<T>` / `Memory<T>` em parsing de body HTTP | Código usa `Span<byte>` ou `ReadOnlySpan<char>` para parsing; sem string intermediárias desnecessárias |
| R40 | `ArrayPool<T>` para buffers de serialização | Serialização usa `ArrayPool<byte>.Shared` para buffers; sem `new byte[]` em hot path |
| R41 | Sem reflection em runtime (source-gen STJ) | `JsonSerializer.IsReflectionEnabledByDefault` é falso; tudo via source generator |
| R42 | BenchmarkDotNet suite — baseline de alocação e throughput | Projeto de benchmarks existe; roda com `dotnet run -c Release`; reporta GC gen0/alloc por op |
| R43 | No boxing de value types em Result | `Result<int>` não boxea `int`; verificado via BenchmarkDotNet IL/benchmarks |

### 7. Testing

| # | Requirement | Checkable |
|---|------------|-----------|
| R44 | TDD obrigatório — ciclo Red-Green-Refactor documentado em commits | Cada feature tem commit com teste failing primeiro, depois implementação |
| R45 | Cobertura mínima de 90% | `dotnet test /p:CollectCoverage=true` reporta >= 90% line coverage |
| R46 | Testes de todos os cenários HTTP: timeout, cancellation, network error, serialization error | Testes com mock HTTP cobrem: 200 OK, 4xx, 5xx, timeout, cancellation, DNS fail, invalid JSON |
| R47 | Testes de alocação/GC — validação de zero-allocation claims | `AllocationTests` verificam 0 allocs em `Result.Ok()`, `result.Map()`, `result.Bind()` |
| R48 | Testes de serialização JSON — round-trip fidelity | Testes serializam e desserializam todos os tipos; verificam igualdade |
| R49 | Testes de funcional composition — Map, Bind, Tap, Ensure (sync + async) | Testes cobrem success path, failure path, e short-circuit para todos os métodos |

### 8. Code Quality

| # | Requirement | Checkable |
|---|------------|-----------|
| R50 | Zero warnings no build | `dotnet build` com `<TreatWarningsAsErrors>true` passa limpo |
| R51 | Documentação XML completa em todos os tipos públicos | `<GenerateDocumentationFile>true` no csproj; todos os tipos/membros públicos com `<summary>` |
| R52 | SRP aplicado — Core, Error, Serialization, HTTP separados em namespaces/módulos distintos | Namespaces: `Responses.Core`, `Responses.Error`, `Responses.Serialization`, `Responses.Http`; sem cross-dependências indevidas |
| R53 | Thread safety — zero `Func<>` mutáveis ou estado compartilhado mutável | Código não usa `static` mutável; `AssemblyContext` imutável; verificado por review |
| R54 | Retrocompatibilidade conceitual — Ok/Fail surface mantida | Código existente que usa `Result.Ok()` / `Result.Fail()` compila sem mudanças na API surface |

---

## v2 Scope — Competitive Differentiators (Deferred)

| # | Requirement | Reason Deferred |
|---|------------|---------------|
| R55 | Roslyn analyzer: warn when Result return value is discarded | Complexo; não bloqueia funcionalidade core; pode ser adicionado em paralelo |
| R56 | Roslyn analyzer: warn when `.Value` accessed without `.IsSuccess` check | Mesmo motivo — analyzer complexo, funcionalidade core independe |
| R57 | Source generator: auto-generate HTTP result mappings | Depende de analyzer infra; v2 feature |
| R58 | Source generator: auto-generate `[ProducesResponseType]` from endpoints | Depende de ASP.NET Core; v2 feature |
| R59 | FluentAssertions test extensions | DX improvement; não essencial para v1 |
| R60 | `Try(Func<T>)` exception capture | Útil mas não table stakes; pode ser v2.1 |
| R61 | `Merge()` — combine multiple Results | Depende de multiple error collection; v2 |
| R62 | Deconstruction support (`var (isSuccess, value, error) = result`) | DX; pode ser adicionado sem breaking change |
| R63 | Implicit conversions (`Result<T>` from `T`) | DX; flagged como cuidado em pitfalls (Pitfall 1.2) |
| R64 | `Result.Setup(...)` global factory customization | Complexidade adicional; v2 se demandado |

---

## v3+ Scope — Ecosystem Integration (Deferred)

| # | Requirement | Reason Deferred |
|---|------------|---------------|
| R65 | FluentValidation integration (`ValidateToResultAsync`) | Pacote separado; depende de tipos estáveis do v1 |
| R66 | MediatR pipeline behavior (auto-validate + short-circuit) | Depende de FluentValidation; pacote v3 |
| R67 | ASP.NET Core `IResult` / `ActionResult` mapping | Depende de RFC 9457 implementado; v3 |
| R68 | Minimal API extensions (`.ToOkResult()`, `.ToProblemResult()`) | Depende de ASP.NET Core mapping; v3 |
| R69 | Swagger/OpenAPI metadata conventions | Depende de ASP.NET Core mapping; v3 |
| R70 | Entity Framework Core concurrency handling | Pacote separado; v3 |
| R71 | Structured logging (`[LoggerMessage]` on failure) | Depende de Microsoft.Extensions.Logging; v3 |
| R72 | Correlation ID / distributed tracing | Depende de Activity/HttpContext; v3 |
| R73 | Polly integration points (retry/resilience hooks) | Design for it, don't build it — extension points apenas |
| R74 | Pagination header parsing (Link, X-Total-Count) | Nice to have; não table stakes |

---

## Out of Scope — Deliberately NOT Built

| Item | Reason |
|------|--------|
| XML serialization | Dead weight. STJ é o padrão. |
| UI/CLI/Example apps no repo | Bloat. Exemplos pertencem a samples repo separado. |
| Suporte a .NET < 10 | Target é net10.0. Back-porting bloqueia Span/Memory APIs. |
| Outros HTTP clients (Refit, RestSharp, HttpClient raw) | Flurl é a abstraction escolhida. Violates SRP. |
| Exception replacement (Result como substituto total de exceções) | Result é para expected failures, não exceptional conditions. |
| Built-in retry/resilience logic | Polly owns this. Extension points apenas. |
| Dynamic/runtime error code generation | Breaks serialization contracts. Error codes são compile-time constants. |
| Full monad tutorial / FP evangelism | API self-documenting. |
| Database/ORM integration beyond EF Core concurrency | Result é transport-agnostic. |

---

## Dependencies & Build Order

```
Phase 1: Core Foundation (R1-R9, R10-R16)
  └─ readonly struct + init-only properties
  └─ Factory methods (Ok, Fail, OkIf, FailIf)
  └─ Error model (IError, Error, ErrorType, Metadata)
  └─ Match, Else, functional composition

Phase 2: Serialization (R23-R27)
  └─ System.Text.Json migration
  └─ ResultJsonConverter, ErrorJsonConverter
  └─ JsonSerializerContext source generator

Phase 3: HTTP Extensions (R28-R37)
  └─ HttpResponseInfo struct
  └─ ReceiveResult<T>() Flurl extension
  └─ RFC 9457 ProblemDetails parsing
  └─ Error mapping (status code -> ErrorType)

Phase 4: Performance (R38-R43)
  └─ readonly struct zero-allocation verification
  └─ Span<T>, ArrayPool<T> optimization
  └─ BenchmarkDotNet suite

Phase 5: Testing + Quality (R44-R54)
  └─ 90%+ coverage
  └─ Zero warnings
  └─ XML documentation
  └─ Thread safety verification
```

---

## Key Decisions (reaffirmadas)

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| `readonly struct` para Result | Zero heap alloc, GC friendly, padrão de mercado | Implementado em R1-R3 |
| System.Text.Json exclusivo | Performance, zero dependência, AOT compatible | R23, sem Newtonsoft |
| HTTP metadata via extensão | Não poluir core Result com HTTP info | R28-R37 |
| Manter Ok/Fail surface | Retrocompatibilidade conceitual | R7, R54 |
| TDD Red-Green-Refactor | Qualidade garantida, regressão prevenida | R44 |
| Cobertura mínima 90% | Regressão prevenida, documentação viva | R45 |
| Multiple error collection | #1 feature que separa bibliotecas modernas | R11 |
| RFC 9457 ProblemDetails | Table stakes para .NET 10 API developers | R33 |
| Flurl 4.x exclusivo | SRP — foco em uma abstraction | R29, anti-feature |

---

## Success Metrics

1. [ ] Zero warnings no build (`dotnet build` limpo com TreatWarningsAsErrors)
2. [ ] 90%+ cobertura de testes (`coverlet` >= 90%)
3. [ ] Zero alocações desnecessárias no hot path (validado com BenchmarkDotNet)
4. [ ] Todos os testes passing antes de cada commit
5. [ ] API pública documentada com XML docs
6. [ ] Princípios do Righting Software aplicados e documentados
7. [ ] Retrocompatibilidade conceitual mantida (Ok/Fail funcionam)
8. [ ] HTTP integration é o diferenciador único (metadata completo)

---

## Phase Traceability

> Updated: 6 de abril de 2026 — ROADMAP.md created

| Requirement | Phase | Status |
|-------------|-------|--------|
| R1-R9, R15-R22 | Phase 1: Core Foundation | Mapped |
| R10-R14, R23-R27 | Phase 2: Error Model + Serialization | Mapped |
| R28-R37 | Phase 3: HTTP Extensions (Flurl) | Mapped |
| R38-R43, R50-R54 | Phase 4: Performance + Quality | Mapped |
| R44-R49 | Phase 5: Testing + Polish | Mapped |

**Coverage:** 54/54 v1 requirements mapped (100%). 0 orphaned.

---

*Rightsized from research: FEATURES.md (25 table stakes + 26 differentiators analyzed)*
*v1 scope: 54 requirements across 8 categories*
*v2 scope: 10 requirements deferred*
*v3+ scope: 10 requirements deferred*
*Out of scope: 10 items deliberately excluded*
