using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Mvc;
using TenantDoc.Api.Jobs;
using TenantDoc.Api.Stores;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(
    IBackgroundJobClient jobClient,
    IDocumentStore store,
    IFileStorageService storage) : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient = jobClient;
    private readonly IDocumentStore _store = store;
    private readonly IFileStorageService _storage = storage;

    /// <summary>
    /// Upload a document file for processing
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="file">Document file (PDF, PNG, or JPG)</param>
    /// <returns>Upload result with document ID and job ID</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromQuery] string tenantId, IFormFile file)
    {
        // Validate file type
        var allowedTypes = new[] { "application/pdf", "image/png", "image/jpeg", "image/jpg" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { error = "Invalid file type. Allowed: PDF, PNG, JPG" });
        }

        // Validate file size (10MB max)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = "File too large. Maximum size: 10MB" });
        }

        if (file.Length == 0)
        {
            return BadRequest(new { error = "File is empty" });
        }

        // Create document entity
        var document = new Document
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FileName = file.FileName,
            FileSize = file.Length,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow
        };

        // Save file to storage
        using var stream = file.OpenReadStream();
        var filePath = await _storage.SaveAsync(stream, tenantId, document.Id.ToString(), file.FileName);
        document.FilePath = filePath;

        // Store metadata
        _store.Add(document);

        // Enqueue validation job
        var jobId = _jobClient.Enqueue<ValidationJob>(x => x.ValidateDocument(document.Id));

        return Ok(new
        {
            documentId = document.Id,
            jobId,
            fileName = file.FileName,
            fileSize = file.Length
        });
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>Document details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDocument(Guid id)
    {
        var document = _store.Get(id);
        if (document == null)
        {
            return NotFound();
        }
        return Ok(document);
    }

    /// <summary>
    /// Test endpoint to enqueue multiple jobs for queue priority testing
    /// </summary>
    /// <returns>Test job IDs</returns>
    [HttpPost("test-queue-priority")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestQueuePriority()
    {
        var result = new
        {
            criticalJobs = new List<string>(),
            defaultJobs = new List<string>(),
            batchJobs = new List<string>()
        };

        Console.WriteLine("[TestQueuePriority] Enqueueing 20 test jobs (10 critical, 5 default, 5 batch)");

        // Enqueue 10 jobs to critical queue (VIP tenant)
        for (int i = 0; i < 10; i++)
        {
            var jobId = _jobClient.Create(() => TestJob($"Critical-{i + 1}"), new EnqueuedState("critical"));
            result.criticalJobs.Add(jobId);
        }

        // Enqueue 5 jobs to default queue
        for (int i = 0; i < 5; i++)
        {
            var jobId = _jobClient.Create(() => TestJob($"Default-{i + 1}"), new EnqueuedState("default"));
            result.defaultJobs.Add(jobId);
        }

        // Enqueue 5 jobs to batch queue
        for (int i = 0; i < 5; i++)
        {
            var jobId = _jobClient.Create(() => TestJob($"Batch-{i + 1}"), new EnqueuedState("batch"));
            result.batchJobs.Add(jobId);
        }

        Console.WriteLine($"[TestQueuePriority] Enqueued {result.criticalJobs.Count} critical, {result.defaultJobs.Count} default, {result.batchJobs.Count} batch jobs");

        return Ok(new
        {
            message = "Test jobs enqueued successfully",
            totalJobs = 20,
            criticalQueueJobs = result.criticalJobs.Count,
            defaultQueueJobs = result.defaultJobs.Count,
            batchQueueJobs = result.batchJobs.Count,
            jobIds = result
        });
    }

    /// <summary>
    /// Test job method for queue priority testing
    /// </summary>
    public static async Task TestJob(string jobName)
    {
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"[TestJob] {jobName} started at {startTime:HH:mm:ss.fff}");

        // Simulate work with 2-5 second delay
        var delay = Random.Shared.Next(2000, 5000);
        await Task.Delay(delay);

        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        Console.WriteLine($"[TestJob] {jobName} completed in {duration:F2}s");
    }
}
