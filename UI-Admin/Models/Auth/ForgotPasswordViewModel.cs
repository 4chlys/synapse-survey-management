using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public record ForgotPasswordViewModel([Required][EmailAddress] string Email);
