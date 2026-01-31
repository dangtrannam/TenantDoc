using Microsoft.Extensions.Hosting;
using TenantDoc.Core.Interfaces;

namespace TenantDoc.Infrastructure.Storage;

/// <summary>
/// Local file system implementation of document storage
/// Storage structure: wwwroot/uploads/{tenantId}/{documentId}/{fileName}
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IHostEnvironment env)
    {
        _basePath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");

        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream fileStream, string tenantId, string documentId, string fileName)
    {
        // Create tenant and document directories
        var documentDir = Path.Combine(_basePath, tenantId, documentId);
        Directory.CreateDirectory(documentDir);

        // Save file
        var filePath = Path.Combine(documentDir, fileName);
        using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamOutput);

        return filePath;
    }

    public async Task<Stream?> GetAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public Task DeleteAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public long GetFileSize(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return 0;
        }

        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
    }
}
