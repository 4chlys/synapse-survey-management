namespace SynapseEnqueteManagement.API.Services;

public sealed record ExportSheet(string Key, string Name, string[] Headers, IEnumerable<string[]> Rows);
