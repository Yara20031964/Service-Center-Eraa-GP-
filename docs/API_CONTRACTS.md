# KHDMA — API & Real-Time Contracts
### The reference document for the Flutter team

| | |
|---|---|
| **Version** | 1.0 |
| **Status** | Draft for review — **implement Flutter models against this** |
| **Backend** | .NET 9, ASP.NET Core + SignalR |
| **Related** | [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md) · [`ARCHITECTURE_REVIEW.md`](./ARCHITECTURE_REVIEW.md) |

> **Why this document exists.** The Flutter app already has the complete UI for the booking, dispatch, tracking and chat flow — but every screen renders hardcoded i18n keys and has no data layer. These contracts were **designed from what those screens actually display**, so each DTO field below is annotated with the widget and `lang/en.json` key that requires it. If a field is here, a pixel depends on it.

---

## 1. Conventions

Every endpoint follows these. They are not negotiable per-endpoint.

### 1.1 JSON casing
**camelCase** — the ASP.NET Core default. C# `NameEn` serializes as `nameEn`.

> ⚠️ The existing `core/api/dio_consumer.dart` was inherited from an unrelated Laravel project and expects `snake_case` (`name_en`, `created_at`). **New models must use camelCase.** The auth models in `features/auth/data/models/` are the old convention — do not copy them.

### 1.2 Response envelope
Every single-object response is wrapped in `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Success",
  "data": { },
  "statusCode": 200
}
```

Every list response is wrapped in `PagedResponse<T>`:

```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": [ ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

These already exist server-side (`KHDMA.Domain/Common/ApiResponse.cs`, `PagedResponse.cs`) and are used by all current admin endpoints — write **one** generic Dart wrapper and reuse it everywhere.

On failure: `success: false`, `data: null`, `message` carries a human-readable reason, `statusCode` carries the HTTP code.

### 1.3 Localisation
Bilingual fields ship **both** values — `nameEn` **and** `nameAr`. The app switches language at runtime via `config/locale/app_localizations.dart`; it must **not** re-fetch on language change.

Status values ship **three** fields: the machine value plus both labels —
`status: "EnRoute"`, `statusLabelEn: "En Route"`, `statusLabelAr: "في الطريق"`.
Switch on `status`, display the label. Never parse the label.

### 1.4 Types
| Concept | Format | Example |
|---|---|---|
| Ids (booking, service, category) | GUID string | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| Ids (user, provider, customer) | string (Identity id) | `"a1b2c3d4-..."` |
| Timestamps | **UTC** ISO-8601 | `"2026-07-22T14:30:00Z"` |
| Money | decimal, always with `currency` | `250.00`, `"EGP"` |
| Distance | kilometres, 1 dp | `2.5` |
| Duration / ETA | whole minutes | `18` |
| Enums | PascalCase string, never int | `"InProgress"` |

### 1.5 Authentication
- REST: `Authorization: Bearer <accessToken>`
- SignalR: `?access_token=<accessToken>` on the hub URL — browsers cannot set headers on a WebSocket handshake
- Access token 15 min, refresh token 30 days with rotation (`POST /api/auth/refresh-token`)
- `401` → refresh once, then route to login. The existing `AppInterceptors.onError` already emits `eventBus.emitUnauthorized()` on 401 — keep that.

---

## 2. Endpoint Index

### 2.1 Public — no auth *(SRS §10.1)*
| Method | Route | Response |
|---|---|---|
| GET | `/api/categories/public` | `List<PublicCategoryDto>` |
| GET | `/api/services/public?categoryId=&search=&page=&pageSize=` | `Paged<PublicServiceDto>` |
| GET | `/api/services/public/{id}` | `PublicServiceDetailDto` |
| GET | `/api/providers/{id}/public` | `ProviderPublicProfileDto` |

### 2.2 Customer — `Roles = "Customer"`
| Method | Route | Request | Response |
|---|---|---|---|
| POST | `/api/bookings` | `CreateBookingDto` | `BookingDetailDto` |
| GET | `/api/bookings?tab=all\|active\|completed\|cancelled` | — | `Paged<BookingListItemDto>` |
| GET | `/api/bookings/{id}` | — | `BookingDetailDto` |
| POST | `/api/bookings/{id}/cancel` | `CancelBookingDto` | `BookingDetailDto` |
| GET | `/api/bookings/{id}/eta` | — | `EtaDto` |
| POST | `/api/payments/intent/{bookingId}` | — | `string` (clientSecret) |
| POST | `/api/payments/confirm/{intentId}` | — | `bool` |
| GET | `/api/chat/{bookingId}/history?page=` | — | `Paged<ChatMessageDto>` |
| GET | `/api/chat/threads` | — | `List<ChatThreadDto>` |

### 2.3 Provider — `Roles = "Provider"`
| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/provider/dashboard` | — | `ProviderDashboardDto` |
| GET | `/api/provider/jobs?tab=today\|upcoming\|past` | — | `Paged<BookingListItemDto>` |
| POST | `/api/provider/jobs/{id}/accept` | — | `AcceptResultDto` |
| POST | `/api/provider/jobs/{id}/decline` | — | `string` |
| PUT | `/api/provider/jobs/{id}/status` | `UpdateJobStatusDto` | `BookingDetailDto` |
| PUT | `/api/provider/availability` | `{ "status": "Online" }` | `string` |
| PUT | `/api/location/update` | `UpdateLocationDto` | `string` |
| GET | `/api/providers/earnings?period=daily\|weekly\|monthly` | — | `EarningsDto` |
| GET | `/api/providers/wallet` | — | `WalletDto` |

### 2.4 Shared — any authenticated role
| Method | Route | Response |
|---|---|---|
| GET | `/api/profile` | `CustomerProfileDto` \| `ProviderProfileDto` \| `AdminProfileDto` |
| PUT | `/api/profile` | updated profile |
| GET | `/api/notifications?page=` | `Paged<NotificationDto>` |
| PUT | `/api/notifications/read/{id}` | `string` |

### 2.5 Admin — `Roles = "Admin"`
| Method | Route | Response |
|---|---|---|
| GET | `/api/admin/dashboard/summary` | `AdminDashboardSummaryDto` |
| GET | `/api/admin/chat/{bookingId}/transcript?page=` | `Paged<ChatMessageDto>` |
| *(existing admin routes unchanged)* | | |

---

## 3. Real-Time Contracts (SignalR)

### 3.1 Hubs
| Hub | URL | Purpose |
|---|---|---|
| `BookingHub` | `/hubs/booking` | Dispatch, status pipeline, live location |
| `ChatHub` | `/hubs/chat` | 1:1 messaging scoped to a booking |

### 3.2 Connecting
```dart
final connection = HubConnectionBuilder()
    .withUrl('$baseUrl/hubs/booking?access_token=$accessToken')
    .withAutomaticReconnect()
    .build();

await connection.start();
await connection.invoke('JoinBooking', args: [bookingId]);
```
Add `signalr_netcore` to `pubspec.yaml` — **it is not currently a dependency.**

### 3.3 Groups
| Group | Members | Joined |
|---|---|---|
| `user:{userId}` | that one user | automatically on connect |
| `booking:{bookingId}` | the customer + assigned provider | `JoinBooking(bookingId)` |
| `providers:online` | providers with `AvailabilityStatus == Online` | automatically on connect |

> `JoinBooking` performs an **ownership check**. Calling it for a booking you are not party to throws a `HubException` — handle it.

### 3.4 Server → Client Events

| Event | Payload | To | Fires when |
|---|---|---|---|
| `JobDispatched` | `JobCardDto` | matching providers | A booking is broadcast — start the 60s countdown |
| `JobDispatchExpired` | `{ bookingId }` | round's providers | 60s elapsed with no accept — dismiss the card |
| `JobTaken` | `{ bookingId }` | losing providers | Another provider won — dismiss the card |
| `JobCancelled` | `{ bookingId, reason }` | assigned provider | Customer or admin cancelled |
| `BookingStatusChanged` | `BookingStatusEventDto` | `booking:{id}` | Any status transition — drives the tracking stepper |
| `ProviderAssigned` | `ProviderCardDto` | customer | A provider accepted — show "Provider Found!" |
| `ProviderLocation` | `ProviderLocationDto` | `booking:{id}` | ~every 5s while En Route — move the map pin |
| `NoProviderFound` | `{ bookingId, roundsTried }` | customer | All dispatch rounds exhausted |
| `PaymentStatusChanged` | `PaymentStatusEventDto` | `booking:{id}` | Payment confirmed / failed / refunded |
| `ReceiveMessage` | `ChatMessageDto` | `booking:{id}` | New chat message |
| `MessageRead` | `{ bookingId, messageId }` | `booking:{id}` | Peer read a message |
| `ChatLocked` | `{ bookingId }` | `booking:{id}` | Booking completed/cancelled — disable the input bar |
| `PresenceChanged` | `{ userId, isOnline }` | `booking:{id}` | Peer connected/disconnected — drives `chat_status_online` |

### 3.5 Client → Server Methods
| Hub | Method | Notes |
|---|---|---|
| Booking | `JoinBooking(Guid)` / `LeaveBooking(Guid)` | ownership-checked |
| Chat | `JoinBooking(Guid)` / `LeaveBooking(Guid)` | ownership-checked |
| Chat | `SendMessage(Guid bookingId, string text, string type)` | `type` ∈ `Text` \| `Image` \| `QuickReply` |
| Chat | `MarkRead(Guid bookingId, Guid messageId)` | |

### 3.6 Reconnect rule
SignalR delivery is **not** guaranteed across a dropped connection. Chat messages are always persisted **before** broadcast, so on `onreconnected`:
1. re-`JoinBooking`
2. call `GET /api/chat/{bookingId}/history?page=1` and merge by message `id`

Do **not** rely on the socket alone for chat history.

---

## 4. DTO Reference

Each DTO lists the Flutter widget and `lang/en.json` keys it was derived from.

### 4.1 `JobCardDto` — the incoming-request card
**Drives:** `provider_jobs_job_card.dart`, `provider_incoming_*` keys, `/provider-incoming-request` route
**Delivered by:** `JobDispatched` event

```json
{
  "bookingId": "3fa85f64-...",
  "serviceNameEn": "Pipe Leakage Repair",
  "serviceNameAr": "إصلاح تسرب المواسير",
  "categoryNameEn": "Plumbing",
  "categoryNameAr": "سباكة",
  "customerFirstName": "Sara",
  "customerAvatarUrl": "/uploads/profiles/abc.jpg",
  "distanceKm": 2.5,
  "providerEarning": 212.50,
  "currency": "EGP",
  "estimatedDurationMin": 45,
  "estimatedDurationMax": 90,
  "bookingType": "Immediate",
  "scheduledTime": null,
  "expiresAt": "2026-07-22T14:31:00Z",
  "countdownSeconds": 60
}
```

> ⚠️ **Two deliberate design points.**
> 1. **`providerEarning`, not the customer total.** The mock shows `provider_incoming_price = "EGP 212"` against a 250 service — that is 250 × 0.85, net of the 15% commission. The provider sees what they will be paid.
> 2. **`distanceKm` only — no address.** SRS §7.1 requires the job card to show *distance only, not the exact address*. The full address is returned only after accept, in `BookingDetailDto`.
>
> Render the countdown from `expiresAt` (absolute), not `countdownSeconds` (which is only a convenience) — the card may arrive after network delay.

### 4.2 `BookingListItemDto` — booking / job list rows
**Drives:** `booking_service_card.dart`, `bookings_screen.dart`, `provider_jobs_screen.dart`
**Keys:** `bookings_service_*`, `bookings_provider_*`, `bookings_time_*`, `bookings_status_*`, `bookings_action_rate`, `bookings_action_rebook`

```json
{
  "id": "3fa85f64-...",
  "serviceNameEn": "Plumbing Service",
  "serviceNameAr": "خدمة سباكة",
  "serviceImageUrl": "/uploads/services/x.jpg",
  "categoryNameEn": "PLUMBING",
  "categoryNameAr": "سباكة",
  "counterpartyName": "FixIt Experts",
  "counterpartyAvatarUrl": "/uploads/profiles/y.jpg",
  "scheduledTime": "2026-10-24T10:00:00Z",
  "timeRangeStart": "2026-10-24T10:00:00Z",
  "timeRangeEnd": "2026-10-24T11:00:00Z",
  "status": "Accepted",
  "statusLabelEn": "ACTIVE",
  "statusLabelAr": "نشط",
  "totalPrice": 250.00,
  "currency": "EGP",
  "canRate": false,
  "canRebook": false,
  "canCancel": true,
  "hasUnreadMessages": true
}
```

> `counterpartyName` is deliberately role-neutral: the **provider's** name when the customer fetches, the **customer's** name when the provider fetches. One DTO, both screens — `bookings_provider_fixit` on one side, `provider_jobs_card_1_customer` on the other.
>
> `canRate` / `canRebook` / `canCancel` are computed **server-side** from status and the cancellation policy (SRS §5.2). The app must not re-derive them — that is how the two clients drift.

### 4.3 `BookingDetailDto`
**Drives:** `booking_details_screen.dart`, `provider_tracking_screen.dart`
Everything in `BookingListItemDto`, plus:

```json
{
  "descriptionEn": "Replacement and sealing of kitchen water fixtures.",
  "descriptionAr": "...",
  "address": "123 Gardenia St, New Cairo",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "notes": "Please bring extra pipes",
  "attachmentUrl": null,
  "bookingType": "Scheduled",
  "priceBreakdown": { },
  "provider": { },
  "payment": { },
  "review": { },
  "etaMinutes": 18,
  "statusHistory": [
    { "status": "Pending",     "statusLabelEn": "Paid",     "changedAt": "..." },
    { "status": "Dispatching", "statusLabelEn": "Searching","changedAt": "..." },
    { "status": "Accepted",    "statusLabelEn": "Accepted", "changedAt": "..." }
  ]
}
```

`statusHistory` drives the tracking stepper directly. The UI's seven steps map as:

| UI chip (`lang/en.json`) | `status` |
|---|---|
| `home_provider_tracking_step_paid` → "Paid" | `Pending` (payment confirmed) |
| `home_provider_tracking_step_searching` → "Searching..." | `Dispatching` |
| `home_provider_tracking_step_accepted` → "Accepted" | `Accepted` |
| `home_provider_tracking_step_en_route` → "En Route" | `EnRoute` |
| `home_track_live_step_arrived` → "Arrived" | `Arrived` |
| `home_track_live_step_in_progress` → "In Progress" | `InProgress` |
| `home_track_live_step_job_completed` → "Job Completed" | `Completed` |

### 4.4 `PriceBreakdownDto`
**Drives:** `almost_done_price_breakdown_card.dart`
**Keys:** `home_almost_done_service_fee`, `home_almost_done_vat` ("VAT (10%)"), `home_almost_done_total`

```json
{
  "serviceFee": 250.00,
  "vatRate": 0.10,
  "vatAmount": 25.00,
  "total": 275.00,
  "currency": "EGP"
}
```

> ⚠️ **Schema gap.** The UI shows a VAT line but the `Payment` entity has **no VAT field** — only `Amount`, `CommissionAmount`, `ProviderEarning`. Phase 0 of the implementation plan adds `ServiceFee` and `VatAmount`.
>
> Commission is calculated on the **service fee**, not the VAT-inclusive total: `providerEarning = serviceFee × (1 − commissionRate)`. VAT is collected on behalf of the tax authority and is not platform revenue.

### 4.5 `ProviderCardDto`
**Drives:** `provider_found_provider_card.dart`, `provider_tracking_provider_card.dart`
**Keys:** `home_provider_found_*`, `home_provider_tracking_provider_*`
**Delivered by:** `ProviderAssigned` event, and nested in `BookingDetailDto`

```json
{
  "providerId": "a1b2c3d4-...",
  "fullName": "Mohamed Hassan",
  "jobTitle": "Professional Plumber",
  "avatarUrl": "/uploads/profiles/z.jpg",
  "rating": 4.9,
  "reviewCount": 87,
  "isVerified": true,
  "etaMinutes": 18,
  "distanceKm": 3.2,
  "phoneNumber": "+201234567890",
  "currentLatitude": 30.0500,
  "currentLongitude": 31.2400
}
```
`rating` + `reviewCount` render `home_provider_found_provider_rating_reviews` = "★ 4.9 (87 reviews)". `phoneNumber` backs `home_track_live_call`.

> Per SRS §8, an overall rating is only displayed once the provider has **≥ 3 reviews**. When `reviewCount < 3`, `rating` is returned as `null` — render "New provider" rather than 0.0.

### 4.6 `BookingStatusEventDto`
**Delivered by:** `BookingStatusChanged`

```json
{
  "bookingId": "3fa85f64-...",
  "status": "EnRoute",
  "statusLabelEn": "En Route",
  "statusLabelAr": "في الطريق",
  "changedAt": "2026-07-22T14:35:00Z",
  "etaMinutes": 18,
  "message": null
}
```

### 4.7 `ProviderLocationDto`
**Drives:** `track_live_screen.dart`
**Delivered by:** `ProviderLocation`, ~every 5s while En Route (SRS §7.2)

```json
{
  "bookingId": "3fa85f64-...",
  "providerId": "a1b2c3d4-...",
  "latitude": 30.0512,
  "longitude": 31.2388,
  "headingDegrees": 145.0,
  "etaMinutes": 16,
  "updatedAt": "2026-07-22T14:36:05Z"
}
```
> Location is held in a 5-minute TTL cache and **never persisted** (SRS §7.2). After a booking completes there is no location history to query.

### 4.8 `ChatThreadDto`
**Drives:** `chat_thread_card.dart`, `chats_screen.dart`
**Keys:** `chat_thread_*`, `chat_status_online`

```json
{
  "bookingId": "3fa85f64-...",
  "peerId": "a1b2c3d4-...",
  "peerName": "Ahmed",
  "peerRoleEn": "Plumbing",
  "peerRoleAr": "سباكة",
  "peerAvatarUrl": "/uploads/profiles/a.jpg",
  "lastMessage": "I can be there in 20 minutes.",
  "lastMessageAt": "2026-07-22T14:20:00Z",
  "unreadCount": 2,
  "isOnline": true,
  "isLocked": false,
  "scheduleChipEn": "FAUCET INSTALLATION — TODAY 10:00 AM",
  "scheduleChipAr": "تركيب حنفية — اليوم ١٠:٠٠ ص"
}
```
`chat_thread_ahmed_plumber` renders as `"$peerName - $peerRoleEn"`. `unreadCount` drives the red badge. `lastMessageAt` is a **timestamp** — the "2m" / "1h" relative label is formatted client-side.

### 4.9 `ChatMessageDto`
**Drives:** `chat_message_bubble.dart`
**Delivered by:** `ReceiveMessage`, and `GET /api/chat/{bookingId}/history`

```json
{
  "id": "9f8e7d6c-...",
  "bookingId": "3fa85f64-...",
  "senderId": "a1b2c3d4-...",
  "isMine": false,
  "messageType": "Text",
  "messageText": "Hello! I'm on my way to your location.",
  "attachmentUrl": null,
  "sentAt": "2026-07-22T09:15:00Z",
  "isRead": true
}
```
> **`isMine` is computed server-side** from the JWT. It maps directly onto the widget's existing `isOutgoing` parameter — do not compare ids client-side.
>
> `messageType` ∈ `Text` | `Image` | `QuickReply`. Quick replies (`chat_quick_reply_home`, `chat_quick_reply_call_first`, `chat_quick_reply_on_my_way`) are sent as `QuickReply` with the **key** in `messageText`, so the recipient renders them in *their* language.
>
> **PII filter:** `SendMessage` rejects text containing a phone number or email with `400` (SRS §7.3). Surface the rejection message in the input bar.
>
> **Auto-lock:** once the booking is `Completed`/`Cancelled`, sends are rejected and `ChatLocked` fires. History stays readable.

### 4.10 `ProviderPublicProfileDto`
**Drives:** `provider_profile_screen.dart` + its 8 section widgets
**Keys:** `provider_profile_*`

```json
{
  "id": "a1b2c3d4-...",
  "fullName": "Mohamed Hassan",
  "jobTitle": "Plumbing Expert",
  "avatarUrl": "/uploads/profiles/z.jpg",
  "isVerified": true,
  "isOnline": true,
  "rating": 4.9,
  "reviewCount": 87,
  "numberOfJobsDone": 143,
  "experienceYears": 5,
  "descriptionEn": "Highly skilled plumbing expert with over 5 years...",
  "descriptionAr": "...",
  "servicesOffered": [
    { "id": "...", "nameEn": "Drain Unblocking", "nameAr": "تسليك مجاري" }
  ],
  "workingAreas": [ "Nasr City", "Maadi", "Heliopolis" ],
  "portfolioImages": [ "/uploads/portfolio/1.jpg" ],
  "certificates": [
    { "id": "...", "imageUrl": "/uploads/certificates/1.jpg",
      "name": "Advanced Plumbing Cert", "issuer": "National Trades" }
  ],
  "reviews": [
    { "id": "...", "customerName": "Sarah Ahmed",
      "customerAvatarUrl": "/uploads/profiles/s.jpg",
      "rating": 5, "comment": "Fixed my kitchen leak in no time.",
      "createdAt": "2026-07-18T12:00:00Z" }
  ]
}
```

> ⚠️ **Two schema gaps to resolve before this ships.**
> 1. `Provider.ServiceArea` is a **single `string?`**, but the UI renders multiple area chips (`provider_profile_area_nasr_city`, `_maadi`, `_heliopolis`). The API returns a split array; a proper `ProviderServiceArea` table should follow.
> 2. `ProviderCertificateImage` holds **only `ImageUrl`** — no name, no issuer. But the card shows `provider_profile_certificate_name` and `provider_profile_certificate_issuer`. Either add those columns or drop the labels from the UI. Until resolved the API returns `name: null, issuer: null`.

### 4.11 `ProviderDashboardDto`
**Drives:** `provider/home` screens
**Keys:** `provider_stat_jobs`, `provider_stat_earnings`, `provider_stat_rating`, `provider_status_online`, `provider_incoming_section`, `provider_todays_jobs`

```json
{
  "fullName": "Mohamed",
  "avatarUrl": "/uploads/profiles/z.jpg",
  "availabilityStatus": "Online",
  "todayJobsCount": 3,
  "todayEarnings": 750.00,
  "rating": 4.9,
  "currency": "EGP",
  "incomingRequest": { },
  "todaysJobs": [ ]
}
```
`incomingRequest` is a `JobCardDto` or `null`; `todaysJobs` is `List<BookingListItemDto>`.

### 4.12 `EarningsDto` / `WalletDto` / `PayoutDto`
**Drives:** `provider_tab_wallet`, `provider_quick_payouts`, `/provider-earnings` route

```json
// EarningsDto
{
  "period": "weekly",
  "from": "2026-07-16T00:00:00Z",
  "to": "2026-07-22T23:59:59Z",
  "totalGross": 1500.00,
  "totalCommissionDeducted": 225.00,
  "totalEarned": 1275.00,
  "bookingsCount": 6,
  "currency": "EGP",
  "breakdown": [
    { "bookingId": "...", "serviceName": "Pipe Repair",
      "date": "2026-07-20T10:00:00Z",
      "gross": 250.00, "commission": 37.50, "net": 212.50 }
  ]
}

// WalletDto
{
  "availableBalance": 1275.00,
  "pendingBalance": 250.00,
  "totalWithdrawn": 4000.00,
  "currency": "EGP",
  "nextPayoutDate": "2026-07-26T00:00:00Z",
  "recentPayouts": [
    { "id": "...", "amount": 1200.00, "status": "Paid",
      "requestedAt": "...", "paidAt": "...", "reference": "po_123" }
  ]
}
```
> `commission` is **15%** (`CommissionSettings`), configurable by admin. Note the backend currently hardcodes 10% in `StripePaymentService` — Phase 4 fixes this. Do not hardcode either value in Dart; read it from the response.

### 4.13 `PublicServiceDto` / `PublicCategoryDto`
**Drives:** `home_screen.dart`, `category_service_card.dart`, `service_details_screen.dart`
**Keys:** `home_popular_services`, `home_service_*`, `home_category_*`

```json
// PublicCategoryDto
{ "id": "...", "nameEn": "Plumbing", "nameAr": "سباكة",
  "iconUrl": "/uploads/categories/p.png", "serviceCount": 12 }

// PublicServiceDto
{
  "id": "...",
  "nameEn": "AC Maintenance", "nameAr": "صيانة تكييف",
  "descriptionEn": "...", "descriptionAr": "...",
  "imageUrl": "/uploads/services/ac.jpg",
  "imageUrls": [ "/uploads/services/ac1.jpg" ],
  "categoryId": "...", "categoryNameEn": "HVAC", "categoryNameAr": "تكييف",
  "fixedPrice": 250.00, "currency": "EGP",
  "estimatedDurationMin": 45, "estimatedDurationMax": 90,
  "rating": 4.8, "reviewCount": 142
}
```
`rating` + `reviewCount` render `home_service_rating_label` = "4.8 (142 reviews)". Mirrors the existing internal `ServiceDto` minus `isActive`; the endpoint filters to active services only.

`PublicServiceDetailDto` adds `whatsIncluded: [ { textEn, textAr } ]` — backing `home_service_include_item_1..3`.

> ⚠️ **Not yet modelled.** `Service` has no "what's included" column. Either add a `ServiceInclusion` table or drop the section. Returns `[]` until resolved.

### 4.14 Request DTOs

```json
// CreateBookingDto — POST /api/bookings
{
  "serviceId": "...",
  "bookingType": "Immediate",          // or "Scheduled"
  "scheduledTime": null,               // required when Scheduled
  "address": "123 Gardenia St, New Cairo",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "addressId": null,                   // optional: use a saved address instead
  "notes": "Gate code 1234",
  "attachmentUrl": null
}
```
> **`totalPrice` is deliberately absent.** The server snapshots `Service.FixedPrice` — a client-supplied price is never trusted.

```json
// CancelBookingDto
{ "reason": "Changed my mind" }

// UpdateJobStatusDto — PUT /api/provider/jobs/{id}/status
{ "status": "EnRoute" }              // EnRoute | Arrived | InProgress | Completed

// UpdateLocationDto — PUT /api/location/update
{ "latitude": 30.0512, "longitude": 31.2388, "headingDegrees": 145.0 }
```

### 4.15 `EtaDto` / `AcceptResultDto`
```json
// EtaDto — GET /api/bookings/{id}/eta
{ "bookingId": "...", "etaMinutes": 18, "distanceKm": 6.4,
  "source": "GoogleMaps", "calculatedAt": "2026-07-22T14:36:00Z" }

// AcceptResultDto — POST /api/provider/jobs/{id}/accept
{ "bookingId": "...", "accepted": true, "booking": { } }
```
`source` is `"GoogleMaps"` or `"Haversine"` (fallback when the Maps API is unavailable). Show ETA as approximate when it is `"Haversine"`.

**On `409` from accept:** another provider won. Dismiss the card silently — a `JobTaken` event will also arrive.

---

## 5. Booking Status Reference

| `status` | EN label | AR label | Set by |
|---|---|---|---|
| `Pending` | Paid | تم الدفع | Booking created, payment confirmed |
| `Dispatching` | Searching... | جاري البحث | Dispatch engine broadcast |
| `Accepted` | Accepted | تم القبول | First provider accepted |
| `EnRoute` | En Route | في الطريق | Provider started navigation |
| `Arrived` | Arrived | وصل | Provider marked arrived |
| `InProgress` | In Progress | جاري التنفيذ | Provider started the job |
| `Completed` | Completed | مكتمل | Provider marked complete |
| `Cancelled` | Cancelled | ملغي | Customer, provider or admin |
| `Failed` | Failed | فشل | Payment or system error |
| `NoProviderFound` | No Provider Found | لا يوجد مزود | All dispatch rounds exhausted |

> `Failed` and `NoProviderFound` are **new** in Phase 0. Enum members are appended, never reordered — existing rows store the integer value.

---

## 6. Known Contract Mismatches
> Flagged here so they are decided, not discovered mid-integration.

| # | Issue | Resolution |
|---|---|---|
| 1 | Job card price is the **provider's net**, not the customer total | Documented — field named `providerEarning` |
| 2 | UI shows VAT 10%; `Payment` has no VAT field | Phase 0 adds `ServiceFee` + `VatAmount` |
| 3 | `Provider.ServiceArea` is one string; UI shows multiple chips | API returns an array; table migration later |
| 4 | `ProviderCertificateImage` has no name/issuer; UI shows both | Add columns **or** drop the labels — **decision needed** |
| 5 | `Service` has no "what's included"; UI shows 3 bullets | Add table **or** drop the section — **decision needed** |
| 6 | `AuthResponseDto` returns `{ isSuccess, token, errorMessage }`, breaking the `{ success, message, data }` envelope every other endpoint uses | Align to `ApiResponse<TokenResponseDto>` — **breaking change for the existing Flutter `AuthRespModel`** |
| 7 | `dio_consumer.dart` base URL still points at `https://api.world-apm.com/api` | Point at the KHDMA backend |
| 8 | `signalr_netcore` is not in `pubspec.yaml` | Add it |
| 9 | No `google_maps_flutter` / `geolocator` in `pubspec.yaml`, but live tracking needs both | Add them |

---

## 7. Open Questions

1. **Currency** — mocks show both `EGP` (most screens) and `AED` (`notifications_payment_success_desc`). Single currency, or per-country? Stripe is currently hardcoded to `usd` in `StripePaymentService.cs:36`.
2. **Certificates** — add `name`/`issuer` columns, or simplify the UI card?
3. **"What's included"** — model it, or remove the section from `service_details_screen`?
4. **Vehicle plate** — `home_track_live_plate` shows "DXB 4821", but there is no vehicle concept anywhere in the domain. Drop it, or add a provider vehicle?
5. **Aspect ratings** — `Review` has `PunctualityRating`, `WorkQualityRating`, `CleanlinesRating`, but SRS §8 also lists **Communication**, which has no column. Add it?
