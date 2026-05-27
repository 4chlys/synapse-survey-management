namespace SynapseEnqueteManagement.UI.Admin.Models.Enums;

// Local mirror of the API's QuestionFormat. Backing values are kept aligned with
// the API enum (and UI-User/Models/Survey/QuestionFormat.cs). UI-Admin currently
// sends/reads this format by name, but the values are aligned so a future numeric
// round-trip stays correct.
public enum QuestionFormat : byte
{
    Open = 1,
    SingleChoice = 2,
    MultipleChoice = 3,
    Likert = 4
}
