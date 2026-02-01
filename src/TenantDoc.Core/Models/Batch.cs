namespace TenantDoc.Core.Models;

public class Batch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public List<Guid> DocumentIds { get; set; } = new();
    public BatchStatus Status { get; set; } = BatchStatus.Processing;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
