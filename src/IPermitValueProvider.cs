using Microsoft.AspNetCore.Http;

namespace PermitSDK.AspNet;

/// <summary>
/// Provides a string from the current request.
/// </summary>
public interface IPermitValueProvider
{
    /// <summary>
    /// Extract a string from the current request.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    Task<string> GetValueAsync(HttpContext httpContext);
}