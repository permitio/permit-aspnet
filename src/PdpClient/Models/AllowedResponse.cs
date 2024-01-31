namespace PermitSDK.AspNet.PdpClient.Models;

[Serializable]
internal record AllowedResponse(bool Allow, AllowedResponseDebug? Debug);

[Serializable]
internal record AllowedResponseDebug(AllowedResponseRbac? Rbac);

[Serializable]
internal record AllowedResponseRbac(string Reason);