namespace MiddlewareInfocert.Models;

// ---- GET ----
public sealed class DmGetByExternalIdRequest
{
    public string sessionID { get; set; } = string.Empty;
    public string objectID { get; set; } = string.Empty;
    public string fieldExternalId { get; set; } = string.Empty;
}

public sealed class DmGetByExternalIdResponse
{
    public DmGetByExternalIdResult result { get; set; } = new();
}

public sealed class DmGetByExternalIdResult
{
    public bool HasError { get; set; }
    public string Error { get; set; } = string.Empty;
    public DmGetDocumentMinimal? Document { get; set; }
}

public sealed class DmGetDocumentMinimal
{
    public string? FileName { get; set; }
    public string? FileBase64Content { get; set; }
}

// ---- SET ----
public sealed class DmSetProfileDocumentRequest
{
    public string sessionID { get; set; } = string.Empty;
    public string objectId { get; set; } = string.Empty;
    public string fileName { get; set; } = string.Empty;
    public string fileContentBase64 { get; set; } = string.Empty;
    public string? fieldExternalId { get; set; }
    public bool raiseWorkflowEvents { get; set; } = false;
}

public sealed class DmSetProfileDocumentResponse
{
    public bool HasError { get; set; }
    public string Error { get; set; } = string.Empty;
}
