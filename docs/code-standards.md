# Code Standards & Structure

## 1. Architectural Patterns
TenantDoc follows a **Clean Architecture** approach:

### 1.1 Project Structure
- **TenantDoc.Api**: The entry point. Contains controllers/endpoints, Hangfire job definitions, and filters.
- **TenantDoc.Core**: The domain layer. Contains business entities, enums, and interface definitions. No dependencies on other projects.
- **TenantDoc.Infrastructure**: The implementation layer. Contains concrete services for storage, OCR, and image processing.

### 1.2 Naming Conventions
- **PascalCase** for classes, methods, and public properties.
- **camelCase** for private fields (with `_` prefix, e.g., `_documentStore`).
- **Interfaces** prefixed with `I` (e.g., `IDocumentStore`).
- **Jobs** suffixed with `Job` (e.g., `ValidationJob`).

## 2. Coding Guidelines

### 2.1 Dependency Injection
- Prefer constructor injection.
- Register services in `Program.cs` with appropriate lifetimes (usually `Singleton` for in-memory stores and `Scoped` for transient services).

### 2.2 Error Handling
- Use structured logging (standard `Console.WriteLine` for now, moving to `ILogger` later).
- In Hangfire jobs, exceptions should be allowed to bubble up to trigger the retry mechanism, unless they are terminal errors.

### 2.3 Hangfire Best Practices
- **Job Parameterization:** Pass primitive IDs (e.g., `Guid documentId`) to jobs rather than full objects to ensure data consistency at execution time.
- **Queue Assignment:** Use `[Queue("name")]` attributes or explicit configuration for priority management.
- **Idempotency:** Jobs should be designed to be idempotent where possible (e.g., check status before processing).

## 3. Testing Standards
- **Unit Tests:** Target 80%+ coverage for core logic and job business logic.
- **Integration Tests:** End-to-end testing of the job pipeline using `Hangfire.InMemory`.
- **Naming:** Use `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `ValidateDocument_NotFound_ReturnsEarly`).

## 4. Documentation
- Keep Markdown documentation in `/docs` updated after each phase.
- Use XML comments for complex methods and enums.
