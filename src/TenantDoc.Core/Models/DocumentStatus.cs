namespace TenantDoc.Core.Models;

/// <summary>
/// Represents the lifecycle of a document through the processing pipeline.
/// Status transitions: Uploaded → Validating → (ValidationFailed | OcrPending) 
///                    → OcrProcessing → (OcrFailed | Ready)
/// </summary>
public enum DocumentStatus
{
    Uploaded,
    Validating,
    ValidationFailed,
    OcrPending,
    OcrProcessing,
    OcrFailed,
    Ready
}
