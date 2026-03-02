using System.Net.Mime;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class TextComment(string text, ModerationStatus moderationStatus, Idea idea) : Comment(idea)
{
    public string Text { get; set; } = text;
    public ModerationStatus ModerationStatus { get; set; } = moderationStatus;
}