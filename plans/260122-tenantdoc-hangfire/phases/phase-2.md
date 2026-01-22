---
phase: 2
title: "Document Storage & Tesseract OCR Setup"
day: 2
duration: 6h
status: pending
dependencies: [1]
---

# Phase 2: Document Storage & Tesseract OCR Setup (Day 2)

**Duration:** 6 hours  
**Goal:** Real file upload/storage working, Tesseract OCR integrated (or mocked with swap plan)

## Tasks

### 2.1 Local File Storage Service
**Duration:** 1.5h  
**Dependencies:** Phase 1 complete

**Actions:**
- Create `Infrastructure/FileStorage/LocalFileStorageService.cs`
- Interface: `IFileStorageService` with methods:
  - `Task<string> SaveAsync(Stream fileStream, string tenantId, string fileName)`
  - `Task<Stream> GetAsync(string filePath)`
  - `Task DeleteAsync(string filePath)`
- Storage structure: `wwwroot/uploads/{tenantId}/{documentId}/{fileName}`
- Register in DI container

**Acceptance Criteria:**
- ✅ Files saved to tenant-isolated directories
- ✅ File retrieval works (verify with test file)
- ✅ Directory created automatically if missing
- ✅ Service registered as scoped in DI

**Code Example:**
```csharp
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _basePath = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream fileStream, string tenantId, string fileName)
    {
        var tenantDir = Path.Combine(_basePath, tenantId);
        Directory.CreateDirectory(tenantDir);
        
        var filePath = Path.Combine(tenantDir, fileName);
        using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs);
        
        return filePath;
    }
}
```

---

### 2.2 Real File Upload Endpoint
**Duration:** 1h  
**Dependencies:** 2.1

**Actions:**
- Update `/api/documents/upload` to accept `IFormFile`
- Validate file type (PDF, PNG, JPG) and size (<10MB)
- Save file using `IFileStorageService`
- Store document metadata in in-memory collection (`ConcurrentDictionary<Guid, Document>`)
- Enqueue ValidationJob with actual file path

**Acceptance Criteria:**
- ✅ Upload 5MB PDF file successfully
- ✅ File saved to correct tenant directory
- ✅ Invalid file types rejected (e.g., .exe)
- ✅ Files >10MB rejected with 400 Bad Request
- ✅ Document metadata retrievable by ID

**Code Example:**
```csharp
app.MapPost("/api/documents/upload", async (
    string tenantId,
    IFormFile file,
    IFileStorageService storage,
    IBackgroundJobClient jobClient) =>
{
    // Validate file
    var allowedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
    if (!allowedTypes.Contains(file.ContentType))
        return Results.BadRequest("Invalid file type");
    
    if (file.Length > 10 * 1024 * 1024)
        return Results.BadRequest("File too large (max 10MB)");
    
    var document = new Document
    {
        TenantId = tenantId,
        FileName = file.FileName,
        Status = DocumentStatus.Uploaded
    };
    
    // Save file
    using var stream = file.OpenReadStream();
    var filePath = await storage.SaveAsync(stream, tenantId, document.Id.ToString());
    
    // Store metadata
    DocumentStore.Documents[document.Id] = document;
    
    // Enqueue validation
    jobClient.Enqueue<ValidationJob>(x => x.ValidateDocument(document.Id));
    
    return Results.Ok(new { documentId = document.Id });
});
```

---

### 2.3 Tesseract OCR Integration (Primary Path)
**Duration:** 2.5h  
**Dependencies:** 2.1

**Actions:**
- Install NuGet: `Tesseract` (wrapper for Tesseract OCR)
- Download tessdata files (English language data)
- Create `Infrastructure/OCR/TesseractOcrService.cs`:
  - Method: `Task<string> ExtractTextAsync(string imagePath)`
  - Initialize TesseractEngine with tessdata path
  - Handle common errors (corrupted files, unsupported formats)
- Create interface `IOcrService`
- Test with sample image files (PNG, JPG, PDF)

**Acceptance Criteria:**
- ✅ OCR extracts text from PNG/JPG images
- ✅ Handles PDF files (convert to image first if needed)
- ✅ Returns empty string for non-text images (gracefully)
- ✅ Throws `OcrException` for corrupted files
- ✅ Service registered in DI

**Code Example:**
```csharp
public class TesseractOcrService : IOcrService
{
    private readonly string _tessdataPath;

    public TesseractOcrService(IConfiguration config)
    {
        _tessdataPath = config["Tesseract:DataPath"] ?? "./tessdata";
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        try
        {
            using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            
            var text = page.GetText();
            return await Task.FromResult(text);
        }
        catch (Exception ex)
        {
            throw new OcrException($"OCR failed for {imagePath}", ex);
        }
    }
}
```

**Fallback Path (if Tesseract setup blocked):**
Create mock OCR service:
```csharp
public class MockOcrService : IOcrService
{
    public async Task<string> ExtractTextAsync(string imagePath)
    {
        await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate processing
        return $"Mock OCR text from {Path.GetFileName(imagePath)} - Lorem ipsum dolor sit amet...";
    }
}
```
Register mock in DI, document swap process for later.

---

### 2.4 Update ValidationJob with Real Logic
**Duration:** 1h  
**Dependencies:** 2.2

**Actions:**
- Inject `IFileStorageService` into ValidationJob
- Verify file exists on disk
- Check file size matches metadata
- Mock virus scan (random delay 1-3s)
- Update document status in store
- Log validation results

**Acceptance Criteria:**
- ✅ ValidationJob retrieves file from storage
- ✅ Job fails gracefully if file missing (update status to ValidationFailed)
- ✅ Successful validation updates status to OcrPending
- ✅ Validation duration logged (visible in dashboard)

---

## Phase 2 Success Metrics

- ✅ Real file uploads working (PDF/PNG/JPG)
- ✅ Files stored in tenant-isolated directories
- ✅ Tesseract OCR integrated (or mock service ready)
- ✅ ValidationJob processes actual files
- ✅ OCR service tested with sample images

## Risks & Mitigations

**Risk:** Tesseract native binaries missing/incompatible  
**Mitigation:** Use NuGet package with bundled binaries; fallback to mock OCR service; document swap process

**Risk:** Large file uploads slow down API  
**Mitigation:** Acceptable for learning project; document streaming upload pattern for production

---

**Navigation:**
- [← Previous Phase: Foundation & Project Setup](phase-1.md)
- [Back to Plan Overview](../plan.md)
- [Next Phase: Delayed Jobs & Continuations →](phase-3.md)
