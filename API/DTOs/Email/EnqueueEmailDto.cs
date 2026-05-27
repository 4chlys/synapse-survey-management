using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Email;

// What UI-Admin POSTs to api/admin/emails/queue. One DTO == one queue row ==
// one Resend send. The broadcast UI splits N participants into multiple POSTs
// (one per language × ≤49 BCC chunk).
//
// Category is a string (not the EmailJobCategory enum) because the API uses
// the default System.Text.Json setup, which binds enums from numbers only —
// sending the name as a string would 400. The controller parses it.
public record EnqueueEmailDto(
    [Required][EmailAddress][MaxLength(200)] string To,
    string[] Bcc,
    [EmailAddress][MaxLength(200)] string ReplyTo,
    [Required][MaxLength(200)] string Subject,
    [Required][MaxLength(8000)] string Body,
    [Required] string Category,
    [MaxLength(10)] string LanguageCode,
    int? ProjectPlatformId,
    int? ProjectId,
    int? IdeaId,
    [MaxLength(450)] string CreatedByUserId);
