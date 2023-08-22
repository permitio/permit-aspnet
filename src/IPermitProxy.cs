using PermitSDK.Models;

namespace PermitSDK.AspNet;

/// <summary>
/// Wrapper for the Permit SDK. Used for testing.
/// </summary>
public interface IPermitProxy
{
    /// <summary>
    /// Run the Permit authorization process
    /// </summary>
    /// <param name="user"></param>
    /// <param name="action"></param>
    /// <param name="resource"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<bool> CheckAsync(
        UserKey user,
        string action,
        ResourceInput resource,
        Dictionary<string, string>? context = null);
}