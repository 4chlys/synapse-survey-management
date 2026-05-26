namespace SynapseEnqueteManagement.API.Services;

public sealed class SummaryRenderException(string message, Exception inner)
    : Exception(message, inner);