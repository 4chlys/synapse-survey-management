namespace SynapseEnqueteManagement.BL.AiSupport;

/// <summary>
/// Holds the Vertex AI chat model identifier (read from <c>VertexAi:Model</c> in
/// configuration). Shared by every service that talks to <c>IChatClient</c> so
/// usage logging records which model was billed.
/// </summary>
public sealed class AiChatModelOptions
{
    public string Model { get; init; } = "";
}
