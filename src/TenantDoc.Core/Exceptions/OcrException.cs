namespace TenantDoc.Core.Exceptions;

/// <summary>
/// Exception thrown when OCR processing fails
/// </summary>
public class OcrException : Exception
{
    public OcrException(string message) : base(message)
    {
    }

    public OcrException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
