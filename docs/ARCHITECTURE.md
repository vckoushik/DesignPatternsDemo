# Architecture

## Guiding Principle

Each layer communicates **downward only** via abstractions (interfaces). No layer may reference a layer above it. Concrete types are wired exclusively in `Program.cs`.

## Layer Diagram

```
┌─────────────────────────────────────────┐
│              Controllers                │  HTTP in/out · model binding · status codes
├─────────────────────────────────────────┤
│               Services                  │  Business logic · orchestration · mapping
├─────────────────────────────────────────┤
│             Repositories                │  Data access (CRUD) · no business logic
├─────────────────────────────────────────┤
│           Models / Mapping              │  Entities · DTOs · AutoMapper profiles
└─────────────────────────────────────────┘
```

## Dependency Rules

| Layer | May Depend On | Must NOT Depend On |
|---|---|---|
| Controllers | `Services/Interfaces/`, Client Models | Repository, Entities |
| Services | `Repository/Interfaces/`, all Models, Mapping | Controllers |
| Repositories | `Models/Data/` (Entities) | Services, Controllers |
| Models | *(nothing)* | Any application layer |

## Namespace Conventions

| Folder | Namespace |
|---|---|
| `Controllers/` | `DesignPatternsDemo.Controllers` |
| `Services/Interfaces/` | `DesignPatternsDemo.Services.Interfaces` |
| `Services/` | `DesignPatternsDemo.Services` |
| `Services/<Pattern>/` | `DesignPatternsDemo.Services.<Pattern>` |
| `Repository/Interfaces/` | `DesignPatternsDemo.Repository.Interfaces` |
| `Repository/InMemory/` | `DesignPatternsDemo.Repository.InMemory` |
| `Models/Data/` | `DesignPatternsDemo.Models.Data` |
| `Models/Client/Requests/` | `DesignPatternsDemo.Models.Client.Requests` |
| `Models/Client/Responses/` | `DesignPatternsDemo.Models.Client.Responses` |
| `Mapping/` | `DesignPatternsDemo.Mapping` |

## Dependency Injection Conventions

| Abstraction | Concrete | Lifetime | Reason |
|---|---|---|---|
| `IProductService` | `ProductService` | **Scoped** | One instance per HTTP request |
| `IProductRepository` | `InMemoryProductRepository` | **Singleton** | State must persist across requests |
| AutoMapper `IMapper` | `MappingProfile` assembly scan | **Singleton** | Stateless; safe to share |

## Adding a New Pattern

1. Add the service interface to `Services/Interfaces/I<Name>Service.cs`.
2. Add the implementation in `Services/<PatternName>/<Name>Service.cs`.
3. If data access is needed, add a repository interface to `Repository/Interfaces/` and an implementation under `Repository/InMemory/` or `Repository/<DataSource>/`.
4. Register all new types in `Program.cs`.
5. Add a controller in `Controllers/<Name>Controller.cs` if HTTP exposure is required.
6. Document the pattern in `docs/PATTERNS_CATALOG.md`.
