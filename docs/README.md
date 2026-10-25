# DesignPatternsDemo

A .NET 10 Web API project serving as an **educational and practical playground** for implementing, studying, and comparing Software Design Patterns (Creational, Structural, Behavioral).

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An IDE: Visual Studio 2022+, Rider, or VS Code with the C# Dev Kit extension

### Run the API

```bash
cd DesignPatternsDemo
dotnet run
```

The API will be available at `http://localhost:5097` (HTTP) or `https://localhost:7097` (HTTPS).

### Explore the API

Open the OpenAPI spec at:
```
http://localhost:5097/openapi/v1.json
```

Use the included [`DesignPatternsDemo.http`](../DesignPatternsDemo/DesignPatternsDemo.http) file for end-to-end HTTP request testing in VS Code (REST Client extension) or Visual Studio.

## Project Structure

```
DesignPatternsDemo/
├── Controllers/         # API Controllers (BaseApiController + resource controllers)
├── Services/
│   ├── Interfaces/      # Centralized service contracts (all patterns share this folder)
│   └── *.cs             # Service implementations (+ pattern subfolders as added)
├── Repository/
│   ├── Interfaces/      # Centralized repository contracts
│   └── InMemory/        # Thread-safe ConcurrentDictionary store (no external dependencies)
├── Models/
│   ├── Client/
│   │   ├── Requests/    # Inbound DTOs with DataAnnotations validation
│   │   └── Responses/   # Outbound DTOs (sanitized, consumer-facing)
│   └── Data/            # Domain / persistence entities
├── Mapping/             # AutoMapper profiles
└── docs/                # Architecture, principles, and pattern catalog
```

## Documentation Index

| Document | Purpose |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layering rules, dependency flow, DI conventions |
| [PRINCIPLES.md](PRINCIPLES.md) | SOLID, DRY, KISS, and pattern guidelines |
| [PATTERNS_CATALOG.md](PATTERNS_CATALOG.md) | Index and contribution template for future patterns |

## Implemented Patterns

| Pattern | Category | Location |
|---|---|---|
| *(baseline — no patterns yet)* | — | — |

> Patterns are added as isolated service modules. Existing functionality is never disrupted.  
> See [PATTERNS_CATALOG.md](PATTERNS_CATALOG.md) for the contribution guide.
