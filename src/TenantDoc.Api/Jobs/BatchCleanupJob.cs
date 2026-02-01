using Hangfire;
using TenantDoc.Api.Stores;

namespace TenantDoc.Api.Jobs;

/// <summary>
/// Cleans up old completed batches from memory to prevent memory leaks.
/// Runs as a recurring job to maintain bounded memory usage.
/// </summary>
[Queue("batch")]
public class BatchCleanupJob
{
    /// <summary>
    /// Removes batches older than 24 hours from BatchStore.
    /// Keeps recent batches for status queries but prevents unbounded growth.
    /// </summary>
    public Task CleanupOldBatches()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var batches = BatchStore.GetAll().ToList();
        var initialCount = batches.Count;
        var removedCount = 0;

        Console.WriteLine($"[BatchCleanupJob] Starting cleanup. Total batches: {initialCount}");

        foreach (var batch in batches)
        {
            // Only clean up completed batches older than 24 hours
            if (batch.CompletedAt.HasValue && batch.CompletedAt.Value < cutoffTime)
            {
                BatchStore.Remove(batch.Id);
                removedCount++;
                Console.WriteLine($"[BatchCleanupJob] Removed batch {batch.Id} (completed: {batch.CompletedAt.Value:yyyy-MM-dd HH:mm:ss})");
            }
        }

        Console.WriteLine($"[BatchCleanupJob] Cleanup complete. Removed {removedCount} batches. Remaining: {initialCount - removedCount}");
        return Task.CompletedTask;
    }
}
