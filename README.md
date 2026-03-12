# Ftareqi.API — Smart Carpooling Backend (ASP.NET Core 8)

Ftareqi is a backend API for a smart carpooling platform. It provides secure authentication, driver onboarding and moderation, wallet top-ups, and real-time notifications.

## Key Features

This backend provides:

- Authentication and account security (JWT + Refresh Tokens + OTP workflows)
- User profile management with profile image upload/update
- Driver onboarding flow (driver profile + car + document images)
- Moderator approval/rejection workflow for driver requests
- Admin user and role management endpoints
- Wallet service with Paymob card and mobile-wallet top-ups
- Notification center with read/unread management
- Real-time notifications via SignalR and push notifications via FCM
- Background jobs via Hangfire (media processing + license-expiry checks)
- Structured logging with Serilog and Seq

## Architecture

This solution follows Clean Architecture:

- **Domain**: Entities, enums, value objects, and core business models
- **Application**: DTOs, orchestrators, interfaces, validators, mappers, and result wrappers
- **Infrastructure**: External integrations (Cloudinary, Paymob, Firebase, SignalR, Hangfire jobs)
- **Persistence**: EF Core DbContext, repositories, Unit of Work, and migrations
- **API**: Controllers, startup configuration, filters, and global error handling

### Solution Structure

```text
Ftareqi.sln
├── Ftareqi.API
├── Ftareqi.Application
├── Ftareqi.Domain
├── Ftareqi.Infrastructure
└── Ftareqi.Persistence
```

## Technology Stack

- **Framework**: ASP.NET Core Web API (.NET 8)
- **Architecture**: Clean Architecture
- **Database**: SQL Server + Entity Framework Core
- **Identity/Auth**: ASP.NET Core Identity + JWT + Refresh Tokens
- **Validation**: FluentValidation
- **Background Jobs**: Hangfire + SQL Server storage
- **Real-time Communication**: SignalR
- **Push Notifications**: Firebase Cloud Messaging (FCM)
- **Media Storage**: Cloudinary
- **Payments**: Paymob
- **Logging**: Serilog + Seq
- **API Documentation**: Swagger / OpenAPI
- **Containerization**: Docker + Docker Compose

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
- Phone number verification and confirmation tracking

### Driver Registration & Moderation

- Create driver profile with license info and document images
- Add car details with documents and license expiry
- Update profiles with re-submission for review
- Driver status workflow: Pending → Active / Rejected / Expired
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

### Notifications

- Notification center with CRUD operations (list, get, mark read, delete)
- Unread count tracking for UI badge updates
- Device token management for FCM registration
- Real-time delivery via SignalR (`/notificationHub`)
- Push notifications via Firebase Cloud Messaging (FCM)
- Broadcast and user-targeted notification support

### Background Jobs

- Asynchronous image upload and deletion via Hangfire
- Automatic retry on failure with exponential backoff
- Batch processing for driver and car documents
- Scheduled driver license expiry checks
- Status transitions on upload completion

## API Endpoint Groups

### Auth (`/api/auth`)

- `POST /register`
- `POST /login`
- `POST /logout`
- `POST /logout/all`
- `POST /token/refresh`
- `POST /phone/verify`
- `POST /phone/resend-otp`
- `POST /password/reset/request-otp`
- `POST /password/reset/verify-otp`
- `POST /password/reset`
- `POST /password/change`

### Profile (`/api/profile`)

- `GET /api/profile`
- `GET /api/profile/driver`
- `GET /api/profile/driver/car`
- `POST /api/profile/image`
- `PUT /api/profile/image`

### Driver Registration

- `POST /api/users/{userId}/driver-profile`
- `POST /api/users/{userId}/driver-profile/car`
- `PATCH /api/users/{userId}/driver-profile`
- `PATCH /api/users/{userId}/driver-profile/car`

### Wallet (`/api/wallet`)

- `GET /api/wallet`
- `GET /api/wallet/transactions`
- `POST /api/wallet/top-up/mobile-wallet`
- `POST /api/wallet/top-up/card`
- `POST /api/wallet/callback`

### Notification (`/api/notification`)

- `GET /api/notification`
- `GET /api/notification/{id}`
- `PUT /api/notification/{id}/mark-as-read`
- `PUT /api/notification/mark-all-as-read`
- `GET /api/notification/unread-count`
- `DELETE /api/notification/{id}`
- `POST /api/notification/register-fcm-token`
- `POST /api/notification/deactivate-fcm-token`

### Admin (`/api/admin`)

- `GET /api/admin/users`
- `GET /api/admin/users/{userId}`
- `POST /api/admin/users/{userId}/add-role/{role}`
- `DELETE /api/admin/users/{userId}/remove-role/{role}`

### Moderator (`/api/moderator/driver-requests`)

- `GET /pending`
- `GET /{driverId}`
- `POST /{driverId}/approve`
- `POST /{driverId}/reject`

## Configuration

Configure these settings using `appsettings.*`, environment variables, or user secrets:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__HangfireConnection`
- `JWTSettings__SignInKey`
- `JWTSettings__Audience`
- `JWTSettings__Issuer`
- `JWTSettings__AccessTokenExpiryInMinutes`
- `CloudinarySettings__CloudName`
- `CloudinarySettings__ApiKey`
- `CloudinarySettings__ApiSecret`
- `PaymobSettings__HMAC`
- `PaymobSettings__APIKey`
- `PaymobSettings__MerchantID`
- `PaymobSettings__CardIntegrationId`
- `PaymobSettings__IframeId`
- `PaymobSettings__WalletIntegrationId`
- `FirebaseSettings__ProjectId`
- `FirebaseSettings__PrivateKeyId`
- `FirebaseSettings__PrivateKey`
- `FirebaseSettings__ClientEmail`
- `FirebaseSettings__ClientId`
- `FirebaseSettings__AuthUri`
- `FirebaseSettings__TokenUri`
- `FirebaseSettings__AuthProviderX509CertUrl`
- `FirebaseSettings__ClientX509CertUrl`

## How to Run

### Option A — Docker (Recommended)

1. Create a `.env` file in the root directory (same level as `docker-compose.yml`):

```env
DB_NAME=FtareqiDb
DB_USER=sa
DB_PASSWORD=YourStrong(!)Password
```

2. Build and run containers:

```bash
docker compose up -d --build
```

3. Open:

- Swagger: `http://localhost:5342/swagger`
- Seq: `http://localhost:5300`

### Option B — Run Locally with .NET

1. Install prerequisites:
   - .NET 8 SDK
   - SQL Server

2. Configure settings in `Ftareqi.API/appsettings.json` (or via environment variables/user secrets).

3. Apply migrations:

```bash
dotnet ef database update --project Ftareqi.Persistence --startup-project Ftareqi.API
```

4. Run API:

```bash
dotnet run --project Ftareqi.API
```

5. Open:

- Swagger: `http://localhost:5124/swagger`
- Hangfire Dashboard: `http://localhost:5124/hangfire`

## Observability

- Structured logging with Serilog
- Seq integration for centralized logs
- Rolling file logs in `Ftareqi.API/Logs`
- Hangfire dashboard at `/hangfire`

## Contributing

- Fork the repository
- Create a feature branch (`git checkout -b feature/your-feature`)
- Commit your changes (`git commit -m "Add your feature"`)
- Push your branch (`git push origin feature/your-feature`)
- Open a pull request

## Contact

- **GitHub:** [@ZiadRawash](https://github.com/ZiadRawash)
- **Project Link:** [Ftareqi.Api](https://github.com/ZiadRawash/Ftareqi.Api)
