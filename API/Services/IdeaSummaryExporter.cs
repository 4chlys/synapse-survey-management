using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Services;

public sealed class IdeaSummaryExporter
{
    static IdeaSummaryExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public (byte[] Bytes, string ContentType, string FileName) Export(
        string summaryText,
        IdeaSummaryExportFormat format,
        int projectId)
    {
        ArgumentNullException.ThrowIfNull(summaryText);
        return format switch
        {
            IdeaSummaryExportFormat.Pdf => ExportPdf(summaryText, projectId),
            IdeaSummaryExportFormat.Png => ExportPng(summaryText, projectId),
            _                          => ExportTxt(summaryText, projectId),
        };
    }

    private static (byte[], string, string) ExportTxt(string text, int projectId)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return (bytes, "text/plain; charset=utf-8", $"summary-{projectId}.txt");
    }

    private static (byte[], string, string) ExportPdf(string text, int projectId)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).LineHeight(1.4f));

                page.Content().Column(col => RenderMarkdown(col, text));
            });
        }).GeneratePdf();

        return (bytes, "application/pdf", $"summary-{projectId}.pdf");
    }

    private static void RenderMarkdown(ColumnDescriptor col, string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                col.Item().Height(4);
                continue;
            }

            if (line.StartsWith("# "))
            {
                col.Item().PaddingBottom(6).Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(18).Bold());
                    RenderInline(t, line[2..]);
                });
            }
            else if (line.StartsWith("## "))
            {
                col.Item().PaddingTop(10).PaddingBottom(4).Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(14).Bold());
                    RenderInline(t, line[3..]);
                });
            }
            else if (line.StartsWith("### "))
            {
                col.Item().PaddingTop(6).PaddingBottom(2).Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(12).Bold());
                    RenderInline(t, line[4..]);
                });
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(14).PaddingTop(1).Text("•");
                    row.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.LineHeight(1.4f));
                        RenderInline(t, line[2..]);
                    });
                });
            }
            else
            {
                col.Item().Text(t =>
                {
                    t.DefaultTextStyle(s => s.LineHeight(1.5f));
                    RenderInline(t, line);
                });
            }
        }
    }

    private static void RenderInline(TextDescriptor t, string text)
    {
        var segments = text.Split("**");
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length == 0) continue;
            if (i % 2 == 1)
                t.Span(segments[i]).Bold();
            else
                t.Span(segments[i]);
        }
    }

    private static (byte[], string, string) ExportPng(string text, int projectId)
    {
        const int width = 1200;
        const float fontSize = 18f;
        const float titleFontSize = 24f;
        const float padding = 40f;
        const float lineHeight = fontSize * 1.6f;

        var stripped = StripMarkdown(text, out var title);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.Default,
        };

        var lines = WrapText(stripped, paint, width - padding * 2);
        var titleAreaHeight = string.IsNullOrEmpty(title) ? 0f : titleFontSize * 2.5f;
        int height = (int)(padding * 2 + lines.Count * lineHeight + titleAreaHeight);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        float y = padding;

        if (!string.IsNullOrEmpty(title))
        {
            using var titlePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = titleFontSize,
                IsAntialias = true,
                FakeBoldText = true,
            };
            y += titleFontSize;
            canvas.DrawText(title, padding, y, titlePaint);
            y += titleAreaHeight - titleFontSize;
        }

        foreach (var line in lines)
        {
            canvas.DrawText(line, padding, y, paint);
            y += lineHeight;
        }

        using var ms = new System.IO.MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Png, 95);
        return (ms.ToArray(), "image/png", $"summary-{projectId}.png");
    }

    private static string StripMarkdown(string text, out string title)
    {
        title = string.Empty;
        var sb = new System.Text.StringBuilder();
        var titleFound = false;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (!titleFound && line.StartsWith("# "))
            {
                title = line[2..];
                titleFound = true;
                continue;
            }

            if (line.StartsWith("### ")) line = line[4..];
            else if (line.StartsWith("## ")) line = line[3..];
            else if (line.StartsWith("# ")) line = line[2..];
            else if (line.StartsWith("- ") || line.StartsWith("* ")) line = "• " + line[2..];

            line = System.Text.RegularExpressions.Regex.Replace(line, @"\*\*(.+?)\*\*", "$1");
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var result = new List<string>();
        foreach (var rawLine in text.Split('\n'))
        {
            var words = rawLine.Split(' ');
            var current = new System.Text.StringBuilder();
            foreach (var word in words)
            {
                var test = current.Length == 0 ? word : $"{current} {word}";
                if (paint.MeasureText(test) > maxWidth && current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                }
            }
            result.Add(current.ToString());
        }
        return result;
    }
}