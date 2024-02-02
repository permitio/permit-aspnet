namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represents the allowed response from the PDP
/// </summary>
/// <param name="Allow"></param>
/// <param name="Debug"></param>
[Serializable]
public record AllowedResponse(bool Allow, AllowedResponseDebug? Debug);

/// <summary>
/// Represents the debug information in an allowed response 
/// </summary>
/// <param name="Rbac"></param>
[Serializable]
public record AllowedResponseDebug(AllowedResponseRbac? Rbac);

/// <summary>
/// Represents the RBAC information in an allowed response
/// </summary>
/// <param name="Reason"></param>
[Serializable]
public record AllowedResponseRbac(string Reason);