namespace TenantDoc.Core.Models;

public class ProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
