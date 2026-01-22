# Deployment Guide

## 1. Local Development Environment
TenantDoc is currently configured for local development and learning.

### 1.1 Prerequisites
- .NET 10 SDK
- IDE (Visual Studio, VS Code, or Rider)

### 1.2 Running the Application
1.  Navigate to the solution root.
2.  Run `dotnet run --project src/TenantDoc.Api`.
3.  The API will be available at `https://localhost:5001` (or as configured in `launchSettings.json`).
4.  The Hangfire Dashboard is accessible at `https://localhost:5001/hangfire`.

## 2. Configuration
- **Storage:** Uses `Hangfire.InMemory` for job storage and `InMemoryDocumentStore` for domain data. No external database setup is required.
- **Queues:** Currently using the `default` queue. More queues (`critical`, `batch`) will be added in Phase 4.

## 3. Production Considerations (Future)
- **Persistence:** Switch to `Hangfire.SqlServer` or `Hangfire.PostgreSql`.
- **Scaling:** Deploy multiple instances of the API/Worker with a shared job storage.
- **Security:** Implement real authentication for the dashboard and API endpoints.
- **Storage:** Use Azure Blob Storage or AWS S3 for document files instead of local filesystem.
