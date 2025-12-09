namespace MiddlewareInfocert.Models;

public sealed class SignAutoPadesResponse //risposta a call da docsmrashal
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? SignedObjectId { get; set; }
    public string? SignedFileName { get; set; }
    public string? Sha256 { get; set; }
    public string? ProxysignRef { get; set; }
}
