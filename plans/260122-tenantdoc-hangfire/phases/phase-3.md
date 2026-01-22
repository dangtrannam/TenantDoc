---
phase: 3
title: "Delayed Jobs & Job Continuations"
day: 3
duration: 6h
status: pending
dependencies: [2]
---

# Phase 3: Delayed Jobs & Job Continuations (Day 3)

**Duration:** 6 hours  
**Goal:** Delayed OCR job scheduling, continuation-based thumbnail generation, end-to-end pipeline working

## Tasks

### 3.1 OcrJob with Delayed Execution
**Duration:** 2h  
**Dependencies:** Phase 2 complete

**Actions:**
- Create `Jobs/OcrJob.cs`:
  - Method: `Task ProcessOcr(Guid documentId)`
  - Inject `IOcrService`, `IFileStorageService`
  - Retrieve document, load file, call OCR service
  - Update document with extracted text
  - Update status to Ready
- Modify ValidationJob to schedule OcrJob:
  - Use `BackgroundJob.Schedule()` with 30-second delay
  - Pass documentId to OcrJob
- Test delay behavior in dashboard

**Acceptance Criteria:**
- ✅ ValidationJob schedules OcrJob with 30s delay
- ✅ Dashboard shows job in "Scheduled" tab with countdown
- ✅ OcrJob executes after delay (visible in "Processing" tab)
- ✅ OCR text extracted and stored in document metadata
- ✅ Job duration logged (OCR processing time)

**Code Example:**
```csharp
public class OcrJob
{
    private readonly IOcrService _ocrService;
    private readonly IFileStorageService _storage;

    public OcrJob(IOcrService ocrService, IFileStorageService storage)
    {
        _ocrService = ocrService;
        _storage = storage;
    }

    public async Task ProcessOcr(Guid documentId)
    {
        var document = DocumentStore.Documents[documentId];
        document.Status = DocumentStatus.OcrProcessing;
        
        var filePath = Path.Combine("wwwroot/uploads", document.TenantId, documentId.ToString(), document.FileName);
        
        var text = await _ocrService.ExtractTextAsync(filePath);
        
        document.OcrText = text;
        document.Status = DocumentStatus.Ready;
        document.ProcessedAt = DateTime.UtcNow;
        
        Console.WriteLine($"[OcrJob] Extracted {text.Length} characters from {documentId}");
    }
}

// In ValidationJob:
public async Task ValidateDocument(Guid documentId, IBackgroundJobClient jobClient)
{
    // ... validation logic ...
    
    if (isValid)
    {
        document.Status = DocumentStatus.OcrPending;
        
        // Schedule OCR job with 30s delay
        jobClient.Schedule<OcrJob>(
            x => x.ProcessOcr(documentId),
            TimeSpan.FromSeconds(30));
    }
}
```

---

### 3.2 Thumbnail Generation Service
**Duration:** 1.5h  
**Dependencies:** Phase 2 complete

**Actions:**
- Install NuGet: `SixLabors.ImageSharp`
- Create `Infrastructure/Thumbnail/ImageSharpThumbnailService.cs`:
  - Method: `Task<string> GenerateThumbnailAsync(string imagePath, int width, int height)`
  - Resize image to 200x200, maintain aspect ratio
  - Save thumbnail with `-thumb` suffix
  - Return thumbnail path
- Create interface `IThumbnailService`
- Test with sample images

**Acceptance Criteria:**
- ✅ Thumbnail generated for PNG/JPG files
- ✅ Thumbnail maintains aspect ratio
- ✅ Output file size <50KB (check compression)
- ✅ Handles PDF files (extract first page as image)
- ✅ Service registered in DI

**Code Example:**
```csharp
public class ImageSharpThumbnailService : IThumbnailService
{
    public async Task<string> GenerateThumbnailAsync(string imagePath, int width, int height)
    {
        using var image = await Image.LoadAsync(imagePath);
        
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));
        
        var thumbnailPath = Path.ChangeExtension(imagePath, null) + "-thumb.jpg";
        await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 80 });
        
        return thumbnailPath;
    }
}
```

---

### 3.3 ThumbnailJob with Continuation
**Duration:** 2h  
**Dependencies:** 3.1, 3.2

**Actions:**
- Create `Jobs/ThumbnailJob.cs`:
  - Method: `Task GenerateThumbnail(Guid documentId)`
  - Inject `IThumbnailService`
  - Generate thumbnail, update document metadata
  - Log completion
- Modify OcrJob to use `ContinueJobWith()`:
  - Schedule ThumbnailJob as continuation
  - Continuation only runs if OcrJob succeeds
- Test end-to-end pipeline: Upload → Validate → OCR (30s delay) → Thumbnail

**Acceptance Criteria:**
- ✅ ThumbnailJob only runs after OcrJob completes
- ✅ Dashboard shows continuation relationship (parent job link)
- ✅ Thumbnail generated and path stored
- ✅ Failed OcrJob skips ThumbnailJob (test with corrupted image)
- ✅ End-to-end pipeline completes in ~35-40s

**Code Example:**
```csharp
public class ThumbnailJob
{
    private readonly IThumbnailService _thumbnailService;

    public ThumbnailJob(IThumbnailService thumbnailService)
    {
        _thumbnailService = thumbnailService;
    }

    public async Task GenerateThumbnail(Guid documentId)
    {
        var document = DocumentStore.Documents[documentId];
        
        var filePath = Path.Combine("wwwroot/uploads", document.TenantId, documentId.ToString(), document.FileName);
        var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(filePath, 200, 200);
        
        document.ThumbnailPath = thumbnailPath;
        
        Console.WriteLine($"[ThumbnailJob] Generated thumbnail for {documentId}");
    }
}

// In OcrJob:
public async Task ProcessOcr(Guid documentId, IBackgroundJobClient jobClient)
{
    // ... OCR logic ...
    
    // Schedule continuation
    jobClient.ContinueJobWith<ThumbnailJob>(
        jobId, // current job ID (passed via PerformContext)
        x => x.GenerateThumbnail(documentId));
}
```

---

### 3.4 Pipeline Testing & Verification
**Duration:** 30min  
**Dependencies:** 3.3

**Actions:**
- Upload 5 test documents (mix of PDF/PNG/JPG)
- Monitor dashboard for pipeline execution
- Verify all jobs complete successfully
- Check output: OCR text extracted, thumbnails generated
- Test failure scenario: upload corrupted image, verify pipeline stops at OcrJob

**Acceptance Criteria:**
- ✅ 5 documents processed end-to-end
- ✅ All jobs visible in dashboard with correct statuses
- ✅ Corrupted image fails OcrJob, skips ThumbnailJob
- ✅ Total pipeline time: 35-40s per document

---

## Phase 3 Success Metrics

- ✅ Delayed jobs working (30s delay observable)
- ✅ Job continuations working (ThumbnailJob follows OcrJob)
- ✅ End-to-end pipeline functional
- ✅ Dashboard shows job relationships
- ✅ Failure scenarios handled gracefully

## Risks & Mitigations

**Risk:** Continuation jobs not triggering  
**Mitigation:** Verify Hangfire version supports `ContinueJobWith()`; check documentation for correct API usage

**Risk:** Thumbnail generation fails for PDFs  
**Mitigation:** Use ImageSharp PDF support or fallback to placeholder thumbnail; document limitation

---

**Navigation:**
- [← Previous Phase: Document Storage & Tesseract OCR](phase-2.md)
- [Back to Plan Overview](../plan.md)
- [Next Phase: Queue System & Recurring Jobs →](phase-4.md)
