using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using SkiaSharp;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal static class OfficeFileWriter
{
    static OfficeFileWriter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void Write(string path, string extension, string title, string body, MailboxPersona mailbox)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        switch (extension)
        {
            case "pdf":
                WritePdf(path, title, body, mailbox);
                break;
            case "docx":
                WriteDocx(path, title, body, mailbox);
                break;
            case "eml":
                WriteEml(path, title, body, mailbox);
                break;
            case "png":
                WriteScan(path, title, body, mailbox);
                break;
            case "doc":
                WriteRtf(path, title, body, mailbox);
                break;
            default:
                File.WriteAllText(path, body);
                break;
        }
    }

    private static void WritePdf(string path, string title, string body, MailboxPersona mailbox)
    {
        var plain = StripMarkup(body);
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(t => t.FontFamily(FontName).FontSize(10.5f).FontColor(Colors.Grey.Darken4).LineHeight(1.35f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(36).Height(36).Background("#1c1b18").AlignCenter().AlignMiddle()
                            .Text(mailbox.Mark).FontColor(Colors.White).FontSize(8).Bold();
                        row.RelativeItem().PaddingLeft(10).Column(c =>
                        {
                            c.Item().Text(mailbox.Firm).FontSize(12).Bold().FontColor("#1c1b18");
                            c.Item().Text(mailbox.AddressLine).FontSize(8.5f).FontColor("#6f6c65");
                            c.Item().Text(mailbox.IdsLine).FontSize(8).FontColor("#6f6c65");
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor("#c9c3b6");
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Item().AlignRight().Text($"{mailbox.City}, {mailbox.DateLabel}").FontSize(9).FontColor("#6f6c65");
                    col.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Vec: ").Bold();
                        text.Span(title);
                    });
                    col.Item().PaddingTop(12).Text(plain);
                    col.Item().PaddingTop(22).Text("S úctou,");
                    col.Item().PaddingTop(16).Text(mailbox.Signatory).Bold();
                    col.Item().Text(mailbox.SignatoryRole).FontSize(9).FontColor("#6f6c65");
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span(mailbox.Footer).FontSize(7.5f).FontColor("#9a958a");
                    t.Span("  ·  ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(path);
    }

    private static void WriteDocx(string path, string title, string body, MailboxPersona mailbox)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
        var bodyEl = main.Document.Body!;

        void Para(string text, bool bold = false, string? size = "22")
        {
            var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            run.RunProperties = new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
                new FontSize { Val = size });
            if (bold)
            {
                run.RunProperties.AppendChild(new Bold());
            }

            bodyEl.AppendChild(new Paragraph(run));
        }

        Para(mailbox.Firm, bold: true, size: "28");
        Para(mailbox.AddressLine, size: "18");
        Para(mailbox.IdsLine, size: "18");
        Para("");
        Para($"Interný predpis · {title}", bold: true, size: "26");
        Para($"{mailbox.City} · {mailbox.DateLabel}", size: "18");
        Para("");
        foreach (var line in StripMarkup(body).Split('\n'))
        {
            Para(line.Length == 0 ? " " : line);
        }

        Para("");
        Para($"Schválil(a): {mailbox.Signatory}, {mailbox.SignatoryRole}", size: "18");
        main.Document.Save();
    }

    private static void WriteEml(string path, string title, string body, MailboxPersona mailbox)
    {
        var sent = mailbox.SentAt.ToString("r", CultureInfo.InvariantCulture);
        var messageId = $"<{Guid.NewGuid():N}@{mailbox.Domain}>";
        var sb = new StringBuilder();
        sb.Append("From: ").Append(mailbox.FromHeader).Append("\r\n");
        sb.Append("To: ").Append(mailbox.ToHeader).Append("\r\n");
        sb.Append("Cc: ").Append(mailbox.CcHeader).Append("\r\n");
        sb.Append("Subject: ").Append(EncodeHeader(title)).Append("\r\n");
        sb.Append("Date: ").Append(sent).Append("\r\n");
        sb.Append("Message-ID: ").Append(messageId).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=UTF-8; format=flowed\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit\r\n");
        sb.Append("X-Mailer: Microsoft Outlook 16.0\r\n");
        sb.Append("\r\n");
        sb.Append("Dobrý deň,\r\n\r\n");
        sb.Append(StripMarkup(body).Replace("\n", "\r\n"));
        sb.Append("\r\n\r\n");
        sb.Append("S pozdravom\r\n");
        sb.Append(mailbox.Signatory).Append("\r\n");
        sb.Append(mailbox.SignatoryRole).Append("\r\n");
        sb.Append(mailbox.Firm).Append("\r\n");
        sb.Append(mailbox.AddressLine).Append("\r\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteRtf(string path, string title, string body, MailboxPersona mailbox)
    {
        var text = StripMarkup($"{mailbox.Firm}\n{mailbox.AddressLine}\n{mailbox.IdsLine}\n\n{title}\n{mailbox.DateLabel}\n\n{body}\n\n{mailbox.Signatory}, {mailbox.SignatoryRole}");
        var rtf = new StringBuilder();
        rtf.Append(@"{\rtf1\ansi\ansicpg1250\deff0{\fonttbl{\f0\fswiss Calibri;}}\f0\fs22 ");
        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                rtf.Append(@"\par ");
            }
            else if (ch is '\\' or '{' or '}')
            {
                rtf.Append('\\').Append(ch);
            }
            else if (ch > 127)
            {
                rtf.Append(@"\u").Append((int)ch).Append('?');
            }
            else
            {
                rtf.Append(ch);
            }
        }

        rtf.Append('}');
        File.WriteAllText(path, rtf.ToString(), Encoding.ASCII);
    }

    private static void WriteScan(string path, string title, string body, MailboxPersona mailbox)
    {
        const int width = 1240;
        const int height = 1754;
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(236, 230, 214));

        using var noise = new SKPaint { Color = new SKColor(120, 110, 90, 18), IsAntialias = false };
        var rng = new Random(title.GetHashCode());
        for (var i = 0; i < 1400; i++)
        {
            canvas.DrawRect(rng.Next(width), rng.Next(height), rng.Next(1, 3), rng.Next(1, 3), noise);
        }

        canvas.Save();
        canvas.RotateDegrees(0.55f, width / 2f, height / 2f);

        using var paper = new SKPaint { Color = new SKColor(248, 244, 232) };
        canvas.DrawRect(48, 40, width - 96, height - 80, paper);

        using var rule = new SKPaint { Color = new SKColor(180, 40, 40), StrokeWidth = 2, IsStroke = true, IsAntialias = true };
        canvas.DrawRect(70, 58, width - 140, height - 120, rule);

        using var typeface = SKTypeface.FromFamilyName(FontName, SKFontStyle.Normal)
            ?? SKTypeface.FromFamilyName("Arial")
            ?? SKTypeface.Default;
        using var bold = SKTypeface.FromFamilyName(FontName, SKFontStyle.Bold)
            ?? typeface;

        void Draw(string text, float x, float y, float size, SKTypeface face, SKColor color)
        {
            using var font = new SKFont(face, size);
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawText(text, x, y, font, paint);
        }

        Draw(mailbox.Firm.ToUpperInvariant(), 96, 120, 18, bold, new SKColor(40, 38, 34));
        Draw(mailbox.AddressLine, 96, 148, 13, typeface, new SKColor(90, 86, 78));
        Draw(mailbox.IdsLine, 96, 168, 12, typeface, new SKColor(90, 86, 78));
        Draw("SKENOVANÉ / SCANNED  ·  " + mailbox.DateLabel, 96, 210, 11, bold, new SKColor(160, 42, 42));
        Draw(title, 96, 250, 16, bold, new SKColor(28, 27, 24));

        var y = 290f;
        var plain = StripMarkup(body);
        foreach (var line in Wrap(plain, 88))
        {
            if (y > height - 140)
            {
                Draw("[… ďalšia strana skenu …]", 96, y, 12, typeface, new SKColor(120, 110, 90));
                break;
            }

            Draw(line, 96, y, 13, typeface, new SKColor(36, 34, 30));
            y += 22;
        }

        using var stampPaint = new SKPaint
        {
            Color = new SKColor(170, 30, 30, 160),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        canvas.Save();
        canvas.RotateDegrees(-12, 980, 1480);
        canvas.DrawRoundRect(860, 1400, 220, 90, 8, 8, stampPaint);
        Draw("DOŠLO", 910, 1445, 22, bold, new SKColor(170, 30, 30, 180));
        Draw(mailbox.DateLabel, 890, 1478, 12, typeface, new SKColor(170, 30, 30, 180));
        canvas.Restore();
        canvas.Restore();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 88);
        File.WriteAllBytes(path, data.ToArray());
        File.WriteAllText(path + ".ocr.txt", $"SKENOVANÉ / SCANNED\n{mailbox.Firm}\n{title}\n\n{plain}", Encoding.UTF8);
    }

    internal static string StripMarkup(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var t = Regex.Replace(text, @"^---\s*$", string.Empty, RegexOptions.Multiline);
        t = Regex.Replace(t, @"^#+\s*", string.Empty, RegexOptions.Multiline);
        t = Regex.Replace(t, @"\*\*(.+?)\*\*", "$1");
        t = Regex.Replace(t, @"`([^`]+)`", "$1");
        t = Regex.Replace(t, @"^\|\s*.+\s*\|$", string.Empty, RegexOptions.Multiline);
        t = t.Replace("\r\n", "\n").Trim();
        return t;
    }

    private static string EncodeHeader(string value)
    {
        if (value.All(static c => c < 128))
        {
            return value;
        }

        return "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "?=";
    }

    private static System.Collections.Generic.List<string> Wrap(string text, int width)
    {
        var lines = new System.Collections.Generic.List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > width && current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0)
                {
                    current.Append(' ');
                }

                current.Append(word);
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }
        }

        return lines;
    }

    private const string FontName = "Segoe UI";
}

internal sealed record MailboxPersona(
    string Firm,
    string AddressLine,
    string IdsLine,
    string City,
    string DateLabel,
    DateTimeOffset SentAt,
    string FromHeader,
    string ToHeader,
    string CcHeader,
    string Domain,
    string Signatory,
    string SignatoryRole,
    string Footer,
    string Mark);
