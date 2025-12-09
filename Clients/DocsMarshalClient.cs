using Microsoft.Extensions.Options;
using MiddlewareInfocert.Models;

public sealed class DocsmarshalClient
{
    private readonly HttpClient _http;
    private readonly DocsmarshalOptions _opt;

    public DocsmarshalClient(HttpClient http, IOptions<DocsmarshalOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    /// <summary>
    /// Recupera Base64 e FileName del documento da DocsMarshal.
    /// Usa il fieldExternalId configurato (InputFieldExternalId).
    /// </summary>
    public async Task<(string? Base64, string? FileName)> GetDocumentAsync(string objectId)
    {
        var payload = new DmGetByExternalIdRequest
        {
            sessionID = _opt.SessionId,
            objectID = objectId,
            fieldExternalId = _opt.InputFieldExternalId
        };

        using var res = await _http.PostAsJsonAsync("DMDocuments/GetProfileDocumentByObjectIdFieldExternalId", payload);
        res.EnsureSuccessStatusCode();

        var dto = await res.Content.ReadFromJsonAsync<DmGetByExternalIdResponse>();
        if (dto?.result.HasError == false && dto.result.Document is not null)
            return (dto.result.Document.FileBase64Content, dto.result.Document.FileName);

        return (null, null);
    }

    /// <summary>
    /// Carica un documento (mantieni il fileName).
    /// Usa il fieldExternalId configurato (OutputFieldExternalId se presente, altrimenti InputFieldExternalId).
    /// </summary>
    public async Task<(bool Success, string? Error)> SetProfileDocumentAsync(
        string objectId,
        string fileName,
        string fileBase64,
        bool raiseEvents)
    {
        var fieldExternalId = string.IsNullOrWhiteSpace(_opt.OutputFieldExternalId)
            ? _opt.InputFieldExternalId
            : _opt.OutputFieldExternalId;

        var payload = new DmSetProfileDocumentRequest
        {
            sessionID = _opt.SessionId,
            objectId = objectId,
            fileName = fileName,
            fileContentBase64 = fileBase64,
            fieldExternalId = fieldExternalId,
            raiseWorkflowEvents = raiseEvents
        };

        using var res = await _http.PostAsJsonAsync("DMDocuments/SetProfileDocument", payload);
        res.EnsureSuccessStatusCode();

        var dto = await res.Content.ReadFromJsonAsync<DmSetProfileDocumentResponse>();
        return (dto?.HasError == false, dto?.Error);
    }

    /// <summary>
    /// Scarica il documento, sostituisce il contenuto con newPdf mantenendo il nome file
    /// e ricarica su DocsMarshal.
    /// </summary>
    public async Task ReplaceDocumentAsync(string objectId, byte[] newPdf, bool raiseEvents)
    {
        var (_, fileName) = await GetDocumentAsync(objectId);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Documento non trovato o nome file mancante in DocsMarshal.");

        var base64 = Convert.ToBase64String(newPdf);
        var (success, error) = await SetProfileDocumentAsync(objectId, fileName, base64, raiseEvents);
        if (!success)
            throw new InvalidOperationException($"Errore SetProfileDocument: {error}");
    }
}
