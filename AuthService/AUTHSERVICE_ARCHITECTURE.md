# AuthService — Architecture & Completeness Assessment

> **ASP.NET Core 9.0 authentication microservice for the PacketFlow platform**
>
> Clean Architecture with JWT auth, refresh tokens, RBAC, account lockout, and audit logging.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Directory Structure](#directory-structure)
4. [Layer-by-Layer Analysis](#layer-by-layer-analysis)
5. [Data Model](#data-model)
6. [API Surface](#api-surface)
7. [Authentication Flow](#authentication-flow)
8. [Security Features](#security-features)
9. [Testing](#testing)
10. [Completeness Assessment](#completeness-assessment)
11. [What's Missing / Incomplete](#whats-missing--incomplete)
12. [Build & Runtime Issues](#build--runtime-issues)
13. [File Reference Table](#file-reference-table)

---

## Overview

The AuthService is a standalone authentication microservice built on ASP.NET Core 9.0 following **Clean Architecture** principles. It provides JWT-based authentication with refresh token rotation, role-based access control, account security features (lockout, password policy), and comprehensive audit logging.

### Design Goals (per README)

| Goal | Status |
|------|--------|
| JWT-based authentication | ✅ Implemented |
| Refresh token rotation | ✅ Implemented |
| Multi-Factor Authentication (TOTP) | 🟡 Model only — no validation, no endpoints |
| OAuth 2.0 (Google, GitHub, Microsoft) | ❌ Not implemented (v1.1 roadmap) |
| Email verification & password reset | 🟡 Stub — always succeeds, no email sending |
| Role-Based Access Control (RBAC) | ✅ Implemented |
| Account lockout & audit logging | ✅ Implemented |
| API Key management | ❌ Not implemented |
| Rate limiting | ❌ Not implemented (config sections exist, no middleware) |

---

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│  AuthService.API (Presentation)                                │
│  ┌─────────────────┐  ┌──────────────────┐                     │
│  │ AuthController   │  │ UserController   │                     │
│  │ /api/auth/*      │  │ /api/user/*      │                     │
│  └────────┬─────────┘  └────────┬─────────┘                     │
│           │                      │                              │
│  ┌────────┴──────────────────────┴─────────┐                    │
│  │  Program.cs — DI, JWT, Swagger, CORS    │                    │
│  └────────────────────┬────────────────────┘                    │
└───────────────────────┼─────────────────────────────────────────┘
                        │ depends on
┌───────────────────────┼─────────────────────────────────────────┐
│  AuthService.Core (Domain — zero dependencies)                  │
│  ┌──────────┐ ┌──────────────┐ ┌───────────┐ ┌──────────────┐  │
│  │ Models   │ │ Interfaces    │ │ DTOs      │ │ Exceptions   │  │
│  │ User     │ │ IAuthService  │ │ LoginDto  │ │ Unauthorized │  │
│  │ Role     │ │ ITokenService │ │ Register  │ │ AccountLocked│  │
│  │ Refresh  │ │ IPasswordSvc  │ │ TokenResp │ │ Validation   │  │
│  │ AuthLog  │ │ IUserRepo     │ │ UserInfo  │ │              │  │
│  └──────────┘ └──────────────┘ └───────────┘ └──────────────┘  │
└───────────────────────┬─────────────────────────────────────────┘
                        │ implemented by
┌───────────────────────┼─────────────────────────────────────────┐
│  AuthService.Infrastructure                                     │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────┐    │
│  │ Services         │ │ Repositories     │ │ Data         │    │
│  │ AuthService.cs   │ │ UserRepository   │ │ AuthDbContext │    │
│  │ TokenService.cs  │ │                  │ │ Migrations   │    │
│  │ PasswordService  │ │                  │ │              │    │
│  └──────────────────┘ └──────────────────┘ └──────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Rules (Clean Architecture)

| Layer | Depends On | Rationale |
|-------|-----------|-----------|
| **Core** | Nothing | Pure domain — models, interfaces, DTOs, exceptions |
| **Infrastructure** | Core only | Implements Core interfaces, wraps EF Core + BCrypt + JWT |
| **API** | Core + Infrastructure | Wires DI, serves HTTP, depends on both for registration |
| **Tests** | All layers | Unit tests mock interfaces; integration tests use WebApplicationFactory |

---

## Directory Structure

```
AuthService/
├── AuthService.sln
├── README.md                          # User-facing docs (partially aspirational)
├── CONTRIBUTING.md                    # Developer guide (comprehensive)
│
├── src/
│   ├── AuthService.API/               # ASP.NET Core 9.0 Web API
│   │   ├── Program.cs                 # App entry point, DI, middleware pipeline
│   │   ├── appsettings.json           # PostgreSQL + JWT config
│   │   ├── appsettings.Development.json
│   │   ├── AuthService.API.csproj     # net9.0, JwtBearer, EF, Swagger
│   │   ├── Properties/launchSettings.json
│   │   └── Controllers/
│   │       ├── AuthController.cs      # /api/auth/* (7 endpoints + cookie helpers)
│   │       └── UserController.cs      # /api/user/* (3 endpoints)
│   │
│   ├── AuthService.Core/              # Pure domain (class library)
│   │   ├── AuthService.Core.csproj    # net9.0, no NuGet dependencies
│   │   ├── Configuration/
│   │   │   └── JwtSettings.cs         # POCO: SecretKey, Issuer, Audience, expiries
│   │   ├── DTOs/
│   │   │   ├── LoginDto.cs            # UsernameOrEmail, Password, RememberMe, MfaCode?
│   │   │   ├── RegisterDto.cs         # Username, Email, Password, ConfirmPassword
│   │   │   ├── RefreshTokenDto.cs     # RefreshToken string
│   │   │   ├── TokenResponseDto.cs    # AccessToken, RefreshToken, ExpiresAt, User
│   │   │   └── UserInfoDto.cs         # Id, Username, Email, Roles, MfaEnabled
│   │   ├── Exceptions/
│   │   │   ├── UnauthorizedException.cs
│   │   │   ├── AccountLockedException.cs
│   │   │   └── ValidationException.cs
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs        # Register, Login, Refresh, Revoke, VerifyEmail, Reset
│   │   │   ├── ITokenService.cs       # GenerateAccess, GenerateRefresh, Validate
│   │   │   ├── IPasswordService.cs    # Hash, Verify, Validate
│   │   │   └── IUserRepository.cs     # CRUD + Exists + GetByUsernameOrEmail
│   │   └── Models/
│   │       ├── User.cs                # Core entity: 20+ properties, nav properties, computed
│   │       ├── RefreshToken.cs        # Token lifecycle: expiry, revocation, rotation
│   │       ├── Role.cs                # Admin, User, Premium (seeded)
│   │       ├── UserRole.cs            # Many-to-many join
│   │       └── AuthLog.cs             # Audit events: login, logout, failures
│   │
│   └── AuthService.Infrastructure/    # EF Core + service implementations
│       ├── AuthService.Infrastructure.csproj
│       ├── Data/
│       │   └── AuthDbContext.cs        # 5 DbSets, full Fluent API config, seed data
│       ├── Repositories/
│       │   └── UserRepository.cs       # Implements IUserRepository
│       ├── Services/
│       │   ├── AuthService.cs          # Main auth logic: 352 lines
│       │   ├── TokenService.cs         # JWT generation + validation
│       │   └── PasswordService.cs      # BCrypt hashing + policy validation
│       └── Migrations/
│           ├── InitialCreate.cs
│           ├── InitialCreate.Designer.cs
│           └── AuthDbContextModelSnapshot.cs
│
└── tests/
    ├── AuthService.UnitTests/          # 26 tests (Moq + InMemory DB)
    │   └── Services/
    │       ├── AuthServiceTests.cs           # 6 tests
    │       ├── ComprehensiveAuthServiceTests.cs  # 12 tests
    │       ├── PasswordServiceTests.cs       # 9 tests (1 theory × 8 cases)
    │       └── TokenServiceTests.cs          # 6 tests
    │
    └── AuthService.IntegrationTests/   # 14 tests (WebApplicationFactory)
        └── Controllers/
            ├── AuthControllerTests.cs        # 5 tests
            ├── ComprehensiveAuthTests.cs     # 8 tests
            └── UserControllerTests.cs        # 4 tests
```

---

## Layer-by-Layer Analysis

### Core Layer — Models

**User** (20+ properties):
- Identity: `Id` (Guid), `Username`, `Email`, `PasswordHash`
- Profile: `FirstName`, `LastName`, `AvatarUrl`
- Status: `IsActive`, `IsLocked`, `LockoutEnd`, `FailedLoginAttempts`, `EmailVerified`
- Security: `MfaEnabled`, `MfaSecret`, `OAuthProvider`, `OAuthId`
- Timestamps: `CreatedAt`, `UpdatedAt`, `LastLoginAt`
- Navigation: `RefreshTokens`, `UserRoles`, `AuthLogs`
- Computed: `FullName`, `IsCurrentlyLocked`

**RefreshToken**: Token lifecycle with rotation tracking (`ReplacedByToken`), device info (`IpAddress`, `UserAgent`), computed properties (`IsExpired`, `IsActive`).

**Role**: Simple name + description. Three seeded: Admin, User, Premium (hardcoded GUIDs in `OnModelCreating`).

**AuthLog**: Audit trail — `EventType` (login, login_failed, registration, token_refreshed, token_revoked, mfa_required), `IpAddress`, `UserAgent`, `ErrorMessage`, `Metadata` (JSON).

### Core Layer — Interfaces

All four interfaces are well-defined and follow Single Responsibility:

- **IAuthService** (7 methods): RegisterAsync, LoginAsync, RefreshTokenAsync, RevokeTokenAsync, VerifyEmailAsync, RequestPasswordResetAsync, ResetPasswordAsync
- **ITokenService** (3 methods): GenerateAccessToken, GenerateRefreshToken, ValidateToken
- **IPasswordService** (3 methods): HashPassword, VerifyPassword, ValidatePassword
- **IUserRepository** (8 methods): GetById, GetByUsername, GetByEmail, GetByUsernameOrEmail, Exists, Add, Update, Delete, SaveChanges

### Core Layer — DTOs

Clean separation of request/response objects with DataAnnotations validation:
- `LoginDto` — `[Required]` on UsernameOrEmail and Password, optional MfaCode
- `RegisterDto` — `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Compare]` for password confirmation
- `TokenResponseDto` — 3 constructors for flexibility, includes UserInfoDto
- `UserInfoDto` — Flattened user data with roles list

### Infrastructure Layer — AuthService.cs

The main service (352 lines) implements the full auth workflow:

**RegisterAsync**: validate password → check duplicates → hash password → create user → assign "User" role → log event → generate tokens

**LoginAsync**: find user → check lockout → verify password → check MFA requirement → reset failed attempts → log event → generate tokens

**RefreshTokenAsync**: find token (with User.Roles eager-loaded) → validate IsActive → revoke old token → create new token (rotation) → generate new access token → log event

**RevokeTokenAsync**: null-safe (doesn't throw on missing token) → sets IsRevoked + RevokedAt

**VerifyEmailAsync / RequestPasswordResetAsync / ResetPasswordAsync**: STUB implementations — always return true or silently succeed. No actual email verification tokens, no password reset tokens, no email sending.

**Private helpers**: `GenerateTokenResponseAsync` (creates refresh token with configurable expiry based on RememberMe), `MapToUserInfo` (projects User → UserInfoDto), `LogAuthEventAsync` (writes AuthLog + saves).

### Infrastructure Layer — TokenService.cs

- `GenerateAccessToken`: Creates JWT with NameIdentifier, Name, Email, email_verified, and Role claims. Uses HMAC-SHA256 signing.
- `GenerateRefreshToken`: 64-byte cryptographically random (RNGCryptoServiceProvider), Base64-encoded.
- `ValidateToken`: Strict validation — issuer, audience, lifetime (zero clock skew), signing key.

### Infrastructure Layer — PasswordService.cs

- BCrypt work factor: **12** (industry standard)
- `ValidatePassword`: Enforces 5 rules — min 8 chars, uppercase, lowercase, digit, special character. Returns tuple `(bool, string?)`.
- `VerifyPassword`: Exception-safe — catches BCrypt format errors, returns false.

### Infrastructure Layer — AuthDbContext.cs

Entity Framework Core 9.0 context with 5 DbSets and comprehensive Fluent API configuration:
- Snake_case table naming (`users`, `refresh_tokens`, `roles`, `user_roles`, `auth_logs`)
- Unique indexes on Username, Email, OAuth (Provider + Id), Token
- Composite key on UserRole (UserId + RoleId)
- Cascade delete on User → RefreshTokens, User → UserRoles
- SetNull on User → AuthLogs (preserves audit trail on user deletion)
- Seed data: 3 roles with hardcoded GUIDs

### Infrastructure Layer — UserRepository.cs

Standard EF Core repository implementing IUserRepository. All read methods eager-load UserRoles → Role. `GetByUsernameOrEmail` is the login lookup method.

### API Layer — Controllers

**AuthController** (`/api/auth`):
| Endpoint | Auth | Implementation |
|----------|------|----------------|
| `POST /register` | Anonymous | Calls RegisterAsync, sets refresh token cookie, returns 201 |
| `POST /login` | Anonymous | Calls LoginAsync, sets cookie, returns 200 |
| `POST /refresh` | Anonymous | Reads cookie, calls RefreshTokenAsync, sets new cookie |
| `POST /logout` | Authorized | Reads cookie, calls RevokeTokenAsync, deletes cookie |
| `POST /verify-email` | Anonymous | Calls VerifyEmailAsync (stub) |
| `POST /forgot-password` | Anonymous | Calls RequestPasswordResetAsync (stub) |
| `POST /reset-password` | Anonymous | Calls ResetPasswordAsync (stub) |

All endpoints include try/catch → typed HTTP responses (400 for ValidationException, 401 for Unauthorized/AccountLocked).

**UserController** (`/api/user`):
| Endpoint | Auth | Implementation |
|----------|------|----------------|
| `GET /me` | Authorized | Returns current user profile as UserInfoDto |
| `PUT /profile` | Authorized | Updates FirstName, LastName, AvatarUrl |
| `GET /{id}` | Admin only | Returns any user by ID |

**Refresh token** is delivered via `HttpOnly` + `Secure` + `SameSite=Strict` cookie — good security practice. The cookie is also returned in the JSON response body for clients that can't use cookies.

### API Layer — Program.cs

Startup configuration (169 lines):
1. **DbContext**: PostgreSQL via connection string, falls back to InMemory if missing
2. **JWT Authentication**: SymmetricSecurityKey, strict validation (issuer/audience/lifetime, zero clock skew)
3. **DI Registration**: IUserRepository, IAuthService, ITokenService, IPasswordService — all Scoped
4. **CORS**: Configurable origins from appsettings, allows credentials
5. **Swagger**: OpenAPI with JWT Bearer security definition, served at root in Development
6. **Database init**: `EnsureCreated()` in Development (not Migrate — see issues below)

---

## Data Model

```
┌──────────┐       ┌──────────────┐       ┌──────────┐
│   Role   │       │  UserRole    │       │   User   │
│──────────│       │──────────────│       │──────────│
│ Id (PK)  │──1:N──│ UserId (PK,FK)│──N:1──│ Id (PK)  │
│ Name     │       │ RoleId (PK,FK)│       │ Username │
│ Desc     │       │ AssignedAt   │       │ Email    │
│ CreatedAt│       └──────────────┘       │ Password │
└──────────┘                              │ ...      │
                                          └────┬─────┘
                                               │
                          ┌────────────────────┼────────────────────┐
                          │ 1:N                │ 1:N                │
                   ┌──────┴──────┐     ┌──────┴──────┐
                   │ RefreshToken│     │  AuthLog    │
                   │─────────────│     │─────────────│
                   │ Id (PK)     │     │ Id (PK)     │
                   │ UserId (FK) │     │ UserId (FK) │ (nullable)
                   │ Token       │     │ EventType   │
                   │ ExpiresAt   │     │ Success     │
                   │ IsRevoked   │     │ IpAddress   │
                   │ ReplacedBy  │     │ UserAgent   │
                   │ DeviceInfo  │     │ ErrorMsg    │
                   └─────────────┘     └─────────────┘
```

**Seeded roles**: Admin (`11111111-...`), User (`22222222-...`), Premium (`33333333-...`)

---

## Authentication Flow

```
Register                    Login                      Token Refresh
─────────                   ─────                      ─────────────
POST /api/auth/register     POST /api/auth/login       POST /api/auth/refresh
│                           │                          │
├─ Validate password        ├─ Find user by            ├─ Read cookie
├─ Check duplicates            username/email          ├─ Find token in DB
├─ Hash password (BCrypt)   ├─ Check lockout           ├─ Validate IsActive
├─ Create User entity       ├─ Verify password         ├─ Revoke old token
├─ Assign "User" role       ├─ If MFA: require code    ├─ Generate new refresh
├─ Generate access JWT      ├─ Reset failed attempts   ├─ Generate new access
├─ Generate refresh token   ├─ Log success event       ├─ Set new cookie
├─ Store refresh in DB      ├─ Set cookie              └─ Return tokens
├─ Set HttpOnly cookie      └─ Return tokens
└─ Return 201 + tokens
```

---

## Security Features

| Feature | Implementation | Assessment |
|---------|---------------|------------|
| **Password hashing** | BCrypt, work factor 12 | ✅ Strong |
| **Password policy** | 8+ chars, upper/lower/digit/special | ✅ Enforced at registration |
| **JWT signing** | HMAC-SHA256, configurable secret | ✅ Standard |
| **Token expiry** | 15 min access, 7-30 day refresh | ✅ Configurable |
| **Refresh rotation** | Old token revoked, `ReplacedByToken` set | ✅ Prevents replay |
| **Account lockout** | 5 failed attempts → 30 min lock | ✅ Implemented |
| **HttpOnly cookies** | Refresh token in Secure+SameSite cookie | ✅ Good practice |
| **Audit logging** | All auth events logged to `auth_logs` | ✅ Comprehensive |
| **Email verification** | Stub — always returns true | ❌ Not implemented |
| **Password reset** | Stub — always returns true | ❌ No email, no tokens |
| **MFA/TOTP** | Model has MfaSecret field | ❌ No validation logic |
| **Rate limiting** | Config in appsettings | ❌ No middleware |
| **OAuth 2.0** | User has OAuthProvider/OAuthId fields | ❌ Not implemented |
| **API keys** | Not started | ❌ Not implemented |

---

## Testing

### Unit Tests — 26 tests (Moq + EF Core InMemory)

| File | Tests | Coverage |
|------|-------|----------|
| `AuthServiceTests.cs` | 6 | Register (valid, weak pw, mismatch pw, dup email), Login (valid, invalid) |
| `ComprehensiveAuthServiceTests.cs` | 12 | Register (dup username), Refresh (valid, invalid, revoked, expired), Revoke (valid, nonexistent), Login (wrong pw increments, lockout after 5, RememberMe extends), PasswordReset (valid email, nonexistent email) |
| `PasswordServiceTests.cs` | 9 | Hash, Verify (correct/incorrect/invalid hash), Validate (8 theory cases covering all policy rules), salt uniqueness |
| `TokenServiceTests.cs` | 6 | Generate access (valid, includes claims), Refresh uniqueness, Validate (valid, invalid, expired placeholder) |

### Integration Tests — 14 tests (WebApplicationFactory\<Program\>)

| File | Tests | Coverage |
|------|-------|----------|
| `AuthControllerTests.cs` | 5 | Register (valid, invalid pw), Login (valid, invalid), Refresh (valid), Logout (authenticated) |
| `ComprehensiveAuthTests.cs` | 8 | Register (mismatch pw, dup email, weak pw), Login (by username, lockout after 5), Refresh (invalid token), Profile (invalid token, update), Logout (unauthenticated) |
| `UserControllerTests.cs` | 4 | GetProfile (authenticated, unauthenticated), UpdateProfile (authenticated, unauthenticated) |

### Test Quality Assessment

**Good:**
- Unit tests use InMemory database → no PostgreSQL required
- Integration tests use `WebApplicationFactory` → full middleware pipeline
- Moq used properly with Setup/Verify patterns
- Comprehensive coverage of registration and login flows

**Issues:**
- `ValidateToken_WithExpiredToken_ShouldReturnNull` is a placeholder (`Assert.True(true)`)
- No tests for MFA flow (doesn't exist)
- No tests for email verification (stub only)
- No tests for admin endpoints (don't exist)
- Integration tests depend on `net9.0` (won't run on net8.0-only machines)

---

## Completeness Assessment

### What's Complete ✅

| Component | Status | Notes |
|-----------|--------|-------|
| User registration | ✅ | Password validation, duplicate check, role assignment |
| User login | ✅ | Credential check, lockout, event logging |
| Token refresh with rotation | ✅ | Old token revoked, new token tracks predecessor |
| Token revocation (logout) | ✅ | Null-safe, sets RevokedAt |
| JWT generation & validation | ✅ | Standard claims, HMAC-SHA256, strict validation |
| Password hashing (BCrypt) | ✅ | Work factor 12, salt, full policy enforcement |
| RBAC (seeded roles) | ✅ | Admin/User/Premium roles, [Authorize(Roles=)] |
| Audit logging | ✅ | All auth events written to auth_logs table |
| User profile (get/update) | ✅ | Self-service + Admin lookup |
| Swagger/OpenAPI | ✅ | JWT Bearer security definition |
| CORS configuration | ✅ | Configurable origins, allows credentials |
| EF Core data model | ✅ | Full Fluent API, indexes, seed data, migrations |
| Refresh token in HttpOnly cookie | ✅ | Secure, SameSite=Strict |

### What's Incomplete 🟡

| Component | Status | What's Missing |
|-----------|--------|----------------|
| Email verification | 🟡 Stub | `VerifyEmailAsync` always returns true. No `EmailVerificationToken` entity, no email sending service (MailKit is referenced in README but not used in code) |
| Password reset | 🟡 Stub | `RequestPasswordResetAsync` silently returns. `ResetPasswordAsync` always returns true. No `PasswordResetToken` entity, no email. Security-wise, the email enumeration protection is correctly implemented (always returns success). |
| MFA/TOTP | 🟡 Model only | `User.MfaEnabled` and `User.MfaSecret` exist. Login checks for MFA and throws if code missing, but there's no MFA setup endpoint, no TOTP validation (Otp.NET referenced in README but not used), and no QR code generation |
| MFA enable/disable endpoints | 🟡 Not built | The README documents `POST /api/users/me/mfa/enable` and `/disable` — these don't exist in UserController |
| Admin endpoints | 🟡 Partial | Only `GET /api/user/{id}` exists. Missing: list all users, lock/unlock user, delete user (documented in README) |
| Account lockout | 🟡 Logic only | The lockout logic is implemented in `LoginAsync` but there's no way to manually lock/unlock accounts (admin endpoint missing), and no automatic unlock after `LockoutEnd` |

### What's Missing ❌

| Feature | README Promised | Actual |
|---------|----------------|--------|
| OAuth 2.0 | "Google, GitHub, Microsoft" | User model has OAuth fields but no OAuth flow, no challenge/redirect, no provider integration |
| API key management | Section in README | No model, no endpoints, no service |
| Rate limiting middleware | Config section in appsettings | No middleware registered in pipeline |
| Health check endpoints | `/health`, `/health/ready`, `/health/live` | Not present in Program.cs |
| Structured logging (Serilog) | README mentions Serilog | Program.cs uses `AddConsole()` + `AddDebug()` only |
| Prometheus metrics | `/metrics` endpoint | Not implemented |
| Change password endpoint | `PUT /api/users/me/password` | Not in UserController |

---

## Build & Runtime Issues

### 1. Framework mismatch
The project targets **net9.0** across all 5 projects. This machine has the **.NET 8.0 SDK/runtime**. Building requires installing the .NET 9.0 SDK.

### 2. Database initialization uses EnsureCreated
`Program.cs` line 162 calls `dbContext.Database.EnsureCreated()` in Development. This creates tables from the current model snapshot but **does not run migrations**. The migrations folder exists but is not applied. If the database already exists from a previous EF version, this could fail silently. Production should use `Migrate()`.

### 3. Hardcoded role GUIDs
The seed data in `AuthDbContext.OnModelCreating` uses hardcoded GUIDs (`11111111-...`, `22222222-...`, `33333333-...`). These will be the same across all deployments. Not a security issue per se, but means all AuthService instances share the same role PKs. If role IDs ever need to differ per environment (unlikely but possible), this would be a problem.

### 4. JWT secret in appsettings.json
Both `appsettings.json` and `appsettings.Development.json` contain hardcoded JWT secret keys. The README correctly advises environment variables for production, but the development defaults are committed to source control. This is acceptable for dev but worth noting.

### 5. InMemory fallback hides misconfiguration
If the `DefaultConnection` string is empty or missing, the app silently falls back to InMemory database. This is great for development/testing but could mask configuration errors in staging environments. Consider logging a warning when falling back.

### 6. No IOptions validation
`JwtSettings.SecretKey` defaults to empty string. If the config section is missing, JWT operations will fail at runtime with a cryptic error rather than at startup. Adding `ValidateOnStart()` or `[Required]` attributes on JwtSettings would catch this early.

### 7. Refresh token cookie expiry mismatch
The cookie is always set with 7-day expiry (`DateTime.UtcNow.AddDays(7)`) regardless of the `rememberMe` setting. The actual token in the database gets the correct expiry (7 or 30 days), but the cookie may expire before the database token, causing unnecessary refresh failures.

---

## File Reference Table

| File | Lines | Purpose |
|------|-------|---------|
| `src/AuthService.API/Program.cs` | 169 | App startup: DI, JWT, EF, CORS, Swagger, middleware |
| `src/AuthService.API/Controllers/AuthController.cs` | 211 | 7 auth endpoints + cookie management + inline DTOs |
| `src/AuthService.API/Controllers/UserController.cs` | 123 | 3 user endpoints (profile, update, admin lookup) |
| `src/AuthService.API/appsettings.json` | 25 | PostgreSQL + JWT + CORS config |
| `src/AuthService.Core/Models/User.cs` | 99 | Core user entity: 20+ properties |
| `src/AuthService.Core/Models/RefreshToken.cs` | 42 | Token entity with lifecycle properties |
| `src/AuthService.Core/Models/Role.cs` | 26 | Role entity |
| `src/AuthService.Core/Models/UserRole.cs` | 20 | Many-to-many join |
| `src/AuthService.Core/Models/AuthLog.cs` | 35 | Audit log entity |
| `src/AuthService.Core/DTOs/*.cs` | 5 files | Request/response DTOs with validation |
| `src/AuthService.Core/Interfaces/*.cs` | 4 files | Service contracts |
| `src/AuthService.Core/Exceptions/*.cs` | 3 files | Custom exception types |
| `src/AuthService.Core/Configuration/JwtSettings.cs` | 16 | JWT options POCO |
| `src/AuthService.Infrastructure/Data/AuthDbContext.cs` | 127 | EF Core context, 5 DbSets, Fluent API |
| `src/AuthService.Infrastructure/Services/AuthService.cs` | 352 | Main auth logic |
| `src/AuthService.Infrastructure/Services/TokenService.cs` | 91 | JWT generation + validation |
| `src/AuthService.Infrastructure/Services/PasswordService.cs` | 55 | BCrypt + policy validation |
| `src/AuthService.Infrastructure/Repositories/UserRepository.cs` | 85 | EF Core repository |
| `src/AuthService.Infrastructure/Migrations/*.cs` | 3 files | Initial EF Core migration |
| `tests/UnitTests/Services/AuthServiceTests.cs` | 287 | 6 unit tests |
| `tests/UnitTests/Services/ComprehensiveAuthServiceTests.cs` | 426 | 12 unit tests |
| `tests/UnitTests/Services/PasswordServiceTests.cs` | 108 | 9 unit tests |
| `tests/UnitTests/Services/TokenServiceTests.cs` | 147 | 6 unit tests |
| `tests/IntegrationTests/Controllers/AuthControllerTests.cs` | 183 | 5 integration tests |
| `tests/IntegrationTests/Controllers/ComprehensiveAuthTests.cs` | 257 | 8 integration tests |
| `tests/IntegrationTests/Controllers/UserControllerTests.cs` | 106 | 4 integration tests |

---

## Summary

The AuthService has a **solid Clean Architecture foundation** with well-separated layers, comprehensive domain modeling, and strong security fundamentals (BCrypt, JWT with rotation, account lockout, audit logging). The core auth flow — register, login, refresh, logout — is fully implemented and tested with 40 tests (26 unit + 14 integration).

However, the README **significantly overstates** what's actually built. Features documented as "✅" in the README (OAuth, email verification, password reset, MFA, rate limiting, API keys) are either stubs, model-only, or entirely absent. The project is approximately **60% complete** relative to its stated goals.

**Verdict: Functional auth microservice with excellent core — needs ~2 weeks of work to match the README's feature list.**

---

*Document generated from source code analysis of AuthService · 2026-06-26*
