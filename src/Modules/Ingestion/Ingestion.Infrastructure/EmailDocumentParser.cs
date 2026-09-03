using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal static class EmailDocumentParser
{
    public static SourceDocument Parse(string path)
    {
        var raw = File.ReadAllText(path);
        var steps = new List<ParseStep>
        {
            new("Detect", "message/rfc822", true)
        };

        string? from = null, to = null, subject = null;
        DateTimeOffset? sent = null;
        var bodyStart = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (bodyStart < 0)
        {
            bodyStart = raw.IndexOf("\n\n", StringComparison.Ordinal);
        }

        var headerBlock = bodyStart >= 0 ? raw[..bodyStart] : raw;
        var body = bodyStart >= 0 ? raw[(bodyStart + (raw[bodyStart] == '\r' ? 4 : 2))..].Trim() : string.Empty;

        foreach (var line in headerBlock.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            {
                from = line[5..].Trim();
            }
            else if (line.StartsWith("To:", StringComparison.OrdinalIgnoreCase))
            {
                to = line[3..].Trim();
            }
            else if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                subject = line[8..].Trim();
            }
            else if (line.StartsWith("Date:", StringComparison.OrdinalIgnoreCase)
                     && DateTimeOffset.TryParse(line[5..].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
            {
                sent = parsed;
            }
        }

        steps.Add(new("MIME", $"From {from ?? "—"}; subject {subject ?? "—"}", body.Length > 0));

        var extracted = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            extracted.Append("Subject: ").AppendLine(subject);
        }

        if (!string.IsNullOrWhiteSpace(from))
        {
            extracted.Append("From: ").AppendLine(from);
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            extracted.Append("To: ").AppendLine(to);
        }

        extracted.AppendLine().Append(body);

        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = "eml",
            Title = subject ?? Path.GetFileNameWithoutExtension(path),
            ExtractedText = extracted.ToString().Trim(),
            Steps = steps,
            ContentType = "message/rfc822",
            From = from,
            To = to,
            Subject = subject,
            SentAt = sent
        };
    }
}

internal static class RtfDocumentParser
{
    public static SourceDocument Parse(string path)
    {
        var raw = File.ReadAllText(path);
        var steps = new List<ParseStep>
        {
            new("Detect", Path.GetExtension(path).Equals(".doc", StringComparison.OrdinalIgnoreCase)
                ? "application/msword (RTF payload — typical SK e-mail attachment)"
                : "application/rtf", true)
        };

        var extracted = raw.TrimStart().StartsWith("{\\rtf", StringComparison.OrdinalIgnoreCase)
            ? StripRtf(raw)
            : raw;
        steps.Add(new("RTF strip", $"{extracted.Length} characters", extracted.Length > 0));

        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
            Title = Path.GetFileNameWithoutExtension(path).Replace('-', ' '),
            ExtractedText = extracted.Trim(),
            Steps = steps,
            ContentType = Path.GetExtension(path).Equals(".doc", StringComparison.OrdinalIgnoreCase)
                ? "application/msword"
                : "application/rtf"
        };
    }

    private static string StripRtf(string rtf)
    {
        var withoutGroups = Regex.Replace(rtf, @"\\'[0-9a-fA-F]{2}", " ");
        withoutGroups = Regex.Replace(withoutGroups, @"\\[a-zA-Z]+-?\d* ?", " ");
        withoutGroups = withoutGroups.Replace("{", " ").Replace("}", " ");
        return Regex.Replace(withoutGroups, @"\s+", " ").Trim();
    }
}

internal static class TextDocumentParser
{
    public static SourceDocument Parse(string path)
    {
        var text = File.ReadAllText(path);
        return new SourceDocument
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = "text",
            Title = Path.GetFileNameWithoutExtension(path).Replace('-', ' '),
            ExtractedText = text,
            Steps = [new("Plain text", $"{text.Length} characters", text.Length > 0)],
            ContentType = "text/plain"
        };
    }
}
