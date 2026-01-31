# Codebase Summary

**Date:** 2026-01-31
**Status:** Phase 3 Complete (Delayed Jobs & Job Continuations)

## Overview
TenantDoc is a multi-tenant document processing engine built with .NET 8 and Hangfire. It demonstrates advanced background job processing patterns including fire-and-forget, delayed, recurring, continuation, and batch jobs through a complete document processing pipeline.

## Current Progress: Phase 3
- **Project Structure:** 3-project clean architecture (Api, Core, Infrastructure).
- **Hangfire Integration:** Configured with In-Memory storage, Dashboard, and job scheduling.
- **Domain Models:** Complete document lifecycle with statuses: Uploaded → Validating → OcrPending → OcrProcessing → Ready.
- **Background Jobs:** Fire-and-forget (Validation), Delayed (OCR), Continuation (Thumbnail).
- **Job Pipeline:** Full end-to-end processing: Upload → Validation → (30s delay) → OCR → Thumbnail.
- **Image Processing:** SixLabors.ImageSharp integration for thumbnail generation.
- **API Endpoints:** Document upload, status retrieval, and Hangfire dashboard access.

## Project Structure
- `src/TenantDoc.Api/`: ASP.NET Core Web API project.
  - `Filters/`: Custom Hangfire dashboard filters (`LocalhostAuthorizationFilter`).
  - `Jobs/`: Background job implementations (`ValidationJob`, `OcrJob`, `ThumbnailJob`).
  - `Program.cs`: Hangfire configuration, service registration, endpoint mapping.
  - `Controllers/`: DocumentsController for upload/status endpoints.

- `src/TenantDoc.Core/`: Core domain logic and abstractions.
  - `Interfaces/`: Service contracts (`IDocumentStore`, `IFileStorageService`, `IOcrService`, `IThumbnailService`).
  - `Models/`: Domain entities (`Document`, `DocumentStatus`, `Tenant`, `ProcessingResult`).
  - `Exceptions/`: Custom exceptions (`OcrException`).

- `src/TenantDoc.Infrastructure/`: External service implementations.
  - `Storage/`: Storage services (`InMemoryDocumentStore`, `LocalFileStorageService`).
  - `OCR/`: OCR implementations (`MockOcrService`).
  - `Thumbnail/`: Image processing (`ImageSharpThumbnailService`).

## Key Components

### Core Models
- **Document:** Represents a document in the system with properties for ID, TenantId, FileName, Status, and processing results (OCR text, thumbnail path).
- **DocumentStatus:** Enum tracking the lifecycle: `Uploaded` → `Validating` → `OcrPending` → `OcrProcessing` → `Ready`.
- **Tenant:** Represents a system tenant with ID, Name, and Tier (Standard/VIP).

### Background Jobs
- **ValidationJob** (`src/TenantDoc.Api/Jobs/ValidationJob.cs`): Fire-and-forget job that validates documents.
  - Checks file existence and size consistency
  - Performs mock virus scan (1-3s delay)
  - 90% success rate (simulated validation)
  - Schedules OcrJob with 30-second delay on success
  - Captures OcrJob ID for continuation scheduling

- **OcrJob** (`src/TenantDoc.Api/Jobs/OcrJob.cs`): Delayed job that processes OCR.
  - Scheduled by ValidationJob with 30-second delay
  - Uses IOcrService to extract text (MockOcrService currently)
  - Updates document with extracted text
  - Sets status to Ready on success or OcrFailed on error
  - Records ProcessedAt timestamp

- **ThumbnailJob** (`src/TenantDoc.Api/Jobs/ThumbnailJob.cs`): Continuation job for image processing.
  - Runs as continuation of OcrJob (only if OcrJob succeeds)
  - Generates 200x200 thumbnails with aspect ratio preservation
  - Uses IThumbnailService (ImageSharpThumbnailService)
  - Stores thumbnail path in document record

### Infrastructure Services
- **InMemoryDocumentStore:** Thread-safe in-memory document repository using `ConcurrentDictionary`.
- **LocalFileStorageService:** File system operations (save, retrieve, delete, check existence).
- **MockOcrService:** Simulated OCR text extraction (90% success rate).
- **ImageSharpThumbnailService:** Image resizing and thumbnail generation using SixLabors.ImageSharp.

### Core Models
- **Document:** Represents a document with ID, TenantId, FileName, FilePath, Status, OcrText, ThumbnailPath, FileSize, ProcessedAt.
- **DocumentStatus:** Enum: Uploaded → Validating → ValidationFailed, OcrPending → OcrProcessing → OcrFailed, Ready.
- **Tenant:** Represents a tenant with ID, Name, and Tier (Standard/VIP).
- **ProcessingResult:** Captures extraction results (success flag, output, error message).

### API Endpoints
- `POST /api/documents/upload`: Accept document, store file, enqueue ValidationJob.
- `GET /api/documents/{id}`: Retrieve document with current processing status.
- `/hangfire`: Dashboard (localhost only) for job monitoring.

## Dependencies
- **Hangfire.InMemory** 2.0.18: In-memory job storage and background processing.
- **SixLabors.ImageSharp** 3.1.12: Image processing library for thumbnail generation.

## Next Steps
- **Phase 4:** Implement queue prioritization (VIP vs Standard) and recurring jobs (cleanup, reporting).
- **Phase 5:** Batch processing for bulk uploads.
- **Phase 6:** Advanced error handling, custom filters, retry policies.
