# Design Guidelines

## 1. API Design
- **RESTful Principles:** Use standard HTTP verbs (POST, GET, etc.).
- **Minimal APIs:** Utilize .NET Minimal APIs for concise endpoint definitions.
- **Asynchronous by Default:** All long-running operations must be offloaded to Hangfire.
- **Response Format:** Standard JSON responses with appropriate status codes (200 OK, 201 Created, 404 Not Found, etc.).

## 2. Background Job Design
- **Small & Focused:** Each job should perform one logical task (e.g., Validate, OCR, Thumbnail).
- **Idempotency:** Ensure jobs can be safely retried without side effects.
- **State Persistence:** Update the `IDocumentStore` at each significant stage of the job lifecycle.
- **Logging:** Include relevant IDs (TenantId, DocumentId) in all job logs for traceability.

## 3. Multi-Tenancy Design
- **Tenant Context:** Always propagate `TenantId` through the job metadata or parameters.
- **Isolation:** Ensure one tenant's jobs cannot access or interfere with another's data.
- **Resource Allocation:** Use different queues to prevent one tenant from monopolizing workers.

## 4. UI/Dashboard Guidelines
- **Dashboard Access:** Restricted to localhost in development via `LocalhostAuthorizationFilter`.
- **Customization:** (Future) Add custom dashboard widgets for tenant-specific metrics.
