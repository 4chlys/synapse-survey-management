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
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Content().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(18).Bold());
                        t.Span("Ideeënsamenvatting");
                    });

                    col.Item().PaddingTop(12).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.LineHeight(1.5f));
                        t.Span(text);
                    });
                });
            });
        }).GeneratePdf();

        return (bytes, "application/pdf", $"summary-{projectId}.pdf");
    }

    private static (byte[], string, string) ExportPng(string text, int projectId)
    {
        const int width = 1200;
        const float fontSize = 18f;
        const float padding = 40f;
        const float lineHeight = fontSize * 1.6f;

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.Default,
        };

        var lines = WrapText(text, paint, width - padding * 2);
        int height = (int)(padding * 2 + lines.Count * lineHeight + 60);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        float y = padding + 50;
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24f,
            IsAntialias = true,
            FakeBoldText = true,
        };
        canvas.DrawText("Ideeënsamenvatting", padding, y - lineHeight, titlePaint);

        foreach (var line in lines)
        {
            canvas.DrawText(line, padding, y, paint);
            y += lineHeight;
        }

        using var ms = new System.IO.MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Png, 95);
        return (ms.ToArray(), "image/png", $"summary-{projectId}.png");
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