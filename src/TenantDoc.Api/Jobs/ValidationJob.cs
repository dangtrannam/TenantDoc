using Hangfire;
using Hangfire.States;
using TenantDoc.Api.Stores;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

[Queue("default")]
public class ValidationJob(IDocumentStore store, IFileStorageService storage, IBackgroundJobClient jobClient)
{
    private readonly IDocumentStore _store = store;
    private readonly IFileStorageService _storage = storage;
    private readonly IBackgroundJobClient _jobClient = jobClient;

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

                // Determine queue based on tenant tier (VIP → critical, Standard → default)
                var queueName = TenantStore.GetQueueForTenant(document.TenantId);
                var tenant = TenantStore.GetTenant(document.TenantId);
                var tierLabel = tenant?.Tier.ToString() ?? "Unknown";

                if (tenant == null)
                {
                    Console.WriteLine($"[ValidationJob] WARNING: Tenant {document.TenantId} not found in TenantStore. Defaulting to 'default' queue.");
                }

                Console.WriteLine($"[ValidationJob] Tenant: {document.TenantId} (Tier: {tierLabel}) → Queue: {queueName}");

                // Schedule OCR job with 30-second delay in tenant-specific queue
                // Create the job in Scheduled state with the specified queue
                var ocrJobId = _jobClient.Create<OcrJob>(
                    x => x.ProcessOcr(documentId),
                    new EnqueuedState(queueName));

                // Change the job state to scheduled with 30 second delay
                _jobClient.ChangeState(ocrJobId, new ScheduledState(TimeSpan.FromSeconds(30)));

                Console.WriteLine($"[ValidationJob] Scheduled OcrJob for {documentId} with 30s delay in '{queueName}' queue (JobId: {ocrJobId})");

                // Schedule ThumbnailJob as continuation (runs only if OcrJob succeeds)
                _jobClient.ContinueJobWith<ThumbnailJob>(
                    ocrJobId,
                    x => x.GenerateThumbnail(documentId));
                Console.WriteLine($"[ValidationJob] Scheduled ThumbnailJob continuation for {documentId}");
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
