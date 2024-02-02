namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represent the tenant a user has access to
/// </summary>
/// <param name="Key"></param>
/// <param name="Attributes"></param>
[Serializable]
public record GetPermissionResponse(string Key, Dictionary<string, object> Attributes);