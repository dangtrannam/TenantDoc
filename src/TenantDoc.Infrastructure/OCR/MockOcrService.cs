using TenantDoc.Core.Interfaces;

namespace TenantDoc.Infrastructure.OCR;

/// <summary>
/// Mock OCR service for testing without Tesseract dependencies
/// Simulates OCR processing with delay and returns mock text
/// </summary>
public class MockOcrService : IOcrService
{
    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // Simulate OCR processing time
        await Task.Delay(TimeSpan.FromSeconds(2));

        var fileName = Path.GetFileName(imagePath);
        var mockText = $"""
            Mock OCR Result for: {fileName}

            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            This is simulated text extraction from the document.
            Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

            Invoice Number: INV-{Random.Shared.Next(1000, 9999)}
            Total Amount: ${Random.Shared.Next(100, 5000)}.00

            Thank you for your business.
            """;

        return mockText;
    }
}
