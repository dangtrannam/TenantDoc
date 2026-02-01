// Test script to verify Phase 4 implementation

// Test 1: Verify queue configuration in Program.cs
Console.WriteLine("=== Test 1: Queue Configuration ===");

var programContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Program.cs");

if (programContent.Contains("options.Queues = new[] { \"critical\", \"default\", \"batch\" }"))
{
    Console.WriteLine("✅ 3-tier queue configuration found");
}
else
{
    Console.WriteLine("❌ Queue configuration not found");
}

if (programContent.Contains("options.WorkerCount = 7"))
{
    Console.WriteLine("✅ Worker count set to 7");
}
else
{
    Console.WriteLine("❌ Worker count not configured");
}

// Test 2: Verify recurring jobs registration
Console.WriteLine("\n=== Test 2: Recurring Jobs Registration ===");

if (programContent.Contains("RecurringJob.AddOrUpdate<CleanupJob>"))
{
    Console.WriteLine("✅ CleanupJob recurring job registered");
}
else
{
    Console.WriteLine("❌ CleanupJob not registered");
}

if (programContent.Contains("RecurringJob.AddOrUpdate<UsageReportJob>"))
{
    Console.WriteLine("✅ UsageReportJob recurring job registered");
}
else
{
    Console.WriteLine("❌ UsageReportJob not registered");
}

if (programContent.Contains("0 2 * * *"))
{
    Console.WriteLine("✅ CleanupJob scheduled daily at 2 AM");
}
else
{
    Console.WriteLine("❌ CleanupJob cron schedule not found");
}

if (programContent.Contains("0 * * * *"))
{
    Console.WriteLine("✅ UsageReportJob scheduled hourly");
}
else
{
    Console.WriteLine("❌ UsageReportJob cron schedule not found");
}

// Test 3: Verify job queue assignments
Console.WriteLine("\n=== Test 3: Job Queue Assignments ===");

var validationJobContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Jobs/ValidationJob.cs");
if (validationJobContent.Contains("[Queue(\"default\")]"))
{
    Console.WriteLine("✅ ValidationJob assigned to default queue");
}
else
{
    Console.WriteLine("❌ ValidationJob queue not assigned");
}

var thumbnailJobContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Jobs/ThumbnailJob.cs");
if (thumbnailJobContent.Contains("[Queue(\"default\")]"))
{
    Console.WriteLine("✅ ThumbnailJob assigned to default queue");
}
else
{
    Console.WriteLine("❌ ThumbnailJob queue not assigned");
}

var cleanupJobContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Jobs/recurring-cleanup-job.cs");
if (cleanupJobContent.Contains("[Queue(\"batch\")]"))
{
    Console.WriteLine("✅ CleanupJob assigned to batch queue");
}
else
{
    Console.WriteLine("❌ CleanupJob queue not assigned");
}

var usageReportJobContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Jobs/recurring-usage-report-job.cs");
if (usageReportJobContent.Contains("[Queue(\"batch\")]"))
{
    Console.WriteLine("✅ UsageReportJob assigned to batch queue");
}
else
{
    Console.WriteLine("❌ UsageReportJob queue not assigned");
}

// Test 4: Verify tenant-based queue routing
Console.WriteLine("\n=== Test 4: Tenant-Based Queue Routing ===");

var tenantStoreContent = System.IO.File.ReadAllText("src/TenantDoc.Api/Stores/in-memory-tenant-store.cs");

if (tenantStoreContent.Contains("public static string GetQueueForTenant"))
{
    Console.WriteLine("✅ GetQueueForTenant method exists");
}
else
{
    Console.WriteLine("❌ GetQueueForTenant method not found");
}

if (tenantStoreContent.Contains("TenantTier.VIP ? \"critical\" : \"default\""))
{
    Console.WriteLine("✅ VIP tenants routed to critical queue");
}
else
{
    Console.WriteLine("❌ Queue routing logic not implemented");
}

if (tenantStoreContent.Contains("\"tenant-vip-1\"") && tenantStoreContent.Contains("\"tenant-std-1\""))
{
    Console.WriteLine("✅ Test tenants configured (VIP and Standard)");
}
else
{
    Console.WriteLine("❌ Test tenants not configured");
}

// Test 5: Verify TenantTier enum
Console.WriteLine("\n=== Test 5: TenantTier Enum ===");

var tenantModelContent = System.IO.File.ReadAllText("src/TenantDoc.Core/Models/Tenant.cs");

if (tenantModelContent.Contains("enum TenantTier"))
{
    Console.WriteLine("✅ TenantTier enum defined");
}
else
{
    Console.WriteLine("❌ TenantTier enum not found");
}

if (tenantModelContent.Contains("Standard") && tenantModelContent.Contains("VIP"))
{
    Console.WriteLine("✅ TenantTier has Standard and VIP values");
}
else
{
    Console.WriteLine("❌ TenantTier values not properly defined");
}

// Test 6: Verify dynamic queue assignment in ValidationJob
Console.WriteLine("\n=== Test 6: Dynamic Queue Assignment ===");

if (validationJobContent.Contains("TenantStore.GetQueueForTenant"))
{
    Console.WriteLine("✅ ValidationJob uses TenantStore.GetQueueForTenant");
}
else
{
    Console.WriteLine("❌ Dynamic queue assignment not found");
}

if (validationJobContent.Contains("new EnqueuedState(queueName)"))
{
    Console.WriteLine("✅ Queue name passed to EnqueuedState");
}
else
{
    Console.WriteLine("❌ EnqueuedState not configured with queue");
}

Console.WriteLine("\n=== Test Complete ===");
