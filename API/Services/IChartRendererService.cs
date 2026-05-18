using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services;

public interface IChartRendererService
{
    Task<byte[]> InjectChartsAsync(byte[] xlsxBytes, SystemStatsDto stats, CancellationToken ct = default);
}
