# **Ftareqi.API — Smart Carpooling Platform Backend (ASP.NET Core 8)**

## Project Overview

**Ftareqi.API** Smart carpooling backend API built with .NET 8. Connects drivers and passengers for shared rides with real-time tracking, trip management, wallet payments, and admin control for a safe, transparent experience.

---

## Tech Stack

- **Framework:** .NET 8 (ASP.NET Core Web API)
- **Architecture:** Clean Architecture (Domain → Application → Infrastructure → API)
- **Database:** SQL Server 2022 (Entity Framework Core)
- **Authentication:** JWT (Access + Refresh Tokens)
- **Verification:** OTP-based Phone Verification
- **Authorization:** Role-based + Policy-based
- **Validation:** FluentValidation
- **Mapping:** AutoMapper / Custom Mappers
- **Logging:** Serilog + Seq
- **Containerization:** Docker & Docker Compose
- **Documentation:** Swagger / OpenAPI

---

## Authentication & Security

### **JWT Authentication System**

- Secure access & refresh tokens
- Logout (current session)
- Logout from all devices
- Secure credential hashing

### **Phone Verification (OTP)**

- OTP code generation
- OTP validation
- Resend OTP workflow

### **Password Management**

- Request OTP for password reset
- Validate reset OTP
- Reset password using reset token
- Change password with validation

### **Role & Policy-Based Authorization**

- Roles: Admin, User
- Custom policies using claims (e.g., "Driver" claim for drivers)

### **User Management**

- Secure user registration with input validation
- Account/profile management
- Session tracking

---

## Architecture

Ftareqi.API follows a layered **Clean Architecture**, ensuring maintainability and separation of concerns.

### Layer Responsibilities

| Layer           | Responsibility                                        |
|-----------------|-------------------------------------------------------|
| **Domain**      | Core models, enums, and business rules                |
| **Application** | DTOs, interfaces, orchestrators, validation, mapping |
| **Infrastructure** | External services and integrations                |
| **Persistence** | EF Core DbContext, repositories, migrations          |
| **API**         | Controllers, middleware, startup config, Swagger      |

### Implemented Features

#### Authentication Module

- Register
- Login
- Logout (single session)
- Logout all sessions
- Refresh token generation
- Phone verification (OTP)
- Resend OTP
- Request password reset OTP
- Validate password reset OTP
- Reset password
- Change password

#### Logging

- Centralized structured logging using **Serilog**
- **Seq** dashboard for monitoring logs in real-time

#### Containerized Infrastructure

- API + SQL Server + Seq using Docker Compose
- Separate Docker network
- Persistent volumes for DB and log storage

### API Documentation

Swagger UI is enabled in development mode:  
`http://localhost:5342/swagger`

### Authentication Endpoints

| Method | Endpoint                                    | Description                  |
|--------|---------------------------------------------|------------------------------|
| POST   | `/api/auth/register`                        | Register a new user          |
| POST   | `/api/auth/login`                           | Authenticate user            |
| POST   | `/api/auth/logout`                          | Logout current session       |
| POST   | `/api/auth/logout/all`                      | Logout all sessions          |
| POST   | `/api/auth/token/refresh`                   | Refresh access token         |
| POST   | `/api/auth/phone/verify`                    | Verify phone with OTP        |
| POST   | `/api/auth/phone/resend-otp`                | Resend OTP                   |
| POST   | `/api/auth/password/reset/request-otp`      | Send password reset OTP      |
| POST   | `/api/auth/password/reset/verify-otp`       | Verify reset OTP             |
| POST   | `/api/auth/password/reset`                  | Reset password               |
| POST   | `/api/auth/password/change`                 | Change password              |

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/ZiadRawash/Ftareqi.Api
```

#### 2. Add a `.env` File

```env
DB_NAME=FtareqiDb
DB_USER=sa
DB_PASSWORD=YourStrong(!)Passwor
JWT_SIGNIN_KEY=fsfJiongre9098UHyB%4%t8H--9u_bi+ub4WGBO[po+i89iYV5)6RFvj*68%>O_+8124wbguhif7##!
JWT_AUDIENCE=http://localhost:5124/
JWT_ISSUER=http://localhost:5124/
JWT_ACCESS_TOKEN_EXPIRY=30
```

#### 3. Build & Start Containers

```bash
docker compose up -d --build
```

#### 4. Exposed Services

| Service          | Port   |
|------------------|--------|
| API              | **5342** |
| SQL Server       | **1433** |
| Seq Logging UI   | **5300** |

### Contributing

- Fork the repository
- Create a feature branch (`git checkout -b feature/AmazingFeature`)
- Commit your changes (`git commit -m 'Add some AmazingFeature'`)
- Push to the branch (`git push origin feature/AmazingFeature`)
- Open a Pull Request

### Contact

- **GitHub:** [@ZiadRawash](https://github.com/ZiadRawash)
- **Project Link:** [Ftareqi.Api](https://github.com/ZiadRawash/Ftareqi.Api)
