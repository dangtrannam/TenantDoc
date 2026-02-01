# TenantDoc: Comprehensive Hangfire Learning Project

TenantDoc is a production-grade multi-tenant document processing engine designed to demonstrate and master advanced Hangfire features through real-world patterns.

## 🚀 Overview

The project simulates a document processing pipeline where documents are uploaded by different tenants (Standard vs. VIP) and processed through various stages: Validation, OCR, and Thumbnail generation. It covers a wide range of background job patterns:

- **Fire-and-forget:** Document validation.
- **Delayed:** Asynchronous OCR text extraction.
- **Continuation:** Thumbnail generation triggered after OCR completion.
- **Batch Processing:** Bulk uploads processed atomically.
- **Recurring Jobs:** Daily cleanup and hourly reporting.

## 🛠️ Tech Stack

- **Runtime:** .NET 8
- **Framework:** ASP.NET Core Minimal APIs
- **Background Jobs:** [Hangfire](https://www.hangfire.io/) (using InMemory storage for development)
- **Image Processing:** [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
- **Architecture:** Clean Architecture (Core, Infrastructure, Api)

## 🏗️ Architecture

The project follows a 3-project Clean Architecture structure:

- **TenantDoc.Core:** Domain models, enums, and service abstractions.
- **TenantDoc.Infrastructure:** Concrete implementations of storage and external services.
- **TenantDoc.Api:** Web API endpoints, Hangfire job implementations, and configuration.

### Processing Pipeline
```
Upload → ValidationJob (Fire-Forget) → OcrJob (Delayed) → ThumbnailJob (Continuation)
```

## 📅 Roadmap

The project is structured into 7 implementation phases:

- [x] **Phase 1: Foundation & Project Setup** - Basic solution structure, Hangfire integration, and first job.
- [x] **Phase 2: Document Storage & Tesseract OCR** - Real file handling and OCR integration.
- [x] **Phase 3: Delayed Jobs & Continuations** - Pipeline orchestration.
- [x] **Phase 4: Queue System & Recurring Jobs** - Multi-tenant queue prioritization (VIP vs Standard).
- [x] **Phase 5: Batch Processing** - Handling bulk uploads with custom batch tracking.
- [ ] **Phase 6: Advanced Error Handling & Filters** - Retries, circuit breakers, and custom filters.
- [ ] **Phase 7: Production Readiness** - Graceful shutdown, performance testing, and persistence.

## 🚦 Getting Started

### Prerequisites
- .NET 8 SDK
- IDE (Visual Studio 2022, VS Code, or Rider)

### Installation & Run
1. Clone the repository.
2. Navigate to the root directory.
3. Run the project:
   ```bash
   dotnet run --project src/TenantDoc.Api
   ```
4. Access the **Hangfire Dashboard** at: `http://localhost:<port>/hangfire`
5. Access the **Swagger UI** at: `http://localhost:<port>/swagger`

## 🧪 Learning Goals
- Master Hangfire job types and lifecycles.
- Implement multi-tenant job isolation and prioritization.
- Build resilient background processing with custom filters and retry policies.
- Understand batch processing and job continuations.

---
*Created as part of the Hangfire Advanced Learning Path.*
