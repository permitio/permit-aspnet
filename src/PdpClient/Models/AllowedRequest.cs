namespace PermitSDK.AspNet.PdpClient.Models;

internal record AllowedRequest(
    UserKey User, 
    ResourceInput Resource, 
    string Action,
    Dictionary<string, string>? Context);