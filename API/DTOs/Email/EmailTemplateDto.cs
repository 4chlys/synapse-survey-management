namespace SynapseEnqueteManagement.API.DTOs.Email;

public class EmailTemplateDto
{
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsEnabled { get; set; }
}
