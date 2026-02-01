using System.Collections.Concurrent;
using Hangfire;

namespace TenantDoc.Api.Stores;

/// <summary>
/// Custom batch tracking implementation for coordinating multiple background jobs
/// and triggering continuations when all jobs complete.
/// Alternative to Hangfire.Pro batch API for free/open-source implementation.
/// </summary>
public static class CustomBatchTracker
{
    private class BatchProgress
    {
        public int TotalJobs { get; set; }
        public int CompletedJobs; // Field for Interlocked operations
        public object Lock { get; } = new();
    }

    private static readonly ConcurrentDictionary<Guid, BatchProgress> _batchProgress = new();

    /// <summary>
    /// Initializes tracking for a new batch of jobs.
    /// </summary>
    public static void StartBatch(Guid batchId, int totalJobs)
    {
        _batchProgress[batchId] = new BatchProgress
        {
            TotalJobs = totalJobs,
            CompletedJobs = 0
        };
        Console.WriteLine($"[CustomBatchTracker] Started tracking batch {batchId} with {totalJobs} jobs");
    }

    /// <summary>
    /// Marks one job as completed. When all jobs complete, triggers the continuation callback.
    /// Thread-safe using Interlocked operations.
    /// NOTE: Cleanup is NOT done here to avoid race condition. Call CompleteBatch() from completion handler.
    /// </summary>
    public static void IncrementCompleted(Guid batchId, Action<Guid> onCompleteCallback)
    {
        if (!_batchProgress.TryGetValue(batchId, out var progress))
        {
            Console.WriteLine($"[CustomBatchTracker] WARNING: Batch {batchId} not found in tracker");
            return;
        }

        // Thread-safe increment
        var completed = Interlocked.Increment(ref progress.CompletedJobs);
        Console.WriteLine($"[CustomBatchTracker] Batch {batchId} progress: {completed}/{progress.TotalJobs}");

        // Check if all jobs completed (only one thread will pass this condition)
        if (completed == progress.TotalJobs)
        {
            Console.WriteLine($"[CustomBatchTracker] Batch {batchId} complete! Triggering continuation...");

            // Trigger the continuation callback
            onCompleteCallback(batchId);

            // NOTE: DO NOT clean up here! The completion handler needs to read progress.
            // Call CompleteBatch() from OnBatchComplete() instead.
        }
    }

    /// <summary>
    /// Gets current progress for a batch.
    /// </summary>
    public static (int completed, int total) GetProgress(Guid batchId)
    {
        if (_batchProgress.TryGetValue(batchId, out var progress))
        {
            return (progress.CompletedJobs, progress.TotalJobs);
        }
        return (0, 0);
    }

    /// <summary>
    /// Marks a batch as complete and removes it from tracking.
    /// Should be called from OnBatchComplete() after processing is done.
    /// </summary>
    public static void CompleteBatch(Guid batchId)
    {
        if (_batchProgress.TryRemove(batchId, out _))
        {
            Console.WriteLine($"[CustomBatchTracker] Cleaned up tracking data for batch {batchId}");
        }
    }

    /// <summary>
    /// Clears all batch tracking data (useful for testing).
    /// </summary>
    public static void Clear()
    {
        _batchProgress.Clear();
    }
}
