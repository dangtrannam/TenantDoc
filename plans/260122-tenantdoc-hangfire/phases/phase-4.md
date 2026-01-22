---
phase: 4
title: "Queue System & Recurring Jobs"
day: 4
duration: 7h
status: pending
dependencies: [3]
---

# Phase 4: Queue System & Recurring Jobs (Day 4)

**Duration:** 7 hours  
**Goal:** 3-tier queue system (critical/default/batch), recurring cleanup/reports, priority behavior verified

## Tasks

### 4.1 Queue Configuration
**Duration:** 1.5h  
**Dependencies:** Phase 3 complete

**Actions:**
- Configure Hangfire server with 3 queues in `Program.cs`:
  - `options.Queues = new[] { "critical", "default", "batch" }`
  - `options.WorkerCount = 7` (4 critical, 2 default, 1 batch)
- Document queue allocation strategy
- Assign jobs to queues using `[Queue("queueName")]` attribute:
  - ValidationJob → default queue
  - OcrJob → default queue (will change based on tenant tier later)
  - ThumbnailJob → default queue
- Test queue visibility in dashboard

**Acceptance Criteria:**
- ✅ Dashboard shows 3 queues: critical, default, batch
- ✅ Worker allocation visible (critical: 4, default: 2, batch: 1)
- ✅ Jobs appear in correct queue tabs
- ✅ Server config documented in comments

**Code Example:**
```csharp
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "critical", "default", "batch" };
    options.WorkerCount = 7; // Total workers across all queues
    // Hangfire allocates workers proportionally based on queue order
    // Approx: critical=4, default=2, batch=1
});

// Job attribute
[Queue("default")]
public class ValidationJob
{
    // ...
}
```

---

### 4.2 Tenant-Based Queue Assignment
**Duration:** 2h  
**Dependencies:** 4.1

**Actions:**
- Add `TenantTier` enum to Tenant model: Standard, VIP
- Create in-memory tenant store (mock VIP tenants)
- Modify OcrJob queue assignment:
  - VIP tenants → critical queue
  - Standard tenants → default queue
- Use dynamic queue assignment: `BackgroundJob.Enqueue()` with queue parameter
- Test priority behavior: enqueue 10 VIP jobs + 10 standard jobs simultaneously

**Acceptance Criteria:**
- ✅ VIP tenant jobs appear in critical queue
- ✅ Standard tenant jobs appear in default queue
- ✅ VIP jobs complete faster (4 workers vs 2 workers)
- ✅ Dashboard shows queue distribution
- ✅ Tenant tier logic documented

**Code Example:**
```csharp
public static class TenantStore
{
    public static Dictionary<string, Tenant> Tenants = new()
    {
        ["tenant-vip-1"] = new Tenant { Id = "tenant-vip-1", Name = "VIP Corp", Tier = TenantTier.VIP },
        ["tenant-std-1"] = new Tenant { Id = "tenant-std-1", Name = "Standard Inc", Tier = TenantTier.Standard }
    };
}

// In ValidationJob when scheduling OcrJob:
var tenant = TenantStore.Tenants[document.TenantId];
var queue = tenant.Tier == TenantTier.VIP ? "critical" : "default";

jobClient.Schedule<OcrJob>(
    x => x.ProcessOcr(documentId),
    TimeSpan.FromSeconds(30),
    queue);
```

---

### 4.3 Recurring Cleanup Job
**Duration:** 1.5h  
**Dependencies:** 4.1

**Actions:**
- Create `Jobs/CleanupJob.cs`:
  - Method: `Task CleanupOldDocuments()`
  - Delete documents + files older than 24 hours
  - Log cleanup statistics (files deleted, bytes freed)
- Register recurring job in `Program.cs`:
  - Use `RecurringJob.AddOrUpdate()`
  - Schedule: Daily at 2 AM (cron: `0 2 * * *`)
  - Queue: batch
- Test manual trigger via dashboard "Trigger now" button

**Acceptance Criteria:**
- ✅ Recurring job visible in "Recurring jobs" tab
- ✅ Next execution time displayed correctly
- ✅ Manual trigger executes immediately
- ✅ Cleanup logic tested (create old files, verify deletion)
- ✅ Cleanup runs in batch queue

**Code Example:**
```csharp
public class CleanupJob
{
    private readonly IFileStorageService _storage;

    public async Task CleanupOldDocuments()
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-24);
        var oldDocuments = DocumentStore.Documents.Values
            .Where(d => d.UploadedAt < cutoffDate)
            .ToList();

        foreach (var doc in oldDocuments)
        {
            var filePath = Path.Combine("wwwroot/uploads", doc.TenantId, doc.Id.ToString());
            if (Directory.Exists(filePath))
            {
                Directory.Delete(filePath, recursive: true);
            }
            DocumentStore.Documents.TryRemove(doc.Id, out _);
        }

        Console.WriteLine($"[CleanupJob] Deleted {oldDocuments.Count} old documents");
    }
}

// In Program.cs (after app.Run()):
RecurringJob.AddOrUpdate<CleanupJob>(
    "daily-cleanup",
    x => x.CleanupOldDocuments(),
    "0 2 * * *", // Daily at 2 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc, Queue = "batch" });
```

---

### 4.4 Recurring Usage Reports Job
**Duration:** 1.5h  
**Dependencies:** 4.3

**Actions:**
- Create `Jobs/UsageReportJob.cs`:
  - Method: `Task GenerateHourlyReport()`
  - Aggregate metrics per tenant: document count, total OCR characters, success rate
  - Log report to console (JSON format)
  - Store report in in-memory collection (for later API retrieval)
- Register recurring job:
  - Schedule: Hourly (cron: `0 * * * *`)
  - Queue: batch
- Test report generation with sample data

**Acceptance Criteria:**
- ✅ Hourly recurring job registered
- ✅ Report includes all active tenants
- ✅ Metrics accurate (verify counts manually)
- ✅ Report stored in memory (retrievable via API)
- ✅ Job runs in batch queue

**Code Example:**
```csharp
public class UsageReportJob
{
    public Task GenerateHourlyReport()
    {
        var report = DocumentStore.Documents.Values
            .GroupBy(d => d.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                DocumentCount = g.Count(),
                TotalOcrCharacters = g.Sum(d => d.OcrText?.Length ?? 0),
                SuccessRate = g.Count(d => d.Status == DocumentStatus.Ready) / (double)g.Count()
            })
            .ToList();

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine($"[UsageReportJob] Hourly report:\n{json}");

        return Task.CompletedTask;
    }
}

RecurringJob.AddOrUpdate<UsageReportJob>(
    "hourly-usage-report",
    x => x.GenerateHourlyReport(),
    "0 * * * *",
    new RecurringJobOptions { Queue = "batch" });
```

---

### 4.5 Queue Priority Testing
**Duration:** 30min  
**Dependencies:** 4.2

**Actions:**
- Create test script:
  - Enqueue 20 jobs: 10 critical, 5 default, 5 batch
  - Measure completion times per queue
  - Verify critical queue jobs complete first
- Document observations in test log
- Test queue starvation: flood batch queue, verify critical/default still responsive

**Acceptance Criteria:**
- ✅ Critical queue jobs complete fastest (avg <10s)
- ✅ Default queue jobs complete within 30s
- ✅ Batch queue jobs complete last (may take 1+ min)
- ✅ No queue starvation (critical/default never blocked by batch)
- ✅ Dashboard shows live queue depth

---

## Phase 4 Success Metrics

- ✅ 3-tier queue system operational
- ✅ Tenant-based queue routing working
- ✅ Recurring jobs registered and executing
- ✅ Priority behavior verified (critical > default > batch)
- ✅ Dashboard shows queue/recurring job metrics

## Risks & Mitigations

**Risk:** Worker allocation not proportional  
**Mitigation:** Hangfire distributes workers based on queue order; document actual behavior; adjust WorkerCount if needed

**Risk:** Recurring job cron syntax errors  
**Mitigation:** Use online cron validator; test with short intervals (every minute) during development

---

**Navigation:**
- [← Previous Phase: Delayed Jobs & Continuations](phase-3.md)
- [Back to Plan Overview](../plan.md)
- [Next Phase: Batch Processing →](phase-5.md)
