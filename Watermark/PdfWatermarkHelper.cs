using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

public static class PdfWatermarkHelper
{
    /// <param name="config">IConfiguration per leggere Watermark:LogoPath</param>
    public static byte[] AddLeftVerticalWatermark(
        byte[] pdfIn,
        string nomeFirmatario,
        IConfiguration config,                 // <-- per leggere il path dal config
        DateTime? data = null,
        float leftMarginPt = 18f,
        float belowCenterOffsetPt = -300f,
        float fontSize = 7.5f,
        float opacity = 0.65f)
    {
        Console.WriteLine("[Watermark] Inizio AddLeftVerticalWatermark");

        if (pdfIn == null || pdfIn.Length == 0)
            throw new ArgumentException("PDF vuoto.", nameof(pdfIn));
        if (string.IsNullOrWhiteSpace(nomeFirmatario))
            throw new ArgumentException("Nome firmatario mancante.", nameof(nomeFirmatario));

        // Legge il path del logo da appsettings
        string logoPath = config?["Watermark:LogoPath"];
        byte[] logoBytes = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            logoBytes = File.ReadAllBytes(logoPath);
            Console.WriteLine($"[Watermark] Logo caricato da: {logoPath}, size={logoBytes.Length} bytes");
        }
        else
        {
            Console.WriteLine("[Watermark] Nessun logo trovato o path non valido: Watermark:LogoPath");
        }

        var dataStr = (data ?? DateTime.Today).ToString("dd/MM/yyyy");

        try
        {
            using var src = new MemoryStream(pdfIn);
            using var dst = new MemoryStream();

            Console.WriteLine("[Watermark] Creo PdfReader e PdfWriter...");
            var reader = new PdfReader(src);

            // Disabilito SmartMode per evitare dipendenze a BouncyCastle
            var writerProps = new WriterProperties();
            var writer = new PdfWriter(dst, writerProps);

            using var pdfDoc = new PdfDocument(reader, writer);

            int pageCount = pdfDoc.GetNumberOfPages();
            Console.WriteLine($"[Watermark] Numero pagine: {pageCount}");

            for (int i = 1; i <= pageCount; i++)
            {
                Console.WriteLine($"[Watermark] Elaboro pagina {i}...");
                var page = pdfDoc.GetPage(i);
                Rectangle ps = page.GetPageSize();

                string text = $"File firmato digitalmente da Dottor {nomeFirmatario.ToUpper()} in data {dataStr} (PAGINA {i})";
                float x = leftMarginPt;
                float y = ps.GetHeight() / 2f + belowCenterOffsetPt;

                var pdfCanvas = new PdfCanvas(page);
                var canvas = new Canvas(pdfCanvas, ps);

                // Testo blu con opacità
                var blue = ColorConstants.BLUE;

                // Calcolo dimensione massima del logo per non “sbordare”
                // margine di sicurezza 5pt dal bordo destro
                float safeWidth = Math.Max(0, ps.GetWidth() - x - 5f);
                float desiredIcon = 42f;                  // “un po’ più grande” del 35 usato prima
                float iconSize = Math.Min(desiredIcon, safeWidth);

                // costruisco tabella 1 riga, 3 colonne
                var table = new Table(new float[] { iconSize, 1, iconSize }) // larghezze fisse
                    .SetBorder(Border.NO_BORDER);

                // cella logo before
                if (logoBytes != null)
                {
                    var imgData = ImageDataFactory.Create(logoBytes);
                    var imgBefore = new Image(imgData).ScaleToFit(iconSize, iconSize);
                    table.AddCell(new Cell().Add(imgBefore)
                        .SetBorder(Border.NO_BORDER)
                        .SetPaddingRight(5)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE));
                }
                else
                {
                    table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }

                // cella testo
                table.AddCell(new Cell().Add(new Paragraph(text)
                        .SetFontSize(fontSize)
                        .SetFontColor(ColorConstants.BLUE, opacity))
                    .SetBorder(Border.NO_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE));

                // cella logo after
                if (logoBytes != null)
                {
                    var imgData = ImageDataFactory.Create(logoBytes);
                    var imgAfter = new Image(imgData).ScaleToFit(iconSize, iconSize);
                    table.AddCell(new Cell().Add(imgAfter)
                        .SetBorder(Border.NO_BORDER)
                        .SetPaddingLeft(5)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE));
                }
                else
                {
                    table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }

                // stampa tabella come oggetto
                canvas.ShowTextAligned(
                    new Paragraph().Add(table),
                    x, y, i,
                    TextAlignment.LEFT,
                    VerticalAlignment.MIDDLE,
                    (float)(Math.PI / 2)
                );



                canvas.Close();
            }

            Console.WriteLine("[Watermark] Chiudo documento PDF");
            pdfDoc.Close();
            return dst.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Watermark][ERRORE] " + ex.ToString());
            throw;
        }
    }
}
