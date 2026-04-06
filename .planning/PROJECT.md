# PROJECT: Responses — Refatoração Completa

## One-liner

Refatorar a biblioteca Responses (Result Pattern + extensão Flurl) seguindo os princípios do livro "Righting Software" de Juval Löwy, modernizando para .NET 10, Flurl 4.x, System.Text.Json, com otimização extrema de memória/GC e TDD obrigatório.

---

## Contexto Atual (Brownfield)

O projeto é uma biblioteca de Result Pattern (Notification Pattern) com dois componentes:

1. **Responses** (core) — `Result`, `Result<T>`, `Result<TValue, TError>`, `Error`, `IError`
2. **Responses.Http** — Extensões do Flurl para converter `HttpResponseMessage`/`IFlurlResponse` em `Result`

**Problemas identificados no código atual:**

- Target framework: `netstandard2.1` (obsoleto, .NET 10 é o alvo)
- Dependência de `Newtonsoft.Json` (alocação extra, não nativo)
- `Result` como `struct` mas com setters privados e propriedades mutáveis internamente — inconsistente
- `Error` com setters públicos — quebra imutabilidade
- `HttpResponseMessageExtensions` lê o body múltiplas vezes (`ReadAsStringAsync` em catch blocks) — alocação desnecessária e bugs potenciais
- Sem informação de HTTP status code, headers, raw body ou reason phrase no retorno — apenas `Error` genérico
- `AssemblyContext` usa `Func<string>` mutável — thread safety questionável
- Testes em `net7.0`, não cobrem cenários HTTP completos
- Nenhum teste para a extensão Flurl com erro de serialização
- Sem otimizações de `Span<T>`, `ArrayPool`, ou padrões de zero-alocação
- `ErrorResolver` cria `Error` sem informações completas do contexto HTTP

---

## Princípios "Righting Software" (Juval Löwy) Aplicados

Todos os princípios do livro serão aplicados sistematicamente:

- **Responsabilidade Única (SRP)** — Cada classe/módulo com uma única razão para mudar
- **Interfaces coesas** — Interfaces pequenas, com propósito claro
- **Segregação de responsabilidades** — Separação clara entre core Result, HTTP, e serialização
- **Componentização** — Baixo acoplamento entre componentes, alta coesão interna
- **Estabilidade de dependências** — Dependências apontam na direção da estabilidade
- **Princípio do ponto de instabilidade** — Pontos instáveis isolados e protegidos
- **Reuso equivalente** — Componentes reutilizáveis com contratos estáveis
- **Release-Reuse-Commonality** — Versões e releases com semântica clara
- **Abstractness-Stability-Main sequence** — Equilíbrio entre abstração e concretude

---

## Requisitos

### Validated (existem no código atual)

- ✓ Result pattern com três variantes: sem valor, com valor, com valor e erro tipado
- ✓ Error com Code, Message, Layer, ApplicationName, e Errors (key-value pairs)
- ✓ IError interface para erros customizados
- ✓ Extensão HTTP para converter HttpResponseMessage/IFlurlResponse em Result
- ✓ Suporte a serialização JSON
- ✓ Factory methods estáticos: Ok(), Fail()
- ✓ Testes unitários com xUnit

### Active (novos/refatorados)

- [ ] [R1] Migrar target framework para `.NET 10` em todos os projetos
- [ ] [R2] Substituir `Newtonsoft.Json` por `System.Text.Json` com zero alocação desnecessária
- [ ] [R3] Redesenhar `Result`, `Result<T>`, `Result<TValue, TError>` como structs imutáveis com `init`-only properties
- [ ] [R4] Redesenhar `Error` como struct imutável com factory methods
- [ ] [R5] Aplicar SRP: separar serialização, criação de erros, e core Result em módulos distintos
- [ ] [R6] Adicionar metadata HTTP ao Result quando originado de requisição: StatusCode, Headers, RawBody, ReasonPhrase
- [ ] [R7] Extensão Flurl moderna (Flurl 4.x) com retorno completo de informações HTTP
- [ ] [R8] Otimização extrema: usar `Span<T>`, `Memory<T>`, `ArrayPool<T>`, `ref struct` onde aplicável para minimizar GC
- [ ] [R9] Eliminar leituras duplicadas do body HTTP (bug atual no código)
- [ ] [R10] Thread safety: eliminar `Func<>` mutáveis, usar padrões imutáveis
- [ ] [R11] TDD obrigatório com ciclo Red-Green-Refactor, cobertura mínima 90%
- [ ] [R12] Manter retrocompatibilidade conceitual (nomes de métodos Ok/Fail) mas redesenhar API internamente
- [ ] [R13] Testes específicos para cenários HTTP: timeout, cancellation, erro de rede, erro de serialização
- [ ] [R14] Documentação XML completa em todos os tipos públicos
- [ ] [R15] Source generators ou analyzers para validar uso correto do Result em compile-time (se viável)

### Out of Scope

- Implementação de HTTP client — apenas a extensão de parsing da resposta
- Suporte a outros frameworks HTTP (Refit, RestSharp) — foco apenas em Flurl
- UI, CLI, ou aplicações de exemplo — biblioteca pura
- Suporte a serialização XML — apenas JSON
- Suporte a .NET Framework ou versões antigas — apenas .NET 10

---

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| System.Text.Json no lugar de Newtonsoft.Json | Performance, zero dependência externa, nativo do .NET 10, melhor para GC | Adotar STJ |
| Result como struct imutável | Evitar alocação heap, GC friendly, alinhado com perf goals | Struct com init-only |
| Metadata HTTP dentro do Result via extensão | Não poluir o core Result com HTTP info, mas disponibilizar quando necessário | `Result<T>.WithHttpInfo()` pattern |
| Redesenhar API mantendo Ok/Fail surface | Equilíbrio entre breaking change e familiaridade | Manter nomes, redesenhar internals |
| Flurl 4.x | Última versão, suporte .NET 8+, melhor performance | Upgrade |
| Structs imutáveis + Span/Memory/ArrayPool | Máxima eficiência de memória, mínimo GC pressure | Padrão de implementação |
| TDD Red-Green-Refactor estrito | Qualidade garantida, regressão prevenida, documentação viva | Cobertura mínima 90% |
| Todos os princípios do Righting Software | Refatoração completa, não parcial | Aplicação sistemática |

---

## Arquitetura Proposta (esboço)

```
Responses/
├── Core/
│   ├── Result.cs              — struct imutável base
│   ├── ResultOfT.cs           — struct com valor
│   ├── ResultOfTValueTError.cs — struct com valor + erro tipado
│   └── ResultFactory.cs       — Ok(), Fail() factories
├── Error/
│   ├── IError.cs              — interface
│   ├── Error.cs               — struct imutável padrão
│   ├── ErrorBuilder.cs        — builder para erros customizados
│   └── ErrorCollection.cs     — múltiplos erros (agregado)
├── Serialization/
│   ├── ResultJsonConverter.cs  — System.Text.Json converter
│   └── ErrorJsonConverter.cs   — System.Text.Json converter
└── Context/
    └── ResultContext.cs        — contexto imutável (Layer, AppName)

Responses.Http/
├── Http/
│   ├── HttpResponseInfo.cs    — struct com StatusCode, Headers, RawBody, ReasonPhrase
│   ├── HttpResultExtensions.cs — ReceiveResult() moderno
│   └── FlurlExtensions.cs     — Extensões específicas Flurl 4.x
└── Serialization/
    └── HttpErrorParser.cs     — parser otimizado de erro HTTP

test/
├── Core/ResultTests.cs
├── Core/ResultOfTTests.cs
├── Core/ResultOfTValueTErrorTests.cs
├── Error/ErrorTests.cs
├── Serialization/JsonTests.cs
├── Http/HttpResultTests.cs
├── Http/FlurlExtensionTests.cs
└── Perf/AllocationTests.cs    — testes de alocação/GC
```

---

## Métricas de Sucesso

1. **Zero warnings** no build
2. **90%+ cobertura** de testes
3. **Zero alocações desnecessárias** em hot path (validado com BenchmarkDotNet)
4. **Todos os testes passing** antes de cada commit
5. **API pública documentada** com XML docs
6. **Princípios do Righting Software** aplicados e documentados nas decisões de design

---

## Riscos

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Breaking change para consumidores existentes | Alto | Versionamento semântico (2.0.0), documentação de migração |
| Complexidade de Span/ref struct em APIs públicas | Médio | Abstrair com interfaces limpas, usar internamente |
| Flurl 4.x incompatibilidade | Baixo | Testar extensivamente, fallback documentado |
| TDD slows development | Baixo | Parallelização de testes, tooling eficiente |

---
*Last updated: 6 de abril de 2026 after initialization*
