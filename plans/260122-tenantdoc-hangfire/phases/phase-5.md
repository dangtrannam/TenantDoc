---
phase: 5
title: "Batch Processing"
day: 5
duration: 7h
status: pending
dependencies: [4]
---

# Phase 5: Batch Processing (Day 5)

**Duration:** 7 hours  
**Goal:** Atomic batch uploads using Hangfire Batch API, continuation on batch completion, partial failure handling

## Tasks

### 5.1 Hangfire.Pro Batch Setup (or Alternative)
**Duration:** 1h  
**Dependencies:** Phase 4 complete

**Actions:**
- **Option A (Recommended):** Install `Hangfire.Pro` trial (30 days free)
  - Register for trial license key
  - Install `Hangfire.Pro` NuGet package
  - Configure license in `Program.cs`
- **Option B (Free Alternative):** Implement custom batch tracking
  - Create `BatchTracker` class with batch ID, job IDs, completion counter
  - Use `ContinueJobWith()` for last job in batch
- Document chosen approach

**Acceptance Criteria:**
- ✅ Batch API available (Hangfire.Pro or custom)
- ✅ License configured (if using Pro)
- ✅ Batch jobs visible in dashboard
- ✅ Approach documented in README

**Code Example (Hangfire.Pro):**
```csharp
// Program.cs
GlobalConfiguration.Configuration.UseBatches();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage()
    .UseBatches()); // Enable batch support
```

---

### 5.2 Bulk Upload Endpoint
**Duration:** 1.5h  
**Dependencies:** 5.1

**Actions:**
- Create `/api/documents/bulk-upload` endpoint:
  - Accept `tenantId` + multiple files (IFormFileCollection)
  - Validate all files (fail fast if any invalid)
  - Save all files to storage
  - Return `batchId` + list of `documentId`s
- Add batch metadata tracking (in-memory):
  - BatchId, TenantId, DocumentIds[], Status, CreatedAt
- Test with 10-file batch upload

**Acceptance Criteria:**
- ✅ Endpoint accepts multiple files (test with 10 files)
- ✅ All files validated before processing
- ✅ Files saved to storage
- ✅ Batch metadata stored
- ✅ Returns batchId + documentIds

**Code Example:**
```csharp
app.MapPost("/api/documents/bulk-upload", async (
    string tenantId,
    IFormFileCollection files,
    IFileStorageService storage,
    IBatchJobClient batchClient) =>
{
    // Validate all files first
    foreach (var file in files)
    {
        if (!IsValidFile(file))
            return Results.BadRequest($"Invalid file: {file.FileName}");
    }

    var batchId = Guid.NewGuid();
    var documentIds = new List<Guid>();

    // Save all files
    foreach (var file in files)
    {
        var document = new Document
        {
            TenantId = tenantId,
            FileName = file.FileName,
            Status = DocumentStatus.Uploaded
        };

        using var stream = file.OpenReadStream();
        await storage.SaveAsync(stream, tenantId, document.Id.ToString());

        DocumentStore.Documents[document.Id] = document;
        documentIds.Add(document.Id);
    }

    // Store batch metadata
    BatchStore.Batches[batchId] = new Batch
    {
        Id = batchId,
        TenantId = tenantId,
        DocumentIds = documentIds,
        Status = BatchStatus.Processing,
        CreatedAt = DateTime.UtcNow
    };

    return Results.Ok(new { batchId, documentIds });
});
```

---

### 5.3 BatchProcessingJob Implementation
**Duration:** 2.5h  
**Dependencies:** 5.2

**Actions:**
- Create `Jobs/BatchProcessingJob.cs`:
  - Method: `Task ProcessBatch(Guid batchId)`
  - Use `BatchJob.StartNew()` to create batch
  - Enqueue ValidationJob for each document in batch
  - Configure batch queue
- Create batch completion continuation:
  - Method: `Task OnBatchComplete(Guid batchId)`
  - Aggregate results (success/failure counts)
  - Update batch status
  - Log batch summary
- Modify bulk upload endpoint to enqueue BatchProcessingJob
- Test with 10-document batch

**Acceptance Criteria:**
- ✅ Batch job creates child jobs (visible in dashboard)
- ✅ All child jobs execute in parallel
- ✅ Batch completion continuation triggers only after all jobs finish
- ✅ Dashboard shows batch progress (X/Y jobs completed)
- ✅ Batch summary logged (success/failure counts)

**Code Example (Hangfire.Pro):**
```csharp
public class BatchProcessingJob
{
    public Task ProcessBatch(Guid batchId, IBackgroundJobClient jobClient)
    {
        var batch = BatchStore.Batches[batchId];
        var tenant = TenantStore.Tenants[batch.TenantId];
        var queue = tenant.Tier == TenantTier.VIP ? "critical" : "batch";

        // Create batch of validation jobs
        var batchJobId = BatchJob.StartNew(x =>
        {
            foreach (var docId in batch.DocumentIds)
            {
                x.Enqueue<ValidationJob>(job => job.ValidateDocument(docId));
            }
        });

        // Schedule continuation
        BatchJob.ContinueBatchWith(batchJobId, x => x.OnBatchComplete(batchId));

        Console.WriteLine($"[BatchProcessingJob] Started batch {batchId} with {batch.DocumentIds.Count} documents");
        return Task.CompletedTask;
    }

    public Task OnBatchComplete(Guid batchId)
    {
        var batch = BatchStore.Batches[batchId];
        var documents = batch.DocumentIds.Select(id => DocumentStore.Documents[id]).ToList();

        var successCount = documents.Count(d => d.Status == DocumentStatus.Ready);
        var failureCount = documents.Count(d => d.Status == DocumentStatus.ValidationFailed || d.Status == DocumentStatus.OcrFailed);

        batch.Status = BatchStatus.Completed;
        batch.CompletedAt = DateTime.UtcNow;

        Console.WriteLine($"[BatchProcessingJob] Batch {batchId} complete: {successCount} succeeded, {failureCount} failed");
        return Task.CompletedTask;
    }
}

// In bulk upload endpoint:
jobClient.Enqueue<BatchProcessingJob>(x => x.ProcessBatch(batchId));
```

**Alternative (Custom Batch Tracking):**
```csharp
public class CustomBatchTracker
{
    private static ConcurrentDictionary<Guid, BatchProgress> _batches = new();

    public static void StartBatch(Guid batchId, int totalJobs)
    {
        _batches[batchId] = new BatchProgress { TotalJobs = totalJobs, CompletedJobs = 0 };
    }

    public static void IncrementCompleted(Guid batchId, IBackgroundJobClient jobClient)
    {
        var progress = _batches[batchId];
        var completed = Interlocked.Increment(ref progress.CompletedJobs);

        if (completed == progress.TotalJobs)
        {
            // Trigger continuation
            jobClient.Enqueue<BatchProcessingJob>(x => x.OnBatchComplete(batchId));
        }
    }
}
```

---

### 5.4 Partial Failure Handling
**Duration:** 1.5h  
**Dependencies:** 5.3

**Actions:**
- Test batch with intentional failures:
  - Upload batch with 2 corrupted files + 8 valid files
  - Verify failed jobs don't block batch completion
  - Verify continuation runs after all jobs finish (even with failures)
- Create batch status endpoint:
  - `GET /api/batches/{batchId}`
  - Return batch metadata + per-document status
- Document partial failure behavior

**Acceptance Criteria:**
- ✅ Batch completes even if some jobs fail
- ✅ Continuation receives failure count
- ✅ Failed documents identifiable via status endpoint
- ✅ Dashboard shows batch with mixed success/failure
- ✅ Partial failure behavior documented

**Code Example:**
```csharp
app.MapGet("/api/batches/{batchId:guid}", (Guid batchId) =>
{
    if (!BatchStore.Batches.TryGetValue(batchId, out var batch))
        return Results.NotFound();

    var documents = batch.DocumentIds
        .Select(id => DocumentStore.Documents[id])
        .Select(d => new
        {
            d.Id,
            d.FileName,
            d.Status,
            d.ProcessedAt
        })
        .ToList();

    return Results.Ok(new
    {
        batch.Id,
        batch.Status,
        batch.CreatedAt,
        batch.CompletedAt,
        TotalDocuments = documents.Count,
        SuccessCount = documents.Count(d => d.Status == DocumentStatus.Ready),
        FailureCount = documents.Count(d => d.Status == DocumentStatus.ValidationFailed || d.Status == DocumentStatus.OcrFailed),
        Documents = documents
    });
});
```

---

### 5.5 Performance Testing
**Duration:** 30min  
**Dependencies:** 5.4

**Actions:**
- Test large batch: 100 documents
- Measure metrics:
  - Total batch completion time
  - Average time per document
  - Queue depth during processing
  - Worker utilization (dashboard)
- Test VIP vs standard batch performance
- Document performance observations

**Acceptance Criteria:**
- ✅ 100-document batch completes successfully
- ✅ VIP batch completes faster than standard batch
- ✅ No memory leaks (monitor app memory during test)
- ✅ Dashboard remains responsive during batch processing
- ✅ Performance metrics documented

---

## Phase 5 Success Metrics

- ✅ Batch processing working (Hangfire.Pro or custom)
- ✅ Bulk upload endpoint functional
- ✅ Batch completion continuations working
- ✅ Partial failures handled gracefully
- ✅ 100-document batch tested successfully

## Risks & Mitigations

**Risk:** Hangfire.Pro trial expires  
**Mitigation:** Document trial duration; implement custom batch tracking as fallback

**Risk:** Large batches exhaust memory  
**Mitigation:** Limit batch size to 100 documents; document scaling considerations for production

---

**Navigation:**
- [← Previous Phase: Queue System & Recurring Jobs](phase-4.md)
- [Back to Plan Overview](../plan.md)
- [Next Phase: Advanced Error Handling & Custom Filters →](phase-6.md)
