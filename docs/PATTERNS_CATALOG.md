# Patterns Catalog

This document indexes all implemented design patterns and provides a contribution template for adding new ones.

## Implemented Patterns

| # | Pattern | Category | Service Interface | Implementation Path | Controller |
|---|---|---|---|---|---|
| — | *(none yet — baseline only)* | — | — | — | — |

---

## Categories

### Creational
> Concerned with object creation mechanisms, aiming to create objects in a manner suitable to the situation.

| Pattern | Status | Notes |
|---|---|---|
| Factory Method | 🔲 Planned | — |
| Abstract Factory | 🔲 Planned | — |
| Builder | 🔲 Planned | — |
| Prototype | 🔲 Planned | — |
| Singleton | 🔲 Planned | Illustrated via DI lifetime (`AddSingleton`) |

### Structural
> Concerned with composing objects and classes into larger structures while keeping them flexible and efficient.

| Pattern | Status | Notes |
|---|---|---|
| Adapter | 🔲 Planned | — |
| Bridge | 🔲 Planned | — |
| Composite | 🔲 Planned | — |
| Decorator | 🔲 Planned | — |
| Facade | 🔲 Planned | — |
| Flyweight | 🔲 Planned | — |
| Proxy | 🔲 Planned | — |

### Behavioral
> Concerned with communication, responsibility, and algorithms between objects.

| Pattern | Status | Notes |
|---|---|---|
| Chain of Responsibility | 🔲 Planned | — |
| Command | 🔲 Planned | — |
| Iterator | 🔲 Planned | — |
| Mediator | 🔲 Planned | — |
| Memento | 🔲 Planned | — |
| Observer | 🔲 Planned | — |
| State | 🔲 Planned | — |
| Strategy | 🔲 Planned | — |
| Template Method | 🔲 Planned | — |
| Visitor | 🔲 Planned | — |

---

## Contribution Template

When adding a new pattern, copy and complete the following block and add it above this section:

```markdown
### [Pattern Name] — [Category]

**Intent:** One-sentence description of the pattern's purpose.

**When to Use:**
- Bullet-point list of applicable scenarios.

**Implementation:**

| Artifact | Path |
|---|---|
| Interface | `Services/Interfaces/I[Name]Service.cs` |
| Service | `Services/[PatternFolder]/[Name]Service.cs` |
| Controller | `Controllers/[Name]Controller.cs` |
| Tests | *(future)* |

**Key Design Decisions:**
- Note any deliberate tradeoffs made in this implementation.

**References:**
- [GoF] *Design Patterns: Elements of Reusable Object-Oriented Software*, p. XX
- [refactoring.guru](https://refactoring.guru/design-patterns/[pattern-slug])
```
