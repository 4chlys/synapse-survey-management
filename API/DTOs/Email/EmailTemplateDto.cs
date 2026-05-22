namespace SynapseEnqueteManagement.API.DTOs.Email;

public record EmailTemplateDto(string Subject, string Body, bool IsEnabled);
