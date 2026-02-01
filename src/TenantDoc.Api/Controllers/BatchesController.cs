using Microsoft.AspNetCore.Mvc;
using TenantDoc.Api.Stores;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController(IDocumentStore documentStore) : ControllerBase
{
    private readonly IDocumentStore _documentStore = documentStore;

    /// <summary>
    /// Get batch status and progress
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <returns>Batch metadata with per-document status</returns>
    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBatchStatus(Guid batchId)
    {
        var batch = BatchStore.Get(batchId);
        if (batch == null)
        {
            return NotFound(new { error = $"Batch {batchId} not found" });
        }

        // Get document details
        var documents = batch.DocumentIds
            .Select(id => _documentStore.Get(id))
            .Where(d => d != null)
            .Select(d => new
            {
                d!.Id,
                d.FileName,
                d.Status,
                d.ProcessedAt,
                d.FileSize
            })
            .ToList();

        // Calculate statistics
        var successCount = documents.Count(d => d.Status == DocumentStatus.Ready);
        var failureCount = documents.Count(d =>
            d.Status == DocumentStatus.ValidationFailed ||
            d.Status == DocumentStatus.OcrFailed);
        var processingCount = documents.Count(d =>
            d.Status == DocumentStatus.Uploaded ||
            d.Status == DocumentStatus.Validating ||
            d.Status == DocumentStatus.OcrPending ||
            d.Status == DocumentStatus.OcrProcessing);

        // Get current progress from tracker (if batch is still processing)
        var (completedJobs, totalJobs) = CustomBatchTracker.GetProgress(batchId);

        return Ok(new
        {
            batchId = batch.Id,
            tenantId = batch.TenantId,
            status = batch.Status.ToString(),
            createdAt = batch.CreatedAt,
            completedAt = batch.CompletedAt,
            duration = batch.CompletedAt.HasValue
                ? (batch.CompletedAt.Value - batch.CreatedAt).TotalSeconds
                : (DateTime.UtcNow - batch.CreatedAt).TotalSeconds,
            totalDocuments = documents.Count,
            statistics = new
            {
                successCount,
                failureCount,
                processingCount,
                successRate = documents.Count > 0 ? (double)successCount / documents.Count * 100 : 0
            },
            progress = totalJobs > 0 ? new
            {
                completedJobs,
                totalJobs,
                percentComplete = (double)completedJobs / totalJobs * 100
            } : null,
            documents
        });
    }

    /// <summary>
    /// Get all batches
    /// </summary>
    /// <returns>List of all batches</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllBatches()
    {
        var batches = BatchStore.GetAll()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.TenantId,
                b.Status,
                b.CreatedAt,
                b.CompletedAt,
                documentCount = b.DocumentIds.Count,
                duration = b.CompletedAt.HasValue
                    ? (double?)(b.CompletedAt.Value - b.CreatedAt).TotalSeconds
                    : null
            })
            .ToList();

        return Ok(new
        {
            totalBatches = batches.Count,
            batches
        });
    }
}
