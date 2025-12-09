using System.Net;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using MiddlewareInfocert.Security;

namespace MiddlewareInfocert.Clients;

// Eccezione specifica per risposte KO di ProxySign (o HTTP != 200)
public sealed class ProxySignException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }                // <error-code>
    public string? ErrorDescription { get; }         // <error-description>
    public string? ErrorCodeSignature { get; }       // <error-code-signature>
    public string? ProxySignErrorCode { get; }       // <proxysign-error-code>
    public string? ProxySignErrorDescription { get; }// <proxysign-error-description>

    public ProxySignException(string message, int statusCode,
        string? errorCode = null, string? errorDescription = null,
        string? errorCodeSignature = null, string? proxysignErrorCode = null, string? proxysignErrorDescription = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
        ErrorCodeSignature = errorCodeSignature;
        ProxySignErrorCode = proxysignErrorCode;
        ProxySignErrorDescription = proxysignErrorDescription;
    }
}

public sealed class ProxySignClient
{
    private readonly HttpClient _http;
    private readonly ProxySignOptions _opt;

    public ProxySignClient(HttpClient http, IOptions<ProxySignOptions> opt)
    {
        _http = http;
        _opt = opt.Value;

        _http.Timeout = TimeSpan.FromSeconds(Math.Max(5, _opt.TimeoutSeconds));
        // Volendo: accetta PDF in risposta
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.1));
    }

    /// <summary>
    /// Firma PAdES "automatica": POST /{AutoContext}/sign/pades/{alias}
    /// Invia pin + contentToSign-0 (PDF). Ritorna PDF firmato (bytes).
    /// </summary>
    public async Task<byte[]> SignPadesAutoAsync(string alias, string pin, byte[] pdfBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Alias mancante.", nameof(alias));
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN mancante.", nameof(pin));
        if (pdfBytes is null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF vuoto.", nameof(pdfBytes));

        var path = $"/{_opt.AutoContext}/sign/pades/{Uri.EscapeDataString(alias)}";

        using var form = new MultipartFormDataContent();

        // Campo obbligatorio: PIN (stringa)
        form.Add(new StringContent(pin), "pin");

        // Opzionale
        if (!string.IsNullOrWhiteSpace(_opt.Language))
            form.Add(new StringContent(_opt.Language!), "LANGUAGE");

        // PDF da firmare: contentToSign-0 (application/pdf)
        var pdfContent = new ByteArrayContent(pdfBytes);
        pdfContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(pdfContent, "contentToSign-0", "document.pdf");

        using var res = await _http.PostAsync(path, form, ct);

        if (res.StatusCode == HttpStatusCode.OK)
        {
            // 200 => corpo = PDF firmato (bytes)
            return await res.Content.ReadAsByteArrayAsync(ct);
        }

        // KO => corpo XML con <response><status>KO</status><error>...</error></response>
        var errBody = await res.Content.ReadAsStringAsync(ct);
        var (err, desc, sig, psCode, psDesc) = TryParseErrorXml(errBody);

        throw new ProxySignException(
            message: $"Firma KO ({(int)res.StatusCode}). {(psCode ?? err) ?? "N/A"} - {(psDesc ?? desc) ?? "N/A"}",
            statusCode: (int)res.StatusCode,
            errorCode: err,
            errorDescription: desc,
            errorCodeSignature: sig,
            proxysignErrorCode: psCode,
            proxysignErrorDescription: psDesc
        );
    }

    private static (string? errorCode, string? errorDescription, string? errorCodeSignature, string? proxysignErrorCode, string? proxysignErrorDescription)
        TryParseErrorXml(string xml)
    {
        try
        {
            var x = XDocument.Parse(xml);
            var err = x.Root?.Element("error");
            return (
                err?.Element("error-code")?.Value,
                err?.Element("error-description")?.Value,
                err?.Element("error-code-signature")?.Value,
                err?.Element("proxysign-error-code")?.Value,
                err?.Element("proxysign-error-description")?.Value
            );
        }
        catch
        {
            return (null, null, null, null, null);
        }
    }
}
