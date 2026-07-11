---
paths:
  - "Responses/**/*.cs"
  - "Responses.Http/**/*.cs"
---

# Regra: API pública completa

**Toda mudança em API pública deve chegar completa: XML doc + teste + prova de não-alocação.**

Uma mudança em `Responses/` ou `Responses.Http/` só está pronta quando:

1. **Build limpo** — `dotnet build` sem warnings (`TreatWarningsAsErrors` está ativo; warning = erro).
2. **XML doc em tudo que é público** — `GenerateDocumentationFile` está ativo; membro público sem `<summary>` quebra o build.
3. **Teste cobrindo o comportamento novo** — em `test/Responses.Tests`, seguindo o padrão existente (um arquivo por tema: `ResultTests`, `SerializationCoverageTests`, etc.).
4. **Zero alocações no hot path** — tipos de resultado são `readonly struct` com `[StructLayout(LayoutKind.Auto)]`. Mudança que introduza boxing, closure ou alocação em caminho de sucesso deve ser validada em `AllocationTests` e, se relevante, em `benchmarks/Responses.Benchmarks`.

## Convenções do código

- Factories estáticas (`Ok()`, `Fail(...)`) com `[DebuggerStepThrough]`.
- Acesso a `Error` em resultado de sucesso lança `InvalidOperationException` com mensagem de `ResultMessages` — nunca retorna default silencioso.
- Nullable habilitado em todos os projetos; não suprimir com `!` sem justificativa em comentário.
- Comentários e XML docs em inglês (código é publicado no NuGet/GitHub).
