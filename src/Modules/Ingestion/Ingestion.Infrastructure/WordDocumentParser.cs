using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal static class WordDocumentParser
{
    public static SourceDocument Parse(string path)
    {
        var steps = new List<ParseStep>
        {
            new("Detect", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)
        };

        var extracted = string.Empty;
        try
        {
            using var document = WordprocessingDocument.Open(path, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                steps.Add(new("Open XML", "Empty document body", false));
            }
            else
            {
                var builder = new StringBuilder();
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    var line = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
                    if (line.Length > 0)
                    {
                        builder.AppendLine(line);
                    }
                }

                extracted = builder.ToString().Trim();
                steps.Add(new("Open XML", $"{extracted.Length} characters from DOCX", extracted.Length > 0));
            }
        }
        catch (Exception ex)
        {
            steps.Add(new("Open XML", ex.Message, false));
        }

        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = "docx",
            Title = Path.GetFileNameWithoutExtension(path).Replace('-', ' '),
            ExtractedText = extracted,
            Steps = steps,
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
    }
}
