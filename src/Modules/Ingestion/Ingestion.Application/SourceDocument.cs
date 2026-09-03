using System;
using System.Collections.Generic;

namespace Moser.RagAi.Ingestion.Application;

public sealed record ParseStep(string Name, string Detail, bool Ok, double? Confidence = null);

/// <summary>
/// One ingested office file after the converter ran (PDF / Word / mail / scan OCR).
/// </summary>
public sealed class SourceDocument
{
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public required string Kind { get; init; }
    public required string Title { get; init; }
    public required string ExtractedText { get; init; }
    public required IReadOnlyList<ParseStep> Steps { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public string? From { get; init; }
    public string? To { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset? SentAt { get; init; }
}

public interface ISourceLibrary
{
    IReadOnlyList<SourceDocument> Current { get; }

    void Replace(IReadOnlyList<SourceDocument> documents);
}
