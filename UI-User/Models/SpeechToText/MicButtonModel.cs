namespace SynapseEnqueteManagement.UI.User.Models.SpeechToText;

public sealed record MicButtonModel(
    string TargetSelector,
    string SizeClass  = "w-10 h-10",
    string ExtraClass = "",
    string AriaLabel  = null);