namespace SynapseEnqueteManagement.UI.Admin.Models.Enums;

// Local mirror of the API's QuestionMediaType. Kept in sync by value: this enum
// crosses the HTTP boundary as a numeric JSON value, so the backing numbers must
// match the API's enum exactly. Mirrors UI-User/Models/Survey/QuestionMediaType.cs.
public enum QuestionMediaType : byte
{
    Image = 1,
    Video,
    Audio
}
