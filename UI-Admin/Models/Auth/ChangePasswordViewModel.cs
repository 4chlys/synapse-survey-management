using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record ChangePasswordViewModel
{
    [Required(ErrorMessage = "Auth_FieldRequired")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Auth_FieldRequired")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Auth_PasswordTooShort")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Auth_FieldRequired")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Auth_PasswordsDontMatch")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
