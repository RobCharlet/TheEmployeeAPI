# TheEmployeeAPI

A comprehensive employee management REST API built with .NET 8, featuring authentication, authorization, and full CRUD operations for employee and user management.

## Features

### Core Functionality
- **Employee Management**: Complete CRUD operations for employee records
- **User Management**: User registration, authentication, and profile management
- **Authentication & Authorization**: Cookie-based authentication with ASP.NET Core Identity
- **Audit Trail**: Automatic tracking of creation and modification timestamps
- **Data Validation**: FluentValidation for request validation
- **Benefits System**: Employee benefits management with cost tracking

### Technical Features
- RESTful API design with OpenAPI/Swagger documentation
- Entity Framework Core with PostgreSQL database
- Testcontainers for database testing
- Comprehensive unit and integration tests
- Structured error handling with RFC 7807 Problem Details
- Automatic database migrations and seeding

## Tech Stack

- **.NET 8** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **ASP.NET Core Identity** - Authentication and user management
- **FluentValidation** - Request validation
- **Swagger/OpenAPI** - API documentation
- **Testcontainers** - Containerized testing with PostgreSQL
- **SQLite** - Development database option

## Getting Started

### Prerequisites
- .NET 8 SDK
- Docker (for PostgreSQL container)
- IDE: Visual Studio, VS Code, or Rider

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd TheEmployeeAPI
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run --project TheEmployeeAPI
   ```

The API will automatically:
- Start a PostgreSQL container using Testcontainers
- Apply database migrations
- Seed initial data
- Be available at `https://localhost:5001` or `http://localhost:5000`

### API Documentation

Once running, access the interactive API documentation at:
- **Swagger UI**: `https://localhost:5001/swagger`

## API Endpoints

### Authentication & User Management
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - User login
- `POST /api/users/logout` - User logout
- `GET /api/users/current` - Get current user profile
- `GET /api/users` - Get all users (with pagination)
- `PUT /api/users/profile` - Update current user profile
- `PUT /api/users/change-password` - Change password
- `POST /api/users/forgot-password` - Request password reset
- `POST /api/users/reset-password` - Reset password with token

### Employee Management
- `GET /api/employees` - Get all employees (with pagination and filtering)
- `GET /api/employees/{id}` - Get employee by ID
- `POST /api/employees` - Create new employee
- `PUT /api/employees/{id}` - Update employee
- `DELETE /api/employees/{id}` - Delete employee

### Query Parameters
Both users and employees endpoints support:
- `page` - Page number (default: 1)
- `recordsPerPage` - Records per page (default: 10)
- `firstNameContains` - Filter by first name
- `lastNameContains` - Filter by last name
- Additional filters specific to each entity

## Data Models

### Employee
```csharp
public class Employee : AuditableEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public List<EmployeeBenefit> Benefits { get; set; }
}
```

### User (extends IdentityUser)
```csharp
public class User : IdentityUser, IAuditableEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    // Audit fields and computed properties
}
```

## Configuration

### Password Requirements
- Minimum 8 characters
- Requires uppercase letter
- Requires digit
- Non-alphanumeric characters optional

### Security Features
- Cookie-based authentication
- Secure cookie settings (HttpOnly, Secure, SameSite)
- Account lockout after 6 failed attempts
- 24-hour session duration with sliding expiration

## Testing

Run the test suite:
```bash
dotnet test
```

### Test Coverage
- **Integration Tests**: Full API endpoint testing with test database
- **Controller Tests**: Unit tests for both EmployeesController and UsersController
- **Custom Web Application Factory**: Isolated test environment setup

## Project Structure

```
TheEmployeeAPI/
├── TheEmployeeAPI/ # Main API project
│ ├── Employees/ # Employee-related controllers and models
│ ├── Users/ # User-related controllers and models
│ ├── Migrations/ # EF Core migrations
│ ├── .cs # Core models and configurations
│ └── appsettings.json # Configuration
├── TheEmployeeAPI.Tests/ # Test project
└── .sln # Solution file
```
## Key Features

### Audit Trail
All entities inherit from `AuditableEntity` providing automatic tracking of:
- Creation timestamp and user
- Last modification timestamp and user

### Validation
Request validation using FluentValidation with custom filters for consistent error responses.

### Error Handling
Standardized error responses following RFC 7807 Problem Details specification.

### Database
- PostgreSQL for production (via Testcontainers)
- SQLite option available for development
- Automatic migrations and data seeding