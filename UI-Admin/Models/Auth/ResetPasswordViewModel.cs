using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record ResetPasswordViewModel
{
    [Required] public string Email { get; init; } = string.Empty;
    [Required] public string Token { get; init; } = string.Empty;
    [Required][DataType(DataType.Password)][StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; init; } = string.Empty;
    [Required][DataType(DataType.Password)][Compare(nameof(NewPassword), ErrorMessage = "Wachtwoorden komen niet overeen.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
