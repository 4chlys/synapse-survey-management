using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services;

public interface IChartImageService
{
    /// <summary>Renders the four system-stats charts as PNG byte buffers.</summary>
    /// <returns>Tuples of (file-name-stem, png-bytes). The stems are stable identifiers
    /// like "participants-per-project" so callers can name files inside a zip and
    /// match them to specific charts when embedding in Excel.</returns>
    IReadOnlyList<(string Stem, byte[] PngBytes)> RenderAll(SystemStatsDto stats);

    /// <summary>Renders only the completion-time box plot. Used by the XLSX export
    /// to embed a PNG image because native Excel chartex box plots are brittle.</summary>
    byte[] RenderCompletionBoxPlot(SystemStatsDto stats);

    /// <summary>Renders the project-dashboard charts (timeline + top categories) as PNG byte buffers.</summary>
    IReadOnlyList<(string Stem, byte[] PngBytes)> RenderProjectCharts(ProjectExportDataDto data);
}
