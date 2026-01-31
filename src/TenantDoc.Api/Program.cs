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

builder.Services.AddHangfireServer();

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

app.Run();