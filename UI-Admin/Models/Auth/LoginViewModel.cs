using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record LoginViewModel
{
    [Required] [EmailAddress] public string Email { get; init; }
    [Required] [DataType(DataType.Password)] public string Password { get; init; }
    public string ReturnUrl { get; init; }
}
