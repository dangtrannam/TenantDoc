using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

public class ThumbnailJob(IThumbnailService thumbnailService, IDocumentStore store, IFileStorageService storage)
{
    private readonly IThumbnailService _thumbnailService = thumbnailService;
    private readonly IDocumentStore _store = store;
    private readonly IFileStorageService _storage = storage;

    public async Task GenerateThumbnail(Guid documentId)
    {
        var startTime = DateTime.UtcNow;
        var document = _store.Get(documentId);

        if (document == null)
        {
            Console.WriteLine($"[ThumbnailJob] Document {documentId} not found in store");
            return;
        }

        Console.WriteLine($"[ThumbnailJob] Starting thumbnail generation for document {documentId} ({document.FileName})");

        try
        {
            // Verify file exists
            if (string.IsNullOrEmpty(document.FilePath) || !_storage.FileExists(document.FilePath))
            {
                Console.WriteLine($"[ThumbnailJob] File not found: {document.FilePath}");
                return;
            }

            // Check if file type is supported for thumbnail generation
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tga" };

            if (!supportedExtensions.Contains(extension))
            {
                Console.WriteLine($"[ThumbnailJob] Skipping thumbnail generation for unsupported file type: {extension} (Document: {documentId})");
                Console.WriteLine($"[ThumbnailJob] Supported formats: {string.Join(", ", supportedExtensions)}");
                return;
            }

            // Generate thumbnail (200x200)
            var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(document.FilePath, 200, 200);

            // Update document with thumbnail path
            document.ThumbnailPath = thumbnailPath;

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            Console.WriteLine($"[ThumbnailJob] Generated thumbnail for {documentId} at {thumbnailPath} in {duration:F2}s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThumbnailJob] Thumbnail generation error for {documentId}: {ex.Message}");
        }
    }
}
