# Design Pattern Challenges — E-Commerce API ("ShopSphere")

This file defines **23 practice problems**, one for each Gang of Four design pattern, all set in the same e-commerce API. Each problem gives the scenario, names the pattern to use, explains why, and lists what a correct implementation must do.

**No solutions are included.** The developer writes the implementation; a reviewer (human or AI agent) checks it against the problem's requirements and checklist.

---

## How to Use This File

### For the developer
1. Pick a problem by its ID (e.g. `P-07`).
2. Read the scenario and the "Requirements" section. The requirements are the contract.
3. Implement in any language (C# / ASP.NET Core assumed in wording, but not required).
4. Submit using the template at the bottom of this file.

### For the reviewing agent
1. Identify the problem by its **Problem ID** in the submission.
2. Review **only against that problem's** "Requirements" and "Reviewer Checklist". Do not penalize for things the problem does not ask for.
3. For each checklist item, mark **Pass / Partial / Fail** and quote the code that justifies the verdict.
4. Check that the submission actually solves the **stated pain points**, not just that the pattern's class names appear. A pattern used in name only is a Fail.
5. Check for the **common mistakes** listed under each problem.
6. Verify the implementation is **not secretly a different pattern** (e.g. a "Strategy" that is really a Factory, or a "Proxy" that is really a Decorator). Each problem's "Alternatives" section explains the boundaries.
7. End with: an overall verdict (Meets / Partially meets / Does not meet), the top 3 issues ranked by impact, and one concrete suggestion per issue. Do not rewrite the full solution for the developer.

### Shared domain (applies to all problems)
ShopSphere is a multi-seller marketplace API. Core concepts used throughout:

| Concept | Meaning |
|---|---|
| Product / SKU | A sellable item; a SKU is a specific variant (size, colour) |
| Cart / Line item | A customer's in-progress selection |
| Order | A placed purchase, with a lifecycle |
| Seller | A third-party merchant who lists products |
| Payment provider | External service that charges the customer (Stripe, PayPal, etc.) |
| Carrier | External shipping company (FedEx, UPS, local courier) |
| Region | Market the store operates in (US, EU, IN) with its own rules |

---

## Index

| ID | Pattern | Category | Problem |
|---|---|---|---|
| P-01 | Singleton | Creational | Currency exchange rate cache |
| P-02 | Factory Method | Creational | Payment provider selection |
| P-03 | Abstract Factory | Creational | Region-specific compliance services |
| P-04 | Builder | Creational | Product search query construction |
| P-05 | Prototype | Creational | Duplicating seller product listings |
| P-06 | Adapter | Structural | Incompatible shipping carrier APIs |
| P-07 | Bridge | Structural | Notification types × delivery channels |
| P-08 | Composite | Structural | Nested product bundles |
| P-09 | Decorator | Structural | Caching, logging, retry on catalog service |
| P-10 | Facade | Structural | Fat checkout controller |
| P-11 | Flyweight | Structural | Memory blow-up from SKU variant data |
| P-12 | Proxy | Structural | Seller data isolation in a shared order service |
| P-13 | Chain of Responsibility | Behavioral | Pre-order validation and fraud checks |
| P-14 | Command | Behavioral | Admin catalog changes with undo and audit |
| P-15 | Interpreter | Behavioral | Marketing promotion rule language |
| P-16 | Iterator | Behavioral | Traversing paginated order exports |
| P-17 | Mediator | Behavioral | Interdependent cart features |
| P-18 | Memento | Behavioral | Restoring checkout state after payment failure |
| P-19 | Observer | Behavioral | Reactions to "order placed" |
| P-20 | State | Behavioral | Order lifecycle transitions |
| P-21 | Strategy | Behavioral | Shipping cost calculation |
| P-22 | Template Method | Behavioral | Importing orders from external marketplaces |
| P-23 | Visitor | Behavioral | Operations over different line item types |

---

# Creational Patterns

---

## P-01 · Singleton — Currency Exchange Rate Cache

### 1. Problem
ShopSphere shows prices in the customer's local currency. Exchange rates come from an external provider that charges per request and allows 100 calls per hour. Today, every `PriceService` instance creates its own `ExchangeRateClient`, which downloads the full rate table on construction.

Symptoms:
- Under load, hundreds of instances are created per minute and the provider starts returning `429 Too Many Requests`.
- Different requests show slightly different converted prices for the same product because each instance loaded rates at a different time.
- The monthly bill from the rate provider is 40× the expected amount.

### 2. Solution Pattern
**Singleton**

### 3. Explanation
**What is actually wrong:** A resource that should exist once per application (a shared, expensive rate table) is being created once per use. There is no single source of truth, so the data is inconsistent and the cost multiplies with traffic.

**How the pattern fixes it:** Exactly one rate cache exists for the whole process. Every consumer reads from it, so rates are fetched once per refresh window and every request sees the same numbers.

**What enables it:** Controlled construction — callers cannot create their own instance; they get the shared one. Initialization must be lazy and thread-safe so two concurrent first requests do not both trigger a download.

**Alternatives:**
- **DI container singleton lifetime** (e.g. `AddSingleton` in ASP.NET Core). This is the preferred modern approach: it gives single-instance behaviour while staying testable. An implementation using this is acceptable if it explains that the container enforces the single instance.
- **`IMemoryCache` / distributed cache (Redis)**: needed if the API runs on multiple servers and rates must be consistent across them. A process-level Singleton only guarantees one instance per process.

### Requirements
1. Only one rate cache instance exists per process, no matter how many requests arrive.
2. First-time initialization is thread-safe (concurrent requests cause exactly one fetch).
3. Rates refresh on a schedule (e.g. every 60 minutes) without blocking readers for long.
4. Consumers depend on an interface/abstraction so they can be unit tested with a fake.
5. If a refresh fails, the last good rates continue to be served.

### Reviewer Checklist
- [ ] Single instance is guaranteed (private constructor + static access, or DI singleton registration).
- [ ] Lazy, thread-safe initialization (e.g. `Lazy<T>`, double-checked locking done correctly, or container-managed).
- [ ] Refresh logic does not create a new instance.
- [ ] Shared mutable state is safe for concurrent reads during refresh (atomic swap of the rate table, or a lock).
- [ ] Consumers are testable (not hard-wired to a static `Instance` everywhere).
- [ ] Failed refresh keeps stale-but-valid data.

**Common mistakes:** non-thread-safe `if (instance == null)` check; using a Singleton for per-request data; making the Singleton depend on a scoped service (e.g. a `DbContext`), which causes captive-dependency bugs.

---

## P-02 · Factory Method — Payment Provider Selection

### 1. Problem
Checkout supports Stripe, PayPal, and Cash on Delivery. The `CheckoutService` contains:

```
if (method == "stripe") { ... new StripeClient(...) ... }
else if (method == "paypal") { ... new PayPalClient(...) ... }
else if (method == "cod") { ... }
```

The business wants to add Apple Pay and UPI next quarter.

Symptoms:
- Every new payment method requires editing and retesting `CheckoutService`.
- `CheckoutService` knows constructor details, API keys and setup steps for every provider.
- Unit testing checkout requires real provider configuration.

### 2. Solution Pattern
**Factory Method**

### 3. Explanation
**What is actually wrong:** The code that *uses* a payment processor is also responsible for *deciding which concrete class to build and how to build it*. Creation logic and business logic are tangled, which violates the Open/Closed Principle.

**How the pattern fixes it:** Creation is moved behind a factory method that returns a common `IPaymentProcessor` abstraction. Checkout asks for "a processor for this method" and only calls the common interface (`Authorize`, `Capture`, `Refund`). Adding a provider means adding a new creator/product pair, not editing checkout.

**What enables it:** Polymorphism over a shared product interface, plus a creator whose subclasses (or registered creators) decide which concrete product to instantiate.

**Alternatives:**
- **Keyed DI services** (`AddKeyedScoped<IPaymentProcessor, StripeProcessor>("stripe")`): a container-driven factory; acceptable if the selection logic is still outside checkout.
- **Simple Factory** (one static method with a switch): moves the switch out of checkout but still requires editing the factory for each new provider. Acceptable as a stepping stone, but the reviewer should note it is not the full Factory Method.
- **Abstract Factory**: only needed when each provider brings a *family* of related objects (see P-03).

### Requirements
1. `CheckoutService` has no reference to any concrete payment class.
2. All processors implement one shared interface.
3. Adding a new payment method requires no changes to `CheckoutService`.
4. An unknown payment method fails with a clear, handled error (not a null reference).
5. Provider-specific configuration (keys, endpoints) lives with the provider, not in checkout.

### Reviewer Checklist
- [ ] No `new StripeClient` (or equivalent) inside checkout logic.
- [ ] Shared processor interface exists and is used by checkout.
- [ ] Creation logic is isolated in a creator / factory method.
- [ ] Demonstrated or explained how a new provider (e.g. Apple Pay) would be added without modifying checkout.
- [ ] Unknown method handled explicitly.
- [ ] Checkout can be unit tested with a fake processor.

**Common mistakes:** moving the `if/else` into a factory and calling it done without discussing extensibility; factory returning concrete types instead of the interface.

---

## P-03 · Abstract Factory — Region-Specific Compliance Services

### 1. Problem
ShopSphere operates in the US, EU, and India. Each region needs its own set of related services at checkout:

| Service | US | EU | IN |
|---|---|---|---|
| Tax calculator | State sales tax | VAT | GST |
| Address validator | ZIP + state | Postcode + country | PIN code |
| Invoice formatter | US format | EU VAT invoice | GST invoice |

Symptoms:
- A bug shipped where an EU order used the EU tax calculator but the US invoice formatter, producing a legally invalid invoice.
- Region `switch` statements are repeated in five different services.
- Launching a new region (UK) means hunting down every switch.

### 2. Solution Pattern
**Abstract Factory**

### 3. Explanation
**What is actually wrong:** These services form a *family* that must be used together. Choosing each one independently allows mismatched combinations, and the region decision is scattered.

**How the pattern fixes it:** One factory per region creates the entire family. Checkout picks the region factory once, then gets the tax calculator, address validator and invoice formatter from that same factory, so the combination is always consistent.

**What enables it:** An abstract factory interface with one creation method per product type, and one concrete factory per region. Clients depend only on the abstract factory and the abstract products.

**Alternatives:**
- **Multiple separate Factory Methods**: works for independent products but does not guarantee the family stays consistent.
- **DI with a region-scoped container / configuration**: valid in larger systems, but the "one place decides the family" principle must still hold.

### Requirements
1. One abstract factory interface exposing creation of all three region services.
2. One concrete factory per region (at least US, EU, IN).
3. Checkout selects the factory **once** based on the order's region and gets all services from it.
4. It must be impossible (by design) for checkout to combine services from two regions.
5. Adding UK requires adding new classes, not editing existing region factories or checkout.

### Reviewer Checklist
- [ ] Abstract factory and abstract product interfaces exist.
- [ ] Each concrete factory returns a consistent family.
- [ ] Region selection happens in exactly one place.
- [ ] No region `switch` left in tax, address or invoice consumers.
- [ ] Extension to a new region is demonstrated or clearly explained.

**Common mistakes:** a single factory with `CreateTaxCalculator(region)` methods (that is just three Factory Methods with switches); factories that return concrete types.

---

## P-04 · Builder — Product Search Query Construction

### 1. Problem
`GET /products/search` supports: keyword, category, brand list, min/max price, minimum rating, in-stock only, seller ID, sort order, page and page size. The search service currently has a method with 12 parameters, most of them nullable, and several overloads.

Symptoms:
- Calls look like `Search("shoes", null, null, 20, null, 4, true, null, "price_asc", 1, 50, null)` and parameters get swapped by mistake.
- Invalid combinations reach the database (`minPrice > maxPrice`, page size 10,000, sort by a field that does not exist).
- Different callers (storefront, mobile app, admin) build queries inconsistently.

### 2. Solution Pattern
**Builder**

### 3. Explanation
**What is actually wrong:** The object being created has many optional parts and rules about how they combine. Constructors/parameter lists cannot express that clearly or enforce the rules.

**How the pattern fixes it:** A builder lets callers set only the parts they need with readable, named steps, then produces the final query object in one `Build()` step that validates everything. The resulting query object is immutable and always valid.

**What enables it:** Separating step-by-step construction (mutable builder) from the final product (immutable, validated object), with validation centralized at build time.

**Alternatives:**
- **C# object initializers / records with `init` properties**: readable, but validation is weaker and spread out.
- **Specification pattern**: good for composable filter logic; can be combined with a builder.

### Requirements
1. Fluent, readable construction of a search query (named steps, chainable).
2. Final query object is immutable once built.
3. `Build()` rejects invalid states with clear error messages (price range, page size limit, allowed sort fields).
4. Sensible defaults (page 1, page size 20, default sort) when not specified.
5. The search service accepts only the built query object, not raw parameters.

### Reviewer Checklist
- [ ] Builder separate from the product object.
- [ ] Product is immutable (no public setters after build).
- [ ] All validation rules from the problem are enforced in one place.
- [ ] Defaults applied.
- [ ] Old multi-parameter method removed or no longer used.
- [ ] Builder can be reused or reset safely (or explicitly documented as single-use).

**Common mistakes:** builder that returns the same mutable object it is still modifying; validation only in the API controller; builder with no validation at all.

---

## P-05 · Prototype — Duplicating Seller Product Listings

### 1. Problem
Sellers often list near-identical products (a T-shirt in 12 colours, a phone case for 30 models). Each listing has nested data: specifications, image gallery, variant matrix, SEO metadata, shipping dimensions. Sellers asked for a "Duplicate listing" button: `POST /products/{id}/duplicate`.

Symptoms of the first attempt:
- Editing the duplicate's specifications also changed the original (shared references).
- The duplicate kept the original's product ID, SKU and review count.
- Creation code that rebuilds a listing field by field breaks every time a new field is added to the product model.

### 2. Solution Pattern
**Prototype**

### 3. Explanation
**What is actually wrong:** Creating a new object by copying an existing complex one is being done by outside code that does not know (and should not need to know) the object's full internal structure, and it is copying references instead of values.

**How the pattern fixes it:** The product knows how to clone itself. The clone is a correct deep copy of what should be copied and a fresh value for what must not be copied (identity, timestamps, reviews, sales counts).

**What enables it:** A clone operation owned by the object, with explicit decisions about deep vs shallow copy for each nested part.

**Alternatives:**
- **Serialization round-trip** (serialize and deserialize): quick deep copy but slow and copies fields that should be reset.
- **C# `record` with `with` expressions**: shallow copy only; nested collections are still shared.
- **Builder seeded from an existing product**: valid when a cloned object needs heavy modification.

### Requirements
1. Product exposes a clone operation; callers do not copy fields manually.
2. Nested collections (specs, images, variants) are deep-copied; modifying the clone never affects the original.
3. Identity and history fields are reset: ID, SKU, created/updated dates, review count, sales count, status (set to Draft).
4. Clone title is marked (e.g. "Copy of …") so sellers can tell them apart.
5. Adding a new simple field to the product does not require changing the duplicate endpoint.

### Reviewer Checklist
- [ ] Clone is implemented on the product (or its prototype interface), not in the controller.
- [ ] Deep copy of every mutable nested object, verified by reasoning or a test.
- [ ] All listed fields reset.
- [ ] Immutable shared data (e.g. category reference) may be shared intentionally, and this is explained.
- [ ] A test proves that mutating the clone leaves the original unchanged.

**Common mistakes:** `MemberwiseClone()` alone (shallow); forgetting to reset SKU, creating a duplicate key error at save time.

---

# Structural Patterns

---

## P-06 · Adapter — Incompatible Shipping Carrier APIs

### 1. Problem
ShopSphere quotes shipping rates and creates labels through three carriers:
- **FedEx**: SOAP/XML API, weights in pounds, returns rates in cents.
- **UPS**: REST/JSON API, weights in kilograms, returns rates as decimal strings.
- **LocalCourier**: a vendor SDK with methods like `GetQuote(int grams, string pin)` that throws its own exception types.

The internal code expects one interface: `IShippingCarrier` with `GetRateAsync(shipment)` and `CreateLabelAsync(shipment)` using kilograms and `decimal` money.

Symptoms:
- Carrier-specific conversion code is copy-pasted across the rate endpoint, the label endpoint and the returns flow.
- A unit-conversion bug charged customers for pounds as if they were kilograms.
- Vendor exceptions leak to API responses.

### 2. Solution Pattern
**Adapter**

### 3. Explanation
**What is actually wrong:** Each external carrier has an interface you cannot change, and it does not match what your code expects. The translation is being done everywhere instead of in one place.

**How the pattern fixes it:** One adapter per carrier implements `IShippingCarrier` and wraps the carrier's real client. All translation (units, money format, request/response shapes, exceptions) happens inside the adapter. The rest of the system only sees `IShippingCarrier`.

**What enables it:** Composition — the adapter holds a reference to the incompatible object (the adaptee) and implements the target interface by delegating and converting.

**Alternatives:**
- **Anti-Corruption Layer** (DDD): a larger version of the same idea for a whole external bounded context.
- **Facade**: simplifies a complex subsystem but does not aim to match an existing target interface (see P-10).

### Requirements
1. One `IShippingCarrier` target interface using internal units (kg, `decimal` currency).
2. One adapter per carrier wrapping that carrier's client.
3. All unit and format conversions live inside adapters only.
4. Vendor-specific exceptions are translated to an internal exception type or result.
5. Business code never references FedEx/UPS/LocalCourier types.

### Reviewer Checklist
- [ ] Target interface defined in internal terms.
- [ ] Adapters use composition (wrap the vendor client) rather than modifying it.
- [ ] Conversions are correct (lb↔kg, cents↔decimal, string↔decimal with culture-safe parsing).
- [ ] Exception translation present.
- [ ] No vendor types outside adapters.
- [ ] Adapters can be tested with a mocked vendor client.

**Common mistakes:** adapter that adds business rules (e.g. free-shipping logic) — that belongs elsewhere; culture-dependent decimal parsing.

---

## P-07 · Bridge — Notification Types × Delivery Channels

### 1. Problem
ShopSphere sends notifications: Order Confirmation, Shipping Update, Price Drop Alert, Password Reset. Each can be delivered by Email, SMS, or Push. The team created a class per combination: `OrderConfirmationEmail`, `OrderConfirmationSms`, `ShippingUpdatePush`, and so on.

Symptoms:
- 4 types × 3 channels = 12 classes. Adding WhatsApp makes 16; adding "Return Approved" makes 20.
- Changing the SMS provider required editing 4 classes.
- Message content logic is duplicated across channel variants.

### 2. Solution Pattern
**Bridge**

### 3. Explanation
**What is actually wrong:** Two independent dimensions of variation (*what* the message is and *how* it is delivered) are combined through inheritance, causing a class explosion (M × N).

**How the pattern fixes it:** Split into two hierarchies: notifications (abstraction) and channels (implementation). A notification holds a reference to a channel and asks it to deliver. Growth becomes M + N instead of M × N.

**What enables it:** Composition across the two hierarchies — the abstraction holds a reference to the implementor interface, so either side can vary and be extended independently.

**Alternatives:**
- **Strategy for the channel**: structurally similar; Bridge is the right name when *both* sides form hierarchies that evolve independently.
- **Template engine + channel dispatcher**: a common real-world equivalent; acceptable if both dimensions are clearly separated.

### Requirements
1. A notification hierarchy responsible for content (subject, body, data).
2. A channel hierarchy responsible for delivery only.
3. Any notification can be sent through any channel without a new class.
4. Adding WhatsApp requires one new class; adding "Return Approved" requires one new class.
5. Channels handle channel limits (e.g. SMS length) without the notification knowing provider details.

### Reviewer Checklist
- [ ] Two separate hierarchies exist.
- [ ] Notification holds a channel via composition (constructor or method injection).
- [ ] No combined classes like `XxxEmail` remain.
- [ ] Extension in both directions is demonstrated or explained.
- [ ] Content logic is not duplicated per channel.

**Common mistakes:** channel classes that contain `if (notification is OrderConfirmation)` checks — that re-couples the two sides.

---

## P-08 · Composite — Nested Product Bundles

### 1. Problem
ShopSphere sells bundles. A "Home Office Bundle" contains a desk, a chair and a "Tech Pack" bundle, which itself contains a keyboard, a mouse and a "Cable Kit" bundle. Bundles can have their own discount (e.g. 10% off the bundle total). Cart, pricing, shipping-weight and stock-availability code must handle both single products and bundles.

Symptoms:
- Code is full of `if (item is Bundle) { foreach (child) { if (child is Bundle) ... } }` and only supports two levels deep.
- Weight calculation ignores items nested three levels down.
- Stock check says a bundle is available even when a nested item is out of stock.

### 2. Solution Pattern
**Composite**

### 3. Explanation
**What is actually wrong:** The data is a tree (bundles contain products and other bundles), but client code treats leaves and containers differently and has to know the depth.

**How the pattern fixes it:** Products and bundles share one interface (`GetPrice`, `GetWeight`, `IsInStock`). A bundle implements each operation by delegating to its children and combining results. Clients treat a single product and a deep bundle the same way.

**What enables it:** A common component interface plus recursive delegation from composite to children.

**Alternatives:**
- **Flattening the bundle at save time**: simpler queries but loses structure and bundle-level discounts.
- **Visitor over the tree** (see P-23): useful when many new operations must be added to the tree.

### Requirements
1. One component interface implemented by both single products and bundles.
2. Bundles can contain products and other bundles to any depth.
3. Price includes bundle-level discounts at each level correctly.
4. Weight sums all leaves at any depth.
5. A bundle is in stock only if every child is in stock (with quantities considered).
6. Cycles (a bundle containing itself, directly or indirectly) are prevented.

### Reviewer Checklist
- [ ] Uniform interface; client code has no type checks for bundle vs product.
- [ ] Recursion works for 3+ levels (test or clear reasoning).
- [ ] Discount application order is defined and correct.
- [ ] Quantity of a child within a bundle is handled.
- [ ] Cycle prevention exists.

**Common mistakes:** leaf classes forced to implement `AddChild` that throws (acceptable only if justified); discounts applied twice at nested levels.

---

## P-09 · Decorator — Caching, Logging and Retry on the Catalog Service

### 1. Problem
`IProductCatalogService.GetProductAsync(id)` reads from a database that occasionally times out. The team wants to add:
- caching (product pages are read 1,000× more than written),
- retry with backoff on transient failures,
- timing/metrics logging.

The first attempt added all of this inside `ProductCatalogService`.

Symptoms:
- The service class grew from 80 to 400 lines and mixes data access with infrastructure.
- Admin endpoints need the uncached version but cannot get it without a flag parameter.
- Unit tests for data mapping now need a cache and a logger.

### 2. Solution Pattern
**Decorator**

### 3. Explanation
**What is actually wrong:** Cross-cutting behaviours are being added by modifying the core class, so it violates Single Responsibility and the behaviours cannot be turned on or off per consumer.

**How the pattern fixes it:** Each behaviour becomes a wrapper class that implements the same interface, holds a reference to the next `IProductCatalogService`, and adds its behaviour before/after delegating. Wrappers stack in any order, and different consumers can receive different stacks.

**What enables it:** Same interface for wrapper and wrapped object, plus composition — the decorator is indistinguishable from the real service to callers.

**Alternatives:**
- **ASP.NET Core middleware / MediatR pipeline behaviours**: same idea at the request level rather than the service level.
- **AOP / interceptors** (e.g. Castle DynamicProxy).
- **Polly** for retry: acceptable *inside* a retry decorator.
- **Proxy**: similar structure but the intent is access control or lazy loading, not adding behaviour (see P-12).

### Requirements
1. Core service contains only data-access logic.
2. Separate decorators for caching, retry and logging/metrics, each implementing `IProductCatalogService`.
3. Decorators can be stacked in any order and registered via DI.
4. Admin consumers can receive an uncached stack without changing the core class.
5. Cache is invalidated when a product is updated.
6. Retry only applies to transient errors, not to "not found" or validation errors.

### Reviewer Checklist
- [ ] Each decorator has one responsibility.
- [ ] Decorators delegate to the inner instance for every interface member (none silently skipped).
- [ ] Order of decoration is deliberate and explained (e.g. cache outside retry).
- [ ] Cache invalidation on write.
- [ ] Retry distinguishes transient vs permanent errors.
- [ ] DI wiring shows how the stack is assembled.

**Common mistakes:** decorator that inherits from the concrete service instead of wrapping the interface; cache stampede not considered (bonus point if addressed).

---

## P-10 · Facade — Fat Checkout Controller

### 1. Problem
`POST /checkout` in `CheckoutController` currently does all of this directly: validates the cart, reserves inventory, applies coupons, calculates tax, calculates shipping, charges payment, creates the order, releases inventory on failure, sends the confirmation email, and records analytics. The controller injects 11 services.

Symptoms:
- The controller method is 250 lines.
- The mobile API and the "Buy Now" endpoint copied the same sequence with small differences, and they have drifted apart.
- On payment failure, one copy forgets to release reserved inventory.

### 2. Solution Pattern
**Facade**

### 3. Explanation
**What is actually wrong:** Clients must understand and coordinate a complex subsystem of many services and their correct ordering. That knowledge is duplicated and inconsistent.

**How the pattern fixes it:** A single checkout facade exposes a simple operation such as `PlaceOrderAsync(request)`. It knows the correct sequence and compensation steps. Controllers depend on the facade only.

**What enables it:** A higher-level interface that delegates to subsystem services; subsystem services stay unaware of the facade.

**Alternatives:**
- **Application service / use-case handler** (Clean Architecture): the same idea under a different name.
- **Saga / orchestration**: needed when the steps span separate microservices with their own databases.
- **Mediator** (P-17) coordinates peers that talk to each other; Facade simply simplifies access to a subsystem.

### Requirements
1. Controllers call one facade method; they no longer inject the 11 services.
2. The facade owns the step order.
3. Failure at any step triggers the correct compensation (release inventory, void payment authorization, etc.).
4. All three entry points (web, mobile, Buy Now) use the same facade.
5. The facade returns a clear result (success with order ID, or a typed failure reason).
6. Subsystem services remain independently usable and do not depend on the facade.

### Reviewer Checklist
- [ ] Controller is thin.
- [ ] Compensation logic is complete for every failure point.
- [ ] Duplication across entry points removed.
- [ ] Facade does not become a "god object" containing tax, shipping or payment logic itself; it only coordinates.
- [ ] Result type communicates failure reasons.

**Common mistakes:** moving all 250 lines into the facade unchanged (relocation, not simplification); swallowing exceptions in compensation.

---

## P-11 · Flyweight — Memory Blow-Up from SKU Variant Data

### 1. Problem
To serve fast faceted search, ShopSphere keeps an in-memory catalog of 4 million SKUs. Each SKU object stores its own copy of: brand name and logo URL, category path and attribute schema, size chart (often 2–5 KB of text), colour definition (name, hex, swatch URL), and seller return policy text.

Most of this data is identical across thousands of SKUs.

Symptoms:
- The search service uses 18 GB of RAM and crashes with out-of-memory on smaller nodes.
- Garbage collection pauses cause latency spikes.
- Updating a brand's logo URL requires updating hundreds of thousands of objects.

### 2. Solution Pattern
**Flyweight**

### 3. Explanation
**What is actually wrong:** A huge number of fine-grained objects each duplicate data that is really shared. Memory scales with SKU count instead of with the number of unique shared values.

**How the pattern fixes it:** Split each SKU's data into **intrinsic** state (shared, immutable: brand, size chart, colour definition, category schema) and **extrinsic** state (unique per SKU: SKU code, price, stock). Shared parts are stored once in a flyweight factory/pool and referenced by many SKUs.

**What enables it:** Immutability of the shared objects (so sharing is safe) and a factory that returns an existing instance for equal keys instead of creating a new one.

**Alternatives:**
- **String interning**: covers only strings.
- **Normalizing to IDs and looking up from a dictionary**: the same idea, less formal.
- **Moving search to a dedicated engine** (Elasticsearch, Azure AI Search): a different architectural choice.

### Requirements
1. Clear split of intrinsic vs extrinsic state, documented for each field.
2. Flyweight objects are immutable.
3. A factory/pool returns the same instance for the same key.
4. The pool is thread-safe for concurrent loading.
5. Updating shared data (e.g. a brand logo) affects all SKUs that use it.
6. Provide an estimate or measurement of memory before vs after.

### Reviewer Checklist
- [ ] Intrinsic/extrinsic split is correct (nothing unique stored in a flyweight).
- [ ] Flyweights immutable.
- [ ] Factory deduplicates correctly (proper equality/key).
- [ ] Thread-safe pool.
- [ ] Update strategy for shared data is sensible (replace instance in pool vs mutate).
- [ ] Memory reasoning provided.

**Common mistakes:** mutable flyweights (one SKU's change affects others unexpectedly); pool that grows forever with near-duplicate keys.

---

## P-12 · Proxy — Seller Data Isolation in a Shared Order Service

### 1. Problem
ShopSphere has an internal `IOrderService` used by admins and by internal jobs. A new Seller Portal API must reuse it so sellers can view and update *their own* orders. The real `OrderService` loads heavy dependencies (reporting connections, warehouse client) on construction.

Symptoms:
- During testing, a seller fetched another seller's order by changing the ID in the URL.
- Ownership checks were added in three controllers, and a fourth endpoint forgot them.
- Constructing `OrderService` for every seller request is slow, even for requests rejected by authorization.

### 2. Solution Pattern
**Proxy** (protection proxy, combined with virtual/lazy proxy)

### 3. Explanation
**What is actually wrong:** Access control for a sensitive object is enforced by every caller instead of at the object boundary, so a single forgotten check causes a data leak. The expensive object is created even when access will be denied.

**How the pattern fixes it:** A proxy implements `IOrderService`, stands in front of the real service, checks that the current seller owns the requested orders, and only then forwards the call. The real service is created lazily on the first permitted call.

**What enables it:** The proxy has the same interface as the real subject, so callers cannot tell the difference, and it controls whether and when the real subject is reached.

**Alternatives:**
- **Decorator**: same structure; intent differs. Proxy *controls access* to the subject; Decorator *adds behaviour*. The reviewer should check that this proxy's main job is access control and lazy creation.
- **Authorization policies / resource-based authorization** in ASP.NET Core: valid, but must be enforced centrally rather than per controller.
- **Row-level security in SQL Server**: database-level enforcement; strong defence in depth.

### Requirements
1. Seller-facing code receives the proxy, never the real `OrderService`.
2. Every method that reads or changes orders checks ownership (including list/search methods, which must filter).
3. Unauthorized access returns a "not found"-style result (do not reveal that the order exists).
4. Real service is created only when a call is permitted.
5. Admin and internal jobs still use the real service without the seller restriction.

### Reviewer Checklist
- [ ] Proxy implements the full interface; no method forwards without a check.
- [ ] List/search results are filtered, not only single-item reads.
- [ ] No information leak through error messages.
- [ ] Lazy creation implemented.
- [ ] DI registration routes seller endpoints to the proxy only.
- [ ] Tests cover cross-seller access attempts.

**Common mistakes:** checking ownership after the data has been fetched and partly returned; forgetting bulk operations.

---

# Behavioral Patterns

---

## P-13 · Chain of Responsibility — Pre-Order Validation and Fraud Checks

### 1. Problem
Before an order is placed, it must pass these checks: cart not empty, shipping address deliverable, all items in stock, customer not blocked, payment amount within the customer's limit, fraud score below threshold (calls an external service, slow and costly), and region-restricted items (e.g. no lithium batteries by air to certain countries).

Today these are one long method with nested `if` statements.

Symptoms:
- The expensive fraud check runs even when the cart is empty.
- Business wants to reorder checks and disable some per region, which requires a code deployment.
- Two checks were accidentally removed during a merge without anyone noticing.

### 2. Solution Pattern
**Chain of Responsibility**

### 3. Explanation
**What is actually wrong:** A request must pass through a sequence of independent handlers, but the sequence is hard-coded in one place, so it cannot be reordered, configured or extended safely.

**How the pattern fixes it:** Each check becomes a handler with one responsibility. Handlers are linked in a chain; each either rejects the order (stopping the chain) or passes it on. The chain is assembled from configuration, so order and membership can change without editing the handlers.

**What enables it:** A common handler interface with a reference to the next handler (or a pipeline runner), so senders do not know which handler will stop the request.

**Alternatives:**
- **Pipeline / middleware pattern** (ASP.NET Core middleware, MediatR behaviours): a modern form of the same idea.
- **FluentValidation**: good for simple field rules but not for ordered, short-circuiting, I/O-heavy checks.
- **Collect-all-errors validation**: when the user should see all failures at once; the problem should state which mode is used (here: stop on first failure, but cheap checks first).

### Requirements
1. Each check is its own handler class.
2. A failing handler stops the chain and returns a specific, user-safe reason.
3. Cheap checks run before expensive ones (fraud check last or near last).
4. Chain order and enabled handlers are configurable per region without code changes to handlers.
5. Adding a new check requires one new class plus registration.
6. Handlers are independently unit-testable.

### Reviewer Checklist
- [ ] Single-responsibility handlers.
- [ ] Short-circuit on failure works.
- [ ] Ordering is configurable and cheap-first by default.
- [ ] Clear failure result type (code + message).
- [ ] No handler depends on another handler's internal state (shared context object is acceptable).
- [ ] Missing/empty chain is handled.

**Common mistakes:** each handler calling `next` even after failing; a "chain" that is a hard-coded list of method calls.

---

## P-14 · Command — Admin Catalog Changes with Undo and Audit

### 1. Problem
The admin panel allows: change price, change stock level, publish/unpublish product, apply bulk category discount (can touch 50,000 products). Requirements from operations:
- Every change must be audit-logged with who, what, when, and before/after values.
- Admins must be able to undo their last change (e.g. a mistaken 90% discount).
- Bulk changes must run in the background, not inside the HTTP request.

Symptoms:
- Each endpoint writes directly to the database; there is no consistent audit or undo.
- A bulk discount timed out after 100 seconds, leaving half the products discounted.
- A wrong price went live for 2 hours because reverting meant manually finding old values.

### 2. Solution Pattern
**Command**

### 3. Explanation
**What is actually wrong:** Actions are performed immediately and anonymously. There is no object representing "this change", so it cannot be queued, logged, retried or reversed.

**How the pattern fixes it:** Each action becomes a command object that holds everything needed to execute it (and to undo it). An invoker executes commands, records them in history and the audit log, and can queue them for background processing. Undo calls the command's reverse operation.

**What enables it:** Encapsulating a request as an object with `Execute` and `Undo`, plus capturing the previous state needed for undo at execution time.

**Alternatives:**
- **CQRS command handlers (MediatR)**: covers encapsulation and pipelines; undo must still be designed.
- **Event sourcing**: full history and replay; heavier.
- **Database temporal tables** (SQL Server): audit history at the data level, but not undo semantics or queuing.

### Requirements
1. Each admin action is a command class with Execute and Undo.
2. Commands capture the previous values needed to undo.
3. An invoker records each executed command to an audit log (user, timestamp, before/after).
4. Bulk commands run asynchronously via a queue and report progress/status.
5. A failed bulk command does not leave partial changes (or partial changes are tracked and undoable).
6. Undo is only allowed if the data has not been changed by someone else since (conflict detection).

### Reviewer Checklist
- [ ] Commands are self-contained (serializable data, not live HTTP context).
- [ ] Undo restores exact previous state.
- [ ] Audit log written for execute and undo.
- [ ] Background execution for bulk commands.
- [ ] Partial failure strategy defined.
- [ ] Concurrency/conflict check before undo.

**Common mistakes:** undo implemented as "apply opposite delta" (e.g. +10%) instead of restoring captured values, which is wrong after rounding.

---

## P-15 · Interpreter — Marketing Promotion Rule Language

### 1. Problem
The marketing team wants to create promotions without developer involvement. They have agreed on a small rule language, for example:

```
cart.total > 100 AND customer.tier == "Gold"
category == "Electronics" OR (brand == "Acme" AND cart.itemCount >= 3)
NOT customer.isFirstOrder AND cart.total BETWEEN 50 AND 200
```

A promotion applies when its rule evaluates to true for the current cart and customer.

Symptoms:
- Every promotion currently needs a developer to write C# and deploy.
- Rules are hard-coded `if` statements spread across the pricing code.
- No way to test a rule before it goes live.

### 2. Solution Pattern
**Interpreter**

### 3. Explanation
**What is actually wrong:** A recurring kind of problem (deciding eligibility) is expressed in code when it would be better expressed as sentences in a small language that non-developers can write.

**How the pattern fixes it:** Define the grammar, represent each grammar rule as a class (literal, variable reference, comparison, AND, OR, NOT, BETWEEN), parse rule text into a tree of these expression objects, and evaluate the tree against a context (cart + customer).

**What enables it:** A class per grammar rule with a common `Evaluate(context)` method; composite expressions evaluate their children recursively (it is structurally a Composite).

**Alternatives:**
- **Rules engine library** (e.g. NRules, Microsoft RulesEngine): more features, less control.
- **C# expression trees / dynamic LINQ**: powerful but riskier to expose to user input.
- **JSON-based rule definitions**: no parser needed, harder for humans to write.

### Requirements
1. Grammar documented (operators, precedence, allowed fields, data types).
2. One expression class per grammar construct, all sharing an evaluate method.
3. A parser turns rule text into an expression tree (a simple hand-written recursive descent parser is fine).
4. Only whitelisted fields can be referenced; unknown fields fail at parse time.
5. Invalid syntax produces a clear error with position.
6. A "test rule" capability evaluates a rule against a sample cart without activating it.
7. No use of code execution / `eval` on user-supplied text.

### Reviewer Checklist
- [ ] Grammar and precedence correct (`AND` binds tighter than `OR`, parentheses respected).
- [ ] Expression classes, not one giant switch on strings at evaluation time.
- [ ] Type checking (string vs number comparisons).
- [ ] Field whitelist enforced.
- [ ] Safe: no arbitrary code execution.
- [ ] Tests for each operator and for error cases.

**Common mistakes:** regex-based "parsing"; evaluating the string every time instead of parsing once and caching the tree.

---

## P-16 · Iterator — Traversing Paginated Order Exports

### 1. Problem
A nightly job exports all orders from the last 24 hours to the accounting system. Orders come from two sources:
- the ShopSphere database (page with offset/limit),
- an external marketplace API (cursor-based pagination, max 100 items per page, rate-limited).

Symptoms:
- The export code mixes page-tracking variables, cursor tokens and retry logic with mapping logic.
- A bug skipped page boundaries, so some orders were exported twice and some never.
- Loading everything into a list first used 6 GB of memory.

### 2. Solution Pattern
**Iterator**

### 3. Explanation
**What is actually wrong:** Consumers have to know how each collection is stored and paged in order to walk through it. Traversal logic is duplicated and error-prone, and the whole data set is loaded at once.

**How the pattern fixes it:** Each source provides an iterator that yields one order at a time and hides paging, cursors and fetching. The export job just loops over orders, and pages are fetched only when needed.

**What enables it:** A standard traversal interface (in .NET, `IEnumerable<T>` / `IAsyncEnumerable<T>` with `yield return`) that separates traversal state from the collection and the consumer.

**Alternatives:**
- **Return a full list**: simple but fails at scale.
- **Reactive streams / channels** (`System.Threading.Channels`, Rx): when producers and consumers run concurrently.
- **Keyset pagination in SQL** (instead of offset): recommended inside the database iterator for correctness and speed.

### Requirements
1. One iterator per source, exposing a uniform async sequence of orders.
2. The consumer contains no paging or cursor variables.
3. Only one page is held in memory at a time per source.
4. No duplicates or gaps across page boundaries, even if new orders arrive during the export (state your approach, e.g. keyset pagination with a fixed upper bound timestamp).
5. Rate limiting and transient retries handled inside the external iterator.
6. Cancellation is supported.
7. The two sources can be combined into one sequence.

### Reviewer Checklist
- [ ] Lazy, streaming traversal (no full materialization).
- [ ] Correct boundary handling; the offset-with-concurrent-inserts problem is addressed.
- [ ] Cancellation token flows through.
- [ ] Consumer code is simple.
- [ ] Errors mid-iteration are handled (resume or fail cleanly).

**Common mistakes:** `ToList()` somewhere in the chain; multiple enumeration causing repeated API calls.

---

## P-17 · Mediator — Interdependent Cart Features

### 1. Problem
The cart has several features that affect each other:
- **Coupon**: some coupons require a minimum total or specific items.
- **Free shipping**: available if the total after coupons exceeds a threshold.
- **Loyalty points**: points can be redeemed, which lowers the total, which can invalidate the coupon or free shipping.
- **Gift wrap**: unavailable for certain items; changes the total.
- **Shipping method**: express is unavailable for some items and affects the total.

Each component currently calls the others directly: `CouponComponent` calls `FreeShippingComponent.Recheck()`, which calls `LoyaltyComponent.Recalculate()`, which calls `CouponComponent.Validate()`…

Symptoms:
- Infinite recalculation loops occurred twice in production.
- Every component depends on every other component (tight coupling).
- Nobody can explain the final cart total for a support ticket.

### 2. Solution Pattern
**Mediator**

### 3. Explanation
**What is actually wrong:** Many peer objects interact in a web of direct references. The interaction logic is spread across all of them, so it is hard to follow and easy to loop.

**How the pattern fixes it:** Components no longer know about each other. They notify a central cart mediator when they change. The mediator owns the rules for how changes affect other components, runs recalculation in a defined order, and stops when the cart is stable.

**What enables it:** Components hold a reference only to the mediator interface; all cross-component coordination logic lives in one class.

**Alternatives:**
- **Observer / events**: decouples senders and receivers but does not centralize the coordination rules or control ordering; loops can still happen.
- **MediatR library**: despite the name, it is mostly an in-process request/handler dispatcher. Using it does not automatically implement this problem's coordination.
- **A pure pricing pipeline** recalculating everything from scratch in a fixed order: a valid simplification, often the best real-world answer; the reviewer should accept it if the developer explains why.

### Requirements
1. Components reference only the mediator, never each other.
2. The mediator recalculates in a documented, deterministic order.
3. Recalculation always terminates (no loops), with a defined maximum or a stable-state check.
4. When a change invalidates something (e.g. coupon no longer eligible), the cart records a user-visible reason.
5. The mediator can produce an explanation of the final total (breakdown of each step).
6. Adding a new feature (e.g. "donation round-up") touches the new component and the mediator only.

### Reviewer Checklist
- [ ] No direct component-to-component references.
- [ ] Deterministic ordering.
- [ ] Termination guaranteed.
- [ ] Invalidation reasons surfaced.
- [ ] Explainable total.
- [ ] Mediator does not contain each component's internal logic (it coordinates; components compute).

**Common mistakes:** mediator that becomes a god class implementing coupon, loyalty and shipping math itself.

---

## P-18 · Memento — Restoring Checkout State After Payment Failure

### 1. Problem
During checkout, ShopSphere changes the cart: it locks prices, applies the final coupon, converts loyalty points into a discount, selects a shipping quote and reserves a gift-card balance. If payment fails (card declined, 3-D Secure timeout), the cart must return exactly to the state before checkout began, so the customer can try again.

Symptoms:
- After a declined payment, loyalty points were deducted but the discount was removed, so customers lost points.
- The "restore" code in `CheckoutService` sets cart fields directly, so the cart's private fields were made public to allow it.
- Every new cart field requires updating the restore code; one was forgotten.

### 2. Solution Pattern
**Memento**

### 3. Explanation
**What is actually wrong:** Outside code needs to save and restore an object's state, but doing so requires breaking the object's encapsulation and duplicating knowledge of its internals.

**How the pattern fixes it:** The cart itself creates a snapshot (memento) of its internal state and can restore itself from that snapshot. The checkout process (caretaker) holds the snapshot without being able to read or change its contents.

**What enables it:** The originator (cart) is the only object that can create and interpret the memento; the memento is opaque to everyone else.

**Alternatives:**
- **Database transaction**: works only if everything is in one database transaction; payment calls are external, so a long transaction is not appropriate.
- **Compensating actions / Saga**: required for external side effects (e.g. releasing a gift-card hold at the gift-card service). Memento restores *the cart's own state*; it does not undo external calls. A good solution combines both and is clear about which is which.
- **Immutable cart with versioning**: keep the previous version instead of a snapshot.

### Requirements
1. Cart creates and restores its own snapshot; no public setters added for restore purposes.
2. The snapshot is opaque to the checkout service.
3. Restoring returns the cart to the exact pre-checkout state, including loyalty points and coupon state.
4. External side effects (gift-card hold, inventory reservation) are reversed separately, and the solution states this explicitly.
5. Snapshot is persisted or keyed so it survives if checkout spans multiple requests (e.g. 3-D Secure redirect).
6. Snapshots expire.

### Reviewer Checklist
- [ ] Encapsulation preserved.
- [ ] Snapshot is a deep copy of state (no shared mutable references).
- [ ] Restore is complete; a test compares the cart before checkout and after restore.
- [ ] External compensation handled distinctly from memento restore.
- [ ] Multi-request flow handled.
- [ ] Snapshot expiry.

**Common mistakes:** memento with public mutable fields; using memento to "undo" a payment charge.

---

## P-19 · Observer — Reactions to "Order Placed"

### 1. Problem
When an order is placed, these things must happen: send a confirmation email, notify the warehouse, award loyalty points, update sales analytics, notify the seller, and (new) trigger a "frequently bought together" recommendation update.

All of this is currently called directly at the end of `OrderService.PlaceOrderAsync`.

Symptoms:
- Each new reaction requires editing `OrderService`.
- If the analytics service is down, order placement fails, even though analytics is not essential.
- The order API takes 3 seconds because all reactions run in sequence before responding.

### 2. Solution Pattern
**Observer** (in practice: domain events / publish–subscribe)

### 3. Explanation
**What is actually wrong:** The subject (order) knows about and depends on every dependent. Adding a dependent changes the subject, and a failure in any dependent breaks the subject.

**How the pattern fixes it:** The order service publishes one "OrderPlaced" event. Interested parties subscribe independently. The order service does not know who listens. New reactions are new subscribers.

**What enables it:** A subscription mechanism where observers register with the subject (or an event dispatcher) and are notified through a common interface.

**Alternatives:**
- **C# `event` / delegates**: in-process, synchronous by default.
- **Message broker** (Azure Service Bus, RabbitMQ) with the **Outbox pattern**: durable, cross-service, survives crashes. Strongly preferred for the warehouse and seller notifications.
- **Mediator** (P-17) when coordination order and rules matter, not just broadcast.

### Requirements
1. `OrderService` publishes an event and has no references to email, warehouse, loyalty, analytics or seller services.
2. Each reaction is an independent subscriber.
3. A failing subscriber does not fail order placement or other subscribers.
4. Non-essential subscribers run asynchronously; the API responds without waiting for them.
5. Events are not lost if the process crashes after the order is saved (describe the approach, e.g. outbox).
6. Subscribers are idempotent (the same event delivered twice does not award points twice).

### Reviewer Checklist
- [ ] Subject decoupled from observers.
- [ ] Failure isolation.
- [ ] Async handling where appropriate.
- [ ] Delivery guarantee reasoning (at-least-once + idempotency).
- [ ] Event payload contains what subscribers need without forcing them to call back into the order service unnecessarily.
- [ ] Subscriber registration is configuration/DI driven.

**Common mistakes:** publishing the event before the order transaction commits; fire-and-forget `Task.Run` with no error handling.

---

## P-20 · State — Order Lifecycle Transitions

### 1. Problem
An order moves through: `PendingPayment → Paid → Packed → Shipped → Delivered`, with branches to `Cancelled`, `ReturnRequested → Returned → Refunded`. Rules include:
- Cancel is allowed only before `Shipped`.
- Return can be requested only within 30 days after `Delivered`.
- Refund only after `Returned`.
- Address change only before `Packed`.

Each method in `Order` (`Cancel`, `Ship`, `RequestReturn`, `ChangeAddress`…) contains a growing `switch (Status)`.

Symptoms:
- An order was shipped after being cancelled because one switch missed a case.
- Adding a new state (`OnHold` for fraud review) means editing every method.
- No single place shows which transitions are valid.

### 2. Solution Pattern
**State**

### 3. Explanation
**What is actually wrong:** An object's behaviour depends on its current state, and that logic is scattered in conditionals across every method. Valid transitions are implicit.

**How the pattern fixes it:** Each state is its own class implementing the same interface (`Cancel`, `Ship`, `RequestReturn`…). The order delegates to its current state object. Each state allows only its valid actions and decides the next state. Adding `OnHold` means one new class plus changing the states that can move into it.

**What enables it:** Delegation from the context (order) to a state object, with state classes responsible for transitions.

**Alternatives:**
- **State machine library** (e.g. Stateless for .NET): declarative transition table; excellent real-world choice.
- **Enum + transition table** (dictionary of allowed transitions): simpler, good when states have little behaviour of their own.
- The reviewer should accept either if the developer explains the trade-off, but this exercise expects the State pattern.

### Requirements
1. One class per state; the order delegates actions to its current state.
2. Invalid actions return a clear domain error (e.g. "Cannot cancel a shipped order").
3. All rules listed in the problem are enforced.
4. The 30-day return window uses an injectable clock (testable).
5. Every transition is recorded (from, to, timestamp, actor).
6. State is persisted as a simple value in the database and the correct state object is restored on load.
7. Add `OnHold` (reachable from `Paid`, returns to `Paid` or goes to `Cancelled`) to prove extensibility.

### Reviewer Checklist
- [ ] No `switch (Status)` left in order methods.
- [ ] Every rule enforced with tests for valid and invalid transitions.
- [ ] Time-based rule testable.
- [ ] Transition history recorded.
- [ ] Persistence mapping between stored value and state object.
- [ ] `OnHold` added with minimal changes.

**Common mistakes:** state classes that mutate unrelated order data; transitions decided outside state classes.

---

## P-21 · Strategy — Shipping Cost Calculation

### 1. Problem
ShopSphere offers several ways to calculate shipping cost, chosen per seller and per promotion:
- **Flat rate** (e.g. $5 per order)
- **Weight-based** (rate per kg with tiers)
- **Free over threshold** (free if subtotal ≥ $50, else flat)
- **Distance-based** (zones by origin/destination)
- **Carrier real-time quote** (calls the carrier through the adapter in P-06)

Symptoms:
- `ShippingCalculator.Calculate()` is one method with a large `switch` on `seller.ShippingMode`.
- A/B testing a new free-shipping threshold requires a deployment.
- Unit tests for one mode break when another mode changes.

### 2. Solution Pattern
**Strategy**

### 3. Explanation
**What is actually wrong:** Several interchangeable algorithms for the same task live in one method and are selected by conditionals, so they cannot be swapped, tested or extended independently.

**How the pattern fixes it:** Each algorithm becomes a strategy class implementing `IShippingCostStrategy`. The context (checkout) receives the right strategy at runtime and calls it without knowing which one it is.

**What enables it:** A shared strategy interface and the context holding a reference to the strategy (composition), so behaviour can change at runtime.

**Alternatives:**
- **Delegates / `Func<>`**: lightweight strategies when there is no state or configuration.
- **Factory Method** (P-02) is about *creating* objects; Strategy is about *choosing behaviour*. A good solution often uses a factory or DI to *select* the strategy, but the strategy itself is the algorithm.
- **Rules/Interpreter** (P-15) if non-developers must define the logic.

### Requirements
1. One strategy class per calculation method, all sharing one interface.
2. Strategies receive configuration (rates, thresholds) as data, not hard-coded values.
3. Checkout selects the strategy for each seller in the cart (a cart can contain items from multiple sellers with different strategies).
4. Strategy selection is outside the strategies (resolver/registry).
5. A new strategy can be added without editing existing ones.
6. Money calculations use `decimal` and a defined rounding rule.

### Reviewer Checklist
- [ ] No `switch` on mode inside calculation logic.
- [ ] Strategies are stateless or safely configured.
- [ ] Multi-seller cart handled.
- [ ] Threshold/rate changes possible via configuration.
- [ ] Rounding defined and tested.
- [ ] Each strategy has independent tests.

**Common mistakes:** strategies that read the seller's mode and branch internally; using `double` for money.

---

## P-22 · Template Method — Importing Orders from External Marketplaces

### 1. Problem
Sellers also sell on Amazon, eBay and Etsy. ShopSphere imports those orders every 15 minutes to manage fulfilment centrally. Each importer currently has its own complete implementation, but they all follow the same steps:

1. Authenticate with the marketplace
2. Fetch new orders since the last sync
3. Parse the marketplace's format
4. Validate
5. Map to the ShopSphere order model
6. Skip duplicates
7. Save
8. Update the last sync time

Only steps 1, 2, 3 and 5 differ per marketplace.

Symptoms:
- The duplicate check was fixed in the Amazon importer but not the others.
- Last-sync time is updated before save in one importer, so orders are lost on failure.
- Adding Walmart means copying 500 lines.

### 2. Solution Pattern
**Template Method**

### 3. Explanation
**What is actually wrong:** Several classes share the same algorithm structure but each re-implements the whole thing, so shared steps drift apart and the order of steps is not protected.

**How the pattern fixes it:** A base class defines the import algorithm once as a fixed sequence (the template method). The marketplace-specific steps are abstract methods that subclasses implement. Shared steps (validate, deduplicate, save, update sync time) are written once.

**What enables it:** Inheritance with a non-overridable skeleton method that calls overridable "primitive" steps and optional "hook" methods.

**Alternatives:**
- **Strategy composition**: one importer pipeline with injected marketplace-specific components (client, parser, mapper). Favours composition over inheritance and is often preferred in modern .NET; acceptable if the fixed sequence is still enforced in one place.
- **Pipeline libraries / ETL tools**.

### Requirements
1. A base importer with a single, non-overridable method that runs all steps in order.
2. Only marketplace-specific steps are abstract; shared steps are implemented once.
3. Optional hooks (e.g. `BeforeSave`) with default no-op behaviour.
4. Last-sync time updates only after a successful save.
5. Partial failure in a batch is handled (one bad order does not block the rest; failures are recorded).
6. Adding Walmart requires implementing only the abstract steps.

### Reviewer Checklist
- [ ] Template method cannot be overridden (`sealed`/non-virtual).
- [ ] Clear distinction between abstract steps and hooks.
- [ ] Shared logic not duplicated in subclasses.
- [ ] Correct ordering of sync-time update.
- [ ] Per-order error handling.
- [ ] Subclasses do not call base steps out of order.

**Common mistakes:** making every step virtual, so subclasses can break the sequence; deep inheritance hierarchies.

---

## P-23 · Visitor — Operations over Different Line Item Types

### 1. Problem
A cart/order contains different line item types:
- **PhysicalProduct** (weight, dimensions, ships from warehouse)
- **DigitalProduct** (download link, no shipping)
- **GiftCard** (not taxable in some regions, no shipping, special accounting)
- **Subscription** (recurring, prorated, taxed differently)

Operations needed over these items keep growing: tax calculation, shipping eligibility, accounting export, refund amount calculation, and (new) carbon-footprint estimate.

Symptoms:
- Each operation is a method with `if (item is PhysicalProduct) ... else if (item is GiftCard) ...`.
- When `Subscription` was added, two operations forgot to handle it and silently returned 0.
- Line item classes keep gaining unrelated methods (`ExportToAccounting`, `CalculateCarbon`) and are becoming bloated.

### 2. Solution Pattern
**Visitor**

### 3. Explanation
**What is actually wrong:** The set of item *types* is fairly stable, but the set of *operations* keeps growing. Adding operations either bloats the item classes or spreads fragile type checks around.

**How the pattern fixes it:** Each operation becomes a visitor class with one method per item type. Each item has a single `Accept(visitor)` method that calls the right visitor method (double dispatch). Adding a new operation means adding one visitor class. If a new item type is added, every visitor fails to compile until it handles it, so nothing is silently missed.

**What enables it:** Double dispatch — the call is resolved by both the item type (via `Accept`) and the visitor type.

**Alternatives:**
- **C# pattern matching with `switch` expressions over a closed hierarchy**: concise; with exhaustive checking (or a default that throws) it can be a reasonable modern alternative. The reviewer should discuss the trade-off if the developer uses it.
- **Polymorphic methods on each item**: good when operations are few and stable (the opposite of this problem).
- The trade-off: Visitor makes adding operations easy but adding new item types harder.

### Requirements
1. A visitor interface with one method per line item type.
2. Each line item implements `Accept`.
3. Implement at least: tax calculation visitor, shipping eligibility visitor, carbon-footprint visitor.
4. No `is`/type checks in operation logic.
5. Adding a new item type causes a compile-time error in every visitor until handled.
6. Visitors that produce a result (tax total, eligibility) return or accumulate it cleanly.

### Reviewer Checklist
- [ ] Double dispatch implemented correctly.
- [ ] No type checks remain.
- [ ] Line item classes are free of operation-specific logic.
- [ ] Compile-time safety for new types demonstrated or explained.
- [ ] Result handling is clear (generic `IVisitor<TResult>` or accumulator).
- [ ] Trade-off acknowledged.

**Common mistakes:** a visitor with a single `Visit(LineItem)` method that type-checks inside (this defeats the pattern).

---

# Submission Template

Copy this when submitting a solution for review:

```
## Submission

Problem ID: P-XX
Pattern: <pattern name>
Language / framework: <e.g. C# 12 / .NET 8 / ASP.NET Core>

### Summary of approach
<2–5 sentences on how the pattern is applied and which requirements it addresses>

### Requirements coverage
| # | Requirement | Where in code | Notes |
|---|---|---|---|
| 1 | ... | ClassName.Method | ... |

### Design decisions and trade-offs
<anything deliberately different from the problem's expectation, and why>

### Tests
<list of tests, or explanation of how each requirement was verified>

### Code
<code or link>
```

# Reviewer Output Format

The reviewing agent should respond in this structure:

```
## Review: P-XX — <Pattern>

Overall verdict: Meets / Partially meets / Does not meet

### Checklist
| Item | Verdict | Evidence |
|---|---|---|
| ... | Pass / Partial / Fail | <code reference or quote> |

### Pain points from the problem statement
<for each symptom listed in the problem: is it solved? yes/no + why>

### Pattern correctness
<is this really the named pattern, or a different one? any confusion with the alternatives listed?>

### Top 3 issues (ranked by impact)
1. Issue — why it matters — concrete suggestion (no full rewrite)
2. ...
3. ...

### What was done well
<brief>
```
