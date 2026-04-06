# ROADMAP — Responses v2.0

> Refatoração completa da biblioteca Responses (Result Pattern + extensão Flurl)
> Target: .NET 10, Flurl 4.x, System.Text.Json, TDD obrigatório
> Date: 6 de abril de 2026

---

## Phase Overview

```
Phase 1: Core Foundation ──► Phase 2: Error Model + Serialization ──► Phase 3: HTTP Extensions ──► Phase 4: Performance + Quality ──► Phase 5: Testing + Polish
```

---

## Phase 1: Core Foundation

**Goal:** Implementar `Result`, `Result<T>`, `Result<TValue, TError>` como `readonly struct` imutáveis com factory methods e composição funcional (Map/Bind/Tap/Ensure).

**Requirements:** R1, R2, R3, R4, R5, R6, R7, R8, R9, R15, R16, R17, R18, R19, R20, R21, R22

**Deliverables:**
- `Result` — `readonly struct` imutável (void success/failure)
- `Result<T>` — `readonly struct` imutável com `Value`, `ValueOrDefault`, `ToString()`
- `Result<TValue, TError>` — `readonly struct` com erro tipado (`where TError : IError`)
- Factory methods: `Ok()`, `Ok<T>(value)`, `Fail(code, message)`, `Fail(Error)`, `OkIf()`, `FailIf()`
- Functional composition: `Map()`, `Bind()`, `Tap()`, `Ensure()` (sync + async)
- Pattern matching: `Match(success, failure)`, `Else(fallbackValue)` (sync + async)
- LINQ query syntax via `SelectMany`
- `[StructLayout(LayoutKind.Auto)]` aplicado

**Success Criteria:**
1. `Result.Ok()`, `Result.Ok(42)`, `Result.Fail("code", "msg")` compilam e funcionam corretamente
2. `result.IsSuccess` e `result.IsFailed` retornam valores opostos corretos em todos os tipos
3. `result.Value` lança `InvalidOperationException` quando failed; `result.ValueOrDefault` retorna `default(T)` sem lançar
4. `result.Map(x => x * 2)` transforma valor em success; propaga erro em failure (short-circuit)
5. `result.Bind(fallibleOp)` encadeia operações; para no primeiro failure (railway-oriented programming)

**Research flag:** ⚠️ **Needs care** — Immutable struct design é breaking change. Decisão de `readonly struct` vs `init`-only tem implicações profundas. Testar exaustivamente antes de prosseguir.

**Confirmation gate:** [ ] All Phase 1 tests passing, core API reviewed

---

## Phase 2: Error Model + Serialization

**Goal:** Redesenhar `Error` como `readonly struct` imutável com tipagem, metadata, e multi-error collection. Migrar para `System.Text.Json` com source generators (zero reflection).

**Requirements:** R10, R11, R12, R13, R14, R23, R24, R25, R26, R27

**Deliverables:**
- `Error` — `readonly struct` imutável com Code, Message, Type, Layer, ApplicationName, Metadata
- `ErrorType` enum — Validation, NotFound, Conflict, Unauthorized, Forbidden, ServerError, Timeout, Cancelled, etc.
- `IError` interface — Code, Message, Type, Metadata para erros customizados
- `ErrorCollection` — `IReadOnlyList<IError>` para múltiplos erros por Result
- `ResultJsonConverter` — STJ converter para `Result`, `Result<T>`, `Result<TValue, TError>`
- `ErrorJsonConverter` — STJ converter para `Error` e `IError` customizados
- `ResultJsonContext` — `JsonSerializerContext` source generator (zero reflection)
- JSON round-trip fidelity entre tipos documentada

**Success Criteria:**
1. `Result.Fail(error)` com `Error` customizado serializa e desserializa com fidelidade
2. `result.Errors` retorna `IReadOnlyList<IError>` imutável; múltiplos erros podem ser adicionados via factory
3. `error.Metadata["key"]` funciona; metadata é serializada em JSON
4. `JsonSerializer.Serialize(result)` e `JsonSerializer.Deserialize<Result<T>>(json)` preservam valor/erro (round-trip)
5. `ResultJsonContext` source generator é usado; `JsonSerializer.IsReflectionEnabledByDefault = false` não quebra serialização

**Research flag:** ✅ Standard patterns — STJ com Source Generator é padrão bem documentado. Baixo risco.

**Confirmation gate:** [ ] All Phase 2 tests passing, serialization round-trip verified

---

## Phase 3: HTTP Extensions (Flurl)

**Goal:** Extensão Flurl moderna com retorno completo de informações HTTP. Single body read (zero leituras duplicadas). RFC 9457 ProblemDetails parsing. Error de serialização e network error tratados graciosamente.

**Requirements:** R28, R29, R30, R31, R32, R33, R34, R35, R36, R37

**Deliverables:**
- `HttpResponseInfo` struct — StatusCode, Headers (`IReadOnlyDictionary<string, IEnumerable<string>>`), RawBody, ReasonPhrase
- `ReceiveResult<T>()` — extensão Flurl que retorna `Result<T>` com metadata HTTP anexado
- `ReceiveResult()` sem tipo — retorna `Result` void com HTTP info
- Single body read pattern — lê body uma vez, reutiliza para parsing de sucesso e erro
- HTTP status code mapeado para `Error.Type` automaticamente (4xx → Validation/Unauthorized/etc., 5xx → ServerError)
- `Result<T>.WithHttpInfo()` — accessor para metadata HTTP
- RFC 9457 ProblemDetails parsing — `application/problem+json` → Error com type, title, detail, instance
- Error de serialização tratado graciosamente (raw body + mensagem de erro de parsing no Error)
- Timeout/cancellation propagados como Error
- Network error (DNS fail, connection refused) capturado como Error

**Success Criteria:**
1. `await url.GetJsonAsync().ReceiveResult<T>()` retorna `Result<T>` com `HttpResponseInfo` anexado
2. Body é lido **uma única vez**; sem bug de double-read (validado via teste com mock HTTP)
3. Response 404 → `Result<T>` com `Error.Type == NotFound`; 500 → `ServerError`; 401 → `Unauthorized`
4. `result.WithHttpInfo()` retorna `(Result<T>, HttpResponseInfo)` com StatusCode, Headers, RawBody, ReasonPhrase
5. Response com `application/problem+json` é parsed para Error com campos RFC 9457 (type, title, detail, instance)
6. Falha ao deserializar body → `Result<T>` com Error contendo raw body e mensagem de parsing (sem lançar exceção)
7. `CancellationToken` cancelado → `Result<T>` com Error tipo "Cancelled"; DNS fail → Error sem exceção

**Research flag:** ⚠️ **Needs care** — HTTP metadata integration é o diferenciador único do Responses. Design decisions aqui definem o posicionamento de mercado. RFC 9457 ProblemDetails pode ser complexo.

**Confirmation gate:** [ ] All Phase 3 tests passing, HTTP scenarios covered, ProblemDetails parsing verified

---

## Phase 4: Performance + Quality

**Goal:** Otimização extrema com `Span<T>`, `ArrayPool<T>`, zero-allocation hot paths. BenchmarkDotNet suite. Zero warnings no build. Documentação XML completa. Thread safety garantida.

**Requirements:** R38, R39, R40, R41, R42, R43, R50, R51, R52, R53, R54

**Deliverables:**
- `readonly struct` zero-allocation verification (BenchmarkDotNet: 0 bytes em `Result.Ok(value)`)
- `Span<T>` / `Memory<T>` em parsing de body HTTP (sem strings intermediárias desnecessárias)
- `ArrayPool<T>` para buffers de serialização (sem `new byte[]` em hot path)
- Zero reflection em runtime (source-gen STJ verificado)
- BenchmarkDotNet suite — baseline de alocação e throughput (GC gen0/alloc por op)
- No boxing de value types em `Result<int>` (verificado via benchmarks)
- Zero warnings no build (`dotnet build` com `<TreatWarningsAsErrors>true`)
- `<GenerateDocumentationFile>true` — XML docs em todos os tipos/membros públicos
- SRP aplicado — namespaces: `Responses.Core`, `Responses.Error`, `Responses.Serialization`, `Responses.Http` (sem cross-dependências indevidas)
- Thread safety — zero `Func<>` mutáveis ou estado compartilhado mutável (verificado por review)
- Retrocompatibilidade conceitual — código existente com `Result.Ok()` / `Result.Fail()` compila sem mudanças

**Success Criteria:**
1. BenchmarkDotNet reporta 0 bytes alocados em `Result.Ok(value)`, `result.Map()`, `result.Bind()`
2. Parsing de body HTTP usa `Span<byte>` ou `ReadOnlySpan<char>`; sem alocações intermediárias
3. Serialização usa `ArrayPool<byte>.Shared`; sem `new byte[]` em hot path
4. `dotnet build` passa com zero warnings (TreatWarningsAsErrors = true)
5. XML docs gerados para todos os tipos/membros públicos; IntelliSense funciona
6. Code review confirma: sem `static` mutável; `AssemblyContext` imutável; namespaces sem cross-dependências

**Research flag:** 🔬 **Likely needs deeper research** — Zero-allocation claims precisam de validação empírica. BenchmarkDotNet suite pode revelar surpresas. `Span<T>` em APIs públicas tem limitações (não pode ser field, async, yield).

**Confirmation gate:** [ ] Benchmarks passing, zero warnings, XML docs complete, thread safety verified

---

## Phase 5: Testing + Polish

**Goal:** Atingir 90%+ cobertura de testes. Testes de todos os cenários HTTP (timeout, cancellation, network error, serialization error). Testes de alocação/GC validando zero-allocation claims. TDD Red-Green-Refactor documentado em commits.

**Requirements:** R44, R45, R46, R47, R48, R49

**Deliverables:**
- TDD cycle documentado em commits (Red-Green-Refactor)
- 90%+ line coverage (`dotnet test /p:CollectCoverage=true` >= 90%)
- Testes de cenários HTTP: 200 OK, 4xx, 5xx, timeout, cancellation, DNS fail, invalid JSON, ProblemDetails
- `AllocationTests` — verificam 0 allocs em `Result.Ok()`, `result.Map()`, `result.Bind()`
- Testes de serialização JSON — round-trip fidelity para todos os tipos
- Testes de functional composition — Map, Bind, Tap, Ensure (sync + async) com success path, failure path, short-circuit

**Success Criteria:**
1. `dotnet test /p:CollectCoverage=true` reporta >= 90% line coverage
2. Testes com mock HTTP cobrem: 200 OK, 400 Validation, 404 NotFound, 401 Unauthorized, 500 ServerError, timeout, cancellation, DNS fail, invalid JSON
3. `AllocationTests` confirmam 0 allocs em hot path de `Result.Ok()`, `result.Map()`, `result.Bind()`
4. Testes serializam e desserializam todos os tipos (`Result`, `Result<T>`, `Result<TValue, TError>`, `Error`); verificam igualdade
5. Testes de Map/Bind/Tap/Ensure cobrem success path, failure path, e short-circuit (railway-oriented)

**Research flag:** ✅ Standard patterns — Test coverage e allocation testing são bem documentados. Baixo risco.

**Confirmation gate:** [ ] 90%+ coverage, all HTTP scenarios tested, allocation tests passing

---

## Requirement Coverage Matrix

| Requirement | Phase | Status |
|-------------|-------|--------|
| R1 | Phase 1 | Mapped |
| R2 | Phase 1 | Mapped |
| R3 | Phase 1 | Mapped |
| R4 | Phase 1 | Mapped |
| R5 | Phase 1 | Mapped |
| R6 | Phase 1 | Mapped |
| R7 | Phase 1 | Mapped |
| R8 | Phase 1 | Mapped |
| R9 | Phase 1 | Mapped |
| R10 | Phase 2 | Mapped |
| R11 | Phase 2 | Mapped |
| R12 | Phase 2 | Mapped |
| R13 | Phase 2 | Mapped |
| R14 | Phase 2 | Mapped |
| R15 | Phase 1 | Mapped |
| R16 | Phase 1 | Mapped |
| R17 | Phase 1 | Mapped |
| R18 | Phase 1 | Mapped |
| R19 | Phase 1 | Mapped |
| R20 | Phase 1 | Mapped |
| R21 | Phase 1 | Mapped |
| R22 | Phase 1 | Mapped |
| R23 | Phase 2 | Mapped |
| R24 | Phase 2 | Mapped |
| R25 | Phase 2 | Mapped |
| R26 | Phase 2 | Mapped |
| R27 | Phase 2 | Mapped |
| R28 | Phase 3 | Mapped |
| R29 | Phase 3 | Mapped |
| R30 | Phase 3 | Mapped |
| R31 | Phase 3 | Mapped |
| R32 | Phase 3 | Mapped |
| R33 | Phase 3 | Mapped |
| R34 | Phase 3 | Mapped |
| R35 | Phase 3 | Mapped |
| R36 | Phase 3 | Mapped |
| R37 | Phase 3 | Mapped |
| R38 | Phase 4 | Mapped |
| R39 | Phase 4 | Mapped |
| R40 | Phase 4 | Mapped |
| R41 | Phase 4 | Mapped |
| R42 | Phase 4 | Mapped |
| R43 | Phase 4 | Mapped |
| R44 | Phase 5 | Mapped |
| R45 | Phase 5 | Mapped |
| R46 | Phase 5 | Mapped |
| R47 | Phase 5 | Mapped |
| R48 | Phase 5 | Mapped |
| R49 | Phase 5 | Mapped |
| R50 | Phase 4 | Mapped |
| R51 | Phase 4 | Mapped |
| R52 | Phase 4 | Mapped |
| R53 | Phase 4 | Mapped |
| R54 | Phase 4 | Mapped |

**Total v1 requirements mapped: 54/54 (100%)**
**Orphaned requirements: 0**

---

## Phase 6: Extended Status Code Mapping

**Goal:** Suporte completo a qualquer HTTP status code, incluindo customizados. `ErrorType` deve cobrir todos os códigos padrão HTTP (429, 502, 503, etc.) e ter fallback robusto para códigos desconhecidos.

**Requirements:** R31 (extended)

**Deliverables:**
- `ErrorType` enum expandido — TooManyRequests (429), BadGateway (502), ServiceUnavailable (503), GatewayTimeout (504), ClientError (4xx genérico), etc.
- `StatusCodeMapping` completo — switch expression cobrindo todos os status codes HTTP padrão
- Fallback inteligente — códigos 4xx não mapeados → Validation/ClientError, 5xx → ServerError
- Suporte a códigos customizados (ex: 418, 450) com fallback para Unknown

**Success Criteria:**
1. `StatusCodeMapping.ToErrorType(429)` → `ErrorType.TooManyRequests`
2. `StatusCodeMapping.ToErrorType(503)` → `ErrorType.ServiceUnavailable`
3. `StatusCodeMapping.ToErrorType(418)` → `ErrorType.Unknown` (código customizado)
4. `StatusCodeMapping.ToErrorType(451)` → `ErrorType.Forbidden` (código 4xx não mapeado → melhor aproximação)
5. Testes cobrindo todos os status codes HTTP 400-599

**Research flag:** ✅ Standard — HTTP status codes são bem documentados (RFC 9110, RFC 6585).

**Confirmation gate:** [ ] All status codes tested, fallback behavior verified

---

## Phase Ordering Rationale

```
Phase 1 (Core) ──► Phase 2 (Error+Serialization) ──► Phase 3 (HTTP) ──► Phase 4 (Performance+Quality) ──► Phase 5 (Testing+Polish)
      │                        │                          │                         │                           │
      │                        └── Depende de Phase 1 ───┘                         │                           │
      │                                                               └── Depende de 1-3 ──────────┘           │
      └────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

**Why this order:**
1. **Core primeiro** — sem Result/Error types, nenhum outro componente existe. Imutabilidade deve ser decidida desde o início.
2. **Serialization antes de HTTP** — testes de HTTP precisam serializar Results; STJ é pré-requisito.
3. **HTTP antes de Performance** — o bug de multiple body reads precisa ser corrigido antes de otimizar.
4. **Performance antes de Testing/Polish** — otimizações podem mudar a API; testes de alocação dependem de implementação final.
5. **Testing/Polish por último** — consolida cobertura 90%+, valida zero-allocation claims, garante qualidade.

---

## Key Decisions (roadmap-level)

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 5 fases em vez de 4 | Performance + Quality separados de Testing — otimizações são distintas de cobertura de testes | Phase 4 foca em benchmarks + zero warnings + XML docs; Phase 5 foca em coverage + scenarios |
| Functional composition no Phase 1 | Map/Bind/Tap/Ensure são core para qualquer uso de Result — sem eles, biblioteca é inútil | R15-R22 no Phase 1 |
| Error model + Serialization juntos | Serialização depende do modelo de Error; testar juntos faz sentido | R10-R14 + R23-R27 no Phase 2 |
| Performance + Quality juntos | Benchmarks e zero-allocation estão ligados a quality gates (zero warnings, XML docs) | R38-R43 + R50-R54 no Phase 4 |

---

*Roadmap created: 6 de abril de 2026*
*Based on: REQUIREMENTS.md (54 v1 requirements), PROJECT.md, research/SUMMARY.md*
*Next: `/gsd:plan-phase 1` or `/gsd:discuss-phase 1`*
