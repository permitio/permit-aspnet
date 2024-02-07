namespace PermitSDK.AspNet.PdpClient.Models;

/// <summary>
/// Represents the response item from a GetUserPermissions request.
/// </summary>
/// <param name="Resource"></param>
/// <param name="Tenant"></param>
/// <param name="Permissions"></param>
/// <param name="Roles"></param>
[Serializable]
public record UserPermissionResponseItem(
    GetTenantResponse? Resource,
    GetPermissionResponse? Tenant,
    string[] Permissions,
    string[] Roles);