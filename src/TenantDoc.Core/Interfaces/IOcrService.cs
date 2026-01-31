namespace TenantDoc.Core.Interfaces;

/// <summary>
/// Service for extracting text from document images using OCR
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extract text from an image file
    /// </summary>
    /// <param name="imagePath">Absolute path to the image file</param>
    /// <returns>Extracted text content, or empty string if no text found</returns>
    /// <exception cref="OcrException">Thrown when OCR processing fails</exception>
    Task<string> ExtractTextAsync(string imagePath);
}
