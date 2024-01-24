using Microsoft.AspNetCore.Http;
using PermitSDK.AspNet.PdpClient.Models;

namespace PermitSDK.AspNet;

/// <summary>
/// 
/// </summary>
public interface IResourceInputBuilder
{
    /// <summary>
    /// Create the <see cref="ResourceInput"/> instance.
    /// </summary>
    /// <returns></returns>
    Task<ResourceInput?> BuildAsync(PermitAttribute attribute, HttpContext httpContext);    
}
