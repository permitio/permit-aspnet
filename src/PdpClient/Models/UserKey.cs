namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represent the user in an authorization request.
/// </summary>
/// <param name="Key"></param>
/// <param name="FirstName"></param>
/// <param name="LastName"></param>
/// <param name="Email"></param>
/// <param name="Attributes"></param>
public record UserKey(string Key,
    string? FirstName = null,
    string? LastName =  null, 
    string? Email = null,
    Dictionary<string, object>? Attributes = null);