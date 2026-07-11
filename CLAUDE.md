# Responses

Biblioteca .NET 10 de Result pattern, publicada no NuGet. Performance é requisito: hot paths não alocam.

Regras detalhadas ficam em `.claude/rules/` (carregadas por escopo de arquivo). Regra de API pública: `.claude/rules/public-api.md`.

## Versionamento

- `Version` no `.csproj` segue SemVer. Breaking change em API pública = major bump obrigatório.
- Commits seguem Conventional Commits (`feat:`, `fix:`, `docs(NN):`, `ci:`).
