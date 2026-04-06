# PITFALLS — Research: Result Pattern Library Mistakes & Prevention

> Research compiled from: Common Result library anti-patterns, .NET performance pitfalls, brownfield refactoring mistakes, and Righting Software principles.
> Downstream consumer: `/gsd:create-roadmap`

---

## 1. Design Pitfalls

### 1.1 Mutable Structs (The Defensive Copy Trap)

**Pitfall:** Structs com setters (mesmo privados) causam defensive copies em certas situações do JIT, especialmente quando acessados via interface ou como readonly field.

**Warning Signs:**
- `Result.IsSuccess` retorna `false` após acessar via interface
- Performance inconsistente entre debug/release builds
- xUnit tests passam localmente mas falham em CI

**Prevention:**
- Usar `readonly struct` em todos os tipos Result/Error
- Propriedades como `init` apenas (não `set`)
- Se setter interno é necessário, usar constructor apenas
- Testar com `[MethodImpl(MethodImplOptions.NoInlining)]` para forçar comportamento real

**Phase:** Phase 1 (Core Foundation) — deve ser corrigido desde o primeiro struct

---

### 1.2 Implicit Conversion Operators (The Silent Bug Factory)

**Pitfall:** `public static implicit operator Result<T>(T value)` parece conveniente mas causa bugs silenciosos: conversões acidentais, ambiguidade com outros operadores, e perda de intenção explícita.

**Warning Signs:**
- Código compila mas faz algo inesperado
- `var result = someValue;` — é um Result ou o valor direto?
- Ambiguidade com `null` conversions

**Prevention:**
- Usar factory methods explícitos: `Result<T>.Ok(value)`
- NÃO usar implicit operators
- Se conveniência é necessária, usar `implicit operator Result<T>(T value)` APENAS com tipos value (structs) e documentar claramente

**Phase:** Phase 1 (Core Foundation) — decisão de design fundamental

---

### 1.3 Polluting Core Result with HTTP Concerns

**Pitfall:** Adicionar `StatusCode`, `Headers`, `RawBody` diretamente no `Result<T>` struct viola SRP e cria dependência de `System.Net.Http` no core.

**Warning Signs:**
- `Responses.Core.csproj` referencia `System.Net.Http`
- `Result<T>` precisa de `using System.Net.Http;`
- Testes do core precisam mockar HTTP

**Prevention:**
- HTTP metadata como campo opcional: `HttpResponseInfo? HttpInfo { get; }`
- `HttpResponseInfo` definido em `Responses.Http`, não em `Responses.Core`
- Extension method `WithHttpInfo()` no namespace `Responses.Http`
- Core não conhece HTTP; Http conhece Core

**Phase:** Phase 1 (Core) + Phase 3 (HTTP) — separação de concerns desde Phase 1

---

### 1.4 Exception-Throwing Value Accessor Without Safe Alternative

**Pitfall:** `result.Value` que lança exceção quando `IsFailed` é o padrão, mas sem `ValueOrDefault` force consumers to check `IsSuccess` primeiro — defeating the purpose of Result pattern.

**Warning Signs:**
- `if (result.IsSuccess) { var v = result.Value; ... }` — padrão repetitivo
- Consumers ignoram Result e fazem `.Value` diretamente (exceção em produção)

**Prevention:**
- Sempre oferecer `ValueOrDefault` como safe alternative
- Documentar: "Use `Value` quando tiver certeza de sucesso; `ValueOrDefault` quando unsure"
- Analyzer (opcional): warning se `.Value` é usado sem `.IsSuccess` check

**Phase:** Phase 1 (Core Foundation)

---

## 2. Performance Pitfalls

### 2.1 Multiple Body Reads (Current Bug in This Project)

**Pitfall:** `HttpResponseMessage.Content.ReadAsStringAsync()` multiple times — o segundo call lança exceção ou retorna string vazia. O conteúdo já foi consumido.

**Warning Signs:**
- `ReadAsStringAsync()` em catch blocks após já ter lido o body
- Body vazio em logs de erro
- `ObjectDisposedException` em testes

**Prevention:**
- Ler body UMA vez, armazenar em variável
- Usar o body armazenado para logging, parsing, e error creation
- Usar `ArrayPool<byte>` para buffer reutilizável se leitura em stream for necessária

```csharp
// WRONG:
try { var body = await response.Content.ReadAsStringAsync(); }
catch { var errorBody = await response.Content.ReadAsStringAsync(); } // BUG: já consumido

// RIGHT:
var body = await response.Content.ReadAsStringAsync();
try { /* parse body */ }
catch { /* use body variable, don't re-read */ }
```

**Phase:** Phase 3 (HTTP Extensions) — bug crítico a corrigir

---

### 2.2 String Allocation in Hot Path

**Pitfall:** Criar strings para parsing temporário (error codes, header values) gera GC pressure desnecessário.

**Warning Signs:**
- `string.Substring()` para extrair partes de headers
- `string.Split()` para parsing
- `string.Format()` ou `$"{...}"` em loops

**Prevention:**
- Usar `ReadOnlySpan<char>` para parsing temporário
- Usar `Span<char>` com `ArrayPool<char>` para buffers
- Usar `Utf8JsonReader` diretamente (não `JsonSerializer.Deserialize` se performance extrema é necessária)

```csharp
// String allocation:
var statusCode = response.StatusCode.ToString(); // GC allocation

// Zero allocation:
var statusCode = (int)response.StatusCode; // int, no GC
```

**Phase:** Phase 4 (Performance Optimization)

---

### 2.3 Boxing of Struct Results

**Pitfall:** Quando `Result<T>` é passado como `object` ou via interface genérica não-constrainted, boxing ocorre — heap allocation + performance hit.

**Warning Signs:**
- `object obj = result;` — boxing silencioso
- `List<Result>` ao invés de `List<Result<T>>` — pode boxar
- Pattern matching: `if (result is IResult r)` — boxing em algumas versões do runtime

**Prevention:**
- Usar `where T : struct` constraint em generics que aceitam Result
- Evir interfaces genéricas não-constrainted
- Benchmark: alocar 1M Results e verificar GC

**Phase:** Phase 4 (Performance Optimization)

---

### 2.4 Newtonsoft.Json Performance Tax

**Pitfall:** Newtonsoft.Json usa reflection cache, cria delegates, e não é AOT-compatible. Para uma biblioteca puramente de serialização, a alocação é desnecessária.

**Warning Signs:**
- `JsonConvert.SerializeObject(result)` em hot path
- Memory profile mostra `ReflectionCache`, `JsonProperty`, `JsonSerializerInternalWriter`

**Prevention:**
- Usar `System.Text.Json` com `JsonSerializerContext` (Source Generator)
- Zero reflection, AOT compatible, 3-5x mais rápido
- já decidido neste projeto

**Phase:** Phase 2 (Serialization)

---

## 3. Brownfield Migration Pitfalls

### 3.1 Breaking Change Without Versioning Strategy

**Pitfall:** Mudar `Result` de mutable struct para `readonly struct` é breaking change para qualquer consumidor que use reflection ou dependa de setters.

**Warning Signs:**
- Consumidores usam `result.GetType().GetProperty("IsSuccess").SetValue(...)`
- Testes de integração falham após mudança de struct
- Versionamento semântico não observado

**Prevention:**
- Versionar como 2.0.0 (major version bump)
- Documentar todas as breaking changes em CHANGELOG.md
- Oferecer migration guide: "Se você fazia X, agora faça Y"
- Manter factory methods `Ok()`/`Fail()` com mesma assinatura (surface compatibility)

**Phase:** Phase 1 (desde o início)

---

### 3.2 Rewriting Without Tests

**Pitfall:** Reescrever Result types sem testes adequados introduz regressões silenciosas.

**Warning Signs:**
- Testes atuais não cobrem cenários de erro HTTP
- Nenhum teste para serialização com erro de parsing
- Testes não validam imutabilidade

**Prevention:**
- TDD obrigatório: teste antes da implementação
- Manter testes existentes como regression suite
- Adicionar testes de imutabilidade:
```csharp
[Fact]
public void Result_IsImmutable_SettersAreInitOnly()
{
    // Compilação deve falhar se tentar setar propriedade
    // typeof(Result).GetProperty("IsSuccess")!.CanWrite.ShouldBeFalse();
}
```
- Adicionar testes de equivalência: novo Result deve produzir mesmos outputs que o antigo para mesmas inputs

**Phase:** Phase 1 (antes de qualquer implementação)

---

### 3.3 Mutating Shared State (Thread Safety)

**Pitfall:** `AssemblyContext` com `Func<string>` mutável causa race conditions em ambientes multi-threaded.

**Warning Signs:**
- `public static Func<string> GetLayerName { get; set; }` — setter público
- Testes que rodam em paralelo interferem entre si
- `Layer` muda entre criação do Result e acesso ao `Layer`

**Prevention:**
- Usar `ResultContext` imutável: set uma vez, nunca muda
- Thread-local storage se contexto precisa variar por thread
- `AsyncLocal<ResultContext>` para contexto async flow

```csharp
// WRONG:
public static Func<string> GetLayerName { get; set; } = () => "Default";

// RIGHT:
private static readonly AsyncLocal<ResultContext> _current = new();
public static ResultContext Current => _current.Value ?? ResultContext.Default;
```

**Phase:** Phase 2 (Context/Serialization)

---

## 4. API Surface Pitfalls

### 4.1 Too Many Factory Methods

**Pitfall:** `Result.Ok()`, `Result.Success()`, `Result.Pass()`, `Result.From()` — múltiplos nomes para a mesma coisa confunde consumidores.

**Warning Signs:**
- Documentação precisa explicar 4 formas de fazer a mesma coisa
- Code reviews discutem qual nome usar ao invés de lógica
- StackOverflow tem respostas conflitantes

**Prevention:**
- Um nome por conceito: `Ok()` e `Fail()` apenas
- Se alias é necessário (retrocompatibilidade), marcar como `[Obsolete("Use Ok()")]`

**Phase:** Phase 1 (Core Foundation)

---

### 4.2 Missing Functional Composition (Map/Bind)

**Pitfall:** Result sem `Map` e `Bind` força consumers a fazer `if (result.IsSuccess) { ... }` — defeating the purpose.

**Warning Signs:**
- Nested `if (result.IsSuccess)` checks
- Código imperativo ao invés de funcional
- Result é apenas um "boolean fancy"

**Prevention:**
- Implementar `Map`, `Bind`, `Tap`, `Ensure` desde Phase 1
- Inspirar em `CSharpFunctionalExtensions` — API madura e testada
- Documentar com exemplos: "Ao invés de `if`, use `.Map()`"

**Phase:** Phase 1 (Core Foundation)

---

### 4.3 No Multi-Error Support

**Pitfall:** Result que suporta apenas um erro força aggregation artificial. Validações múltiplas (ex: formulário com 5 erros) ficam awkward.

**Warning Signs:**
- `result.Errors[0]` — accessa apenas primeiro erro
- Consumidores concatenam mensagens de erro manualmente
- Validações múltiplas não são representáveis

**Prevention:**
- `IReadOnlyList<IError> Errors` desde Phase 1
- `ErrorCollection` para agregar múltiplos erros
- Factory: `Result.Fail(errors)` aceita `IEnumerable<IError>`

**Phase:** Phase 1 (Core) + Phase 2 (ErrorCollection)

---

## 5. Testing Pitfalls

### 5.1 Not Testing HTTP Error Scenarios

**Pitfall:** Testar apenas sucesso HTTP. Erros de rede, timeout, serialization error, e status codes não-200 não são cobertos.

**Warning Signs:**
- Cobertura de 90% mas nenhum teste para `ReceiveResultAsync()` com status 500
- Serialização de erro com JSON mal-formado não é testada
- Timeout não é testado

**Prevention:**
- Testar explicitamente: 200, 400, 401, 404, 500, timeout, cancellation
- Usar `Flurl.Http.Testing.HttpTest` para mockar responses
- Testar serialização de erro com JSON inválido:
```csharp
[Fact]
public async Task ReceiveResultAsync_InvalidJsonErrorBody_FailsGracefully()
{
    using var test = new HttpTest();
    test.RespondWith("{ invalid json", 500);
    var result = await "http://test".GetAsync().ReceiveResultAsync<MyType>();
    result.IsFailed.Should().BeTrue();
    result.Errors.Should().NotBeEmpty(); // Não lançou exceção
}
```

**Phase:** Phase 3 (HTTP Extensions)

---

### 5.2 Not Testing Immutability

**Pitfall:** Testar funcionalidade mas não imutabilidade. Struct "imutável" que vaza mutação em cenários específicos.

**Warning Signs:**
- Nenhum teste que verifica que propriedades não podem ser setadas
- Testes não verificam `Equals`/`GetHashCode` para structs

**Prevention:**
- Teste de compilação: verificar que properties são `init`-only
- Teste de `Equals`/`GetHashCode`: dois Results iguais devem ser iguais por valor
- Teste de threading: criar Results em múltiplas threads simultaneamente

**Phase:** Phase 1 (Core Foundation)

---

### 5.3 Not Testing Zero-Allocation Claims

**Pitfall:** Implementar otimizações de performance sem benchmarks que validam que zero alocação é atingido.

**Warning Signs:**
- "Deveria ser zero-alocação" sem dados
- Nenhum BenchmarkDotNet no projeto
- GC pressure medido apenas em produção

**Prevention:**
- BenchmarkDotNet como parte do test suite
- Baseline de alocação antes de otimizações
- CI falha se alocação aumentar além de threshold

```csharp
[Fact]
public void Result_Ok_ZeroAllocation()
{
    var allocBefore = GC.GetTotalMemory(forceFullCollection: true);
    for (int i = 0; i < 1_000_000; i++) { var r = Result.Ok(); }
    var allocAfter = GC.GetTotalMemory(forceFullCollection: true);
    var allocated = allocAfter - allocBefore;
    allocated.Should().BeLessThan(1000); // Menos de 1KB para 1M Results
}
```

**Phase:** Phase 4 (Performance Optimization)

---

## 6. Pitfall Summary Matrix

| # | Pitfall | Severity | Phase | Detection |
|---|---------|----------|-------|-----------|
| 1.1 | Mutable structs (defensive copy) | **Critical** | Phase 1 | Benchmark + readonly struct |
| 1.2 | Implicit conversion operators | **High** | Phase 1 | Code review |
| 1.3 | Polluting core with HTTP | **High** | Phase 1 | Dependency analysis |
| 1.4 | No safe value accessor | **Medium** | Phase 1 | API review |
| 2.1 | Multiple body reads (current bug) | **Critical** | Phase 3 | Code inspection + tests |
| 2.2 | String allocation in hot path | **Medium** | Phase 4 | Benchmark + profiler |
| 2.3 | Boxing of struct results | **Medium** | Phase 4 | Benchmark + profiler |
| 2.4 | Newtonsoft.Json performance tax | **Medium** | Phase 2 | Already decided |
| 3.1 | Breaking change without versioning | **High** | Phase 1 | Semantic versioning |
| 3.2 | Rewriting without tests | **Critical** | Phase 1 | TDD discipline |
| 3.3 | Mutating shared state | **High** | Phase 2 | Thread safety tests |
| 4.1 | Too many factory methods | **Low** | Phase 1 | API review |
| 4.2 | Missing Map/Bind | **High** | Phase 1 | API review |
| 4.3 | No multi-error support | **Medium** | Phase 1-2 | API review |
| 5.1 | Not testing HTTP errors | **Critical** | Phase 3 | Coverage report |
| 5.2 | Not testing immutability | **High** | Phase 1 | Test review |
| 5.3 | Not testing zero-allocation | **High** | Phase 4 | Benchmark suite |

---

## 7. Phase-by-Phase Prevention Checklist

### Phase 1 (Core Foundation)
- [ ] All structs are `readonly struct`
- [ ] All properties are `init`-only (no `set`)
- [ ] No implicit conversion operators
- [ ] `ValueOrDefault` exists alongside `Value`
- [ ] `Map`, `Bind`, `Tap`, `Ensure` implemented
- [ ] Multi-error support (`IReadOnlyList<IError>`)
- [ ] Factory methods: only `Ok()` and `Fail()`
- [ ] Immutability tests pass
- [ ] TDD for every type

### Phase 2 (Error Handling & Serialization)
- [ ] `ResultContext` is immutable and thread-safe
- [ ] No `Func<>` mutable state
- [ ] System.Text.Json (not Newtonsoft)
- [ ] JsonSerializerContext for AOT

### Phase 3 (HTTP Extensions)
- [ ] Single body read (no duplicate reads)
- [ ] HTTP error scenarios tested (200, 400, 401, 404, 500, timeout, cancellation)
- [ ] Invalid JSON error body handled gracefully
- [ ] HttpResponseInfo in `Responses.Http`, not Core

### Phase 4 (Performance Optimization)
- [ ] BenchmarkDotNet suite passes
- [ ] Zero-allocation claim validated for hot path
- [ ] No boxing in Result operations
- [ ] Span<T>/ArrayPool used in parsers
- [ ] Allocation tests: < 1KB per 1M Result creations

---

*Research completed: 6 de abril de 2026*
