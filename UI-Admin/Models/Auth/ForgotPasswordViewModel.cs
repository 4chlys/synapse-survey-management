using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record ForgotPasswordViewModel(
    [Required(ErrorMessage = "Auth_FieldRequired")]
    [EmailAddress(ErrorMessage = "Auth_InvalidEmail")]
    string Email);
