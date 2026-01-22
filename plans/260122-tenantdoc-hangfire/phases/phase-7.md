---
phase: 7
title: "Production Readiness & Testing"
day: 7
duration: 7h
status: pending
dependencies: [6]
---

# Phase 7: Production Readiness & Testing (Day 7)

**Duration:** 7 hours  
**Goal:** Graceful shutdown, cancellation tokens, performance testing, unit/integration tests, optional SQL Server migration

## Tasks

### 7.1 Graceful Shutdown Implementation
**Duration:** 1.5h  
**Dependencies:** Phase 6 complete

**Actions:**
- Configure `BackgroundJobServer` disposal in `Program.cs`:
  - Use `IHostApplicationLifetime` to hook shutdown events
  - Call `BackgroundJobServer.WaitForShutdown()` with timeout
  - Log shutdown progress
- Test graceful shutdown:
  - Start long-running job (30s+)
  - Trigger app shutdown (Ctrl+C)
  - Verify job completes before shutdown
  - Verify job state preserved in storage
- Configure shutdown timeout: 30 seconds

**Acceptance Criteria:**
- ✅ In-progress jobs complete before shutdown
- ✅ Shutdown timeout respected (force kill after 30s)
- ✅ Shutdown logged clearly
- ✅ No job loss during shutdown (verify in dashboard after restart)
- ✅ Shutdown behavior documented

**Code Example:**
```csharp
var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("[Shutdown] Graceful shutdown initiated...");
    
    var server = app.Services.GetRequiredService<IBackgroundJobServer>();
    server.WaitForShutdown(TimeSpan.FromSeconds(30));
    
    Console.WriteLine("[Shutdown] All background jobs completed");
});

app.Run();
```

---

### 7.2 Job Cancellation Token Support
**Duration:** 1.5h  
**Dependencies:** 7.1

**Actions:**
- Modify OcrJob to accept `IJobCancellationToken`:
  - Check cancellation token during long operations
  - Gracefully abort if cancelled
  - Update document status to "Cancelled"
- Test cancellation:
  - Start long OCR job
  - Delete job from dashboard
  - Verify job aborts cleanly
- Add cancellation support to ThumbnailJob

**Acceptance Criteria:**
- ✅ Jobs accept `IJobCancellationToken` parameter
- ✅ Jobs check cancellation token periodically
- ✅ Cancelled jobs abort without errors
- ✅ Document status updated correctly on cancellation
- ✅ Dashboard "Delete" button cancels jobs

**Code Example:**
```csharp
public class OcrJob
{
    public async Task ProcessOcr(Guid documentId, IJobCancellationToken cancellationToken)
    {
        var document = DocumentStore.Documents[documentId];
        document.Status = DocumentStatus.OcrProcessing;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var filePath = Path.Combine("wwwroot/uploads", document.TenantId, documentId.ToString(), document.FileName);
            
            // Simulate long operation with cancellation checks
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000, cancellationToken.ShutdownToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var text = await _ocrService.ExtractTextAsync(filePath);
            document.OcrText = text;
            document.Status = DocumentStatus.Ready;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[OcrJob] Job cancelled for {documentId}");
            document.Status = DocumentStatus.Cancelled;
            throw;
        }
    }
}
```

---

### 7.3 Performance & Load Testing
**Duration:** 2h  
**Dependencies:** 7.2

**Actions:**
- Create load test script:
  - Enqueue 1000 documents across all queues
  - Mix: 300 critical, 500 default, 200 batch
  - Measure: total completion time, queue depths, worker utilization
  - Monitor memory/CPU usage
- Test sustained load: enqueue 100 docs/minute for 10 minutes
- Identify bottlenecks (OCR processing, file I/O, etc.)
- Document performance characteristics

**Acceptance Criteria:**
- ✅ 1000 documents processed successfully
- ✅ No memory leaks (memory returns to baseline)
- ✅ No queue starvation (all queues progress)
- ✅ Performance metrics documented (docs/second, avg latency)
- ✅ Bottlenecks identified and documented

**Code Example:**
```csharp
// Load test script (simple console app or test)
var client = new BackgroundJobClient();
var stopwatch = Stopwatch.StartNew();

for (int i = 0; i < 1000; i++)
{
    var documentId = Guid.NewGuid();
    var queue = i < 300 ? "critical" : i < 800 ? "default" : "batch";
    
    client.Create<ValidationJob>(
        x => x.ValidateDocument(documentId),
        new EnqueuedState(queue));
    
    if (i % 100 == 0)
        Console.WriteLine($"Enqueued {i} jobs...");
}

stopwatch.Stop();
Console.WriteLine($"Enqueued 1000 jobs in {stopwatch.Elapsed.TotalSeconds:F2}s");

// Monitor completion (check dashboard or poll job counts)
```

---

### 7.4 Unit & Integration Tests
**Duration:** 2h  
**Dependencies:** Phase 6 complete

**Actions:**
- Create test projects:
  - `TenantDoc.Tests.Unit` (xUnit)
  - `TenantDoc.Tests.Integration` (xUnit + Hangfire.InMemory)
- Unit tests:
  - Test ValidationJob logic (mocked dependencies)
  - Test OcrJob logic (mocked IOcrService)
  - Test ThumbnailJob logic
  - Test custom filters in isolation
- Integration tests:
  - End-to-end pipeline test (upload → validate → OCR → thumbnail)
  - Batch processing test
  - Retry policy test
  - Circuit breaker test
- Target 80%+ code coverage for job logic

**Acceptance Criteria:**
- ✅ Unit tests pass (mocked dependencies)
- ✅ Integration tests pass (in-memory Hangfire)
- ✅ Code coverage >80% for Jobs/ folder
- ✅ Tests run in CI-friendly manner (isolated, deterministic)
- ✅ Test documentation included

**Code Example:**
```csharp
// Unit test example
public class ValidationJobTests
{
    [Fact]
    public async Task ValidateDocument_ValidFile_UpdatesStatusToOcrPending()
    {
        // Arrange
        var mockStorage = new Mock<IFileStorageService>();
        var job = new ValidationJob(mockStorage.Object);
        var documentId = Guid.NewGuid();
        DocumentStore.Documents[documentId] = new Document { Id = documentId, Status = DocumentStatus.Uploaded };

        // Act
        await job.ValidateDocument(documentId);

        // Assert
        Assert.Equal(DocumentStatus.OcrPending, DocumentStore.Documents[documentId].Status);
    }
}

// Integration test example
public class PipelineIntegrationTests : IDisposable
{
    private readonly BackgroundJobServer _server;

    public PipelineIntegrationTests()
    {
        GlobalConfiguration.Configuration.UseInMemoryStorage();
        _server = new BackgroundJobServer();
    }

    [Fact]
    public async Task FullPipeline_ValidDocument_CompletesSuccessfully()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document { Id = documentId, TenantId = "test-tenant", FileName = "test.png" };
        DocumentStore.Documents[documentId] = document;

        // Act
        BackgroundJob.Enqueue<ValidationJob>(x => x.ValidateDocument(documentId));

        // Wait for pipeline completion
        await Task.Delay(TimeSpan.FromSeconds(40));

        // Assert
        Assert.Equal(DocumentStatus.Ready, document.Status);
        Assert.NotNull(document.OcrText);
        Assert.NotNull(document.ThumbnailPath);
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
```

---

### 7.5 SQL Server Migration (Optional)
**Duration:** 2h (if time permits)  
**Dependencies:** All previous phases

**Actions:**
- Install NuGet: `Hangfire.SqlServer`
- Setup LocalDB or SQL Server Express
- Create Hangfire database schema
- Update `Program.cs` to use SQL Server storage:
  - Replace `UseInMemoryStorage()` with `UseSqlServerStorage()`
  - Configure connection string
- Migrate existing in-memory jobs (or restart clean)
- Test persistence: restart app, verify jobs survive
- Document migration process

**Acceptance Criteria:**
- ✅ SQL Server storage configured
- ✅ Jobs persist across app restarts
- ✅ Dashboard shows jobs from before restart
- ✅ Performance comparable to in-memory (for small scale)
- ✅ Migration guide documented

**Code Example:**
```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "HangfireDb": "Server=(localdb)\\mssqllocaldb;Database=TenantDocHangfire;Trusted_Connection=True;"
  }
}

// Program.cs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireDb")));

// Run schema migration (first time only)
// Hangfire auto-creates tables on first run
```

---

## Phase 7 Success Metrics

- ✅ Graceful shutdown working (no job loss)
- ✅ Cancellation tokens supported
- ✅ 1000-document load test passed
- ✅ Unit + integration tests passing (80%+ coverage)
- ✅ SQL Server migration documented (optional: completed)

## Risks & Mitigations

**Risk:** SQL Server setup complexity  
**Mitigation:** Use LocalDB for simplicity; document Docker alternative; skip if time constrained

**Risk:** Performance degradation with SQL Server  
**Mitigation:** Expected for small scale; document indexing strategies for production

---

**Navigation:**
- [← Previous Phase: Advanced Error Handling & Custom Filters](phase-6.md)
- [Back to Plan Overview](../plan.md)
