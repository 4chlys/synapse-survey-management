namespace SynapseEnqueteManagement.UI.User.Models.SpeechToText;

public sealed record SpeechToTextRequest
{
    public string AudioData { get; init; } = string.Empty;
    public string MimeType  { get; init; } = "audio/webm;codecs=opus";
    public string Language  { get; init; } = "nl";
}

public sealed record SpeechToTextResponse
{
    public bool   IsSuccess    { get; init; }
    public string Transcript   { get; init; }
    public string ErrorMessage { get; init; }
    public string ErrorCode    { get; init; }
}
