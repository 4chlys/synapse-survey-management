using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Email;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

public class EmailTemplateAdminService(HttpClient http)
{
    public async Task<EmailTemplateViewModel> GetAsync(int projectId)
    {
        return await http.GetFromJsonAsync<EmailTemplateViewModel>($"api/projects/{projectId}/email-template")
               ?? new EmailTemplateViewModel();
    }

    public async Task SaveAsync(int projectId, EmailTemplateViewModel vm)
    {
        await http.PutAsJsonAsync($"api/projects/{projectId}/email-template", vm);
    }
}
