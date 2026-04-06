---
phase: 02-error-serialization
plan: 02
subsystem: serialization
tags: [system-text-json, serialization, source-generator, converters]

requires:
  - phase: 02-01
    provides: "Error com Type, Metadata; ErrorCollection; IError atualizado"
provides:
  - "ErrorJsonConverter e ErrorCollectionJsonConverter"
  - "ResultJsonConverter para Result, Result<T>, Result<TValue,TError>"
  - "ResultJsonContext source generator com tipos registrados"
  - "JsonIgnore em Error e Value properties"
affects:
  - "Phase 3 — HTTP extensions precisarão de serialização funcional"

tech-stack:
  added: [System.Text.Json (built-in .NET 10)]
  patterns:
    - "JsonIgnore em propriedades que lançam (Error, Value)"
    - "Errors serializado como array JSON"
    - "Source generator para tipos concretos conhecidos"

key-files:
  created:
    - Responses/Serialization/ErrorJsonConverter.cs
    - Responses/Serialization/ResultJsonConverter.cs
    - Responses/Serialization/ResultJsonContext.cs
    - test/Responses.Tests/StjSerializationTests.cs
  modified:
    - Responses/Result.cs (JsonIgnore attributes)

key-decisions:
  - "Custom converters são complexos com STJ — source generator com JsonIgnore é preferível"
  - "Error e Value têm JsonIgnore porque lançam em estado oposto"
  - "Errors é a propriedade serializável para multi-error"

patterns-established: []

metrics:
  duration: ~30min
  completed: 2026-04-06
---

# Phase 02: Error Model + Serialization — Plan 02 Summary

**STJ serialization infrastructure scaffolding (converters parciais)**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-04-06T15:00:00Z
- **Completed:** 2026-04-06T15:30:00Z
- **Tasks:** Partial (3/5 completed)

## Accomplishments

- **ErrorJsonConverter** — lê/escreve Error com Type e Metadata (precisa fix do reader state)
- **ResultJsonConverter** — serializers para Result, Result<T>, Result<TValue,TError>
- **ResultJsonContext** — source generator com tipos concretos registrados
- **JsonIgnore** — adicionado a Error e Value properties (não serializáveis)
- **Errors** — property serializável para multi-error

## Issues

- **Converters customizados**: "read too much or not enough" — STJ reader state não está sendo corretamente gerenciado
- **Deserialization**: Round-trip não funciona porque os converters não preservam o estado do reader corretamente
- **Source generator**: Usa serializeAsObject que ignora converters registrados

## Task Commits

1. **Infrastructure** — `530c8ac` (feat) — STJ converters, source generator, JsonIgnore attributes

## Decisions Made

- Custom converters são complexos demais para esta fase — a abordagem preferida é usar source generator puro com JsonIgnore e propriedades seguras (IsSuccess, ValueOrDefault, Errors)
- Para Phase 3 (HTTP), serialização funcional será necessária — esta é uma dependência

## Next Phase Readiness

- **Blocker para Phase 3**: HTTP extensions precisam de serialização funcional
- **Recomendação**: Refatorar STJ para usar source generator puro (sem custom converters), serializando apenas IsSuccess, ValueOrDefault, Errors

---

*Phase: 02-error-serialization*
*Plan: 02*
*Status: Partial — needs converter refactoring*
*Completed: 2026-04-06*
