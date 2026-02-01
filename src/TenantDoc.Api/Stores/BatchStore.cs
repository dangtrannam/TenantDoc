using System.Collections.Concurrent;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Stores;

/// <summary>
/// In-memory storage for batch metadata and state tracking.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public static class BatchStore
{
    private static readonly ConcurrentDictionary<Guid, Batch> _batches = new();

    public static Batch? Get(Guid batchId)
    {
        _batches.TryGetValue(batchId, out var batch);
        return batch;
    }

    public static void Add(Batch batch)
    {
        _batches[batch.Id] = batch;
    }

    public static void Update(Batch batch)
    {
        _batches[batch.Id] = batch;
    }

    public static IEnumerable<Batch> GetAll()
    {
        return _batches.Values;
    }

    public static bool Remove(Guid batchId)
    {
        return _batches.TryRemove(batchId, out _);
    }

    public static void Clear()
    {
        _batches.Clear();
    }
}
