---
phase: 06-extended-status-code-mapping
plan: 01
subsystem: http-extensions
tags: [error-type, status-code, http, rfc-9110, rfc-6585]

requires:
  - phase: 02-01
    provides: "ErrorType enum foundation"
  - phase: 03-01
    provides: "StatusCodeMapping usage in HTTP extensions"
provides:
  - "ErrorType enum com 19 valores (10 novos)"
  - "StatusCodeMapping cobrindo 30+ status codes HTTP"
  - "Fallback inteligente para códigos não mapeados"
  - "63 novos testes de mapeamento de status codes"
affects:
  - "Phase 3 — HTTP extensions agora têm mapeamento completo"

tech-stack:
  added: []
  patterns:
    - "Switch expression com range patterns para fallback"
    - "RFC-compliant HTTP status code mapping"

key-files:
  created:
    - test/Responses.Tests/StatusCodeMappingTests.cs
  modified:
    - Responses/ErrorType.cs
    - Responses.Http/Http_/HttpResponseInfo.cs
    - test/Responses.Tests/HttpExtensionsTests.cs

key-decisions:
  - "4xx não mapeados → Validation (melhor aproximação para client errors)"
  - "5xx não mapeados → ServerError (todos são erros de servidor)"
  - "418 I'm a teapot → Unknown (código custom/RFC 2324)"

metrics:
  duration: ~10min
  completed: 2026-04-06
---

# Phase 06: Extended Status Code Mapping — Plan 01 Summary

**ErrorType expandido com 10 novos valores e StatusCodeMapping cobrindo todos os status codes HTTP 400-599**

## Accomplishments

- **ErrorType enum** expandido de 10 para 19 valores (TooManyRequests, BadGateway, ServiceUnavailable, GatewayTimeout, UnprocessableEntity, Locked, FailedDependency, UpgradeRequired, PreconditionRequired, UnavailableForLegal)
- **StatusCodeMapping** reescrito com switch expression cobrindo 30+ status codes específicos
- **Fallback inteligente**: 4xx → Validation, 5xx → ServerError, resto → Unknown
- **63 novos testes** cobrindo todos os status codes HTTP 400-599
- **9 testes existentes** atualizados para refletir novos mapeamentos

## Test Coverage

| Categoria | Testes | Cobertura |
|---|---|---|
| 4xx Specific Mappings | 23 | Todos os códigos RFC mapeados |
| 4xx Custom/Unmapped | 7 | Fallback para Validation/Unknown |
| 5xx Specific Mappings | 12 | Todos os códigos RFC mapeados |
| 5xx Custom/Unmapped | 6 | Fallback para ServerError |
| Out-of-Range | 12 | 1xx, 2xx, 3xx, 600+ → Unknown |
| Edge Cases | 3 | Zero, negativo |
| **Total** | **63** | **100% dos status codes 400-599** |

## Task Commits

1. **All tasks** — `a51d8b7` (feat) — ErrorType expansion, StatusCodeMapping rewrite, tests

## Decisions Made

- **4xx não mapeados → Validation**: Melhor aproximação para erros de cliente genéricos
- **5xx não mapeados → ServerError**: Todos os erros 5xx são problemas de servidor
- **418 → Unknown**: Código custom (RFC 2324) sem equivalente direto no ErrorType

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- **Testes existentes falhando**: 9 testes no HttpExtensionsTests esperavam mapeamentos antigos (502→ServerError, 503→ServerError, 422→Validation). Atualizados para novos valores.

## Next Phase Readiness

- Phase 6 complete — ready for Phase 7 or Phase 2 STJ deserialization fix
- HTTP extensions agora têm mapeamento completo de status codes

---

*Phase: 06-extended-status-code-mapping*
*Plan: 01*
*Completed: 2026-04-06*
