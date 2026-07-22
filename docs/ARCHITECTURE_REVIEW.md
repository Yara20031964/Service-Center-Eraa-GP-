# KHDMA — Architecture Review (Current State)

| | |
|---|---|
| **Date** | 22 July 2026 |
| **Branch reviewed** | `final_part` |
| **Build status** | ✅ `dotnet build KHDMA.sln` — **0 errors**, 94 warnings |
| **Related** | [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md) · [`API_CONTRACTS.md`](./API_CONTRACTS.md) · `SRS_OnDemand_Service_Platform_v2.docx` |

> **Purpose.** An audit of what exists today, so the implementation plan's premises are verifiable and the team can see what was inherited versus what still has to be built. Every finding carries a `file:line` reference that can be checked.

---

## 1. Executive Summary

The **admin and identity half** of the SRS is largely built and works. The **customer-and-provider transactional core is absent** — there is no way to create a booking through the API, no dispatch engine, no SignalR, no chat, and no live tracking.

Three things need attention before feature work begins:

1. 🔴 **Every admin endpoint is publicly accessible** — `[Authorize(Roles="Admin")]` is commented out on all of them.
2. 🔴 **`Booking.ProviderId` is `NOT NULL`**, which makes the SRS §2.2 dispatch model impossible to store.
3. 🔴 **`GET /api/admin/categories` throws at runtime** — an untranslatable EF projection.

---

## 2. Layer Map

```
KHDMA.Domain          entities, enums, ApiResponse<T>/PagedResponse<T>
KHDMA.Application     DTOs, interfaces … + 4 service IMPLEMENTATIONS
KHDMA.Infrastructure  AppDbContext, GenericRepository/UoW, 8 service impls
KHDMA.API             13 controllers, Program.cs (all DI wiring)
```

**Statistics**

| Metric | Count |
|---|---|
| C# source files (excl. migrations/obj/bin) | 100 |
| Domain entities | 20 |
| Controllers | 13 |
| EF migrations | 6 |
| Test projects | **0** |

### 2.1 Structural problems

**a) Business services are split across two layers, with two different data-access styles.**

| Location | Services | Data access |
|---|---|---|
| `KHDMA.Application/Services/Admin/` | `AdminCustomerService`, `AdminProviderService`, `AdminUserService`, `CommissionService` | `IUnitOfWork` |
| `KHDMA.Infrastructure/Services/Admin/` | `AdminBookingService`, `AdminPaymentService`, `AdminReviewService`, `AdminCategoryService`, `AdminServiceService` | **`AppDbContext` directly** |

Same problem, two answers. The Infrastructure services bypass the repository abstraction entirely; the Application services cannot use EF at all, so they page **in memory** (see §4.1).

**b) Interfaces live in two places** — `Application/Interfaces/Services/Admin/` *and* `Application/Services/Admin/IAdmin*.cs`.

**c) Namespaces are inconsistent** — `Domain.Common` vs `KHDMA.Domain.Entities`, `Application.Services.Admin` vs `KHDMA.Application.Interfaces`, `API.Controllers.Admin` vs `KHDMA.API.Controllers`. The collision forces fully-qualified type names in DI registration at `Program.cs:52-55`.

---

## 3. SRS Gap Analysis

| SRS § | Requirement | Status |
|---|---|---|
| §2.2 | Real-time dispatch engine | ❌ Not started |
| §7.1–7.3 | SignalR (dispatch, tracking, chat) | ❌ No hubs, no package |
| §5.1 | Customer booking flow | ❌ No `BookingsController` |
| §3.2.2 | Provider job accept / status pipeline | ❌ None |
| §6.1 | **Paymob** payment | ⚠️ **Stripe** implemented instead |
| §6.3 | Commission system | ⚠️ Admin API exists but nothing reads it |
| §6.3 | Wallet / weekly payouts | ❌ None |
| §7.3 | In-app chat | ⚠️ `ChatMessage` entity only, no endpoints |
| §8 | Rating & reviews | ⚠️ Entity + admin moderation; no customer create/edit, no provider reply |
| §7.4 | FCM push notifications | ❌ `Notification` table only |
| §2.1 | Redis, Docker, Serilog, blob storage | ❌ None |
| §2.1 | CQRS/MediatR, FluentValidation, AutoMapper | ❌ Plain services, manual mapping |
| §10.3 | Rate limiting, CORS | ❌ None |
| §10.4 | Audit logging | ❌ None |
| §9 | Admin dashboard APIs | ✅ Largely complete |
| §3.1, §10.1 | Auth, JWT, refresh rotation | ✅ Done |
| §3.1.3, §3.2.2 | Profile management | ✅ Done |
| §4 | Category / service CRUD | ✅ Done (flat, not hierarchical) |

**Roughly the admin/identity half is built; the customer-and-provider transactional core is not.**

---

## 4. Findings

### 🔴 Critical

#### 4.1 Every admin endpoint is publicly accessible
`[Authorize(Roles = "Admin")]` is **commented out** on all seven admin controllers, and absent entirely from the two newest:

| File | Line | State |
|---|---|---|
| `Controllers/Admin/AdminCustomersController.cs` | 8 | commented |
| `Controllers/Admin/AdminProvidersController.cs` | 9 | commented |
| `Controllers/Admin/AdminUsersController.cs` | 9 | commented |
| `Controllers/Admin/CommissionController.cs` | 9 | commented |
| `Controllers/AdminBookingsController.cs` | 11 | commented |
| `Controllers/AdminPaymentsController.cs` | 12 | commented |
| `Controllers/AdminReviewsController.cs` | 10 | commented |
| `Controllers/Admin/AdminCategoriesController.cs` | — | **no attribute** |
| `Controllers/Admin/AdminServicesController.cs` | — | **no attribute** |

Anonymous callers can ban users, create admin accounts (`POST /api/admin/users/admins`), issue Stripe refunds, and change the commission rate. `PaymentsController.cs:10` is also open — anyone can mint a payment intent for any `bookingId`.

#### 4.2 `Booking.ProviderId` is `NOT NULL` — the dispatch model cannot be stored
Confirmed in `Migrations/AppDbContextModelSnapshot.cs`:
```csharp
b.Property<string>("ProviderId").IsRequired().HasColumnType("nvarchar(450)");
```
A `Pending` / `Dispatching` booking has no provider **by definition**. Also missing for the lifecycle: `AcceptedAt`, `ArrivedAt`, `CompletedAt`, and any status-history table — `AdminBookingService.cs:152` acknowledges this in a comment. `BookingStatus` also lacks the SRS states `Failed` and `NoProviderFound`.

Same class of problem: `Notification.BookingId` is a required `Guid`, so an account-approval notification (which has no booking) cannot be stored.

#### 4.3 Unrestricted file upload into a served directory
`SaveFileAsync` is duplicated **verbatim** in three places:

| File | Line |
|---|---|
| `Infrastructure/Services/AuthService.cs` | 287 |
| `Infrastructure/Services/ProfileService.cs` | 229 |
| `Infrastructure/Services/Admin/AdminServiceService.cs` | 179 |

All three keep the caller's file extension with **no type whitelist and no size limit**, writing under `wwwroot/uploads/`, which `Program.cs:91` serves via `UseStaticFiles()`. Uploading `x.html` yields stored XSS on the application's own origin. SRS §10.3 requires a jpg/png/pdf whitelist and a 10 MB cap.

#### 4.4 `GET /api/admin/categories` throws at runtime
`Infrastructure/Services/Admin/AdminCategoryService.cs:33`:
```csharp
var items = await query
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(c => MapToDto(c))       // ← static method inside an IQueryable projection
    .ToListAsync();
```
EF Core cannot translate `MapToDto` → `InvalidOperationException`. `AdminServiceService.cs:42` does it correctly (`items.Select(MapToDto)` **after** materialising).
**Fix:** move `.Select` after `ToListAsync()`.

#### 4.5 Commission is hardcoded and contradicts the configurable setting
`Infrastructure/Services/Payment/StripePaymentService.cs:62-63`:
```csharp
CommissionAmount = booking.TotalPrice * 0.1m,     // 10%
ProviderEarning  = booking.TotalPrice * 0.9m,
```
`CommissionSettings` seeds **15%** (`AppDbContext.cs:193-201`), and `CommissionController` + `CommissionService` exist purely to manage a rate **nothing reads**. Provider payouts are currently wrong.

---

### 🟠 Significant

| # | Issue | Location |
|---|---|---|
| 1 | **In-memory paging.** `GetAsync()` calls `ToListAsync()` on the whole table, then callers `.Skip().Take()`. Every provider/customer/admin/notification list loads the full table. | `Repositories/GenericRepository.cs:53`; `AdminProviderService.cs:20-40`, `AdminUserService.cs:24`, `NotificationsController.cs:137` |
| 2 | **No login lockout.** Uses `CheckPasswordAsync` directly, so `AccessFailedCount` never increments. SRS §3.1.2 requires a 5-attempt lockout. Use `SignInManager.PasswordSignInAsync(lockoutOnFailure: true)`. | `AuthService.cs:157` |
| 3 | **Identity roles never assigned to admins.** The seeder inserts via `context.Users.Add` and `CreateAdminAsync` never calls `AddToRoleAsync`, so `AspNetUserRoles` is empty. Role checks pass **only** because the JWT carries a claim built from the `UserRole` enum. Verify before enabling `[Authorize(Roles=...)]`. | `AppDbSeeder.cs:44`, `AdminUserService.cs:94`, `AuthService.cs:253` |
| 4 | **Email confirmation is a no-op.** The token is `Console.WriteLine`'d and returned in the HTTP response. No SendGrid/SMTP. | `AuthService.cs:228-230` |
| 5 | **Email change bypasses validation.** Writes `Email`/`UserName` straight onto the entity — no uniqueness check, no `EmailConfirmed` reset. Two users can end up with the same email. Use `_userManager.SetEmailAsync`. | `ProfileService.cs:55-61` |
| 6 | **Refunds are not idempotent.** No check on current `PaymentStatus`; calling twice refunds twice on Stripe. A *partial* refund also marks the whole payment `Refunded` and force-cancels the booking. | `AdminPaymentService.cs:90-112` |
| 7 | **Certificate/portfolio upload is not role-checked.** A Customer can `POST /api/profile/certificates`; the insert then violates the FK to `Providers` → unhandled `DbUpdateException` → 500. | `ProfileService.cs:169`, `:204` |
| 8 | **Password change does not revoke refresh tokens.** Stolen tokens survive a password reset. | `ProfileService.cs:93` |
| 9 | **`NotificationsController` has no authorization** and takes `userId` from the route — any caller can read or delete anyone's notifications. | `NotificationsController.cs` (whole file) |
| 10 | **No global exception middleware.** Every unhandled exception leaks a stack trace. `UnitOfWork.CommitAsync` also wraps and rethrows as a bare `Exception`. | `Program.cs`, `UintOfWork.cs:26` |
| 11 | **Swagger is unconditional** — no `IsDevelopment()` guard. | `Program.cs:89` |
| 12 | `Provider.Rating`, `ReviewCount`, `NumberOfJobsDone`, `Service.Rating` are **never recalculated** anywhere. | — |
| 13 | Rejecting a provider application sets state to `Banned` — there is no `Rejected` state, so rejection is indistinguishable from a ban. | `AdminProviderService.cs:74` |
| 14 | `providerId.Trim('{','}',' ')` appears twice — a symptom of braced GUIDs leaking in from a caller. Fix the caller. | `AdminPaymentService.cs:116`, `:139` |
| 15 | Stripe currency is hardcoded to `"usd"` while all UI mocks show EGP/AED. | `StripePaymentService.cs:36` |

---

### 🟡 Cleanup

- **Dead / duplicate files** — `Data/Repositories/GenericRepository.cs` and `Data/Repositories/UnitOfWork.cs` are **empty** (the real implementations are in `Repositories/`). Four `Class1.cs` template files remain across the projects.
- **Filename typos** — `UintOfWork.cs` (should be `UnitOfWork.cs`), and `Interfaces/Repositories/ IUnitOfWork.cs` has a **leading space** in the filename.
- **Unused dependency** — `Pomelo.EntityFrameworkCore.MySql` is referenced in both `KHDMA.API.csproj` and `KHDMA.Infrastructure.csproj` while SQL Server is used.
- **Entity naming** — `Category.id` and `Service.id` are lowercase while every other PK is `Id`. `Address.AddresssLine` (three s's) and `Review.CleanlinesRating` are misspelled. Cheap to fix now, expensive once the Flutter client binds to them.
- **Unused entities** — `CustomerFavorite`, `CustomerFavoriteProvider`, `ChatMessage`, `ProviderService` have tables and EF mappings but **no endpoints**.
- **Flat categories** — SRS §4.1 requires a parent/subcategory tree; `Category` has no `ParentId`.
- **Configuration is undocumented** — `appsettings*.json` is gitignored (`.gitignore:10-11`) and no example file is committed, so required keys (`Jwt:Key`, `StripeSettings`, connection string) are discoverable only by reading code.
- **`Program.cs` does all DI registration inline** — consider `AddInfrastructure()` / `AddApplication()` extension methods.
- **Mixed comment languages** — two Arabic XML-doc comments in `StripePaymentService` / `PaymentsController` among otherwise-English code.
- **`docs/` is untracked in git** — even the SRS is not committed.

---

## 5. What Works Well

Worth stating plainly, since the list above is long:

- **`ApiResponse<T>` / `PagedResponse<T>`** (`Domain/Common/`) are clean, consistent, and used by every admin endpoint. Keep them; do not introduce a second envelope.
- **Refresh-token rotation** is correctly implemented — `AuthService.RefreshTokenAsync:189` revokes the old token before issuing a new one, matching SRS §10.1.
- **The EF model is thorough** — 20 entities with explicit relationship configuration, sensible `DeleteBehavior.Restrict` on financial paths, and composite keys on the favourites join tables.
- **`ProfileService`** cleanly polymorphises across Customer/Provider/Admin from one endpoint.
- **The seeder** produces a genuinely useful demo dataset (customer, provider, category, service, three bookings in different states, two payments, one review).
- **Payment confirmation verifies with Stripe directly** (`StripePaymentService.ConfirmPaymentAsync:102`) rather than trusting the client — the right instinct.

---

## 6. Recommended Order of Work

1. **Add `[Authorize(Roles="Admin")]` to all 9 admin controllers**, and `[Authorize]` to `PaymentsController` + `NotificationsController`. One line per file; closes the largest hole.
2. **Fix `AdminCategoryService.GetAllAsync`** — that endpoint is broken today.
3. **Migration**: nullable `Booking.ProviderId` + `Notification.BookingId`, lifecycle timestamps, `BookingStatusHistory`, new `BookingStatus` members. Unblocks everything downstream.
4. **Whitelist upload types and sizes**; extract the triplicated `SaveFileAsync` into one `IFileStorageService`.
5. **Read commission from `CommissionSettings`** in the payment path.
6. Then build the dispatch engine — see [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md).

---

## 7. Deviations to Confirm with the Supervisor

These are design choices rather than defects, but the SRS currently describes something else. Amending the SRS keeps them from reading as unfinished work at defence.

| SRS § | Specified | Implemented | Assessment |
|---|---|---|---|
| §6.1 | Paymob | **Stripe** | Fully working. Paymob would be a gateway swap behind `IStripePaymentService`. |
| §2.1 | CQRS + MediatR, FluentValidation, AutoMapper | Plain services, manual mapping | Simpler; no functional loss. Defensible for a project this size. |
| §10.2 | BCrypt work factor 12 | ASP.NET Identity PBKDF2 | Identity's default is arguably stronger. Amend the SRS text. |
| §4.1 | Hierarchical category tree | Flat `Category → Service` | A real gap if subcategories are demonstrated. |
| §7.4 | FCM push | DB + SignalR only | No Firebase credentials available. |
| §2.1 | .NET 10 | **.NET 9** | `net9.0` in all four `.csproj` files. |
