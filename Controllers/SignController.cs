using Microsoft.AspNetCore.Mvc;
using MiddlewareInfocert.Clients;
using MiddlewareInfocert.Models;
using System.IO;


namespace MiddlewareInfocert.Controllers;

using Microsoft.AspNetCore.Mvc;
using MiddlewareInfocert.Clients;
using MiddlewareInfocert.Models;

[ApiController]
[Route("api/[controller]")]
public class SignController : ControllerBase
{
    // POST api/sign/auto-pades
    [HttpPost("auto-pades")]
    public async Task<ActionResult<SignAutoPadesResponse>> AutoPades(
        [FromBody] SignAutoPadesRequest request,
        [FromServices] DocsmarshalClient dm,
        [FromServices] ProxySignClient ps,
        [FromServices] IConfiguration config)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.ObjectId) ||
            string.IsNullOrWhiteSpace(request.Alias) ||
            string.IsNullOrWhiteSpace(request.Pin))
        {
            return BadRequest(new SignAutoPadesResponse
            {
                Success = false,
                Message = "Parametri mancanti: objectId, alias e pin sono obbligatori."
            });
        }

        try
        {
            // 1) Recupera PDF e nome file da DocsMarshal (fieldExternalId letto da appsettings nel client)
            var (base64, fileName) = await dm.GetDocumentAsync(request.ObjectId);
            if (string.IsNullOrWhiteSpace(base64))
            {
                return NotFound(new SignAutoPadesResponse
                {
                    Success = false,
                    Message = "Documento non trovato in DocsMarshal."
                });
            }

            var pdfBytes = Convert.FromBase64String(base64);

            // 2) Applica watermark con il NomeMedico (se valorizzato)
            var watermarked = PdfWatermarkHelper.AddLeftVerticalWatermark(
                pdfBytes,
                request.NomeMedico ?? string.Empty,
                config
            );

            // 3) Firma PADES automatica con ProxySign
            var signed = await ps.SignPadesAutoAsync(request.Alias, request.Pin, watermarked);

            // 4) Sostituisce il documento su DocsMarshal mantenendo lo stesso nome file
            //    (fieldExternalId e raiseEvents sono gestiti dal client tramite opzioni)
            await dm.ReplaceDocumentAsync(request.ObjectId, signed, raiseEvents: true);

            // 5) OK
            return Ok(new SignAutoPadesResponse
            {
                Success = true,
                Message = "Documento firmato e caricato con successo.",
                SignedObjectId = request.ObjectId
            });
        }
        catch (ProxySignException ex)
        {
            return StatusCode(ex.StatusCode, new SignAutoPadesResponse
            {
                Success = false,
                Message = $"Errore ProxySign: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SignAutoPadesResponse
            {
                Success = false,
                Message = $"Errore interno: {ex.Message}"
            });
        }
    }
}





    //TEMP
    /*
    public sealed class PsTestRequest
    {
        public string Alias { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string PdfBase64 { get; set; } = string.Empty;
    }

    [HttpPost("ps-test")]
    public async Task<IActionResult> ProxySignTest(
    [FromBody] PsTestRequest req,
    [FromServices] ProxySignClient ps,
    CancellationToken ct)
    {
        try
        {
            // Converti da base64 il PDF di input
            var pdf = Convert.FromBase64String(req.PdfBase64);

            // Chiama ProxySign per la firma
            var signed = await ps.SignPadesAutoAsync(req.Alias, req.Pin, pdf, ct);

            // Percorso dove salvare il PDF firmato
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputPath = Path.Combine(desktop, "docfirmato.pdf");

            await System.IO.File.WriteAllBytesAsync(outputPath, signed, ct);

            return Ok(new
            {
                ok = true,
                savedTo = outputPath,
                signedBytes = signed.Length
            });
        }
        catch (ProxySignException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                ok = false,
                error = ex.Message,
                ex.ErrorCode,
                ex.ErrorDescription,
                ex.ErrorCodeSignature,
                ex.ProxySignErrorCode,
                ex.ProxySignErrorDescription
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message });
        }
    } */






