using System.Diagnostics.CodeAnalysis;
using PermitSDK.Models;

namespace PermitSDK.AspNet;

/// <inheritdoc/>
[ExcludeFromCodeCoverage]
public class PermitProxy: IPermitProxy
{
    private readonly Permit _permit;

    /// Create a proxy instance from options
    public PermitProxy(PermitOptions options)
    {
        _permit = new Permit(
            options.ApiKey,
            options.PDP,
            options.DefaultTenant,
            options.UseDefaultTenantIfEmpty,
            options.DebugMode,
            options.ApiUrl,
            options.Level,
            options.Label,
            options.LogAsJson);
    }
    
    /// <inheritdoc/>
    public Task<bool> CheckAsync(UserKey user, string action, ResourceInput resource, Dictionary<string, string>? context = null)
    {
        return _permit.Check(user, action, resource, context);
    }
}