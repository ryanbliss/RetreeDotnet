# Agent Policy

Retree is a .NET 2.1, C# 9.0 project. Core code in `/src`, tests in `/tests`, and benchmarks in `/benchmarks`.

## Reuse Preference

- Avoid changes that reduce existing reuse across shared schemas, contracts, or utilities unless explicitly requested.
- When simplifying outputs or logic, prefer preserving shared abstractions and references over duplicating equivalent inline structures.

## Testing policy

- Always test your changes to verify no regressions.
- There should not be any pre-existing failures, so don't blame anybody else.
- Fix root causes of failures. Do not cheat on tests.
- Always run benchmarks. Store benchmark results in a `BENCHMARKS.md` file in the project root, comparing past benchmarks to new benchmarks. Once you've put yours in and compared them to the most recent benchmarks, move the previous "Latest results" to replace "Prior results" and insert your benchmarks into "Latest results".
