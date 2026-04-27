using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public class LoginViewModel
{
    [Required] [EmailAddress] public string Email { get; set; }
    [Required] [DataType(DataType.Password)] public string Password { get; set; }
    public string ReturnUrl { get; set; }
}
