---
phase: 6
title: "Advanced Error Handling & Custom Filters"
day: 6
duration: 7h
status: pending
dependencies: [5]
---

# Phase 6: Advanced Error Handling & Custom Filters (Day 6)

**Duration:** 7 hours  
**Goal:** Retry policies, dead letter queue, circuit breaker, custom filters (tenant isolation, metrics)

## Tasks

### 6.1 Exponential Backoff Retry Policy
**Duration:** 1h  
**Dependencies:** Phase 5 complete

**Actions:**
- Configure `AutomaticRetryAttribute` on OcrJob:
  - 3 retry attempts
  - Exponential backoff: 0s, 2s, 8s delays
  - Log each retry attempt
- Test retry behavior:
  - Upload corrupted image (triggers OCR failure)
  - Verify 3 retry attempts in dashboard
  - Verify delays between retries
- Document retry configuration

**Acceptance Criteria:**
- ✅ Failed OCR jobs retry 3 times
- ✅ Delays observable in dashboard (0s, 2s, 8s)
- ✅ Each retry logged separately
- ✅ Job fails permanently after 3 attempts
- ✅ Retry count visible in dashboard

**Code Example:**
```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 0, 2, 8 })]
[Queue("default")]
public class OcrJob
{
    public async Task ProcessOcr(Guid documentId, PerformContext context)
    {
        var retryCount = context.GetJobParameter<int>("RetryCount");
        Console.WriteLine($"[OcrJob] Processing {documentId} (attempt {retryCount + 1})");

        var document = DocumentStore.Documents[documentId];
        document.Status = DocumentStatus.OcrProcessing;

        try
        {
            var filePath = Path.Combine("wwwroot/uploads", document.TenantId, documentId.ToString(), document.FileName);
            var text = await _ocrService.ExtractTextAsync(filePath);

            document.OcrText = text;
            document.Status = DocumentStatus.Ready;
            document.ProcessedAt = DateTime.UtcNow;
        }
        catch (OcrException ex)
        {
            Console.WriteLine($"[OcrJob] OCR failed for {documentId}: {ex.Message}");
            document.Status = DocumentStatus.OcrFailed;
            throw; // Re-throw to trigger retry
        }
    }
}
```

---

### 6.2 Dead Letter Queue Filter
**Duration:** 1.5h  
**Dependencies:** 6.1

**Actions:**
- Create `Filters/DeadLetterFilter.cs` implementing `IElectStateFilter`:
  - Detect jobs that failed after max retries
  - Move to "failed" queue (dead letter queue)
  - Log to dedicated error store
  - Optionally send notification (console log for now)
- Register filter globally in `Program.cs`
- Test with job that fails 3+ times
- Create API endpoint to retrieve dead letter queue items

**Acceptance Criteria:**
- ✅ Jobs failing after 3 retries moved to dead letter queue
- ✅ Dead letter items logged to error store
- ✅ Dashboard shows failed jobs in "Failed" tab
- ✅ API endpoint returns dead letter items
- ✅ Filter registered globally

**Code Example:**
```csharp
public class DeadLetterFilter : IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        var failedState = context.CandidateState as FailedState;
        if (failedState != null)
        {
            // Check if max retries exceeded
            var retryAttempts = context.GetJobParameter<int>("RetryCount");
            if (retryAttempts >= 3)
            {
                Console.WriteLine($"[DeadLetterFilter] Job {context.BackgroundJob.Id} moved to dead letter queue after {retryAttempts} attempts");

                // Log to dead letter store
                DeadLetterStore.Items.Add(new DeadLetterItem
                {
                    JobId = context.BackgroundJob.Id,
                    JobType = context.BackgroundJob.Job.Type.Name,
                    Exception = failedState.Exception.Message,
                    FailedAt = DateTime.UtcNow,
                    RetryCount = retryAttempts
                });
            }
        }
    }
}

// Register in Program.cs:
builder.Services.AddHangfire(config => config
    .UseInMemoryStorage()
    .UseFilter(new DeadLetterFilter()));

// API endpoint:
app.MapGet("/api/dead-letter", () =>
{
    return Results.Ok(DeadLetterStore.Items.OrderByDescending(x => x.FailedAt).ToList());
});
```

---

### 6.3 Circuit Breaker Filter
**Duration:** 2h  
**Dependencies:** 6.2

**Actions:**
- Create `Filters/CircuitBreakerFilter.cs` implementing `IServerFilter`:
  - Track OCR service failure rate (sliding window: last 10 jobs)
  - Open circuit if >50% failures in window
  - Pause OCR queue for 5 minutes when circuit open
  - Auto-close circuit after timeout
  - Log circuit state changes
- Create circuit breaker state manager (in-memory)
- Test circuit breaker:
  - Trigger 6+ OCR failures rapidly
  - Verify queue paused
  - Verify queue resumes after 5 minutes
- Create endpoint to check circuit breaker status

**Acceptance Criteria:**
- ✅ Circuit opens after 50% failure rate
- ✅ OCR queue paused when circuit open
- ✅ Circuit auto-closes after 5 minutes
- ✅ Circuit state changes logged
- ✅ Status endpoint shows circuit state

**Code Example:**
```csharp
public class CircuitBreakerFilter : IServerFilter
{
    private static readonly CircuitBreakerState _state = new();

    public void OnPerforming(PerformingContext context)
    {
        // Check if circuit is open
        if (_state.IsOpen && context.BackgroundJob.Job.Type == typeof(OcrJob))
        {
            if (DateTime.UtcNow < _state.OpenedUntil)
            {
                Console.WriteLine($"[CircuitBreaker] OCR circuit is OPEN, rejecting job {context.BackgroundJob.Id}");
                throw new InvalidOperationException("Circuit breaker is OPEN - OCR service unavailable");
            }
            else
            {
                // Auto-close circuit
                _state.Close();
                Console.WriteLine("[CircuitBreaker] Circuit auto-closed after timeout");
            }
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.BackgroundJob.Job.Type == typeof(OcrJob))
        {
            var failed = context.Exception != null;
            _state.RecordResult(failed);

            // Check failure rate
            if (_state.FailureRate > 0.5 && _state.WindowSize >= 10)
            {
                _state.Open(TimeSpan.FromMinutes(5));
                Console.WriteLine($"[CircuitBreaker] Circuit OPENED - failure rate: {_state.FailureRate:P}");
            }
        }
    }
}

public class CircuitBreakerState
{
    private readonly Queue<bool> _results = new(10);
    public bool IsOpen { get; private set; }
    public DateTime OpenedUntil { get; private set; }

    public void RecordResult(bool failed)
    {
        if (_results.Count >= 10)
            _results.Dequeue();
        _results.Enqueue(failed);
    }

    public double FailureRate => _results.Count > 0 ? _results.Count(x => x) / (double)_results.Count : 0;
    public int WindowSize => _results.Count;

    public void Open(TimeSpan duration)
    {
        IsOpen = true;
        OpenedUntil = DateTime.UtcNow.Add(duration);
    }

    public void Close()
    {
        IsOpen = false;
        _results.Clear();
    }
}
```

---

### 6.4 Tenant Isolation Filter
**Duration:** 1.5h  
**Dependencies:** Phase 4 complete

**Actions:**
- Create `Filters/TenantIsolationFilter.cs` implementing `IServerFilter`:
  - Extract tenant ID from job arguments
  - Set tenant context (AsyncLocal or HttpContext-like)
  - Log tenant ID with all job logs
  - Verify tenant isolation in multi-tenant scenarios
- Create `TenantContext` static class for context propagation
- Test with jobs from multiple tenants

**Acceptance Criteria:**
- ✅ Tenant ID extracted from job arguments
- ✅ Tenant context available in all job methods
- ✅ Logs include tenant ID prefix
- ✅ Multiple tenants processed concurrently without interference
- ✅ Filter registered globally

**Code Example:**
```csharp
public static class TenantContext
{
    private static readonly AsyncLocal<string?> _tenantId = new();

    public static string? CurrentTenantId
    {
        get => _tenantId.Value;
        set => _tenantId.Value = value;
    }
}

public class TenantIsolationFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        // Extract tenant ID from job arguments
        var documentId = context.BackgroundJob.Job.Args.OfType<Guid>().FirstOrDefault();
        if (documentId != Guid.Empty && DocumentStore.Documents.TryGetValue(documentId, out var document))
        {
            TenantContext.CurrentTenantId = document.TenantId;
            Console.WriteLine($"[TenantIsolation] Job {context.BackgroundJob.Id} executing for tenant {document.TenantId}");
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        TenantContext.CurrentTenantId = null;
    }
}

// Usage in jobs:
public async Task ProcessOcr(Guid documentId)
{
    var tenantId = TenantContext.CurrentTenantId;
    Console.WriteLine($"[OcrJob] [{tenantId}] Processing document {documentId}");
    // ...
}
```

---

### 6.5 Metrics Collection Filter
**Duration:** 1h  
**Dependencies:** 6.4

**Actions:**
- Create `Filters/MetricsFilter.cs` implementing `IServerFilter`:
  - Measure job execution duration
  - Track success/failure rates per job type
  - Store metrics in in-memory store
  - Calculate percentiles (p50, p95, p99)
- Create metrics aggregation service
- Create API endpoint: `GET /api/metrics`
- Test metrics collection with various job types

**Acceptance Criteria:**
- ✅ Job durations recorded accurately
- ✅ Success/failure rates calculated per job type
- ✅ Metrics endpoint returns aggregated data
- ✅ Percentiles calculated correctly (test with sample data)
- ✅ Metrics survive app restarts (or document limitation)

**Code Example:**
```csharp
public class MetricsFilter : IServerFilter
{
    private Stopwatch _stopwatch = new();

    public void OnPerforming(PerformingContext context)
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void OnPerformed(PerformedContext context)
    {
        _stopwatch.Stop();

        var metric = new JobMetric
        {
            JobType = context.BackgroundJob.Job.Type.Name,
            Duration = _stopwatch.Elapsed,
            Success = context.Exception == null,
            ExecutedAt = DateTime.UtcNow
        };

        MetricsStore.Record(metric);
    }
}

public static class MetricsStore
{
    private static readonly ConcurrentBag<JobMetric> _metrics = new();

    public static void Record(JobMetric metric) => _metrics.Add(metric);

    public static object GetAggregated()
    {
        var grouped = _metrics.GroupBy(m => m.JobType);

        return grouped.Select(g => new
        {
            JobType = g.Key,
            TotalExecutions = g.Count(),
            SuccessRate = g.Count(m => m.Success) / (double)g.Count(),
            AvgDuration = g.Average(m => m.Duration.TotalMilliseconds),
            P50Duration = g.OrderBy(m => m.Duration).ElementAt(g.Count() / 2).Duration.TotalMilliseconds,
            P95Duration = g.OrderBy(m => m.Duration).ElementAt((int)(g.Count() * 0.95)).Duration.TotalMilliseconds
        }).ToList();
    }
}

app.MapGet("/api/metrics", () => Results.Ok(MetricsStore.GetAggregated()));
```

---

## Phase 6 Success Metrics

- ✅ Retry policies working (exponential backoff)
- ✅ Dead letter queue operational
- ✅ Circuit breaker protecting OCR service
- ✅ Custom filters: tenant isolation, metrics
- ✅ All filters registered and tested

## Risks & Mitigations

**Risk:** Circuit breaker false positives (opens too frequently)  
**Mitigation:** Tune thresholds (failure rate, window size); add manual override endpoint

**Risk:** Metrics store grows unbounded  
**Mitigation:** Implement sliding window (keep last 10k metrics); document production alternatives (Prometheus, App Insights)

---

**Navigation:**
- [← Previous Phase: Batch Processing](phase-5.md)
- [Back to Plan Overview](../plan.md)
- [Next Phase: Production Readiness & Testing →](phase-7.md)
