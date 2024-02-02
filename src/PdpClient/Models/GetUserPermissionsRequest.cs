using System.Text.Json.Serialization;

namespace PermitSDK.AspNet.PdpClient.Models;

[Serializable]
internal record GetUserPermissionsRequest(
    UserKey User,
    string[]? Tenants,
    string[]? Resources, 
    [property:JsonPropertyName("resource_types")] string[]? ResourceTypes);