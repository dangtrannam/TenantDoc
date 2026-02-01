using Hangfire;
using TenantDoc.Api.Stores;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

/// <summary>
/// Handles batch document processing using custom batch tracking.
/// Coordinates multiple validation jobs and triggers continuation when all complete.
/// </summary>
[Queue("batch")]
public class BatchProcessingJob(IBackgroundJobClient jobClient, IDocumentStore documentStore)
{
    private readonly IBackgroundJobClient _jobClient = jobClient;
    private readonly IDocumentStore _documentStore = documentStore;

    /// <summary>
    /// Processes a batch of documents by enqueueing individual validation jobs.
    /// Uses CustomBatchTracker to coordinate completion and trigger continuation.
    /// </summary>
    public Task ProcessBatch(Guid batchId)
    {
        var batch = BatchStore.Get(batchId);
        if (batch == null)
        {
            var errorMsg = $"Batch {batchId} not found in store";
            Console.WriteLine($"[BatchProcessingJob] ERROR: {errorMsg}");
            throw new InvalidOperationException(errorMsg);
        }

        // Determine queue based on tenant tier
        var queueName = TenantStore.GetQueueForTenant(batch.TenantId);
        var tenant = TenantStore.GetTenant(batch.TenantId);
        var tierLabel = tenant?.Tier.ToString() ?? "Unknown";

        Console.WriteLine($"[BatchProcessingJob] Starting batch {batchId} with {batch.DocumentIds.Count} documents");
        Console.WriteLine($"[BatchProcessingJob] Tenant: {batch.TenantId} (Tier: {tierLabel}) → Queue: {queueName}");

        // Initialize batch tracking
        CustomBatchTracker.StartBatch(batchId, batch.DocumentIds.Count);

        // Enqueue validation job for each document
        foreach (var documentId in batch.DocumentIds)
        {
            // Create validation job in the appropriate queue
            var jobId = _jobClient.Create<ValidationJob>(
                x => x.ValidateDocument(documentId),
                new Hangfire.States.EnqueuedState(queueName));

            Console.WriteLine($"[BatchProcessingJob] Enqueued ValidationJob {jobId} for document {documentId} in queue '{queueName}'");

            // Schedule a completion tracker job that will increment the batch progress
            // This runs AFTER the validation job completes (success or failure)
            _jobClient.ContinueJobWith(jobId, () => TrackJobCompletion(batchId));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tracks individual job completion and triggers batch continuation when all jobs finish.
    /// Called as a continuation for each job in the batch.
    /// </summary>
    public void TrackJobCompletion(Guid batchId)
    {
        Console.WriteLine($"[BatchProcessingJob] Job completed for batch {batchId}");

        // Increment completion counter and trigger OnBatchComplete when all jobs finish
        CustomBatchTracker.IncrementCompleted(batchId, (completedBatchId) =>
        {
            // Enqueue the completion handler
            _jobClient.Enqueue<BatchProcessingJob>(x => x.OnBatchComplete(completedBatchId));
        });
    }

    /// <summary>
    /// Handles batch completion - aggregates results and updates batch status.
    /// Triggered automatically when all jobs in the batch complete.
    /// </summary>
    public Task OnBatchComplete(Guid batchId)
    {
        var batch = BatchStore.Get(batchId);
        if (batch == null)
        {
            var errorMsg = $"Batch {batchId} not found during completion";
            Console.WriteLine($"[BatchProcessingJob] ERROR: {errorMsg}");
            throw new InvalidOperationException(errorMsg);
        }

        // Aggregate results from all documents
        var documents = batch.DocumentIds
            .Select(id => _documentStore.Get(id))
            .Where(d => d != null)
            .ToList();

        var successCount = documents.Count(d => d!.Status == DocumentStatus.Ready);
        var failureCount = documents.Count(d =>
            d!.Status == DocumentStatus.ValidationFailed ||
            d!.Status == DocumentStatus.OcrFailed);
        var processingCount = documents.Count(d =>
            d!.Status == DocumentStatus.Uploaded ||
            d!.Status == DocumentStatus.Validating ||
            d!.Status == DocumentStatus.OcrPending ||
            d!.Status == DocumentStatus.OcrProcessing);

        // Update batch status
        batch.Status = failureCount > 0 ? BatchStatus.PartialFailure : BatchStatus.Completed;
        batch.CompletedAt = DateTime.UtcNow;
        BatchStore.Update(batch);

        var duration = (batch.CompletedAt.Value - batch.CreatedAt).TotalSeconds;

        Console.WriteLine($"[BatchProcessingJob] ═══════════════════════════════════════");
        Console.WriteLine($"[BatchProcessingJob] Batch {batchId} COMPLETE");
        Console.WriteLine($"[BatchProcessingJob] Status: {batch.Status}");
        Console.WriteLine($"[BatchProcessingJob] Duration: {duration:F2}s");
        Console.WriteLine($"[BatchProcessingJob] Total Documents: {documents.Count}");
        Console.WriteLine($"[BatchProcessingJob] ✓ Success: {successCount}");
        Console.WriteLine($"[BatchProcessingJob] ✗ Failed: {failureCount}");
        Console.WriteLine($"[BatchProcessingJob] ⋯ Processing: {processingCount}");
        Console.WriteLine($"[BatchProcessingJob] ═══════════════════════════════════════");

        // Clean up batch tracking data (after we're done reading progress)
        CustomBatchTracker.CompleteBatch(batchId);

        return Task.CompletedTask;
    }
}
