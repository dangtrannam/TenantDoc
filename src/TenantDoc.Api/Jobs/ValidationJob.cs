using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

public class ValidationJob
{
    private readonly IDocumentStore _store;
    private readonly IFileStorageService _storage;

    public ValidationJob(IDocumentStore store, IFileStorageService storage)
    {
        _store = store;
        _storage = storage;
    }

    public async Task ValidateDocument(Guid documentId)
    {
        var startTime = DateTime.UtcNow;
        var document = _store.Get(documentId);

        if (document == null)
        {
            Console.WriteLine($"[ValidationJob] Document {documentId} not found in store");
            return;
        }

        // Update status to Validating
        document.Status = DocumentStatus.Validating;
        Console.WriteLine($"[ValidationJob] Starting validation for document {documentId} ({document.FileName})");

        try
        {
            // 1. Verify file exists on disk
            if (string.IsNullOrEmpty(document.FilePath) || !_storage.FileExists(document.FilePath))
            {
                Console.WriteLine($"[ValidationJob] File not found: {document.FilePath}");
                document.Status = DocumentStatus.ValidationFailed;
                return;
            }

            // 2. Check file size matches metadata
            var actualFileSize = _storage.GetFileSize(document.FilePath);
            if (actualFileSize != document.FileSize)
            {
                Console.WriteLine($"[ValidationJob] File size mismatch. Expected: {document.FileSize}, Actual: {actualFileSize}");
                document.Status = DocumentStatus.ValidationFailed;
                return;
            }

            // 3. Mock virus scan (random delay 1-3s)
            var scanDelay = Random.Shared.Next(1000, 3000);
            await Task.Delay(scanDelay);
            Console.WriteLine($"[ValidationJob] Virus scan completed in {scanDelay}ms");

            // 4. Mock validation result (90% success rate)
            var isValid = Random.Shared.Next(10) > 0;

            if (isValid)
            {
                document.Status = DocumentStatus.OcrPending;
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                Console.WriteLine($"[ValidationJob] Document {documentId} validated successfully in {duration:F2}s");
            }
            else
            {
                document.Status = DocumentStatus.ValidationFailed;
                Console.WriteLine($"[ValidationJob] Document {documentId} validation failed (simulated failure)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ValidationJob] Validation error for {documentId}: {ex.Message}");
            document.Status = DocumentStatus.ValidationFailed;
        }
    }
}
