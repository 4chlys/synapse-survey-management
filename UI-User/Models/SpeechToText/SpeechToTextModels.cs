namespace SynapseEnqueteManagement.UI.User.Models.SpeechToText;

public sealed class SpeechToTextRequest
{
    public string AudioData { get; set; } = string.Empty;
    public string MimeType  { get; set; } = "audio/webm;codecs=opus";
}

public sealed class SpeechToTextResponse
{
    public bool   IsSuccess    { get; set; }
    public string Transcript   { get; set; }
    public string ErrorMessage { get; set; }
}
