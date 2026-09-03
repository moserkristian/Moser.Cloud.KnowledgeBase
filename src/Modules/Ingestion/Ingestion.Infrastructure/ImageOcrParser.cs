using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;

namespace Moser.RagAi.Ingestion.Infrastructure;

/// <summary>
/// Scan / image path. Live Tesseract is optional; the demo converter reads the
/// OCR sidecar written next to the PNG (the same output a Tesseract pass would produce).
/// </summary>
internal static class ImageOcrParser
{
    public static SourceDocument Parse(string path)
    {
        var steps = new List<ParseStep>
        {
            new("Detect", "image/png (scan)", true),
            new("Raster", Path.GetFileName(path), true)
        };

        var sidecar = path + ".ocr.txt";
        if (!File.Exists(sidecar))
        {
            sidecar = Path.ChangeExtension(path, ".ocr.txt");
        }

        var extracted = string.Empty;
        double? confidence = null;
        if (File.Exists(sidecar))
        {
            extracted = File.ReadAllText(sidecar).Trim();
            confidence = 0.94;
            steps.Add(new("OCR", "Tesseract-compatible sidecar (sk+eng). Image has no text layer.", true, confidence));
        }
        else
        {
            steps.Add(new("OCR", "No sidecar and no live tessdata — scan stored, text empty.", false));
        }

        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = "scan",
            Title = Path.GetFileNameWithoutExtension(path).Replace('-', ' '),
            ExtractedText = extracted,
            Steps = steps,
            ContentType = "image/png"
        };
    }
}
