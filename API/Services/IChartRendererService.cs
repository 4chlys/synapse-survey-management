using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services;

public interface IChartRendererService
{
    Task<byte[]> InjectChartsAsync(byte[] xlsxBytes, SystemStatsDto stats, CancellationToken ct = default);
    Task<byte[]> InjectProjectChartsAsync(byte[] xlsxBytes, ProjectExportDataDto data, CancellationToken ct = default);
}
