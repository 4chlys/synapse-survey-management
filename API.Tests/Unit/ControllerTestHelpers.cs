namespace SynapseEnqueteManagement.API.Tests;

/// <summary>
/// Sets up <see cref="ControllerBase.ControllerContext"/> with an <c>X-Participant-Token</c>
/// header so <see cref="ParticipantControllerBase.GetParticipantAsync()"/> can resolve
/// the session token.
/// </summary>
internal static class ControllerTestHelpers
{
    public static void AttachHttpContext(this ControllerBase controller, Guid sessionToken)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Participant-Token"] = sessionToken.ToString();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
    }
}
