using Microsoft.AspNetCore.Http;

namespace PermitSDK.AspNet;

/// <summary>
/// Provides dictionary of values from the current request.
/// </summary>
public interface IPermitValuesProvider
{
    /// <summary>
    /// Extract a dictionary of values from the current request.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetValues(HttpContext httpContext);
}