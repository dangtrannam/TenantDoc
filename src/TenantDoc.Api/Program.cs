using Hangfire;
using Hangfire.InMemory;
using TenantDoc.Api.Filters;
using TenantDoc.Api.Jobs;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;
using TenantDoc.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalhostAuthorizationFilter() }
});

// Document upload endpoint
app.MapPost("/api/documents/upload", (string tenantId, string fileName, IBackgroundJobClient jobClient, IDocumentStore store) =>
{
    var document = new Document
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        FileName = fileName,
        Status = DocumentStatus.Uploaded,
        UploadedAt = DateTime.UtcNow
    };
    
    store.Add(document);
    
    var jobId = jobClient.Enqueue<ValidationJob>(x => x.ValidateDocument(document.Id));
    
    return Results.Ok(new { documentId = document.Id, jobId });
})
.WithName("UploadDocument")
.WithOpenApi();

// Get document status endpoint
app.MapGet("/api/documents/{id}", (Guid id, IDocumentStore store) =>
{
    var document = store.Get(id);
    if (document == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(document);
})
.WithName("GetDocument")
.WithOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
