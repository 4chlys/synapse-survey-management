using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SynapseEnqueteManagement.API.DTOs.Stats;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace SynapseEnqueteManagement.API.Services;

/// <summary>
/// Injects native Excel charts (bar, doughnut) into an XLSX byte stream produced by
/// ExportService. Charts use literal numCache values, so they are self-contained
/// snapshots and not formula-linked to sheets. The completion-time box plot is
/// rendered separately as an embedded PNG (see IChartImageService) because the
/// chartex namespace is brittle and not worth the OpenXML complexity here.
/// </summary>
public class XlsxChartRendererService(IChartImageService chartImages) : IChartRendererService
{
    private const int TopProjectsForBar = 20;

    public Task<byte[]> InjectChartsAsync(byte[] xlsxBytes, SystemStatsDto stats, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        ms.Write(xlsxBytes, 0, xlsxBytes.Length);
        ms.Position = 0;

        using (var doc = SpreadsheetDocument.Open(ms, true))
        {
            var topParticipants = stats.Projects
                .OrderByDescending(p => p.ParticipantCount)
                .Take(TopProjectsForBar)
                .ToList();
            InjectBarChart(doc, "Projects", "Participants per project",
                topParticipants.Select(p => p.ProjectName).ToList(),
                topParticipants.Select(p => (double)p.ParticipantCount).ToList(),
                anchorRow: 25);

            InjectDoughnutChart(doc, "Summary", "Project states",
                ["Active", "Draft", "Archived"],
                [stats.ActiveProjectCount, stats.DraftProjectCount, stats.ArchivedProjectCount],
                anchorRow: 10);

            var aiByService = stats.AiUsageBySystem
                .GroupBy(s => s.Service)
                .Select(g => (Service: g.Key, Cost: g.Sum(x => (double)x.CostEur)))
                .OrderByDescending(x => x.Cost)
                .ToList();
            InjectBarChart(doc, "AI by service", "AI cost (EUR) per service",
                aiByService.Select(x => x.Service).ToList(),
                aiByService.Select(x => x.Cost).ToList(),
                anchorRow: aiByService.Count + 4);

            // Box plot embedded as PNG (chartex is too brittle to ship).
            var boxPlotPng = chartImages.RenderCompletionBoxPlot(stats);
            if (boxPlotPng.Length > 0)
                EmbedImage(doc, "Projects", boxPlotPng, anchorRow: 50, widthCols: 15, heightRows: 22);
        }

        return Task.FromResult(ms.ToArray());
    }

    // ---------- typed bar / doughnut helpers ----------

    private static void InjectBarChart(SpreadsheetDocument doc, string sheetName, string title,
        IReadOnlyList<string> categories, IReadOnlyList<double> values, int anchorRow)
    {
        if (categories.Count == 0) return;
        var wsPart = FindWorksheetPart(doc, sheetName);
        if (wsPart is null) return;

        var (chartPart, relId) = AddChartPart(wsPart);

        var chartSpace = new ChartSpace();
        chartSpace.Append(new EditingLanguage { Val = "en-US" });

        var chart = new Chart();
        chart.Append(BuildTitle(title));
        chart.Append(new AutoTitleDeleted { Val = false });

        var plotArea = new PlotArea();
        plotArea.Append(new Layout());

        var barChart = new BarChart();
        barChart.Append(new BarDirection { Val = BarDirectionValues.Column });
        barChart.Append(new BarGrouping { Val = BarGroupingValues.Clustered });
        barChart.Append(new VaryColors { Val = false });

        var series = new BarChartSeries(
            new C.Index { Val = 0U },
            new Order { Val = 0U },
            new SeriesText(new NumericValue { Text = title }));
        series.Append(BuildCategoryAxisData(categories));
        series.Append(BuildValues(values));
        barChart.Append(series);

        barChart.Append(new DataLabels(
            new ShowLegendKey { Val = false },
            new ShowValue { Val = false },
            new ShowCategoryName { Val = false },
            new ShowSeriesName { Val = false },
            new ShowPercent { Val = false },
            new ShowBubbleSize { Val = false }));
        barChart.Append(new GapWidth { Val = 150 });
        barChart.Append(new AxisId { Val = 111111111U });
        barChart.Append(new AxisId { Val = 222222222U });

        plotArea.Append(barChart);
        plotArea.Append(BuildCategoryAxis(111111111U, 222222222U));
        plotArea.Append(BuildValueAxis(222222222U, 111111111U));

        chart.Append(plotArea);
        chart.Append(new PlotVisibleOnly { Val = true });
        chart.Append(new DisplayBlanksAs { Val = DisplayBlanksAsValues.Gap });
        chartSpace.Append(chart);

        chartPart.ChartSpace = chartSpace;
        chartPart.ChartSpace.Save();

        AppendDrawing(wsPart, relId, anchorRow);
    }

    private static void InjectDoughnutChart(SpreadsheetDocument doc, string sheetName, string title,
        IReadOnlyList<string> categories, IReadOnlyList<double> values, int anchorRow)
    {
        if (categories.Count == 0 || values.Sum() <= 0) return;
        var wsPart = FindWorksheetPart(doc, sheetName);
        if (wsPart is null) return;

        var (chartPart, relId) = AddChartPart(wsPart);

        var chartSpace = new ChartSpace();
        chartSpace.Append(new EditingLanguage { Val = "en-US" });

        var chart = new Chart();
        chart.Append(BuildTitle(title));
        chart.Append(new AutoTitleDeleted { Val = false });

        var plotArea = new PlotArea();
        plotArea.Append(new Layout());

        var doughnutChart = new DoughnutChart();
        doughnutChart.Append(new VaryColors { Val = true });

        var series = new PieChartSeries(
            new C.Index { Val = 0U },
            new Order { Val = 0U },
            new SeriesText(new NumericValue { Text = title }));
        series.Append(BuildCategoryAxisData(categories));
        series.Append(BuildValues(values));
        doughnutChart.Append(series);

        doughnutChart.Append(new FirstSliceAngle { Val = 0 });
        doughnutChart.Append(new HoleSize { Val = 50 });

        plotArea.Append(doughnutChart);
        chart.Append(plotArea);
        chart.Append(new Legend(
            new LegendPosition { Val = LegendPositionValues.Right },
            new Overlay { Val = false }));
        chart.Append(new PlotVisibleOnly { Val = true });
        chart.Append(new DisplayBlanksAs { Val = DisplayBlanksAsValues.Gap });
        chartSpace.Append(chart);

        chartPart.ChartSpace = chartSpace;
        chartPart.ChartSpace.Save();

        AppendDrawing(wsPart, relId, anchorRow);
    }

    // ---------- shared helpers ----------

    private static WorksheetPart FindWorksheetPart(SpreadsheetDocument doc, string sheetName)
    {
        var sheet = doc.WorkbookPart?.Workbook.Descendants<Sheet>()
            .FirstOrDefault(s => string.Equals(s.Name, sheetName, StringComparison.Ordinal));
        if (sheet?.Id?.Value is null) return null;
        return (WorksheetPart)doc.WorkbookPart.GetPartById(sheet.Id.Value);
    }

    private static (ChartPart Part, string RelationshipId) AddChartPart(WorksheetPart wsPart)
    {
        var drawingsPart = wsPart.DrawingsPart ?? wsPart.AddNewPart<DrawingsPart>();
        if (wsPart.Worksheet.Elements<Drawing>().FirstOrDefault() is null)
        {
            var relId = wsPart.GetIdOfPart(drawingsPart);
            wsPart.Worksheet.Append(new Drawing { Id = relId });
        }

        if (drawingsPart.WorksheetDrawing is null)
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

        var chartPart = drawingsPart.AddNewPart<ChartPart>();
        var relId2 = drawingsPart.GetIdOfPart(chartPart);
        return (chartPart, relId2);
    }

    private static void AppendDrawing(WorksheetPart wsPart, string chartRelId, int anchorRow)
    {
        var drawingsPart = wsPart.DrawingsPart!;
        var worksheetDrawing = drawingsPart.WorksheetDrawing;

        // 15 columns wide x 18 rows tall starting at anchorRow, column A.
        var anchor = new Xdr.TwoCellAnchor(
            new Xdr.FromMarker(
                new Xdr.ColumnId("0"), new Xdr.ColumnOffset("0"),
                new Xdr.RowId((anchorRow - 1).ToString(CultureInfo.InvariantCulture)), new Xdr.RowOffset("0")),
            new Xdr.ToMarker(
                new Xdr.ColumnId("15"), new Xdr.ColumnOffset("0"),
                new Xdr.RowId((anchorRow + 17).ToString(CultureInfo.InvariantCulture)), new Xdr.RowOffset("0")),
            new Xdr.GraphicFrame(
                new Xdr.NonVisualGraphicFrameProperties(
                    new Xdr.NonVisualDrawingProperties { Id = (uint)(worksheetDrawing.ChildElements.Count + 2), Name = "Chart " + chartRelId },
                    new Xdr.NonVisualGraphicFrameDrawingProperties()),
                new Xdr.Transform(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = 0L, Cy = 0L }),
                new A.Graphic(
                    new A.GraphicData(
                        new C.ChartReference { Id = chartRelId }
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" })),
            new Xdr.ClientData());

        worksheetDrawing.Append(anchor);
        worksheetDrawing.Save(drawingsPart);
    }

    private static Title BuildTitle(string text)
    {
        var run = new A.Run(
            new A.RunProperties { Language = "en-US", FontSize = 1400, Bold = true },
            new A.Text(text));
        var paragraph = new A.Paragraph(run);
        var richText = new RichText(
            new A.BodyProperties(),
            new A.ListStyle(),
            paragraph);
        return new Title(new ChartText(richText),
            new Overlay { Val = false });
    }

    private static CategoryAxisData BuildCategoryAxisData(IReadOnlyList<string> categories)
    {
        var sd = new StringLiteral();
        sd.Append(new PointCount { Val = (uint)categories.Count });
        for (uint i = 0; i < categories.Count; i++)
            sd.Append(new StringPoint { Index = i, NumericValue = new NumericValue(categories[(int)i] ?? string.Empty) });
        return new CategoryAxisData(sd);
    }

    private static C.Values BuildValues(IReadOnlyList<double> values)
    {
        var nl = new NumberLiteral();
        nl.Append(new FormatCode("General"));
        nl.Append(new PointCount { Val = (uint)values.Count });
        for (uint i = 0; i < values.Count; i++)
            nl.Append(new NumericPoint { Index = i, NumericValue = new NumericValue(values[(int)i].ToString("R", CultureInfo.InvariantCulture)) });
        return new C.Values(nl);
    }

    private static CategoryAxis BuildCategoryAxis(uint id, uint crossId) =>
        new(new AxisId { Val = id },
            new Scaling(new Orientation { Val = C.OrientationValues.MinMax }),
            new Delete { Val = false },
            new AxisPosition { Val = AxisPositionValues.Bottom },
            new CrossingAxis { Val = crossId });

    private static ValueAxis BuildValueAxis(uint id, uint crossId) =>
        new(new AxisId { Val = id },
            new Scaling(new Orientation { Val = C.OrientationValues.MinMax }),
            new Delete { Val = false },
            new AxisPosition { Val = AxisPositionValues.Left },
            new CrossingAxis { Val = crossId });

    // ---------- image embedding ----------

    private static void EmbedImage(SpreadsheetDocument doc, string sheetName, byte[] pngBytes,
        int anchorRow, int widthCols, int heightRows)
    {
        var wsPart = FindWorksheetPart(doc, sheetName);
        if (wsPart is null) return;

        var drawingsPart = wsPart.DrawingsPart ?? wsPart.AddNewPart<DrawingsPart>();
        if (wsPart.Worksheet.Elements<Drawing>().FirstOrDefault() is null)
        {
            var rid = wsPart.GetIdOfPart(drawingsPart);
            wsPart.Worksheet.Append(new Drawing { Id = rid });
        }
        if (drawingsPart.WorksheetDrawing is null)
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

        var imagePart = drawingsPart.AddImagePart(ImagePartType.Png);
        using (var imgStream = imagePart.GetStream(FileMode.Create))
            imgStream.Write(pngBytes, 0, pngBytes.Length);
        var imageRelId = drawingsPart.GetIdOfPart(imagePart);

        var worksheetDrawing = drawingsPart.WorksheetDrawing;
        uint nextId = (uint)(worksheetDrawing.ChildElements.Count + 2);

        var anchor = new Xdr.TwoCellAnchor(
            new Xdr.FromMarker(
                new Xdr.ColumnId("0"), new Xdr.ColumnOffset("0"),
                new Xdr.RowId((anchorRow - 1).ToString(CultureInfo.InvariantCulture)), new Xdr.RowOffset("0")),
            new Xdr.ToMarker(
                new Xdr.ColumnId(widthCols.ToString(CultureInfo.InvariantCulture)), new Xdr.ColumnOffset("0"),
                new Xdr.RowId((anchorRow - 1 + heightRows).ToString(CultureInfo.InvariantCulture)), new Xdr.RowOffset("0")),
            new Xdr.Picture(
                new Xdr.NonVisualPictureProperties(
                    new Xdr.NonVisualDrawingProperties { Id = nextId, Name = "Picture " + imageRelId },
                    new Xdr.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true })),
                new Xdr.BlipFill(
                    new A.Blip { Embed = imageRelId, CompressionState = A.BlipCompressionValues.Print },
                    new A.Stretch(new A.FillRectangle())),
                new Xdr.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = 0L, Cy = 0L }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })),
            new Xdr.ClientData());

        worksheetDrawing.Append(anchor);
        worksheetDrawing.Save(drawingsPart);
    }
}
