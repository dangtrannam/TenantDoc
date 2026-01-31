namespace TenantDoc.Core.Interfaces;

public interface IThumbnailService
{
    /// <summary>
    /// Generates a thumbnail from an image file
    /// </summary>
    /// <param name="imagePath">Path to source image</param>
    /// <param name="width">Maximum thumbnail width</param>
    /// <param name="height">Maximum thumbnail height</param>
    /// <returns>Path to generated thumbnail file</returns>
    Task<string> GenerateThumbnailAsync(string imagePath, int width, int height);
}
