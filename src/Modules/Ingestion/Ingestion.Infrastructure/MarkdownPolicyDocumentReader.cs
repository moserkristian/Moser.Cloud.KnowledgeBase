using Moser.RagAi.Ingestion.Application;

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal sealed class MarkdownPolicyDocumentReader : IPolicyDocumentReader
{
    public async IAsyncEnumerable<(string Source, string Markdown)> ReadAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = Path.GetFileName(file);
            var markdown = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            yield return (source, markdown);
        }
    }
}
