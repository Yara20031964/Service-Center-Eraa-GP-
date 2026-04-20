# Add Endpoint Skill

You are adding a new endpoint to the **KHDMA Service Center** .NET 9 Clean Architecture API.
Follow every rule below exactly — no deviations. Read each section before writing a single line of code.

---

## STEP 0 — Decide Which Service Layer to Use

Ask yourself: does the service need complex EF queries (multi-level `.Include()`, raw projections via `.Select()` on `IQueryable`, `CountAsync`, `ToListAsync`)?

| Condition | Service Layer | Location |
|---|---|---|
| Simple filter + include via `IUnitOfWork` | **Application** | `KHDMA.Application/Services/Admin/` |
| Complex EF queries, joins, DbContext-level control | **Infrastructure** | `KHDMA.Infrastructure/Services/Admin/` |

This decision affects where the **interface** lives too (see Step 2).

---

## STEP 1 — Create the DTOs

**Location:** `KHDMA.Application/DTOs/Admin/XxxDto.cs`

**Namespace rule:** Always `Application.DTOs.Admin` (no `KHDMA.` prefix).

```csharp
using KHDMA.Domain.Enums;

namespace Application.DTOs.Admin;

// Response DTO
public class XxxDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // ... map from entity
}

// Request DTO (only if endpoint receives a body)
public class CreateXxxDto
{
    public string Name { get; set; } = string.Empty;
    // ... input fields
}
```

**Rules:**
- One file per resource (can have multiple DTOs inside)
- No `[Required]` data annotations — validation is handled in the service
- Never expose navigation properties, only flat scalar fields
- Use `string.Empty` defaults for strings, not `null`

---

## STEP 2 — Create the Service Interface

### If Application-layer service:
**Location:** `KHDMA.Application/Services/Admin/IXxxService.cs`
**Namespace:** `Application.Services.Admin`

```csharp
using Application.DTOs.Admin;
using Domain.Common;

namespace Application.Services.Admin;

public interface IXxxService
{
    // Paginated list
    Task<PagedResponse<XxxDto>> GetAllAsync(string? search, int page, int pageSize);

    // Single item
    Task<ApiResponse<XxxDto>> GetByIdAsync(Guid id);

    // Create
    Task<ApiResponse<XxxDto>> CreateAsync(CreateXxxDto dto);

    // Update / action
    Task<ApiResponse<string>> UpdateAsync(Guid id, CreateXxxDto dto);

    // Delete / status change
    Task<ApiResponse<string>> DeleteAsync(Guid id);
}
```

### If Infrastructure-layer service:
**Location:** `KHDMA.Application/Interfaces/Services/Admin/IXxxService.cs`
**Namespace:** `KHDMA.Application.Interfaces.Services.Admin`

```csharp
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Interfaces.Services.Admin;

public interface IXxxService
{
    Task<PagedResponse<XxxDto>> GetAllAsync(int page, int pageSize, SomeEnum? filter);
    Task<ApiResponse<XxxDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<bool>> DoActionAsync(Guid id, string reason);
}
```

---

## STEP 3 — Implement the Service

### Application-layer implementation:
**Location:** `KHDMA.Application/Services/Admin/XxxService.cs`
**Namespace:** `Application.Services.Admin`
**Inject:** `IUnitOfWork`

```csharp
using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace Application.Services.Admin;

public class XxxService : IXxxService
{
    private readonly IUnitOfWork _unitOfWork;

    public XxxService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── Paginated list ─────────────────────────────────────────────────
    public async Task<PagedResponse<XxxDto>> GetAllAsync(string? search, int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<Entity>()
            .GetAsync(
                e => !e.IsDeleted,                          // filter
                includes: [e => e.NavigationProp],          // eager load (omit if none)
                tracked: false);

        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var totalCount = all.Count();

        var items = all
            .OrderByDescending(e => e.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new XxxDto
            {
                Id = e.Id,
                Name = e.Name,
            });

        return PagedResponse<XxxDto>.Ok(items, totalCount, page, pageSize);
    }

    // ── Single item ────────────────────────────────────────────────────
    public async Task<ApiResponse<XxxDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<Entity>()
            .GetOneAsync(e => e.Id == id && !e.IsDeleted, tracked: false);

        if (entity is null)
            return ApiResponse<XxxDto>.NotFound("Xxx not found");

        return ApiResponse<XxxDto>.Ok(new XxxDto
        {
            Id = entity.Id,
            Name = entity.Name,
        });
    }

    // ── Create ─────────────────────────────────────────────────────────
    public async Task<ApiResponse<XxxDto>> CreateAsync(CreateXxxDto dto)
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreateAt = DateTime.UtcNow,
        };

        await _unitOfWork.Repository<Entity>().CreateAsync(entity);
        await _unitOfWork.CommitAsync();

        return ApiResponse<XxxDto>.Created(new XxxDto
        {
            Id = entity.Id,
            Name = entity.Name,
        });
    }

    // ── Status-change / action ─────────────────────────────────────────
    public async Task<ApiResponse<string>> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<Entity>()
            .GetOneAsync(e => e.Id == id && !e.IsDeleted);

        if (entity is null)
            return ApiResponse<string>.NotFound("Xxx not found");

        entity.IsDeleted = true;
        _unitOfWork.Repository<Entity>().Update(entity);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Xxx deleted successfully");
    }
}
```

### Infrastructure-layer implementation:
**Location:** `KHDMA.Infrastructure/Services/Admin/XxxService.cs`
**Namespace:** `KHDMA.Infrastructure.Services.Admin`
**Inject:** `AppDbContext`

```csharp
using Microsoft.EntityFrameworkCore;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;

namespace KHDMA.Infrastructure.Services.Admin;

public class XxxService : IXxxService
{
    private readonly AppDbContext _context;

    public XxxService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<XxxDto>> GetAllAsync(int page, int pageSize, SomeEnum? filter)
    {
        var query = _context.Entities
            .Include(e => e.RelatedEntity)
            .AsQueryable();

        if (filter.HasValue)
            query = query.Where(e => e.Status == filter.Value);

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new XxxDto
            {
                Id = e.Id,
                Name = e.RelatedEntity.Name,
            })
            .ToListAsync();

        return PagedResponse<XxxDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<XxxDto>> GetByIdAsync(Guid id)
    {
        var entity = await _context.Entities
            .Include(e => e.RelatedEntity)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            return ApiResponse<XxxDto>.NotFound("Xxx not found");

        return ApiResponse<XxxDto>.Ok(new XxxDto { Id = entity.Id });
    }

    public async Task<ApiResponse<bool>> DoActionAsync(Guid id, string reason)
    {
        var entity = await _context.Entities.FindAsync(id);
        if (entity is null)
            return ApiResponse<bool>.NotFound("Xxx not found");

        entity.Status = SomeEnum.Done;
        entity.Reason = reason;

        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Action completed successfully");
    }
}
```

---

## STEP 4 — Create the Controller

**Location:** `KHDMA.API/Controllers/XxxController.cs`
**Namespace:** `API.Controllers.Admin`

```csharp
using Application.DTOs.Admin;
using Application.Services.Admin;   // or KHDMA.Application.Interfaces.Services.Admin for infra services
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/xxx")]
// [Authorize(Roles = "Admin")]
public class XxxController : ControllerBase
{
    private readonly IXxxService _service;

    public XxxController(IXxxService service)
    {
        _service = service;
    }

    // GET api/admin/xxx?search=&page=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(search, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/xxx/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/admin/xxx
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateXxxDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/xxx/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateXxxDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    // DELETE api/admin/xxx/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/xxx/{id}/some-action  (status changes, approvals, etc.)
    [HttpPut("{id}/some-action")]
    public async Task<IActionResult> SomeAction(Guid id, [FromBody] string reason)
    {
        var result = await _service.DoActionAsync(id, reason);
        return StatusCode(result.StatusCode, result);
    }
}
```

**Controller rules — non-negotiable:**
- ALWAYS `return StatusCode(result.StatusCode, result)` — never `return Ok(result)` alone
- ALWAYS comment out `[Authorize]` (auth is not implemented yet)
- ALWAYS `[ApiController]` + `[Route(...)]` on the class
- NEVER put business logic in controllers
- NEVER catch exceptions in controllers — let the service handle it

---

## STEP 5 — Register in Program.cs

Open `KHDMA.API/Program.cs` and add the scoped registration **after the existing registrations**:

### Application-layer service (simple namespace):
```csharp
builder.Services.AddScoped<IXxxService, XxxService>();
```

### Infrastructure-layer service (requires full namespace to avoid ambiguity):
```csharp
builder.Services.AddScoped<
    KHDMA.Application.Interfaces.Services.Admin.IXxxService,
    KHDMA.Infrastructure.Services.Admin.XxxService>();
```

---

## STEP 6 — Build and Verify

```bash
dotnet build
```

Fix any compiler errors before considering the task done. Then run:
```bash
dotnet run --project KHDMA.API
```

Open `http://localhost:{PORT}/swagger` and verify the new endpoints appear.

---

## Response Factory Reference

| Situation | Code to use |
|---|---|
| Success, return data | `ApiResponse<T>.Ok(data)` → HTTP 200 |
| Created new resource | `ApiResponse<T>.Created(data)` → HTTP 201 |
| Bad input / business rule violated | `ApiResponse<T>.Fail("message")` → HTTP 400 |
| Resource not found | `ApiResponse<T>.NotFound("message")` → HTTP 404 |
| Not authenticated | `ApiResponse<T>.Unauthorized()` → HTTP 401 |
| Not permitted | `ApiResponse<T>.Forbidden()` → HTTP 403 |
| Unexpected crash | `ApiResponse<T>.ServerError()` → HTTP 500 |
| Paginated list | `PagedResponse<T>.Ok(items, totalCount, page, pageSize)` → HTTP 200 |

---

## Namespace Cheat Sheet

| File type | Namespace |
|---|---|
| Domain entities | `KHDMA.Domain.Entities` |
| Domain enums | `KHDMA.Domain.Enums` |
| ApiResponse / PagedResponse | `Domain.Common` |
| Admin DTOs | `Application.DTOs.Admin` |
| Auth DTOs | `KHDMA.Application.DTOs.Auth.Request` / `.Response` |
| App-layer service interface | `Application.Services.Admin` |
| App-layer service implementation | `Application.Services.Admin` |
| Infra-layer service interface | `KHDMA.Application.Interfaces.Services.Admin` |
| Infra-layer service implementation | `KHDMA.Infrastructure.Services.Admin` |
| Controllers | `API.Controllers.Admin` |

---

## Common Mistakes to Avoid

1. **Do NOT use `return Ok(result)` in controllers** — always `StatusCode(result.StatusCode, result)`
2. **Do NOT call `_context.SaveChangesAsync()` in Application-layer services** — only `_unitOfWork.CommitAsync()`
3. **Do NOT forget `tracked: false`** on read-only queries to avoid EF change tracking overhead
4. **Do NOT use `DateTime.UtcNow` in seed data or `HasData`** — always use a static date
5. **Do NOT add `[Required]` on DTOs** — validate in service and return `ApiResponse.Fail()`
6. **Do NOT create a new `GenericRepository<T>` directly** — always access through `_unitOfWork.Repository<T>()`
7. **Do NOT skip the `[ApiController]` attribute** on controllers
8. **Do NOT name the interface file differently from the service** — `IXxxService.cs` → `XxxService.cs`
