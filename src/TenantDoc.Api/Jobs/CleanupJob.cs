using Hangfire;
using TenantDoc.Core.Interfaces;

namespace TenantDoc.Api.Jobs;

/// <summary>
/// Recurring job that cleans up old documents and their associated files.
/// Runs daily at 2 AM UTC in the batch queue.
/// </summary>
[Queue("batch")]
public class CleanupJob(IDocumentStore store, IFileStorageService storage)
{
    private readonly IDocumentStore _store = store;
    private readonly IFileStorageService _storage = storage;

    public async Task CleanupOldDocuments()
    {
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"[CleanupJob] Starting cleanup at {startTime:yyyy-MM-dd HH:mm:ss} UTC");

        try
        {
            // Find documents older than 24 hours
            var cutoffDate = DateTime.UtcNow.AddHours(-24);
            var allDocuments = _store.GetAll();
            var oldDocuments = allDocuments.Where(d => d.UploadedAt < cutoffDate).ToList();

            if (oldDocuments.Count == 0)
            {
                Console.WriteLine("[CleanupJob] No old documents found to clean up");
                return;
            }

            Console.WriteLine($"[CleanupJob] Found {oldDocuments.Count} documents older than {cutoffDate:yyyy-MM-dd HH:mm:ss} UTC");

            long totalBytesFreed = 0;
            int filesDeleted = 0;
            int documentsDeleted = 0;

            foreach (var document in oldDocuments)
            {
                try
                {
                    // Delete the document file and thumbnail if they exist
                    if (!string.IsNullOrEmpty(document.FilePath) && _storage.FileExists(document.FilePath))
                    {
                        var fileSize = _storage.GetFileSize(document.FilePath);
                        await _storage.DeleteAsync(document.FilePath);
                        totalBytesFreed += fileSize;
                        filesDeleted++;
                        Console.WriteLine($"[CleanupJob] Deleted file: {document.FilePath} ({FormatBytes(fileSize)})");
                    }

                    if (!string.IsNullOrEmpty(document.ThumbnailPath) && _storage.FileExists(document.ThumbnailPath))
                    {
                        var thumbnailSize = _storage.GetFileSize(document.ThumbnailPath);
                        await _storage.DeleteAsync(document.ThumbnailPath);
                        totalBytesFreed += thumbnailSize;
                        filesDeleted++;
                        Console.WriteLine($"[CleanupJob] Deleted thumbnail: {document.ThumbnailPath} ({FormatBytes(thumbnailSize)})");
                    }

                    // Delete the document folder if it's empty
                    var documentFolder = Path.GetDirectoryName(document.FilePath);
                    if (!string.IsNullOrEmpty(documentFolder) && Directory.Exists(documentFolder))
                    {
                        var remainingFiles = Directory.GetFiles(documentFolder);
                        if (remainingFiles.Length == 0)
                        {
                            Directory.Delete(documentFolder, recursive: false);
                            Console.WriteLine($"[CleanupJob] Deleted empty folder: {documentFolder}");
                        }
                    }

                    // Remove document from store
                    _store.Delete(document.Id);

                    documentsDeleted++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CleanupJob] Error cleaning up document {document.Id}: {ex.Message}");
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            Console.WriteLine($"[CleanupJob] Cleanup completed in {duration:F2}s");
            Console.WriteLine($"[CleanupJob] Statistics: {documentsDeleted} documents deleted, {filesDeleted} files deleted, {FormatBytes(totalBytesFreed)} freed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CleanupJob] Cleanup job failed: {ex.Message}");
            throw;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
