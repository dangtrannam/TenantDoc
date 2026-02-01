using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

using Hangfire;

// Note: Queue is dynamically assigned based on tenant tier in ValidationJob
// VIP tenants → critical queue, Standard tenants → default queue
public class OcrJob(IOcrService ocrService, IDocumentStore store, IFileStorageService storage)
{
    private readonly IOcrService _ocrService = ocrService;
    private readonly IDocumentStore _store = store;
    private readonly IFileStorageService _storage = storage;

    public async Task ProcessOcr(Guid documentId)
    {
        var startTime = DateTime.UtcNow;
        var document = _store.Get(documentId);

        if (document == null)
        {
            Console.WriteLine($"[OcrJob] Document {documentId} not found in store");
            return;
        }

        // Update status to OcrProcessing
        document.Status = DocumentStatus.OcrProcessing;
        Console.WriteLine($"[OcrJob] Starting OCR processing for document {documentId} ({document.FileName})");

        try
        {
            // Verify file exists
            if (string.IsNullOrEmpty(document.FilePath) || !_storage.FileExists(document.FilePath))
            {
                Console.WriteLine($"[OcrJob] File not found: {document.FilePath}");
                document.Status = DocumentStatus.OcrFailed;
                return;
            }

            // Extract text using OCR service
            var extractedText = await _ocrService.ExtractTextAsync(document.FilePath);

            // Update document with OCR results
            document.OcrText = extractedText;
            document.Status = DocumentStatus.Ready;
            document.ProcessedAt = DateTime.UtcNow;

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            Console.WriteLine($"[OcrJob] Extracted {extractedText?.Length ?? 0} characters from {documentId} in {duration:F2}s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OcrJob] OCR processing error for {documentId}: {ex.Message}");
            document.Status = DocumentStatus.OcrFailed;
        }
    }
}
