# Test script for batch upload functionality
# Creates test files and uploads them to the API

param(
    [int]$FileCount = 10,
    [string]$TenantId = "tenant-standard-1",
    [int]$Port = 5000
)

Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Batch Upload Test Script" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Create test directory
$testDir = "test-files"
if (Test-Path $testDir) {
    Remove-Item $testDir -Recurse -Force
}
New-Item -ItemType Directory -Path $testDir -Force | Out-Null

Write-Host "Creating $FileCount test files..." -ForegroundColor Yellow

# Create test files
for ($i = 1; $i -le $FileCount; $i++) {
    $fileName = "test-document-$i.txt"
    $filePath = Join-Path $testDir $fileName
    $content = @"
Test Document $i
Generated: $(Get-Date)
Tenant: $TenantId
File Size: ~500 bytes

This is a test document for batch upload testing.
Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
Ut enim ad minim veniam, quis nostrud exercitation ullamco.
"@
    $content | Out-File -FilePath $filePath -Encoding UTF8
    Write-Host "  ✓ Created: $fileName" -ForegroundColor Green
}

Write-Host ""
Write-Host "Files created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start the API:" -ForegroundColor Yellow
Write-Host "   dotnet run --project src/TenantDoc.Api" -ForegroundColor White
Write-Host ""
Write-Host "2. Open Swagger UI:" -ForegroundColor Yellow
Write-Host "   http://localhost:$Port/swagger" -ForegroundColor White
Write-Host ""
Write-Host "3. Open Hangfire Dashboard:" -ForegroundColor Yellow
Write-Host "   http://localhost:$Port/hangfire" -ForegroundColor White
Write-Host ""
Write-Host "4. Use POST /api/documents/bulk-upload:" -ForegroundColor Yellow
Write-Host "   - tenantId: $TenantId" -ForegroundColor White
Write-Host "   - files: Select all files from '$testDir' folder" -ForegroundColor White
Write-Host ""
Write-Host "5. Monitor progress:" -ForegroundColor Yellow
Write-Host "   GET /api/batches/{batchId}" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Test Scenarios:" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Basic Test (Standard Queue):" -ForegroundColor Yellow
Write-Host "  .\test-batch-upload.ps1 -FileCount 10 -TenantId 'tenant-standard-1'" -ForegroundColor White
Write-Host ""
Write-Host "VIP Test (Critical Queue):" -ForegroundColor Yellow
Write-Host "  .\test-batch-upload.ps1 -FileCount 10 -TenantId 'tenant-vip-1'" -ForegroundColor White
Write-Host ""
Write-Host "Large Batch Test:" -ForegroundColor Yellow
Write-Host "  .\test-batch-upload.ps1 -FileCount 100 -TenantId 'tenant-standard-1'" -ForegroundColor White
Write-Host ""
Write-Host "Test files are in: $testDir" -ForegroundColor Green
Write-Host ""
