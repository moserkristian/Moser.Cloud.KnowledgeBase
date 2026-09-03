using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal sealed class CompositeDocumentReader : IPolicyDocumentReader
{
    public async IAsyncEnumerable<SourceDocument> ReadAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file);
            if (name.StartsWith('.') || name.EndsWith(".ocr.txt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ext = Path.GetExtension(file);
            SourceDocument? document = ext.ToLowerInvariant() switch
            {
                ".pdf" => PdfDocumentParser.Parse(file),
                ".docx" => WordDocumentParser.Parse(file),
                ".doc" or ".rtf" => RtfDocumentParser.Parse(file),
                ".eml" => EmailDocumentParser.Parse(file),
                ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" => ImageOcrParser.Parse(file),
                ".md" or ".txt" => TextDocumentParser.Parse(file),
                _ => null
            };

            if (document is null)
            {
                continue;
            }

            await Task.Yield();
            yield return document;
        }
    }
}
