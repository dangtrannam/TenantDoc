namespace TenantDoc.Core.Interfaces;

/// <summary>
/// Service for managing document file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save a file stream to tenant-isolated storage
    /// </summary>
    /// <param name="fileStream">File content stream</param>
    /// <param name="tenantId">Tenant identifier for isolation</param>
    /// <param name="documentId">Document identifier for organization</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Absolute file path of saved file</returns>
    Task<string> SaveAsync(Stream fileStream, string tenantId, string documentId, string fileName);

    /// <summary>
    /// Retrieve a file stream from storage
    /// </summary>
    /// <param name="filePath">Absolute path to the file</param>
    /// <returns>File stream or null if not found</returns>
    Task<Stream?> GetAsync(string filePath);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="filePath">Absolute path to the file</param>
    Task DeleteAsync(string filePath);

    /// <summary>
    /// Check if a file exists at the given path
    /// </summary>
    /// <param name="filePath">Absolute path to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    /// <param name="filePath">Absolute path to the file</param>
    /// <returns>File size in bytes or 0 if not found</returns>
    long GetFileSize(string filePath);
}
