using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Email;

// What UI-Admin POSTs to api/admin/emails/queue. One DTO == one queue row ==
// one Resend send. The broadcast UI splits N participants into multiple POSTs
// (one per language × ≤49 BCC chunk).
public record EnqueueEmailDto(
    [Required][EmailAddress][MaxLength(200)] string To,
    string[] Bcc,
    [EmailAddress][MaxLength(200)] string ReplyTo,
    [Required][MaxLength(200)] string Subject,
    [Required][MaxLength(8000)] string Body,
    EmailJobCategory Category,
    [MaxLength(10)] string LanguageCode,
    int? ProjectPlatformId,
    int? ProjectId,
    int? IdeaId,
    [MaxLength(450)] string CreatedByUserId);
