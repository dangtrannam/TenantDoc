using System.Collections.Concurrent;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Stores;

/// <summary>
/// In-memory tenant storage for development and testing.
/// Production systems should use a database.
/// </summary>
public static class TenantStore
{
    public static readonly ConcurrentDictionary<string, Tenant> Tenants = new(new Dictionary<string, Tenant>
    {
        ["tenant-vip-1"] = new Tenant
        {
            Id = "tenant-vip-1",
            Name = "VIP Corp",
            Tier = TenantTier.VIP
        },
        ["tenant-vip-2"] = new Tenant
        {
            Id = "tenant-vip-2",
            Name = "Enterprise Solutions Inc",
            Tier = TenantTier.VIP
        },
        ["tenant-std-1"] = new Tenant
        {
            Id = "tenant-std-1",
            Name = "Standard Corp",
            Tier = TenantTier.Standard
        },
        ["tenant-std-2"] = new Tenant
        {
            Id = "tenant-std-2",
            Name = "Small Business LLC",
            Tier = TenantTier.Standard
        },
        ["tenant-std-3"] = new Tenant
        {
            Id = "tenant-std-3",
            Name = "Startup Ventures",
            Tier = TenantTier.Standard
        }
    });

    /// <summary>
    /// Gets a tenant by ID, returns null if not found.
    /// </summary>
    public static Tenant? GetTenant(string tenantId)
    {
        return Tenants.TryGetValue(tenantId, out var tenant) ? tenant : null;
    }

    /// <summary>
    /// Determines the queue name based on tenant tier.
    /// VIP tenants use the critical queue for faster processing.
    /// </summary>
    public static string GetQueueForTenant(string tenantId)
    {
        var tenant = GetTenant(tenantId);
        return tenant?.Tier == TenantTier.VIP ? "critical" : "default";
    }
}
