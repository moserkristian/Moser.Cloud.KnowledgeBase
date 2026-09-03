using Moser.RagAi.Ingestion.Application;

using System.Collections.Generic;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal sealed class SourceLibrary : ISourceLibrary
{
    private volatile IReadOnlyList<SourceDocument> _current = [];

    public IReadOnlyList<SourceDocument> Current => _current;

    public void Replace(IReadOnlyList<SourceDocument> documents)
        => _current = documents ?? [];
}
