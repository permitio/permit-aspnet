namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represents the resource a user has access to
/// </summary>
/// <param name="Type"></param>
/// <param name="Key"></param>
/// <param name="Attributes"></param>
[Serializable]
public record GetTenantResponse(string Type, string Key, Dictionary<string, object> Attributes);