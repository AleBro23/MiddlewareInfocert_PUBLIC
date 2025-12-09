using Microsoft.AspNetCore.Mvc;
using MiddlewareInfocert.Models;
using System.IO;

namespace MiddlewareInfocert.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatermarkController : ControllerBase
{
    private readonly IConfiguration _config;
    public WatermarkController(IConfiguration config) => _config = config;

    public class WatermarkRequest
    {
        public string Nome { get; set; }
        public string FileBase64 { get; set; }
    }

    [HttpPost("crea")]
    public IActionResult CreaConFiligrana([FromBody] WatermarkRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.FileBase64))
            return BadRequest("Nome e FileBase64 sono obbligatori.");

        try
        {
            byte[] pdfBytes = Convert.FromBase64String(request.FileBase64);
            var outputPdf = PdfWatermarkHelper.AddLeftVerticalWatermark(pdfBytes, request.Nome, _config);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, "filigrana.pdf");
            System.IO.File.WriteAllBytes(path, outputPdf);

            return Ok(new { success = true, path });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
