# Project Overview & PDR (Product Development Requirements)

## 1. Project Overview
**Project Name:** TenantDoc  
**Objective:** Build a comprehensive Hangfire learning project that demonstrates production-grade background job patterns in a multi-tenant document processing context.

TenantDoc allows tenants to upload documents which are then processed through a pipeline:
1.  **Validation:** Instant check for file type and integrity.
2.  **OCR:** Text extraction from images/PDFs (simulated with delay).
3.  **Thumbnail Generation:** Creation of preview images.
4.  **Reporting:** Automated generation of tenant usage reports.

## 2. Technical Stack
- **Runtime:** .NET 10
- **Framework:** ASP.NET Core Minimal APIs
- **Background Jobs:** Hangfire (InMemory storage for development)
- **OCR:** Tesseract OCR (mocked initially)
- **Image Processing:** SixLabors.ImageSharp
- **Storage:** Local Filesystem (Infrastructure layer)

## 3. Functional Requirements

### 3.1 Document Processing Pipeline
- **Upload:** Users can upload documents (PDF, PNG, JPG).
- **Validation:** Automatic validation of file size (<10MB) and type.
- **OCR:** Asynchronous text extraction with 30-second simulated delay.
- **Thumbnail:** Generation of a 200x200px preview image.
- **Status Tracking:** Users can poll the status of their document processing.

### 3.2 Job Types Coverage
- **Fire-and-forget:** Document validation.
- **Delayed:** OCR text extraction.
- **Continuation:** Thumbnail generation (after OCR).
- **Batch:** Bulk uploads processed atomically.
- **Recurring:** Daily cleanup of old files and hourly reports.

### 3.3 Multi-Tenancy
- Isolation of document processing context by `TenantId`.
- Queue prioritization: VIP tenants utilize a `critical` queue, while Standard tenants use `default`.

## 4. Non-Functional Requirements
- **Reliability:** Failed jobs should automatically retry with exponential backoff (0s, 2s, 8s).
- **Observability:** Hangfire dashboard must be accessible on localhost for monitoring.
- **Maintainability:** Clean Architecture separation of concerns.
- **Graceful Shutdown:** Jobs should not be lost during application shutdown.

## 5. Success Metrics
- 100% completion of the 7-phase implementation plan.
- Observable priority differences between `critical` and `default` queues.
- Successful handling of 1000+ concurrent jobs in performance testing.

## 6. Implementation Phases (High-Level)
- **Phase 1:** Foundation & Project Setup (Completed)
- **Phase 2:** Document Storage & Tesseract OCR
- **Phase 3:** Delayed Jobs & Continuations
- **Phase 4:** Queue System & Recurring Jobs
- **Phase 5:** Batch Processing
- **Phase 6:** Advanced Error Handling & Filters
- **Phase 7:** Production Readiness & Testing
