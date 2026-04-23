# Ftareqi.API — Smart Carpooling Backend

<!-- markdownlint-disable MD033 MD058 MD060 MD031 -->

> ASP.NET Core Web API (.NET 8) powering a full-featured carpooling platform.

Ftareqi handles secure authentication, driver onboarding and moderation, wallet top-ups, ride lifecycle management, and real-time notifications — all built on Clean Architecture.

---

## Table of Contents

- [Key Features](#key-features)
- [Core Features](#core-features)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [API Endpoint Groups](#api-endpoint-groups)
- [Configuration](#configuration)
- [Runtime Notes](#runtime-notes)
- [Observability](#observability)
- [How to Run](#how-to-run)
- [Contributing](#contributing)
- [Contact](#contact)

---

## Key Features

- **Authentication & Security** — JWT + refresh tokens + OTP via SMS
- **Role-based Access Control** — Admin / Moderator roles and `DriverOnly` claim policy
- **User Profiles** — Profile management with image upload/update
- **Driver Onboarding** — Driver profile, car registration, and document image upload
- **Moderation Workflow** — Approve or reject driver requests
- **Ride Management** — Create, search, and cancel rides
- **Booking Lifecycle** — Request, accept/decline, cancel, and history
- **Reviews & Reputation** — Ride reviews with driver rating aggregation
- **Wallet & Payments** — Paymob card and mobile-wallet top-ups
- **Notifications** — Real-time via SignalR, push via FCM, with read/unread management
- **Background Jobs** — Driver expiry and booking expiry handlers via Hangfire
- **Admin Operations** — Paginated user management with role assignment and removal
- **Rate Limiting** — Redis-backed token-bucket per user ID or client IP
- **Distributed Caching** — Redis-backed distributed caching service
- **Structured Logging** — Serilog + Seq

---

## Core Features

### Authentication & Security

- User registration and password validation
- Login with JWT access and refresh tokens
- Logout support (single session or all devices)
- Token refresh without re-authentication
- OTP-based phone verification and resend
- Secure password reset via OTP
- Password change for authenticated users
- Role-based access control (Admin, Moderator, User)
- Claim-based policy authorization (`DriverOnly` policy)
- Account lockout protection after failed attempts

### User & Profile

- Get authenticated user profile with full details
- Upload and update profile images (async, Cloudinary-backed)
- Get driver and car profile summary with status and documents

### Driver Registration & Moderation

- Create driver profile with license info and document images
- Add car details with documents and license expiry
- Update profiles with re-submission for review
- Driver status workflow: Pending -> Active / Rejected / Expired
- Moderator approval and rejection with notifications
- Automatic driver claim assignment on approval
- Scheduled license expiry checks and deactivation
- Event notifications for registration status changes

### Wallet & Payments

- Wallet balance tracking and management
- Transaction history with pagination and filtering
- Top-up via mobile wallet and card payments (Paymob integration)
- Payment callback processing with signature verification
- Transaction status tracking (pending, success, failed)
- Booking workflow integration with wallet amount lock/release transitions

### Notifications

- Notification center with CRUD operations (list, get, mark read, delete)
- Unread count tracking for UI badge updates
- Device token management for FCM registration
- Real-time delivery via SignalR (`/notificationHub`)
- Push notifications via Firebase Cloud Messaging (FCM)
- Broadcast and user-targeted notification support
- Event-driven notifications for booking, wallet, driver moderation, and review actions

### Background Jobs

- Asynchronous image upload and deletion via Hangfire
- Automatic retry on failure with exponential backoff
- Batch processing for driver and car documents
- Scheduled driver license expiry checks
- Scheduled pending-booking expiration with automatic wallet release
- Status transitions on upload completion

---

## Architecture

This solution follows **Clean Architecture**:

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, value objects, core business models |
| **Application** | DTOs, orchestrators, interfaces, validators, mappers, result wrappers |
| **Infrastructure** | External integrations (Cloudinary, Paymob, Firebase, SignalR, Hangfire, Twilio) |
| **Persistence** | EF Core DbContext, repositories, Unit of Work, migrations |
| **API** | Controllers, startup config, filters, middleware, global error handling |

### Solution Structure

```text
Ftareqi.sln
├── Ftareqi.API
├── Ftareqi.Application
├── Ftareqi.Domain
├── Ftareqi.Infrastructure
└── Ftareqi.Persistence
```

---

## Technology Stack

| Category | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 8) |
| Database | SQL Server + Entity Framework Core |
| Auth | ASP.NET Core Identity + JWT + Refresh Tokens |
| Validation | FluentValidation |
| Background Jobs | Hangfire + SQL Server storage |
| Cache / Rate Limiting | Redis (StackExchange.Redis) |
| Distributed Caching | Redis-backed application caching service |
| Real-time | SignalR |
| Push Notifications | Firebase Cloud Messaging (FCM) |
| SMS / OTP | Twilio |
| Media Storage | Cloudinary |
| Payments | Paymob |
| Geospatial Support | EF Core NetTopologySuite |
| Logging | Serilog + Seq |
| API Docs | Swagger / OpenAPI |
| Containerization | Docker + Docker Compose |

---

## API Endpoint Groups

<details>
<summary><strong>Auth</strong> — <code>/api/auth</code></summary>

| Method | Endpoint |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| POST | `/api/auth/logout` |
| POST | `/api/auth/logout/all` |
| POST | `/api/auth/token/refresh` |
| POST | `/api/auth/phone/verify` |
| POST | `/api/auth/phone/resend-otp` |
| POST | `/api/auth/password/reset/request-otp` |
| POST | `/api/auth/password/reset/verify-otp` |
| POST | `/api/auth/password/reset` |
| POST | `/api/auth/password/change` |
</details>

<details>
<summary><strong>Profile</strong> — <code>/api/profile</code></summary>

| Method | Endpoint |
|---|---|
| GET | `/api/profile` |
| GET | `/api/profile/driver` |
| GET | `/api/profile/driver/{userId}` |
| GET | `/api/profile/driver/car` |
| POST | `/api/profile/image` |
| PUT | `/api/profile/image` |
</details>

<details>
<summary><strong>Driver Registration</strong> — <code>/api/users/{userId}/driver-profile</code></summary>

| Method | Endpoint |
|---|---|
| POST | `/api/users/{userId}/driver-profile` |
| POST | `/api/users/{userId}/driver-profile/car` |
| PATCH | `/api/users/{userId}/driver-profile` |
| PATCH | `/api/users/{userId}/driver-profile/car` |
</details>

<details>
<summary><strong>Rides</strong> — <code>/api/rides</code></summary>

| Method | Endpoint |
|---|---|
| POST | `/api/rides` (DriverOnly) |
| GET | `/api/rides/search` |
| GET | `/api/rides/driver/upcoming` |
| GET | `/api/rides/driver/past` |
| POST | `/api/rides/{rideId}/cancel` (DriverOnly) |
</details>

<details>
<summary><strong>Ride Bookings</strong> — <code>/api/ride-bookings</code></summary>

| Method | Endpoint |
|---|---|
| POST | `/api/ride-bookings` |
| GET | `/api/ride-bookings/{bookingId}` |
| GET | `/api/ride-bookings/driver/requests` |
| GET | `/api/ride-bookings/user/upcoming` |
| GET | `/api/ride-bookings/user/history` |
| POST | `/api/ride-bookings/{bookingId}/accept` (DriverOnly) |
| POST | `/api/ride-bookings/{bookingId}/decline` (DriverOnly) |
| POST | `/api/ride-bookings/{bookingId}/cancel` |
</details>

<details>
<summary><strong>Reviews</strong> — <code>/api/reviews</code></summary>

| Method | Endpoint |
|---|---|
| POST | `/api/reviews` |
| PUT | `/api/reviews/{reviewId}` |
| DELETE | `/api/reviews/{reviewId}` |
| GET | `/api/reviews/driver/{driverProfileId}` |
| GET | `/api/reviews/ride/{rideId}/all` |
| GET | `/api/reviews/ride-booking/{rideBookingId}` |
</details>

<details>
<summary><strong>Wallet</strong> — <code>/api/wallet</code></summary>

| Method | Endpoint |
|---|---|
| GET | `/api/wallet` |
| GET | `/api/wallet/transactions` |
| POST | `/api/wallet/top-up/mobile-wallet` |
| POST | `/api/wallet/top-up/card` |
| POST | `/api/wallet/callback` |
</details>

<details>
<summary><strong>Notifications</strong> — <code>/api/notification</code></summary>

| Method | Endpoint |
|---|---|
| GET | `/api/notification` |
| GET | `/api/notification/{id}` |
| PUT | `/api/notification/{id}/mark-as-read` |
| PUT | `/api/notification/mark-all-as-read` |
| GET | `/api/notification/unread-count` |
| DELETE | `/api/notification/{id}` |
| POST | `/api/notification/register-fcm-token` |
| POST | `/api/notification/deactivate-fcm-token` |
</details>

<details>
<summary><strong>Admin</strong> — <code>/api/admin</code></summary>

| Method | Endpoint |
|---|---|
| GET | `/api/admin/users` |
| GET | `/api/admin/users/{userId}` |
| POST | `/api/admin/users/{userId}/add-role/{role}` |
| DELETE | `/api/admin/users/{userId}/remove-role/{role}` |
</details>

<details>
<summary><strong>Moderator</strong> — <code>/api/moderator/driver-requests</code></summary>

| Method | Endpoint |
|---|---|
| GET | `/api/moderator/driver-requests/pending` |
| GET | `/api/moderator/driver-requests/{driverId}` |
| POST | `/api/moderator/driver-requests/{driverId}/approve` |
| POST | `/api/moderator/driver-requests/{driverId}/reject` |
</details>

---

## Configuration

Configure via `appsettings.*`, environment variables, or user secrets.

<details>
<summary>View all configuration keys</summary>

```text
ConnectionStrings__DefaultConnection
ConnectionStrings__HangfireConnection
ConnectionStrings__RedisConnection

JWTSettings__SignInKey
JWTSettings__Audience
JWTSettings__Issuer
JWTSettings__AccessTokenExpiryInMinutes

TwilioSettings__AccountSID
TwilioSettings__AuthToken
TwilioSettings__TwilioPhoneNumber

CloudinarySettings__CloudName
CloudinarySettings__ApiKey
CloudinarySettings__ApiSecret

PaymobSettings__HMAC
PaymobSettings__APIKey
PaymobSettings__MerchantID
PaymobSettings__CardIntegrationId
PaymobSettings__IframeId
PaymobSettings__WalletIntegrationId

FirebaseSettings__ProjectId
FirebaseSettings__PrivateKeyId
FirebaseSettings__PrivateKey
FirebaseSettings__ClientEmail
FirebaseSettings__ClientId
FirebaseSettings__AuthUri
FirebaseSettings__TokenUri
FirebaseSettings__AuthProviderX509CertUrl
FirebaseSettings__ClientX509CertUrl
```
</details>

---

## Runtime Notes

### Rate Limiting

- Redis-backed token-bucket middleware applies to all requests.
- Authenticated users are throttled by **user ID**; unauthenticated by **client IP**.
- Default buckets are configured separately for authenticated and unauthenticated traffic.
- Exceeded limits return `HTTP 429` with a `Retry-After` header.

### SignalR Notifications

- Hub path: `/notificationHub`
- Requires authentication.
- Accepts JWT via `access_token` query string for WebSocket upgrade scenarios.

### Background Jobs (Hangfire)

| Job | Schedule |
|---|---|
| `deactivate-expired-drivers` | Daily |
| `expire-pending-bookings` | Every 2 minutes |

### Request Pipeline Notes

- Global exception handling is enabled through `GlobalErrorHandler` with standardized problem details responses.
- CORS uses `FlexiblePolicy` to allow credentials with dynamic origins.
- Serilog request logging middleware is enabled for request/response diagnostics.

---

## Observability

- Structured logging with **Serilog**
- Centralized log aggregation via **Seq** (`http://localhost:5300`)
- Rolling file logs at `Ftareqi.API/Logs`
- Hangfire dashboard at `/hangfire`

---

## How to Run

### Option A — Docker (Recommended)

**1. Create a `.env` file** in the root directory (same level as `docker-compose.yml`):

```env
DB_NAME=FtareqiDb
DB_USER=sa
DB_PASSWORD=YourStrong(!)Password

HANGFIRE_DB_NAME=FtareqiHangfireDb
HANGFIRE_DB_USER=sa
HANGFIRE_DB_PASSWORD=YourStrong(!)Password

REDIS_CONNECTION=redis:6379

JWT_SIGNIN_KEY=replace-with-a-long-random-secret
JWT_AUDIENCE=FtareqiAudience
JWT_ISSUER=FtareqiIssuer
JWT_ACCESS_TOKEN_EXPIRY_IN_MINUTES=30

TwilioSettings__AccountSID=your-twilio-account-sid
TwilioSettings__AuthToken=your-twilio-auth-token
TwilioSettings__TwilioPhoneNumber=your-twilio-phone-number

CLOUDINARY_CLOUD_NAME=your-cloud-name
CLOUDINARY_API_KEY=your-cloudinary-api-key
CLOUDINARY_API_SECRET=your-cloudinary-api-secret

PAYMOB_HMAC=your-paymob-hmac
PAYMOB_API_KEY=your-paymob-api-key
PAYMOB_MERCHANT_ID=your-paymob-merchant-id
PAYMOB_CARD_INTEGRATION_ID=your-card-integration-id
PAYMOB_IFRAME_ID=your-iframe-id
PAYMOB_WALLET_INTEGRATION_ID=your-wallet-integration-id

FIREBASE_PROJECT_ID=your-firebase-project-id
FIREBASE_PRIVATE_KEY_ID=your-private-key-id
FIREBASE_PRIVATE_KEY=your-private-key
FIREBASE_CLIENT_EMAIL=your-client-email
FIREBASE_CLIENT_ID=your-client-id
FIREBASE_AUTH_URI=your-auth-uri
FIREBASE_TOKEN_URI=your-token-uri
FIREBASE_AUTH_PROVIDER_X509_CERT_URL=your-auth-provider-cert-url
FIREBASE_CLIENT_X509_CERT_URL=your-client-cert-url
```

**2. Build and start containers:**

```bash
docker compose up -d --build
```

**3. Access:**

| Service | URL |
|---|---|
| Swagger | [http://localhost:5342/swagger](http://localhost:5342/swagger) |
| Hangfire Dashboard | [http://localhost:5342/hangfire](http://localhost:5342/hangfire) |
| Seq | [http://localhost:5300](http://localhost:5300) |

---

### Option B — Local .NET

**Prerequisites:** .NET 8 SDK, SQL Server, Redis

**1. Configure** settings in `Ftareqi.API/appsettings.json` or via environment variables / user secrets.

**2. Apply migrations:**

```bash
dotnet ef database update --project Ftareqi.Persistence --startup-project Ftareqi.API
```

**3. Run:**

```bash
dotnet run --project Ftareqi.API
```

**4. Access:**

| Service | URL |
|---|---|
| Swagger (HTTP) | [http://localhost:5124/swagger](http://localhost:5124/swagger) |
| Swagger (HTTPS) | [https://localhost:7184/swagger](https://localhost:7184/swagger) |
| Hangfire Dashboard | [http://localhost:5124/hangfire](http://localhost:5124/hangfire) |

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m "Add your feature"`
4. Push: `git push origin feature/your-feature`
5. Open a pull request

---

## Contact

- **GitHub:** [@ZiadRawash](https://github.com/ZiadRawash)
- **Repository:** [Ftareqi.Api](https://github.com/ZiadRawash/Ftareqi.Api)
