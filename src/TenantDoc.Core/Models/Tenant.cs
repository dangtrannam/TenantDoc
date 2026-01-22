namespace TenantDoc.Core.Models;

public class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TenantTier Tier { get; set; } = TenantTier.Standard;
}

public enum TenantTier
{
    Standard,
    VIP
}
