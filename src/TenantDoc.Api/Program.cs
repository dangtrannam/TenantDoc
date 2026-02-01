using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Mvc;
using TenantDoc.Api.Filters;
using TenantDoc.Api.Jobs;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;
using TenantDoc.Infrastructure.OCR;
using TenantDoc.Infrastructure.Storage;
using TenantDoc.Infrastructure.Thumbnail;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOcrService, MockOcrService>();
builder.Services.AddScoped<IThumbnailService, ImageSharpThumbnailService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

// Configure Hangfire server with multi-tier queue system
// Workers are distributed across queues proportionally based on order
// Approximate allocation: critical=4, default=2, batch=1
builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["critical", "default", "batch"];
    options.WorkerCount = 7; // Total workers across all queues
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalhostAuthorizationFilter() }
});

// Register recurring jobs
// Note: Queue assignment is handled via [Queue("batch")] attribute on job classes
RecurringJob.AddOrUpdate<CleanupJob>(
    "daily-cleanup",
    x => x.CleanupOldDocuments(),
    "0 2 * * *", // Daily at 2 AM UTC
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });

RecurringJob.AddOrUpdate<UsageReportJob>(
    "hourly-usage-report",
    x => x.GenerateHourlyReport(),
    "0 * * * *", // Every hour at minute 0
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });

app.Run();