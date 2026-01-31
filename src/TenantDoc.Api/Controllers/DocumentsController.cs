using Hangfire;
using Microsoft.AspNetCore.Mvc;
using TenantDoc.Api.Jobs;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IDocumentStore _store;
    private readonly IFileStorageService _storage;

    public DocumentsController(
        IBackgroundJobClient jobClient,
        IDocumentStore store,
        IFileStorageService storage)
    {
        _jobClient = jobClient;
        _store = store;
        _storage = storage;
    }

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
}
