# Codebase Summary

**Date:** 2026-01-22  
**Status:** Phase 1 Complete (Foundation & Project Setup)

## Overview
TenantDoc is a multi-tenant document processing engine built with .NET 10 and Hangfire. It aims to demonstrate advanced background job processing patterns including fire-and-forget, delayed, recurring, continuation, and batch jobs.

## Current Progress: Phase 1
- **Project Structure:** Established a 3-project clean architecture (.Api, .Core, .Infrastructure).
- **Hangfire Integration:** Configured Hangfire with In-Memory storage and the Dashboard.
- **Domain Models:** Defined core entities like `Document`, `Tenant`, and `DocumentStatus`.
- **Background Jobs:** Implemented the first fire-and-forget job (`ValidationJob`).
- **API Endpoints:** Basic endpoints for document upload and status retrieval.

## Project Structure
- `src/TenantDoc.Api/`: ASP.NET Core Web API project.
  - `Filters/`: Custom Hangfire dashboard filters (e.g., `LocalhostAuthorizationFilter`).
  - `Jobs/`: Background job implementations (e.g., `ValidationJob`).
  - `Program.cs`: Application entry point, service configuration, and endpoint mapping.
- `src/TenantDoc.Core/`: Core domain logic and abstractions.
  - `Interfaces/`: Abstractions for storage and services (e.g., `IDocumentStore`).
  - `Models/`: Domain entities and enums (e.g., `Document`, `DocumentStatus`, `Tenant`).
- `src/TenantDoc.Infrastructure/`: External service implementations.
  - `Storage/`: Concrete storage implementations (e.g., `InMemoryDocumentStore`).

## Key Components

### Core Models
- **Document:** Represents a document in the system with properties for ID, TenantId, FileName, Status, and processing results (OCR text, thumbnail path).
- **DocumentStatus:** Enum tracking the lifecycle: `Uploaded` → `Validating` → `OcrPending` → `OcrProcessing` → `Ready`.
- **Tenant:** Represents a system tenant with ID, Name, and Tier (Standard/VIP).

### Background Jobs
- **ValidationJob:** A fire-and-forget job that simulates document validation. It updates the document status based on a mock validation result (90% success rate).

### Infrastructure
- **InMemoryDocumentStore:** A thread-safe in-memory store for documents using `ConcurrentDictionary`.

### API Endpoints
- `POST /api/documents/upload`: Enqueues a validation job for a new document.
- `GET /api/documents/{id}`: Retrieves the current state of a document.

## Next Steps
- **Phase 2:** Implement local file storage and integrate Tesseract OCR.
- **Phase 3:** Implement delayed OCR jobs and continuation thumbnail jobs.
