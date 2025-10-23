# Contributing to AuthService

Thank you for your interest in contributing to the Authentication Service! This document provides guidelines and instructions for contributing to this project.

## 📋 Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [Development Setup](#development-setup)
4. [Project Structure](#project-structure)
5. [Coding Standards](#coding-standards)
6. [Testing Guidelines](#testing-guidelines)
7. [Pull Request Process](#pull-request-process)
8. [Security](#security)
9. [Documentation](#documentation)

---

## 📜 Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment for all contributors, regardless of:
- Experience level
- Gender identity and expression
- Sexual orientation
- Disability
- Personal appearance
- Body size
- Race
- Ethnicity
- Age
- Religion

### Expected Behavior

- Be respectful and inclusive
- Welcome newcomers and help them learn
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment, discrimination, or offensive comments
- Trolling or insulting/derogatory remarks
- Public or private harassment
- Publishing others' private information without permission
- Other conduct which could reasonably be considered inappropriate

---

## 🚀 Getting Started

### Prerequisites

Before contributing, ensure you have:

1. **.NET 8.0 SDK** installed
2. **PostgreSQL 15+** installed and running
3. **Git** for version control
4. A code editor (VS Code, Visual Studio 2022, or Rider)
5. Basic understanding of:
   - C# and ASP.NET Core
   - Entity Framework Core
   - JWT authentication
   - Unit testing with xUnit

### Finding Issues to Work On

- Check the [Issues](https://github.com/yourusername/packet-flow/issues) page
- Look for issues labeled `good first issue` if you're new
- Look for issues labeled `help wanted` for priority items
- Comment on an issue to let others know you're working on it

---

## 🛠️ Development Setup

### 1. Fork and Clone

```bash
# Fork the repository on GitHub, then:
git clone https://github.com/YOUR-USERNAME/packet-flow.git
cd packet-flow/AuthService
```

### 2. Create a Branch

```bash
# Always create a new branch for your changes
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-fix-name
```

Branch naming conventions:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Test improvements
- `chore/` - Maintenance tasks

### 3. Set Up Development Environment

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Set up local database
createdb authservice

# Run migrations
cd src/AuthService.API
dotnet ef database update --project ../AuthService.Infrastructure
```

### 4. Configure appsettings.Development.json

Create `src/AuthService.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authservice_dev;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "development-secret-key-for-local-testing-only-32-chars",
    "Issuer": "AuthService",
    "Audience": "SimulationPlatform"
  }
}
```

### 5. Run the Application

```bash
cd src/AuthService.API
dotnet run
```

Visit `https://localhost:5001/swagger` to test the API.

---

## 📂 Project Structure

### Clean Architecture Layers

```
AuthService/
├── src/
│   ├── AuthService.API/              # Presentation Layer
│   │   ├── Controllers/              # API endpoints
│   │   ├── Middleware/               # Request pipeline
│   │   └── Program.cs                # App configuration
│   │
│   ├── AuthService.Core/             # Domain Layer (No dependencies)
│   │   ├── Models/                   # Domain entities
│   │   │   ├── User.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── Role.cs
│   │   │   └── AuthLog.cs
│   │   ├── Interfaces/               # Abstractions
│   │   │   ├── IAuthService.cs
│   │   │   ├── ITokenService.cs
│   │   │   └── IRepository.cs
│   │   ├── DTOs/                     # Data transfer objects
│   │   └── Exceptions/               # Custom exceptions
│   │
│   └── AuthService.Infrastructure/   # Infrastructure Layer
│       ├── Data/                     # EF Core context
│       ├── Repositories/             # Data access
│       ├── Services/                 # Service implementations
│       └── Migrations/               # Database migrations
│
└── tests/
    ├── AuthService.UnitTests/        # Fast, isolated tests
    └── AuthService.IntegrationTests/ # Full stack tests
```

### Dependency Rules

1. **Core** has no dependencies on other layers
2. **Infrastructure** depends only on Core
3. **API** depends on both Core and Infrastructure
4. **Tests** can depend on all layers

---

## 💻 Coding Standards

### C# Style Guidelines

Follow the [official C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

#### Naming Conventions

```csharp
// Classes, Methods, Properties: PascalCase
public class AuthService
public async Task<TokenResponse> LoginAsync()
public string UserId { get; set; }

// Private fields: _camelCase
private readonly ITokenService _tokenService;

// Parameters, local variables: camelCase
public void ProcessLogin(string username, string password)

// Constants: PascalCase
private const int MaxLoginAttempts = 5;

// Interfaces: I + PascalCase
public interface IAuthService
```

#### Code Organization

```csharp
// 1. Using statements (grouped and sorted)
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// 2. Namespace
namespace AuthService.Infrastructure.Services;

// 3. Class documentation
/// <summary>
/// Provides authentication services including login, registration, and token management.
/// </summary>
public class AuthService : IAuthService
{
    // 4. Private fields
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    
    // 5. Constructor
    public AuthService(ITokenService tokenService, ILogger<AuthService> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }
    
    // 6. Public methods
    public async Task<TokenResponse> LoginAsync(LoginDto dto)
    {
        // Implementation
    }
    
    // 7. Private methods
    private async Task ValidateCredentialsAsync(string username, string password)
    {
        // Implementation
    }
}
```

### SOLID Principles

#### Single Responsibility Principle
```csharp
// Good - Each class has one responsibility
public class PasswordHasher : IPasswordHasher
public class TokenGenerator : ITokenGenerator
public class EmailSender : IEmailSender

// Bad - Too many responsibilities
public class UserService
{
    public void HashPassword() { }
    public void SendEmail() { }
    public void GenerateToken() { }
}
```

#### Dependency Inversion
```csharp
// Good - Depend on abstractions
public class AuthService
{
    private readonly IUserRepository _repository;  // Interface
    
    public AuthService(IUserRepository repository)
    {
        _repository = repository;
    }
}

// Bad - Depend on concrete implementations
public class AuthService
{
    private readonly UserRepository _repository;  // Concrete class
}
```

### Async/Await Best Practices

```csharp
// ✅ Good
public async Task<User> GetUserAsync(Guid id)
{
    return await _context.Users.FindAsync(id);
}

// ❌ Bad - async without await
public async Task<User> GetUserAsync(Guid id)
{
    return _context.Users.Find(id);
}

// ✅ Good - No need for async if just returning Task
public Task<User> GetUserAsync(Guid id)
{
    return _context.Users.FindAsync(id).AsTask();
}
```

### Error Handling

```csharp
// ✅ Good - Specific exceptions with context
public async Task<User> GetUserByIdAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    
    if (user == null)
    {
        throw new UserNotFoundException($"User with ID {id} not found");
    }
    
    return user;
}

// ✅ Good - Validation with helpful messages
public async Task RegisterAsync(RegisterDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Email))
    {
        throw new ValidationException("Email is required");
    }
    
    if (await _repository.ExistsAsync(u => u.Email == dto.Email))
    {
        throw new ValidationException("Email is already registered");
    }
    
    // Process registration
}
```

### XML Documentation

Add XML documentation for all public APIs:

```csharp
/// <summary>
/// Authenticates a user and generates JWT tokens.
/// </summary>
/// <param name="dto">Login credentials including username and password.</param>
/// <param name="ipAddress">The IP address of the client making the request.</param>
/// <param name="userAgent">The user agent string from the client's browser.</param>
/// <returns>
/// A <see cref="TokenResponseDto"/> containing access token, refresh token, and user information.
/// </returns>
/// <exception cref="UnauthorizedException">Thrown when credentials are invalid.</exception>
/// <exception cref="AccountLockedException">Thrown when the account is locked.</exception>
public async Task<TokenResponseDto> LoginAsync(
    LoginDto dto, 
    string ipAddress, 
    string userAgent)
{
    // Implementation
}
```

---

## 🧪 Testing Guidelines

### Test Organization

```
tests/
├── AuthService.UnitTests/
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   ├── TokenServiceTests.cs
│   │   └── PasswordServiceTests.cs
│   └── Validators/
│       └── PasswordValidatorTests.cs
│
└── AuthService.IntegrationTests/
    ├── Controllers/
    │   └── AuthControllerTests.cs
    └── Repositories/
        └── UserRepositoryTests.cs
```

### Unit Test Best Practices

```csharp
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _sut;  // System Under Test
    
    public AuthServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _sut = new AuthService(_mockRepository.Object, _mockTokenService.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!");
        var expectedUser = new User { Id = Guid.NewGuid(), Username = "testuser" };
        
        _mockRepository
            .Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(expectedUser);
        
        _mockTokenService
            .Setup(t => t.GenerateAccessToken(expectedUser))
            .Returns("mock-access-token");
        
        // Act
        var result = await _sut.LoginAsync(loginDto, "127.0.0.1", "TestAgent");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }
    
    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "WrongPassword");
        var user = new User { PasswordHash = "hashed-password" };
        
        _mockRepository
            .Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(loginDto, "127.0.0.1", "TestAgent")
        );
    }
}
```

### Integration Test Example

```csharp
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var registerDto = new
        {
            username = "newuser",
            email = "newuser@example.com",
            password = "SecurePass123!",
            confirmPassword = "SecurePass123!"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(content);
        Assert.NotEmpty(content.AccessToken);
    }
}
```

### Test Coverage Requirements

- **Minimum coverage**: 80% for all code
- **Core business logic**: 95% coverage required
- **Controllers**: 90% coverage required
- All pull requests must include tests for new features

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Run tests in parallel
dotnet test --parallel
```

---

## 🔄 Pull Request Process

### Before Submitting

1. **Update your branch**
   ```bash
   git checkout main
   git pull upstream main
   git checkout your-branch
   git rebase main
   ```

2. **Run all tests**
   ```bash
   dotnet test
   ```

3. **Check code style**
   ```bash
   dotnet format
   ```

4. **Update documentation** if you've changed APIs

### Pull Request Template

When creating a PR, use this template:

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Related Issue
Fixes #(issue number)

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] My code follows the style guidelines
- [ ] I have performed a self-review
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have updated the documentation
- [ ] My changes generate no new warnings
- [ ] All tests pass locally
- [ ] I have added tests that prove my fix/feature works
```

### Review Process

1. **Automated checks** must pass (build, tests, linting)
2. **At least one approval** from a maintainer required
3. **All conversations** must be resolved
4. **Merge conflicts** must be resolved
5. **Squash and merge** for clean commit history

---

## 🔒 Security

### Reporting Security Issues

**DO NOT** create public issues for security vulnerabilities.

Instead:
1. Email security@yourplatform.com
2. Provide detailed description
3. Include steps to reproduce
4. Wait for acknowledgment before disclosure

### Security Checklist

When contributing security-related code:

- [ ] No hardcoded secrets or credentials
- [ ] Input validation for all user inputs
- [ ] Output encoding to prevent XSS
- [ ] Parameterized queries to prevent SQL injection
- [ ] CSRF protection for state-changing operations
- [ ] Rate limiting for authentication endpoints
- [ ] Secure password hashing (BCrypt with work factor ≥ 12)
- [ ] Secure random number generation for tokens
- [ ] HTTPS/TLS enforced in production
- [ ] Security headers configured (CSP, HSTS, etc.)

### Code Security Review

All PRs involving authentication, authorization, or data access will undergo additional security review.

---

## 📝 Documentation

### When to Update Documentation

Update documentation when you:
- Add or modify API endpoints
- Change configuration options
- Add new features
- Fix bugs that affect documented behavior
- Change database schema

### Documentation Locations

- **README.md**: High-level overview, setup instructions
- **CONTRIBUTING.md**: This file
- **API Documentation**: Swagger/OpenAPI (auto-generated)
- **XML Documentation**: Inline code documentation
- **Wiki**: Detailed guides and tutorials

### Documentation Style

```csharp
/// <summary>
/// Clear, concise description of what the method does.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of what is returned.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
/// <example>
/// <code>
/// var result = await service.MethodName(param);
/// </code>
/// </example>
```

---

## 🎯 Development Workflow

### 1. Pick an Issue

Choose an issue from the backlog and assign it to yourself.

### 2. Create a Branch

```bash
git checkout -b feature/issue-123-add-oauth-support
```

### 3. Implement Changes

Write code following the coding standards.

### 4. Write Tests

Add unit and/or integration tests for your changes.

### 5. Update Documentation

Update README, XML docs, or other relevant documentation.

### 6. Commit Changes

```bash
git add .
git commit -m "feat: add OAuth2 social login support

- Implement Google OAuth provider
- Add OAuth configuration
- Update user model to support OAuth
- Add integration tests

Fixes #123"
```

#### Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### 7. Push and Create PR

```bash
git push origin feature/issue-123-add-oauth-support
```

Then create a pull request on GitHub.

---

## 🆘 Getting Help

### Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core)
- [Entity Framework Core Docs](https://docs.microsoft.com/en-us/ef/core)
- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)

### Community

- **Discord**: Join our [Discord server](https://discord.gg/yourserver)
- **Stack Overflow**: Tag questions with `packet-flow`
- **GitHub Discussions**: For general questions

### Code Review

Don't hesitate to ask for help during code review:
- Tag specific reviewers with `@username`
- Ask questions about suggested changes
- Request clarification on coding standards

---

## 📊 Performance Guidelines

### Database Queries

```csharp
// ✅ Good - Use async and avoid N+1 queries
public async Task<List<User>> GetUsersWithRolesAsync()
{
    return await _context.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .ToListAsync();
}

// ❌ Bad - N+1 query problem
public async Task<List<User>> GetUsersWithRolesAsync()
{
    var users = await _context.Users.ToListAsync();
    foreach (var user in users)
    {
        user.Roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync();  // Separate query for each user!
    }
    return users;
}
```

### Caching

```csharp
// Use caching for frequently accessed data
public async Task<User> GetUserByIdAsync(Guid id)
{
    var cacheKey = $"user:{id}";
    
    if (_cache.TryGetValue(cacheKey, out User cachedUser))
    {
        return cachedUser;
    }
    
    var user = await _repository.GetByIdAsync(id);
    
    _cache.Set(cacheKey, user, TimeSpan.FromMinutes(15));
    
    return user;
}
```

---

## 🏆 Recognition

Contributors will be recognized in:
- README.md acknowledgments
- Release notes
- Project website (if applicable)

Top contributors may be invited to join the core team!

---

## 📜 License

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to AuthService! 🎉**

Questions? Open an issue or reach out on Discord!


