---
phase: 1
title: "Foundation & Project Setup"
day: 1
duration: 6h
status: DONE
completed: 2026-01-22
dependencies: []
---

# Phase 1: Foundation & Project Setup (Day 1)

**Duration:** 6 hours  
**Goal:** Functional .NET 10 solution with Hangfire.InMemory, basic fire-forget job, accessible dashboard

## Tasks

### 1.1 Solution Structure Setup
**Duration:** 1h  
**Dependencies:** None

**Actions:**
- Create .NET 10 solution: `dotnet new sln -n TenantDoc`
- Create 3 projects:
  - `TenantDoc.Api` (ASP.NET Core Web API, .NET 10)
  - `TenantDoc.Core` (Class Library, domain models)
  - `TenantDoc.Infrastructure` (Class Library, external services)
- Add projects to solution
- Configure `global.json` with .NET 10 SDK version

**Acceptance Criteria:**
- ✅ Solution builds successfully (`dotnet build`)
- ✅ All 3 projects reference correct .NET 10 TFM (`net10.0`)
- ✅ Project references: Api → Core, Api → Infrastructure, Infrastructure → Core

**Code Example:**
```bash
dotnet new sln -n TenantDoc
dotnet new webapi -n TenantDoc.Api -framework net10.0 --use-minimal-apis
dotnet new classlib -n TenantDoc.Core -framework net10.0
dotnet new classlib -n TenantDoc.Infrastructure -framework net10.0
dotnet sln add src/TenantDoc.Api src/TenantDoc.Core src/TenantDoc.Infrastructure
```

---

### 1.2 Hangfire Package Installation
**Duration:** 30min  
**Dependencies:** 1.1

**Actions:**
- Install NuGet packages in `TenantDoc.Api`:
  - `Hangfire.Core` (latest stable)
  - `Hangfire.AspNetCore`
  - `Hangfire.InMemory`
- Verify package compatibility with .NET 10

**Acceptance Criteria:**
- ✅ Packages installed without conflicts
- ✅ No vulnerability warnings (`dotnet list package --vulnerable`)
- ✅ Project restores successfully

**Code Example:**
```bash
cd src/TenantDoc.Api
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.InMemory
```

---

### 1.3 Hangfire Dashboard Configuration
**Duration:** 1.5h  
**Dependencies:** 1.2

**Actions:**
- Configure Hangfire in `Program.cs`:
  - Add `AddHangfire()` with `UseInMemoryStorage()`
  - Add `AddHangfireServer()` with default queue
  - Add `UseHangfireDashboard("/hangfire")`
- Create `LocalhostAuthorizationFilter` (dashboard accessible on localhost only)
- Test dashboard access at `https://localhost:5001/hangfire`

**Acceptance Criteria:**
- ✅ Application starts without errors
- ✅ Dashboard accessible at `/hangfire`
- ✅ Dashboard shows "Servers: 1", "Queues: default"
- ✅ Dashboard blocks non-localhost requests (test with curl)

**Code Example:**
```csharp
// Program.cs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalhostAuthorizationFilter() }
});
```

---

### 1.4 Core Domain Models
**Duration:** 1h  
**Dependencies:** 1.1

**Actions:**
- Create models in `TenantDoc.Core/Models/`:
  - `Tenant.cs` (Id, Name, TenantTier enum: Standard/VIP)
  - `Document.cs` (Id, TenantId, FileName, Status enum, UploadedAt, ProcessedAt, OcrText, ThumbnailPath)
  - `DocumentStatus` enum: Uploaded, Validating, ValidationFailed, OcrPending, OcrProcessing, OcrFailed, Ready
  - `ProcessingResult.cs` (Success, ErrorMessage, RetryCount)

**Acceptance Criteria:**
- ✅ All models compile
- ✅ Enums have sensible defaults
- ✅ Document model has required fields (Id, TenantId, FileName)
- ✅ Status transitions documented in code comments

**Code Example:**
```csharp
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? OcrText { get; set; }
    public string? ThumbnailPath { get; set; }
}

public enum DocumentStatus
{
    Uploaded,
    Validating,
    ValidationFailed,
    OcrPending,
    OcrProcessing,
    OcrFailed,
    Ready
}
```

---

### 1.5 First Fire-and-Forget Job
**Duration:** 2h  
**Dependencies:** 1.3, 1.4

**Actions:**
- Create `Jobs/ValidationJob.cs` in `TenantDoc.Api`:
  - Method: `ValidateDocument(Guid documentId)`
  - Logic: Mock validation (file type, size checks)
  - Update document status in in-memory collection
- Create minimal `/api/documents/upload` endpoint:
  - Accept `tenantId` and `fileName` (mock, no actual file upload yet)
  - Enqueue `ValidationJob` using `BackgroundJob.Enqueue()`
  - Return `documentId` immediately
- Test via Swagger/curl

**Acceptance Criteria:**
- ✅ POST `/api/documents/upload` returns `documentId` in <100ms
- ✅ ValidationJob appears in dashboard "Processing" tab
- ✅ Job completes within 5 seconds (visible in "Succeeded" tab)
- ✅ Job execution logged to console (document ID, validation result)

**Code Example:**
```csharp
// Jobs/ValidationJob.cs
public class ValidationJob
{
    public async Task ValidateDocument(Guid documentId)
    {
        Console.WriteLine($"[ValidationJob] Starting validation for {documentId}");
        
        // Mock validation logic
        await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate processing
        
        var isValid = Random.Shared.Next(10) > 1; // 90% success rate
        
        if (isValid)
        {
            Console.WriteLine($"[ValidationJob] Document {documentId} validated successfully");
            // Update status to OcrPending (in-memory store)
        }
        else
        {
            Console.WriteLine($"[ValidationJob] Document {documentId} validation failed");
            // Update status to ValidationFailed
        }
    }
}

// Minimal API endpoint
app.MapPost("/api/documents/upload", (string tenantId, string fileName, IBackgroundJobClient jobClient) =>
{
    var documentId = Guid.NewGuid();
    jobClient.Enqueue<ValidationJob>(x => x.ValidateDocument(documentId));
    return Results.Ok(new { documentId });
});
```

---

## Phase 1 Success Metrics

- ✅ Solution builds and runs
- ✅ Hangfire dashboard accessible and functional
- ✅ Fire-and-forget job enqueues and executes
- ✅ Job lifecycle visible in dashboard (Enqueued → Processing → Succeeded)
- ✅ Basic domain models defined

## Risks & Mitigations

**Risk:** .NET 10 compatibility issues with Hangfire  
**Mitigation:** Verify Hangfire latest version supports .NET 10 (check release notes); fallback to .NET 8 if blockers found

**Risk:** In-memory storage resets on app restart  
**Mitigation:** Expected behavior; document for users; plan SQL Server migration in Phase 7

---

**Navigation:**
- [← Back to Plan Overview](../plan.md)
- [Next Phase: Document Storage & Tesseract OCR →](phase-2.md)
