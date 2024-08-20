using Microsoft.AspNetCore.Http;
using PermitSDK.AspNet.Abstractions;
using PermitSDK.AspNet.Services;

namespace PermitSDK.AspNet;

/// <summary>
/// 
/// </summary>
public interface IResourceInputBuilder
{
    /// <summary>
    /// Create the <see cref="Resource"/> instance.
    /// </summary>
    /// <returns></returns>
    Task<Resource?> BuildAsync(IPermitData data, HttpContext httpContext);    
}
