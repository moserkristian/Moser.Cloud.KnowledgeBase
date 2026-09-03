using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UglyToad.PdfPig;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal static class PdfDocumentParser
{
    public static SourceDocument Parse(string path)
    {
        var steps = new List<ParseStep>
        {
            new("Detect", "application/pdf", true)
        };

        var text = new StringBuilder();
        var pages = 0;
        try
        {
            using var document = PdfDocument.Open(path);
            pages = document.NumberOfPages;
            foreach (var page in document.GetPages())
            {
                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    text.AppendLine(page.Text);
                }
            }

            steps.Add(new("PdfPig", $"{pages} page(s), text layer", text.Length > 0));
        }
        catch (Exception ex)
        {
            steps.Add(new("PdfPig", ex.Message, false));
        }

        var extracted = text.ToString().Trim();
        if (extracted.Length == 0)
        {
            steps.Add(new("OCR fallback", "No text layer — would rasterize and OCR. Sidecar not present.", false));
        }

        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = "pdf",
            Title = Path.GetFileNameWithoutExtension(path).Replace('-', ' '),
            ExtractedText = extracted,
            Steps = steps,
            ContentType = "application/pdf"
        };
    }
}
