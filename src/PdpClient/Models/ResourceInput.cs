namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represents a resource to be authorized.
/// </summary>
/// <param name="Type"></param>
/// <param name="Key"></param>
/// <param name="Tenant"></param>
/// <param name="Attributes"></param>
/// <param name="Context"></param>
public record ResourceInput(
    string Type,
    string? Key = null, 
    string? Tenant = null,
    Dictionary<string, object>? Attributes = null,
    Dictionary<string, object>? Context = null);