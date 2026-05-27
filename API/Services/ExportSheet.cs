namespace SynapseEnqueteManagement.API.Services;

// A single tabular section of an export (one xlsx worksheet, or one csv file in a zip).
// Key is the stable slug used for csv file names; Name is the human-readable sheet title.
public sealed record ExportSheet(string Key, string Name, string[] Headers, IEnumerable<string[]> Rows);
