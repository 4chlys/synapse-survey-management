namespace SynapseEnqueteManagement.BL.Domain.Stats;

// One participant's email + the language code they're enrolled in. Used by
// the admin "Send to all participants" broadcast to BCC each group with the
// matching translation. LanguageCode is nullable for participants who haven't
// picked a language yet (rare; broadcast falls back to project default).
public sealed record ParticipantEmailWithLanguage(string Email, string LanguageCode);
