using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

public class ValidationJob
{
    private readonly IDocumentStore _store;

    public ValidationJob(IDocumentStore store)
    {
        _store = store;
    }

    public async Task ValidateDocument(Guid documentId)
    {
        var document = _store.Get(documentId);
        if (document == null)
        {
            Console.WriteLine($"[ValidationJob] Document {documentId} not found in store");
            return;
        }

        // 1. Update status to Validating
        document.Status = DocumentStatus.Validating;
        Console.WriteLine($"[ValidationJob] Starting validation for {documentId}");
        
        // Simulate processing time
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        // 2. Mock validation logic - 90% success rate
        var isValid = Random.Shared.Next(10) > 0;
        
        if (isValid)
        {
            document.Status = DocumentStatus.OcrPending;
            Console.WriteLine($"[ValidationJob] Document {documentId} validated successfully");
        }
        else
        {
            document.Status = DocumentStatus.ValidationFailed;
            Console.WriteLine($"[ValidationJob] Document {documentId} validation failed");
        }
    }
}
