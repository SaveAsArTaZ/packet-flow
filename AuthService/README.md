# Authentication Service

A production-ready, secure authentication microservice for the Collaborative Network Simulation Platform built with ASP.NET Core 8.0.

## 🎯 Overview

This authentication service provides comprehensive user authentication, authorization, and session management with support for:

- ✅ **JWT-based Authentication** with access and refresh tokens
- ✅ **Multi-Factor Authentication (MFA)** using TOTP
- ✅ **OAuth 2.0** integration (Google, GitHub, Microsoft)
- ✅ **Email Verification** and password reset
- ✅ **Role-Based Access Control (RBAC)**
- ✅ **Account Security** (lockout, rate limiting, audit logging)
- ✅ **Secure Password Storage** (BCrypt hashing)
- ✅ **API Key Management** for programmatic access

---

## 📋 Table of Contents

1. [Architecture](#architecture)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Database Setup](#database-setup)
6. [Running the Service](#running-the-service)
7. [API Documentation](#api-documentation)
8. [Security Features](#security-features)
9. [Testing](#testing)
10. [Deployment](#deployment)
11. [Contributing](#contributing)

---

## 🏗️ Architecture

### Project Structure

```
AuthService/
├── src/
│   ├── AuthService.API/              # Web API Layer
│   │   ├── Controllers/              # API endpoints
│   │   ├── Middleware/               # Custom middleware
│   │   ├── Program.cs                # Application entry point
│   │   └── appsettings.json          # Configuration
│   │
│   ├── AuthService.Core/             # Domain Layer
│   │   ├── Models/                   # Domain entities
│   │   ├── Interfaces/               # Service contracts
│   │   ├── DTOs/                     # Data transfer objects
│   │   └── Exceptions/               # Custom exceptions
│   │
│   └── AuthService.Infrastructure/   # Infrastructure Layer
│       ├── Data/                     # Database context
│       ├── Repositories/             # Data access
│       ├── Services/                 # Service implementations
│       └── Migrations/               # EF Core migrations
│
├── tests/
│   ├── AuthService.UnitTests/        # Unit tests
│   └── AuthService.IntegrationTests/ # Integration tests
│
├── AuthService.sln                   # Solution file
├── README.md                         # This file
└── CONTRIBUTING.md                   # Contribution guidelines
```

### Technology Stack

| Component | Technology |
|-----------|-----------|
| **Framework** | ASP.NET Core 8.0 |
| **Database** | PostgreSQL 15+ |
| **ORM** | Entity Framework Core 8.0 |
| **Authentication** | JWT Bearer Tokens |
| **Password Hashing** | BCrypt.Net |
| **MFA** | TOTP (Otp.NET) |
| **Testing** | xUnit, Moq, FluentAssertions |
| **Documentation** | Swagger/OpenAPI |

---

## 📦 Prerequisites

### Required Software

- **.NET 8.0 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **PostgreSQL 15+** ([Download](https://www.postgresql.org/download/))
- **Git** for version control
- **Visual Studio 2022** / **VS Code** / **Rider** (recommended IDEs)

### Optional Tools

- **Docker** for containerized deployment
- **Postman** or **Insomnia** for API testing
- **pgAdmin** for database management

---

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/packet-flow.git
cd packet-flow/AuthService
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Install Required Packages

```bash
# Navigate to API project
cd src/AuthService.API

# Add authentication packages
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.3

# Add Entity Framework
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0

# Add security packages
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Otp.NET --version 1.3.0
dotnet add package QRCoder --version 1.4.3

# Add email
dotnet add package MailKit --version 4.3.0

# Add Swagger
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

---

## ⚙️ Configuration

### appsettings.json

Create `src/AuthService.API/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authservice;Username=postgres;Password=yourpassword"
  },
  
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-characters-long-for-production!!",
    "Issuer": "AuthService",
    "Audience": "SimulationPlatform",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "Simulation Platform",
    "SenderEmail": "noreply@simplatform.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  
  "RateLimiting": {
    "LoginAttemptsPerMinute": 5,
    "RegisterAttemptsPerHour": 3,
    "PasswordResetAttemptsPerHour": 3
  },
  
  "Security": {
    "RequireEmailVerification": true,
    "EnableAccountLockout": true,
    "LockoutDurationMinutes": 30,
    "MaxFailedAccessAttempts": 5,
    "PasswordRequireDigit": true,
    "PasswordRequireLowercase": true,
    "PasswordRequireUppercase": true,
    "PasswordRequireNonAlphanumeric": true,
    "PasswordMinLength": 8
  }
}
```

### Environment Variables (Production)

For production, use environment variables instead of storing secrets in appsettings.json:

```bash
export ConnectionStrings__DefaultConnection="Host=prod-db;..."
export JwtSettings__SecretKey="production-secret-key"
export EmailSettings__Password="secure-password"
```

---

## 🗄️ Database Setup

### 1. Create Database

```bash
# Using psql
psql -U postgres
CREATE DATABASE authservice;
\q
```

### 2. Apply Migrations

```bash
# From AuthService root directory
cd src/AuthService.API

# Create initial migration
dotnet ef migrations add InitialCreate --project ../AuthService.Infrastructure

# Update database
dotnet ef database update --project ../AuthService.Infrastructure
```

### 3. Seed Initial Data (Optional)

The application will automatically seed:
- Default roles (Admin, User, Premium)
- Test admin user (if in Development environment)

---

## 🏃 Running the Service

### Development Mode

```bash
# From AuthService root
cd src/AuthService.API

# Run the application
dotnet run

# Or with hot reload
dotnet watch run
```

The API will be available at:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `https://localhost:5001/swagger`

### Production Mode

```bash
# Build release version
dotnet build -c Release

# Run in production
dotnet run --configuration Release
```

### Using Docker

```bash
# Build Docker image
docker build -t authservice:latest .

# Run container
docker run -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="Host=db;..." \
  -e JwtSettings__SecretKey="production-secret" \
  authservice:latest
```

---

## 📚 API Documentation

### Base URL

```
Development: https://localhost:5001
Production:  https://api.yourplatform.com/auth
```

### Authentication Flow

```
┌──────────┐                                      ┌───────────┐
│  Client  │                                      │   Server  │
└────┬─────┘                                      └─────┬─────┘
     │                                                   │
     │  POST /api/auth/login                             │
     │  { username, password }                           │
     ├──────────────────────────────────────────────────>│
     │                                                   │
     │  200 OK                                           │
     │  { accessToken, refreshToken, user }              │
     │<──────────────────────────────────────────────────┤
     │                                                   │
     │  GET /api/simulations                             │
     │  Authorization: Bearer <accessToken>              │
     ├──────────────────────────────────────────────────>│
     │                                                   │
     │  200 OK { simulations }                           │
     │<──────────────────────────────────────────────────┤
     │                                                   │
     │  (15 minutes later - token expires)               │
     │                                                   │
     │  POST /api/auth/refresh                           │
     │  { refreshToken }                                 │
     ├──────────────────────────────────────────────────>│
     │                                                   │
     │  200 OK                                           │
     │  { accessToken, refreshToken }                    │
     │<──────────────────────────────────────────────────┤
```

### Key Endpoints

#### Authentication

```http
POST   /api/auth/register       # Register new user
POST   /api/auth/login          # Login with credentials
POST   /api/auth/refresh        # Refresh access token
POST   /api/auth/logout         # Logout and revoke tokens
POST   /api/auth/verify-email   # Verify email address
POST   /api/auth/forgot-password        # Request password reset
POST   /api/auth/reset-password         # Reset password with token
```

#### User Management

```http
GET    /api/users/me            # Get current user profile
PUT    /api/users/me            # Update profile
PUT    /api/users/me/password   # Change password
POST   /api/users/me/mfa/enable # Enable MFA
POST   /api/users/me/mfa/disable        # Disable MFA
```

#### Admin (Requires Admin role)

```http
GET    /api/admin/users         # List all users
GET    /api/admin/users/{id}    # Get user by ID
PUT    /api/admin/users/{id}/lock       # Lock user account
DELETE /api/admin/users/{id}    # Delete user
```

### Example Requests

#### Register

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePass123!",
    "confirmPassword": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "kL8h3m2nQ9pR...",
  "expiresAt": "2024-10-23T15:30:00Z",
  "tokenType": "Bearer",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "johndoe",
    "email": "john@example.com",
    "emailVerified": false,
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["User"],
    "mfaEnabled": false
  }
}
```

#### Login

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "johndoe",
    "password": "SecurePass123!",
    "rememberMe": true
  }'
```

#### Authenticated Request

```bash
curl -X GET https://localhost:5001/api/users/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## 🔐 Security Features

### Password Security

- **BCrypt Hashing** with work factor of 12
- **Password Policy Enforcement:**
  - Minimum 8 characters
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one digit
  - At least one special character

### Account Protection

- **Account Lockout:** After 5 failed login attempts (30-minute lockout)
- **Email Verification:** Required for new accounts
- **Rate Limiting:**
  - 5 login attempts per minute per IP
  - 3 registration attempts per hour per IP
  - 3 password reset attempts per hour per email

### Token Security

- **Access Tokens:** Short-lived (15 minutes)
- **Refresh Tokens:** Long-lived (7-30 days), stored securely
- **Token Rotation:** Old tokens revoked on refresh
- **Revocation:** Tokens can be manually revoked

### Multi-Factor Authentication

- **TOTP-based** (compatible with Google Authenticator, Authy)
- **QR Code Generation** for easy setup
- **Backup Codes** for recovery (optional feature)

### Audit Logging

All authentication events are logged:
- Successful/failed logins
- Registration attempts
- Password changes
- Token refreshes
- Account lockouts
- MFA events

---

## 🧪 Testing

### Run All Tests

```bash
# From AuthService root
dotnet test
```

### Run Unit Tests Only

```bash
dotnet test tests/AuthService.UnitTests
```

### Run Integration Tests Only

```bash
dotnet test tests/AuthService.IntegrationTests
```

### Test Coverage

```bash
# Install coverage tool
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura

# Generate HTML report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

### Example Test

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var dto = new LoginDto("johndoe", "SecurePass123!");
    
    // Act
    var result = await _authService.LoginAsync(dto, "127.0.0.1", "TestAgent");
    
    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.AccessToken);
    Assert.NotEmpty(result.RefreshToken);
}
```

---

## 🚢 Deployment

### Docker Deployment

See `Dockerfile` in the root directory.

```bash
# Build
docker build -t authservice:1.0.0 .

# Run
docker-compose up -d
```

### Kubernetes Deployment

```bash
# Apply configuration
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml
```

### Environment Variables for Production

```bash
# Database
ConnectionStrings__DefaultConnection="production-connection-string"

# JWT
JwtSettings__SecretKey="256-bit-secret-key"
JwtSettings__Issuer="YourProductionIssuer"
JwtSettings__Audience="YourProductionAudience"

# Email
EmailSettings__SmtpServer="smtp.sendgrid.net"
EmailSettings__Username="apikey"
EmailSettings__Password="SG.xxx"

# CORS
CORS__AllowedOrigins="https://app.yourplatform.com,https://admin.yourplatform.com"
```

---

## 📊 Monitoring & Logging

### Structured Logging

The service uses Serilog for structured logging:

```csharp
Log.Information("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

### Health Checks

```http
GET /health              # Overall health
GET /health/ready        # Ready for requests
GET /health/live         # Service is alive
```

### Metrics

Prometheus metrics available at `/metrics`:
- Authentication success/failure rates
- Active sessions count
- Token generation rates
- API response times

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines on:

- Code style and conventions
- Branching strategy
- Pull request process
- Testing requirements
- Security guidelines

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

## 🆘 Support

### Documentation
- [API Documentation](https://api.yourplatform.com/docs)
- [Architecture Guide](docs/ARCHITECTURE.md)
- [Security Best Practices](docs/SECURITY.md)

### Issues
- [GitHub Issues](https://github.com/yourusername/packet-flow/issues)
- [Security Vulnerabilities](SECURITY.md)

### Community
- [Discord Server](https://discord.gg/yourserver)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/packet-flow)

---

## 📈 Roadmap

### Version 1.1 (Planned)
- [ ] OAuth2 Social Login (Google, GitHub, Microsoft)
- [ ] Passwordless authentication (Magic Links)
- [ ] Session management dashboard
- [ ] Advanced audit log filtering

### Version 1.2 (Planned)
- [ ] WebAuthn/FIDO2 support
- [ ] Device management
- [ ] Geolocation-based security
- [ ] Advanced rate limiting with Redis

---

## 🙏 Acknowledgments

- ASP.NET Core Team
- Entity Framework Team
- BCrypt.Net contributors
- The open-source community

---

**Built with ❤️ for secure, scalable authentication**


