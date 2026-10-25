# Design Principles

This project is governed by the following software engineering principles. All contributions must adhere to them.

## SOLID

| Principle | Application in This Project |
|---|---|
| **S** — Single Responsibility | Each class has exactly one job: controllers handle HTTP, services handle logic, repositories handle data. |
| **O** — Open/Closed | New patterns are added as new classes/modules; existing code is never modified to accommodate them. |
| **L** — Liskov Substitution | Concrete types (e.g., `InMemoryProductRepository`) are fully substitutable for their interfaces. |
| **I** — Interface Segregation | Interfaces are narrow and focused. No "god" interfaces. |
| **D** — Dependency Inversion | All cross-layer dependencies flow through interfaces registered in the DI container — never concrete types. |

## DRY (Don't Repeat Yourself)

- Shared routing prefix lives in `BaseApiController`, not duplicated per controller.
- Mapping logic is centralized in `MappingProfile`, not scattered in controllers or services.
- Repository contracts are centralized under `Repository/Interfaces/`; service contracts under `Services/Interfaces/`.

## KISS (Keep It Simple, Stupid)

- No premature abstraction. Patterns are introduced only when demonstrating a specific concept.
- In-memory storage (`ConcurrentDictionary`) keeps the infrastructure layer free of external dependencies.
- AutoMapper keeps mapping logic declarative and out of business logic.

## Pattern Implementation Guidelines

1. **Isolation** — Adding a pattern must not break existing functionality.
2. **Subfolder** — Each pattern gets its own subfolder under `Services/` (e.g., `Services/Factory/`, `Services/Strategy/`).
3. **Centralized Interfaces** — Shared interfaces always live in `Services/Interfaces/` — never inside a pattern subfolder.
4. **Documentation** — Every new pattern must be documented in `docs/PATTERNS_CATALOG.md` using the contribution template.
5. **Naming** — Follow `<PatternName>Service`, `<PatternName>Controller`, `I<PatternName>Service` naming conventions.
