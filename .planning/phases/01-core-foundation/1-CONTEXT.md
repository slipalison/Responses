# Phase 1: Core Foundation - Context

**Gathered:** 6 de abril de 2026
**Status:** Ready for planning

<vision>
## How This Should Work

A Phase 1 é sobre construir uma base sólida para railway-oriented programming em .NET. Quando você usa `Result.Ok()` ou `Result.Fail()`, o resultado deve parecer parte natural da linguagem — sem boilerplate, sem fricção.

A composição funcional (Map/Bind/Tap/Ensure) é o coração da experiência: encadeamento linear que para automaticamente no primeiro erro (short-circuit), transformando valores apenas em sucesso. Tanto a versão síncrona quanto async são cidadãos de primeira classe — não há "versão principal e adaptação async". LINQ query syntax (`from x in result select x`) funciona naturalmente via `SelectMany`.

A imutabilidade é não-negociável: `readonly struct` com `LayoutKind.Auto` garante zero-allocation e thread safety desde o início. Cada tipo (`Result`, `Result<T>`, `Result<TValue, TError>`) tem comportamento correto e previsível — `Value` lança exceção quando failed, `ValueOrDefault` retorna default sem lançar.

</vision>

<essential>
## What Must Be Nailed

- **Imutabilidade + struct design** — `readonly struct` correto, `LayoutKind.Auto`, zero-allocation, thread safety. Se a base estrutural estiver errada, todo o resto desmorona.
- **Map/Bind composição funcional** — Railway-oriented programming funcionando perfeitamente: short-circuit em failures, transformações em success, async-first com LINQ query syntax. Sem isso, não é uma biblioteca Result — é apenas um wrapper.

</essential>

<specifics>
## Specific Ideas

- Async-first: `MapAsync()`, `BindAsync()`, `TapAsync()`, `EnsureAsync()` são tão importantes quanto as versões síncronas
- LINQ support: `SelectMany` habilita `from x in result select x` — query syntax natural
- Pattern matching: `Match(success, failure)` e `Else(fallbackValue)` em todas as variantes (sync + async)
- Factory methods: `Ok()`, `Ok<T>(value)`, `Fail(code, message)`, `Fail(Error)`, `OkIf()`, `FailIf()`
- `Result<TValue, TError>` com constraint `where TError : IError` para erros customizados tipados

</specifics>

<notes>
## Additional Context

Usuário escolheu explicitamente "Railway-oriented programming first" sobre "API fluente e natural" — isso sinaliza que a identidade da biblioteca é composição funcional, não conveniência de API.

Usuário escolheu "Ambos igualmente críticos" para imutabilidade + composição — confirma que não há hierarquia de importância entre os dois pilares.

Phase 1 cobre requirements R1-R9, R15-R22 (16 requirements no total).

</notes>

---

*Phase: 01-core-foundation*
*Context gathered: 6 de abril de 2026*
