# .NET Web API Architecture Guide

## Recommended Pattern: Clean Architecture + Vertical Slice

This document defines the recommended architecture for a modern ASP.NET Core Web API.

The goal is to keep the codebase:

- easy to understand
- easy to test
- easy to extend
- resistant to unnecessary coupling
- organized around business features rather than technical folders
- simple enough to avoid over-engineering

The recommended approach combines:

- **Clean Architecture**
- **Vertical Slice Architecture**
- **CQRS-style Commands and Queries**
- **Domain-driven business rules where useful**
- **EF Core for persistence**
- **ASP.NET Core built-in dependency injection**
- **Centralized validation and error handling**

---

# 1. Core Architecture

Use four main projects:

```text
src/
├── MyApp.Api/
├── MyApp.Application/
├── MyApp.Domain/
└── MyApp.Infrastructure/

tests/
├── MyApp.UnitTests/
└── MyApp.IntegrationTests/
```

High-level flow:

```text
HTTP Request
     │
     ▼
┌───────────────┐
│      API      │
│ Controllers   │
│ Middleware    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  Application  │
│ Features      │
│ Use Cases     │
│ Validation    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│    Domain     │
│ Entities      │
│ Rules         │
│ Value Objects │
└───────────────┘
        ▲
        │
┌───────┴───────┐
│Infrastructure │
│ EF Core       │
│ External APIs │
│ Email         │
│ Storage       │
└───────┬───────┘
        │
        ▼
    Database
```

---

# 2. Dependency Direction

The most important rule is dependency direction.

```text
API ───────────────► Application
                       │
                       ▼
                    Domain

Infrastructure ────► Application
       │
       └────────────► Domain
```

Rules:

1. `Domain` depends on nothing.
2. `Application` depends on `Domain`.
3. `Infrastructure` depends on `Application` and `Domain`.
4. `Api` depends on `Application`.
5. `Api` may reference `Infrastructure` only for dependency registration/composition if needed.
6. `Domain` must never depend on EF Core, ASP.NET Core, HTTP, or infrastructure-specific code.

The core of the application should not know how HTTP, databases, email providers, or cloud services work.

---

# 3. Recommended Solution Structure

Example:

```text
MyApp/
│
├── src/
│   │
│   ├── MyApp.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   ├── Configuration/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── MyApp.Application/
│   │   ├── Features/
│   │   │   ├── Products/
│   │   │   ├── Orders/
│   │   │   └── Customers/
│   │   │
│   │   ├── Abstractions/
│   │   ├── Common/
│   │   └── DependencyInjection.cs
│   │
│   ├── MyApp.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   └── Common/
│   │
│   └── MyApp.Infrastructure/
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/
│       │   └── Migrations/
│       │
│       ├── Authentication/
│       ├── Repositories/
│       ├── Services/
│       ├── Storage/
│       └── DependencyInjection.cs
│
├── tests/
│   ├── MyApp.UnitTests/
│   └── MyApp.IntegrationTests/
│
└── MyApp.sln
```

---

# 4. Prefer Vertical Slice Architecture

Avoid organizing the Application layer only by technical type:

```text
Application/
├── Services/
├── Validators/
├── DTOs/
├── Commands/
├── Queries/
└── Handlers/
```

This forces developers to jump through many folders to understand one feature.

Prefer:

```text
Application/
└── Features/
    ├── Products/
    │   ├── CreateProduct/
    │   │   ├── CreateProductCommand.cs
    │   │   ├── CreateProductHandler.cs
    │   │   ├── CreateProductValidator.cs
    │   │   └── CreateProductResponse.cs
    │   │
    │   ├── UpdateProduct/
    │   │   ├── UpdateProductCommand.cs
    │   │   ├── UpdateProductHandler.cs
    │   │   └── UpdateProductValidator.cs
    │   │
    │   ├── DeleteProduct/
    │   │   ├── DeleteProductCommand.cs
    │   │   └── DeleteProductHandler.cs
    │   │
    │   └── GetProducts/
    │       ├── GetProductsQuery.cs
    │       ├── GetProductsHandler.cs
    │       └── ProductDto.cs
    │
    └── Orders/
        ├── CreateOrder/
        ├── ConfirmOrder/
        ├── CancelOrder/
        └── GetOrder/
```

Each folder represents one use case.

The developer should be able to open:

```text
Features/Products/CreateProduct/
```

and immediately understand how product creation works.

---

# 5. API Layer

The API layer is responsible for HTTP concerns.

Responsibilities:

- controllers or minimal API endpoints
- routing
- authentication setup
- authorization setup
- middleware
- request/response mapping
- HTTP status codes
- OpenAPI/Swagger configuration
- dependency composition
- exception handling registration

The API layer should not contain business logic.

---

# 6. Keep Controllers Thin

Bad:

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    CreateProductRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return BadRequest("Name is required.");

    if (request.Price <= 0)
        return BadRequest("Invalid price.");

    var product = new Product
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Price = request.Price
    };

    _db.Products.Add(product);
    await _db.SaveChangesAsync();

    return Ok(product);
}
```

The controller is handling:

- validation
- entity creation
- persistence
- business logic
- response construction

Prefer:

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    CreateProductRequest request,
    CancellationToken cancellationToken)
{
    var command = new CreateProductCommand(
        request.Name,
        request.Price);

    var result = await _sender.Send(
        command,
        cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Id },
        result);
}
```

The controller should mainly translate:

```text
HTTP
 ↓
Application request
 ↓
HTTP response
```

---

# 7. Application Layer

The Application layer defines what the system can do.

Responsibilities:

- use cases
- commands
- queries
- handlers
- DTOs
- validation
- application interfaces
- orchestration
- authorization checks related to use cases
- transaction coordination when required

It should not contain infrastructure implementation.

For example, Application may define:

```csharp
public interface IEmailService
{
    Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken);
}
```

But the actual email provider implementation belongs in Infrastructure.

---

# 8. Commands and Queries

Use a CQRS-style distinction.

CQRS does not mean that you need separate databases.

It simply means distinguishing operations that change data from operations that read data.

## Commands

Commands change system state.

Examples:

```text
CreateProduct
UpdateProduct
DeleteProduct
CreateOrder
ConfirmOrder
CancelOrder
ChangeOrderStatus
```

Example:

```csharp
public sealed record CreateProductCommand(
    string Name,
    decimal Price);
```

Handler:

```csharp
public sealed class CreateProductHandler
{
    private readonly AppDbContext _db;

    public CreateProductHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(
            command.Name,
            command.Price);

        _db.Products.Add(product);

        await _db.SaveChangesAsync(
            cancellationToken);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Price);
    }
}
```

## Queries

Queries retrieve information.

Examples:

```text
GetProduct
GetProducts
GetOrder
GetCustomerOrders
GetDashboard
SearchProducts
```

Example:

```csharp
public sealed record GetProductQuery(
    Guid ProductId);
```

---

# 9. Domain Layer

The Domain contains the core business model.

Typical contents:

```text
Domain/
├── Entities/
├── ValueObjects/
├── Enums/
├── Events/
├── Exceptions/
└── Common/
```

The Domain should contain rules that remain true regardless of:

- database
- API
- UI
- cloud provider
- external integrations

Example:

```csharp
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items =>
        _items.AsReadOnly();

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException(
                "Only pending orders can be confirmed.");
        }

        if (_items.Count == 0)
        {
            throw new DomainException(
                "An order must contain at least one item.");
        }

        Status = OrderStatus.Confirmed;
    }
}
```

The rule:

```text
Only pending orders can be confirmed
```

belongs in the Domain.

It should not be duplicated across controllers or services.

---

# 10. Entities

Entities represent domain concepts with identity.

Example:

```csharp
public sealed class Product
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public decimal Price { get; private set; }

    private Product()
    {
    }

    private Product(
        Guid id,
        string name,
        decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static Product Create(
        string name,
        decimal price)
    {
        if (price <= 0)
            throw new DomainException(
                "Product price must be greater than zero.");

        return new Product(
            Guid.NewGuid(),
            name.Trim(),
            price);
    }
}
```

Prefer controlled creation instead of allowing every property to be changed freely.

---

# 11. Value Objects

Use value objects when a concept has validation or meaning beyond a primitive type.

Instead of:

```csharp
string Email
decimal Money
string PhoneNumber
```

you may introduce:

```text
EmailAddress
Money
PhoneNumber
Address
```

Example:

```csharp
public sealed record Money(
    decimal Amount,
    string Currency);
```

Do not create value objects for every primitive.

Use them where they protect meaningful domain rules.

---

# 12. DTOs

Do not expose entities directly through your API.

Avoid:

```csharp
return Ok(product);
```

Use DTOs:

```csharp
public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal Price);
```

Flow:

```text
Database
   ↓
Domain / Persistence Model
   ↓
Application
   ↓
DTO
   ↓
API
   ↓
JSON
```

This keeps your API contract independent from internal implementation details.

---

# 13. Request Models

API request models may be separate from Application commands.

Example:

```csharp
public sealed record CreateProductRequest(
    string Name,
    decimal Price);
```

Controller:

```csharp
var command = new CreateProductCommand(
    request.Name,
    request.Price);
```

This gives you freedom to change HTTP contracts without changing internal use cases.

For very small applications, sharing simple request objects can be acceptable.

Do not create duplicate models unless the separation gives actual value.

---

# 14. Validation

Keep validation separate from controllers.

Use validation for request/input rules.

Examples:

```text
Name is required
Maximum name length is 200
Price must be greater than zero
Email format must be valid
```

Example using FluentValidation-style syntax:

```csharp
public sealed class CreateProductValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);
    }
}
```

Keep the distinction:

```text
Input validation
    ↓
"Is this request valid?"

Domain rule
    ↓
"Is this operation allowed?"
```

Example:

```text
Name cannot be empty
    → Validation

Order cannot be confirmed twice
    → Domain
```

---

# 15. Infrastructure Layer

Infrastructure contains technical implementation details.

Typical structure:

```text
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   └── Migrations/
│
├── Authentication/
├── Repositories/
├── Services/
├── Email/
├── Storage/
├── Payments/
└── DependencyInjection.cs
```

Examples of infrastructure concerns:

```text
EF Core
PostgreSQL
SQL Server
Redis
SMTP
SendGrid
AWS S3
Azure Blob Storage
Firebase
External REST APIs
Payment providers
```

---

# 16. EF Core

Place EF Core in Infrastructure.

Recommended:

```text
Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    ├── Configurations/
    │   ├── ProductConfiguration.cs
    │   ├── OrderConfiguration.cs
    │   └── CustomerConfiguration.cs
    └── Migrations/
```

Example:

```csharp
public sealed class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(
        EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);
    }
}
```

Keep entity configuration outside `AppDbContext` when the model becomes non-trivial.

---

# 17. Repository Pattern

Do not automatically create repositories for every entity.

EF Core already provides repository-like functionality through:

```text
DbSet<T>
DbContext
```

Simple feature:

```csharp
var products = await _db.Products
    .AsNoTracking()
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .ToListAsync(cancellationToken);
```

This is acceptable.

Create a repository when it represents meaningful domain-specific persistence behavior.

Good:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetPendingOrderForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
```

Avoid creating:

```text
IProductRepository
ProductRepository
```

only to wrap:

```text
Add
Update
Delete
GetById
GetAll
```

with no additional value.

Avoid abstraction for abstraction's sake.

---

# 18. Unit of Work

Do not introduce a custom Unit of Work by default.

EF Core's `DbContext` already behaves as a Unit of Work.

For most applications:

```csharp
_db.Products.Add(product);
_db.Orders.Add(order);

await _db.SaveChangesAsync(
    cancellationToken);
```

is enough.

Add custom transaction abstractions only when there is a clear use case.

---

# 19. Database Query Guidelines

For read-only queries, use:

```csharp
.AsNoTracking()
```

Example:

```csharp
var product = await _db.Products
    .AsNoTracking()
    .Where(x => x.Id == productId)
    .Select(x => new ProductDto(
        x.Id,
        x.Name,
        x.Price))
    .FirstOrDefaultAsync(
        cancellationToken);
```

Prefer projecting directly to DTOs for read-heavy endpoints when appropriate.

Avoid loading full entities when only a few fields are required.

---

# 20. Async Programming

Use async APIs for I/O.

Examples:

```text
Database access
HTTP requests
File access
Cloud storage
Email providers
Message queues
```

Prefer:

```csharp
await _db.Products
    .ToListAsync(cancellationToken);
```

instead of:

```csharp
_db.Products.ToList();
```

Pass `CancellationToken` through the request pipeline.

Example:

```csharp
public async Task<ProductDto?> Handle(
    GetProductQuery query,
    CancellationToken cancellationToken)
```

---

# 21. Error Handling

Use centralized exception handling.

Avoid:

```csharp
try
{
    ...
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}
```

inside every controller.

Recommended flow:

```text
Request
   ↓
Controller
   ↓
Application
   ↓
Exception
   ↓
Global Exception Handler
   ↓
ProblemDetails
   ↓
HTTP Response
```

ASP.NET Core provides standardized error responses through `ProblemDetails`.

Typical mapping:

```text
ValidationException
    → 400 Bad Request

UnauthorizedAccessException
    → 401 Unauthorized

Forbidden operation
    → 403 Forbidden

NotFoundException
    → 404 Not Found

ConflictException
    → 409 Conflict

Unexpected exception
    → 500 Internal Server Error
```

---

# 22. HTTP Status Codes

Use HTTP semantics correctly.

```text
200 OK
    Successful GET or update with response.

201 Created
    Resource created.

204 No Content
    Successful operation with no response body.

400 Bad Request
    Invalid request.

401 Unauthorized
    Authentication required or invalid.

403 Forbidden
    Authenticated but not allowed.

404 Not Found
    Resource does not exist.

409 Conflict
    Current state conflicts with operation.

422 Unprocessable Entity
    Optional for semantic validation scenarios.

500 Internal Server Error
    Unexpected server failure.
```

Do not return `200 OK` for every result.

---

# 23. Result Pattern

A Result pattern can be useful when failures are expected and part of normal application flow.

Example:

```csharp
public sealed record Result<T>(
    bool IsSuccess,
    T? Value,
    Error? Error);
```

Then:

```text
Success
NotFound
ValidationFailure
Conflict
Forbidden
```

can be represented without throwing exceptions for every expected condition.

Use exceptions for truly exceptional situations or according to your team's convention.

Do not combine multiple error-handling styles without a clear rule.

---

# 24. Authentication

Authentication determines who the caller is.

Possible approaches:

```text
ASP.NET Core Identity
JWT Bearer authentication
OpenID Connect
OAuth 2.0 providers
External identity providers
```

Authentication belongs at the boundary of the application.

Do not scatter token parsing across services or handlers.

---

# 25. Authorization

Authorization determines what the authenticated user may do.

Prefer policy-based authorization for non-trivial systems.

Example:

```csharp
[Authorize(Policy = "CanManageProducts")]
```

Instead of repeatedly writing:

```csharp
if (user.Role == "Admin")
{
    ...
}
```

Policies make permissions easier to maintain.

Example permissions:

```text
CanViewProducts
CanManageProducts
CanViewOrders
CanManageOrders
CanManageUsers
```

---

# 26. Dependency Injection

Use ASP.NET Core's built-in dependency injection container.

Example:

```csharp
builder.Services.AddScoped<
    IEmailService,
    EmailService>();
```

Avoid manually constructing dependency trees:

```csharp
var service = new OrderService(
    new OrderRepository(
        new AppDbContext()));
```

Prefer constructor injection.

Example:

```csharp
public sealed class CreateOrderHandler
{
    private readonly IOrderRepository _orders;
    private readonly IEmailService _email;

    public CreateOrderHandler(
        IOrderRepository orders,
        IEmailService email)
    {
        _orders = orders;
        _email = email;
    }
}
```

---

# 27. Dependency Registration

Keep `Program.cs` small.

Instead of:

```csharp
builder.Services.AddDbContext<...>();
builder.Services.AddScoped<...>();
builder.Services.AddScoped<...>();
builder.Services.AddScoped<...>();
builder.Services.AddScoped<...>();
```

for hundreds of lines, use extension methods.

Example:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(
        builder.Configuration);
```

Application:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        return services;
    }
}
```

Infrastructure:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
```

---

# 28. Configuration

Do not hard-code environment-specific settings.

Avoid:

```csharp
const string ConnectionString =
    "Server=192.168.1.20;Database=MyApp;";
```

Use configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

Production secrets should come from:

```text
Environment variables
Cloud secret manager
Deployment platform secrets
User secrets during development
```

Never commit production credentials to Git.

---

# 29. Strongly Typed Configuration

For related configuration, use strongly typed options.

Example:

```csharp
public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; }
}
```

Registration:

```csharp
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));
```

This is safer and easier to maintain than repeatedly reading string keys.

---

# 30. Logging

Use `ILogger<T>`.

Example:

```csharp
_logger.LogInformation(
    "Creating product {ProductName}",
    command.Name);
```

Prefer structured properties over interpolated strings.

Good:

```csharp
_logger.LogInformation(
    "Order {OrderId} confirmed",
    order.Id);
```

Avoid:

```csharp
_logger.LogInformation(
    $"Order {order.Id} confirmed");
```

Do not log:

- passwords
- tokens
- API keys
- private secrets
- sensitive payment information

---

# 31. Observability

For production applications, consider:

```text
Structured logging
Distributed tracing
Metrics
Health checks
OpenTelemetry
```

Possible flow:

```text
Request
   │
   ├── Trace ID
   ├── Logs
   ├── Metrics
   └── Dependency traces
```

Do not add complex observability infrastructure to tiny applications unless needed.

---

# 32. OpenAPI

Expose an OpenAPI specification for the API.

Use it for:

- API documentation
- endpoint discovery
- frontend integration
- testing
- client generation when appropriate

Keep API request and response types clear.

Document important:

```text
status codes
authentication requirements
request formats
response formats
validation failures
```

---

# 33. Pagination

Do not return unbounded collections.

Avoid:

```csharp
return await _db.Products
    .ToListAsync();
```

for large tables.

Use pagination.

Example:

```csharp
public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20);
```

Query:

```csharp
var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

Consider a standard paginated response:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
```

---

# 34. Search, Filtering, and Sorting

Keep query parameters explicit.

Example:

```text
GET /api/products
    ?page=1
    &pageSize=20
    &search=dress
    &category=daily
    &sortBy=name
    &sortDirection=asc
```

Validate allowed sorting fields instead of dynamically accepting arbitrary database property names.

---

# 35. Transactions

Use transactions when multiple changes must succeed or fail together.

Example:

```text
Create Order
   │
   ├── Create Order
   ├── Create Order Items
   └── Update Inventory

All succeed
   ↓
Commit

Any fail
   ↓
Rollback
```

Do not create explicit transactions around a single `SaveChangesAsync()` unless there is a reason.

EF Core already wraps `SaveChanges` operations appropriately.

---

# 36. External Services

Hide external provider implementation details behind application-facing abstractions when useful.

Application:

```csharp
public interface IStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken);
}
```

Infrastructure:

```text
S3StorageService
AzureBlobStorageService
LocalStorageService
```

Application code should care about:

```text
Upload file
```

not:

```text
AWS SDK details
```

---

# 37. Background Work

For work that should not block the HTTP request, use background processing.

Examples:

```text
Sending email
Generating reports
Processing uploaded files
Synchronizing external systems
Long-running calculations
```

Possible approaches:

```text
BackgroundService
IHostedService
Message queue
Job scheduler
```

Do not start uncontrolled fire-and-forget tasks from controllers.

---

# 38. Caching

Add caching when measurements show it is useful.

Possible levels:

```text
In-memory cache
Distributed cache
Redis
HTTP response caching
Database query optimization
```

Do not add Redis simply because the architecture diagram has a cache layer.

---

# 39. Testing Strategy

Use:

```text
tests/
├── MyApp.UnitTests/
└── MyApp.IntegrationTests/
```

---

# 40. Unit Tests

Focus unit tests on business behavior.

Example:

```text
OrderTests
├── Confirm_WithItems_Succeeds
├── Confirm_WithoutItems_Fails
├── Confirm_WhenAlreadyConfirmed_Fails
└── Cancel_ConfirmedOrder_FollowsRule
```

Domain tests should generally not require:

```text
HTTP
Database
Network
External services
```

---

# 41. Application Tests

Test use-case behavior when useful.

Example:

```text
CreateProductHandlerTests

Given valid input
When creating product
Then product is persisted

Given duplicate SKU
When creating product
Then conflict is returned
```

Mock only dependencies that represent true external boundaries.

Do not mock every class in the project.

---

# 42. Integration Tests

Integration tests should test the real application pipeline.

```text
HTTP Request
     ↓
ASP.NET Core
     ↓
Controller / Endpoint
     ↓
Application
     ↓
EF Core
     ↓
Test Database
```

Useful integration tests:

```text
POST /api/products returns 201
GET /api/products/{id} returns 404 when missing
Unauthorized request returns 401
Invalid request returns 400
Database changes are persisted correctly
```

---

# 43. Feature Implementation Flow

When adding a feature, follow this sequence.

Example:

```text
"Create a product"
```

## Step 1 — Define the endpoint

```text
POST /api/products
```

## Step 2 — Define request contract

```csharp
CreateProductRequest
```

## Step 3 — Create the feature

```text
Application/
└── Features/
    └── Products/
        └── CreateProduct/
```

## Step 4 — Define command

```csharp
CreateProductCommand
```

## Step 5 — Add validation

```csharp
CreateProductValidator
```

## Step 6 — Add domain behavior

```csharp
Product.Create(...)
```

## Step 7 — Persist

```text
EF Core / Repository
```

## Step 8 — Return DTO

```csharp
ProductDto
```

## Step 9 — Map to HTTP

```text
201 Created
```

## Step 10 — Add tests

```text
Domain test
Application test if needed
Integration test
```

---

# 44. Example Request Flow

```text
POST /api/orders
       │
       ▼
┌───────────────────┐
│ OrdersController  │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ CreateOrderCommand│
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│CreateOrderHandler │
└───────┬─────┬─────┘
        │     │
        ▼     ▼
   Validation Domain
        │     │
        └──┬──┘
           ▼
      Persistence
           │
           ▼
       Database
           │
           ▼
       OrderDto
           │
           ▼
      201 Created
```

---

# 45. Avoid Generic Service Layers by Default

A common architecture is:

```text
Controller
   ↓
Service
   ↓
Repository
   ↓
Database
```

This can work, but often becomes:

```text
ProductsController
ProductService
ProductRepository
```

where each layer simply forwards the same calls.

Example:

```text
Controller.Create()
    ↓
Service.Create()
    ↓
Repository.Create()
    ↓
DbContext.Add()
```

This adds ceremony without improving design.

Prefer feature/use-case handlers:

```text
CreateProduct
      ↓
CreateProductHandler
      ↓
Domain / EF Core
```

Introduce a service only when it represents a meaningful reusable capability.

Examples:

```text
PricingService
TaxCalculator
InventoryAllocator
PaymentService
FileStorageService
```

---

# 46. Avoid Over-Engineering

Do not create unnecessary layers such as:

```text
IProductService
ProductService
IProductManager
ProductManager
IProductRepository
ProductRepository
IProductProvider
ProductProvider
```

for simple CRUD.

Prefer the smallest useful architecture.

Start:

```text
Endpoint
   ↓
Feature Handler
   ↓
EF Core
```

Then introduce additional abstractions when business complexity requires them.

Architecture should reduce complexity, not create it.

---

# 47. Naming Guidelines

Prefer names that describe business actions.

Good:

```text
CreateProductCommand
ConfirmOrderCommand
CancelOrderCommand
GetCustomerOrdersQuery
GetInventorySummaryQuery
```

Avoid vague names:

```text
ProductManager
Helper
Utility
Processor
CommonService
DataService
BaseService
```

A class name should reveal why it exists.

---

# 48. Folder Guidelines

Prefer:

```text
Features/
└── Orders/
    └── ConfirmOrder/
```

over:

```text
Commands/
Handlers/
Validators/
DTOs/
Services/
```

when the application is large enough that technical folders become difficult to navigate.

Keep feature-specific DTOs and validators near the feature that uses them.

Keep truly shared concepts under:

```text
Application/Common/
```

but do not turn `Common` into a dumping ground.

---

# 49. Security Rules

Always:

- validate user input
- use parameterized queries / EF Core
- authenticate protected endpoints
- authorize sensitive actions
- protect secrets
- use HTTPS
- avoid exposing internal exception details
- rate-limit sensitive endpoints when appropriate
- validate uploaded files
- enforce ownership checks server-side
- do not trust client-supplied roles or permissions

Never assume the frontend has already validated or authorized an operation.

The backend is the security boundary.

---

# 50. Performance Rules

Before adding complex infrastructure:

1. measure
2. identify the bottleneck
3. optimize the actual problem

Common improvements:

```text
AsNoTracking
Projection
Pagination
Proper indexes
Avoiding N+1 queries
Efficient joins
Caching
Batching
Connection pooling
```

Database design and query quality usually matter more than adding additional architectural layers.

---

# 51. Recommended Stack

A practical modern stack:

| Area | Recommendation |
|---|---|
| Runtime | Modern supported .NET |
| Framework | ASP.NET Core |
| Architecture | Clean + Vertical Slice |
| API style | Controllers or Minimal APIs |
| ORM | EF Core |
| Database | PostgreSQL / SQL Server |
| Validation | FluentValidation or equivalent |
| CQRS | Lightweight Commands / Queries |
| Mapping | Manual mapping first |
| Errors | ProblemDetails |
| Authentication | JWT / OIDC / Identity as required |
| Authorization | Policy-based |
| Logging | `ILogger<T>` |
| API documentation | OpenAPI |
| Testing | xUnit or NUnit |
| Observability | OpenTelemetry when needed |
| Deployment | Docker + CI/CD when applicable |

Do not add a dependency just because it is popular.

---

# 52. Golden Rules

1. **Keep controllers thin.**
2. **Organize application code around features.**
3. **Use Commands for writes and Queries for reads.**
4. **Keep business rules in the Domain.**
5. **Keep EF Core and external services in Infrastructure.**
6. **Keep the Domain independent from frameworks.**
7. **Use DTOs for API contracts.**
8. **Do not expose EF/domain entities directly.**
9. **Validate input outside controllers.**
10. **Use centralized error handling.**
11. **Use correct HTTP status codes.**
12. **Use async I/O.**
13. **Pass `CancellationToken` where practical.**
14. **Use dependency injection.**
15. **Use policy-based authorization for complex permissions.**
16. **Do not wrap EF Core in generic repositories without a reason.**
17. **Do not create a custom Unit of Work without a reason.**
18. **Project directly to DTOs for read queries when appropriate.**
19. **Use `AsNoTracking()` for read-only EF Core queries.**
20. **Do not hard-code secrets.**
21. **Use structured logging.**
22. **Test business behavior.**
23. **Use integration tests for the HTTP/database pipeline.**
24. **Keep shared code genuinely shared.**
25. **Avoid abstractions that merely forward method calls.**
26. **Start simple and add complexity only when justified.**

---

# 53. Final Mental Model

For every backend feature, think:

```text
HTTP
 │
 ▼
API
 │
 ▼
Use Case
 │
 ▼
Business Rules
 │
 ▼
Persistence / External Services
 │
 ▼
Result
```

In project terms:

```text
MyApp.Api
    │
    ▼
MyApp.Application
    │
    ▼
MyApp.Domain
    ▲
    │
MyApp.Infrastructure
```

And for a single feature:

```text
Request
   │
   ▼
Controller
   │
   ▼
Command / Query
   │
   ▼
Handler
   │
   ├────────────► Domain
   │
   └────────────► Infrastructure
                       │
                       ▼
                    Database
```

The objective is not to implement every software architecture pattern.

The objective is to make it obvious:

- where HTTP code belongs
- where a use case belongs
- where validation belongs
- where business rules belong
- where database code belongs
- where external integrations belong
- how dependencies should flow
- how a feature should be tested

If those boundaries remain clear, the .NET Web API can grow without becoming difficult to maintain.
