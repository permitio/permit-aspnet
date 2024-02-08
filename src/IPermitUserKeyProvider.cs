using Microsoft.AspNetCore.Http;
using PermitSDK.AspNet.Services;

namespace PermitSDK.AspNet;

/// <summary>
/// Provides the user key from the current request.
/// </summary>
public interface IPermitUserKeyProvider
{
    /// <summary>
    /// Return the user key for the current request.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    Task<User> GetUserKeyAsync(HttpContext httpContext);
}