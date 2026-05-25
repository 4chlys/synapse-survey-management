using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record LoginViewModel
{
    [Required(ErrorMessage = "Auth_FieldRequired")]
    [EmailAddress(ErrorMessage = "Auth_InvalidEmail")]
    public string Email { get; init; }
    [Required(ErrorMessage = "Auth_FieldRequired")]
    [DataType(DataType.Password)]
    public string Password { get; init; }
    public string ReturnUrl { get; init; }
}
