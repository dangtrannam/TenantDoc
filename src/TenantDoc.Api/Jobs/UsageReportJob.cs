using System.Collections.Concurrent;
using System.Text.Json;
using Hangfire;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Api.Jobs;

/// <summary>
/// Recurring job that generates hourly usage reports for all tenants.
/// Runs every hour in the batch queue.
/// </summary>
[Queue("batch")]
public class UsageReportJob(IDocumentStore store)
{
    private readonly IDocumentStore _store = store;

    // In-memory storage for usage reports (for later API retrieval)
    public static readonly ConcurrentDictionary<DateTime, List<TenantUsageReport>> Reports = new();

    public Task GenerateHourlyReport()
    {
        var reportTime = DateTime.UtcNow;
        Console.WriteLine($"[UsageReportJob] Generating hourly usage report at {reportTime:yyyy-MM-dd HH:mm:ss} UTC");

        try
        {
            var allDocuments = _store.GetAll();

            if (!allDocuments.Any())
            {
                Console.WriteLine("[UsageReportJob] No documents found in the system");
                return Task.CompletedTask;
            }

            // Aggregate metrics per tenant
            var tenantReports = allDocuments
                .GroupBy(d => d.TenantId)
                .Select(g => new TenantUsageReport
                {
                    TenantId = g.Key,
                    DocumentCount = g.Count(),
                    TotalOcrCharacters = g.Sum(d => d.OcrText?.Length ?? 0),
                    TotalFileSize = g.Sum(d => d.FileSize),
                    SuccessRate = g.Any() ? (double)g.Count(d => d.Status == DocumentStatus.Ready) / g.Count() : 0,
                    StatusBreakdown = new Dictionary<DocumentStatus, int>
                    {
                        [DocumentStatus.Uploaded] = g.Count(d => d.Status == DocumentStatus.Uploaded),
                        [DocumentStatus.Validating] = g.Count(d => d.Status == DocumentStatus.Validating),
                        [DocumentStatus.ValidationFailed] = g.Count(d => d.Status == DocumentStatus.ValidationFailed),
                        [DocumentStatus.OcrPending] = g.Count(d => d.Status == DocumentStatus.OcrPending),
                        [DocumentStatus.OcrProcessing] = g.Count(d => d.Status == DocumentStatus.OcrProcessing),
                        [DocumentStatus.OcrFailed] = g.Count(d => d.Status == DocumentStatus.OcrFailed),
                        [DocumentStatus.Ready] = g.Count(d => d.Status == DocumentStatus.Ready)
                    },
                    AverageProcessingTime = CalculateAverageProcessingTime([.. g])
                })
                .OrderByDescending(r => r.DocumentCount)
                .ToList();

            // Store report in memory
            Reports[reportTime] = tenantReports;

            // Keep only last 24 hours of reports (24 hourly reports)
            var oldReports = Reports.Keys.Where(k => k < reportTime.AddHours(-24)).ToList();
            foreach (var oldReport in oldReports)
            {
                Reports.TryRemove(oldReport, out _);
            }

            // Log report to console
            var json = JsonSerializer.Serialize(new
            {
                ReportTime = reportTime,
                TotalTenants = tenantReports.Count,
                TotalDocuments = allDocuments.Count(),
                Tenants = tenantReports
            }, new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine($"[UsageReportJob] Hourly usage report:\n{json}");
            Console.WriteLine($"[UsageReportJob] Report stored in memory. Total reports in storage: {Reports.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UsageReportJob] Report generation failed: {ex.Message}");
            throw;
        }

        return Task.CompletedTask;
    }

    private static double CalculateAverageProcessingTime(List<Document> documents)
    {
        var processedDocuments = documents
            .Where(d => d.ProcessedAt.HasValue && d.Status == DocumentStatus.Ready)
            .ToList();

        if (!processedDocuments.Any())
        {
            return 0;
        }

        var totalSeconds = processedDocuments
            .Sum(d => (d.ProcessedAt!.Value - d.UploadedAt).TotalSeconds);

        return totalSeconds / processedDocuments.Count;
    }
}

/// <summary>
/// Tenant usage statistics for a specific reporting period.
/// </summary>
public class TenantUsageReport
{
    public string TenantId { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int TotalOcrCharacters { get; set; }
    public long TotalFileSize { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<DocumentStatus, int> StatusBreakdown { get; set; } = new();
    public double AverageProcessingTime { get; set; }
}
