public sealed class SecurityOptions { public string ApiKey { get; set; } = ""; }

public sealed class DocsmarshalOptions
{
    public string BaseUrl { get; set; } = "";
    public string SessionId { get; set; } = ""; // per le chiamate IDM
    public string InputFieldExternalId { get; set; } = "";
    public string OutputFieldExternalId { get; set; } = "";
    public bool RaiseWorkflowEvents { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class ProxySignOptions
{
    public string BaseUrl { get; set; } = string.Empty; // es. https://testdev.proxysign.it
    public string AutoContext { get; set; } = "auto";   // context per firma automatica
    public string? Language { get; set; } = "it";       // opzionale: inviata come LANGUAGE
    public int TimeoutSeconds { get; set; } = 60;       // timeout HTTP
} 