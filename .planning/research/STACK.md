# STACK — Research: High-Performance .NET Result Pattern Library (2025/2026)

> Research compiled from: .NET 10 documentation, Flurl 4.x docs, System.Text.Json benchmarks, Span<T>/Memory<T> best practices, and performance-focused .NET libraries.
> Downstream consumer: `/gsd:create-roadmap`

---

## 1. Core Stack Recommendations

### 1.1 Target Framework

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **.NET 10** | 10.0.x (LTS) | Target oficial do projeto. LTS até nov/2029. Melhorias de performance em JIT, GC, e Span<T> nativo. | HIGH |
| **Multi-target** | net10.0 only | Decisão explícita do projeto: não suportar versões antigas. Elimina complexidade de `#if` directives. | HIGH |

**What NOT to use:**
- ~~netstandard2.1~~ — Obsoleto, sem melhorias de performance modernas, sem Span<T> otimizado
- ~~net8.0/net9.0~~ — .NET 10 já é o alvo; multi-target adiciona complexidade desnecessária

### 1.2 JSON Serialization

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **System.Text.Json** | .NET 10 built-in | Nativo do runtime, zero dependência externa, `ref struct` UTF8JsonReader/Writer, Source Generator support para trimming AOT. | HIGH |
| **JsonSerializerOptions** | `JsonSerializerDefaults.Web` | camelCase por padrão, melhor para APIs web. | HIGH |

**Key Patterns:**
```csharp
// Source Generator para zero-alocação em compile-time
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<>))]
[JsonSerializable(typeof(Result<,>))]
internal partial class ResultJsonContext : JsonSerializerContext { }

// Usage: zero reflection, AOT-friendly
var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.Result);
```

**What NOT to use:**
- ~~Newtonsoft.Json~~ — Alocação extra (reflection cache), sem AOT support, 3-5x mais lento que STJ em benchmarks

### 1.3 HTTP Library

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **Flurl.Http** | 4.x (latest stable: 4.0.0+) | Alvo do projeto. Fluent API, HttpClient factory integrado, testing via `HttpTest`. | HIGH |
| **HttpClient** | .NET 10 built-in (`IHttpClientFactory`) | Base para Flurl. Usar `SocketsHttpHandler` com `PooledConnectionLifetime` para connection pooling. | HIGH |

**Key Patterns:**
```csharp
// Flurl 4.x com IHttpClientFactory
services.AddFlurlHttpClient();

// Testing com HttpTest
using var test = new HttpTest();
test.RespondWithJson(new { code = "ERROR", message = "Test error" }, 500);
```

**What NOT to use:**
- ~~RestSharp/Refit~~ — Fora de escopo explícito
- ~~HttpClient direto~~ — Flurl já encapsula; duplicar effort viola SRP

### 1.4 Testing Framework

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **xUnit.net** | 2.x (latest: 2.8.x+) | Já usado no projeto. `[Fact]`/`[Theory]` para testes determinísticos. `IClassFixture` para shared state. | HIGH |
| **FluentAssertions** | 6.x ou 7.x | Sintaxe fluente: `result.IsSuccess.Should().BeTrue()`. Melhor legibilidade que asserts nativos. | HIGH |
| **NSubstitute** | 5.x | Mocking para interfaces (IError, services). Mais limpo que Moq para .NET 10. | MEDIUM |
| **coverlet.msbuild** | Latest | Code coverage integrado ao build. `dotnet test /p:CollectCoverage=true`. | HIGH |

**Key Patterns:**
```xml
<!-- .csproj -->
<PackageReference Include="coverlet.msbuild" Version="*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

### 1.5 Performance & Benchmarking

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **BenchmarkDotNet** | 0.14.x+ | Gold standard para .NET benchmarks. Mede alocação, GC, throughput, latência. Integra com CI. | HIGH |
| **JetBrains dotMemory** | 2024.x+ | Profiling de memória: identifica leaks, alocações desnecessárias, GC pressure. | MEDIUM |
| **JetBrains dotTrace** | 2024.x+ | Profiling de performance: hot paths, CPU bottlenecks. | MEDIUM |
| **Microsoft.Diagnostics.Tracing.TraceEvent** | 3.x+ | ETW tracing para análise detalhada de GC (Gen 0/1/2 collections, pause times). | MEDIUM |

**Key Benchmark Patterns:**
```csharp
[MemoryDiagnoser]
[HideColumns("ErrorSD", "Gen1", "Gen2")]
public class ResultAllocationBenchmarks
{
    [Benchmark(Baseline = true)]
    public Result CurrentImplementation() => Result.Ok();

    [Benchmark]
    public Result NewImplementation() => Result.Ok();
}
```

---

## 2. Advanced Performance Stack

### 2.1 Memory Optimization

| Technique | When to Use | Rationale | Confidence |
|-----------|-------------|-----------|------------|
| **`readonly struct`** | All Result/Error types | Imutabilidade garantida em compile-time. JIT otimiza acesso. Sem defensive copies. | HIGH |
| **`Span<T>` / `ReadOnlySpan<T>`** | String parsing, body reading | Zero-alocação para operações temporárias. Stack-only, sem GC pressure. | HIGH |
| **`ArrayPool<T>`** | Buffer de leitura HTTP reutilizável | Reutiliza buffers, evita alocação de arrays em hot path. | HIGH |
| **`IBufferWriter<T>`** | Serialização JSON customizada | Escreve diretamente no buffer, sem string intermediária. | MEDIUM |
| **`ref struct`** | Internal helpers apenas | Não pode ser exposto em API pública (não pode ser field, async, yield). | HIGH |

**What NOT to do:**
- ~~`ref struct` em API pública~~ — Incompatível com async/await, interfaces, boxing
- ~~`Span<T>` como field~~ — Stack-only; usar `Memory<T>` para armazenamento

### 2.2 Source Generators & Analyzers

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **Roslyn Source Generators** | .NET 10 built-in | Gera código em compile-time. Zero runtime overhead. AOT compatible. | HIGH |
| **Roslyn Analyzers** | .NET 10 built-in | Valida uso correto em compile-time: "não ignore Result", "chame Match/Value". | HIGH |

**Use Cases for This Project:**
1. **Result Usage Analyzer** — Warning se Result é criado e ignorado (não consumido via Match/Value/IsSuccess)
2. **Factory Method Generator** — Gera factory methods otimizados para tipos específicos
3. **JsonSerializerContext Generator** — Auto-gera contextos de serialização para AOT

```csharp
// Analyzer rule example (custom RSXXXX code)
// RS0001: Result value is not consumed. Use .Match(), .Value, or .IsSuccess.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResultConsumedAnalyzer : DiagnosticAnalyzer { ... }
```

---

## 3. Build & CI Stack

| Recommendation | Version | Rationale | Confidence |
|---------------|---------|-----------|------------|
| **dotnet build** | .NET 10 CLI | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` para zero warnings. | HIGH |
| **dotnet format** | .NET 10 built-in | `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` no .csproj. | HIGH |
| **Microsoft.CodeAnalysis.NetAnalyzers** | .NET 10 built-in | `<EnableNETAnalyzers>true</EnableNETAnalyzers>`. Regras de qualidade e performance. | HIGH |
| **Nullable enable** | .NET 10 built-in | `<Nullable>enable</Nullable>`. Previne NullReferenceException em compile-time. | HIGH |

---

## 4. Complete Package Reference

### Responses.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>
</Project>
```

### Responses.Http.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Flurl.Http" Version="4.*" />
    <ProjectReference Include="..\Responses\Responses.csproj" />
  </ItemGroup>
</Project>
```

### test.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="coverlet.msbuild" Version="*" />
    <PackageReference Include="BenchmarkDotNet" Version="0.14.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="Flurl.Http" Version="4.*" />
    <ProjectReference Include="..\Responses.Http\Responses.Http.csproj" />
  </ItemGroup>
</Project>
```

---

## 5. Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| .NET 10 as target | HIGH | Decisão já tomada, bem documentada |
| System.Text.Json | HIGH | Benchmark data abundante, decisão já tomada |
| Flurl 4.x | HIGH | Decisão já tomada, versão estável |
| xUnit + FluentAssertions | HIGH | xUnit já usado; FluentAssertions é padrão da indústria |
| BenchmarkDotNet | HIGH | Gold standard para .NET performance testing |
| Source Generators | MEDIUM | Útil mas complexo; pode ser v3.0+ |
| Span<T>/ArrayPool | HIGH | Padrão bem documentado para zero-alocação |
| Roslyn Analyzers | MEDIUM | Custom analyzers são complexos; começar simples |

---

*Research completed: 6 de abril de 2026*
