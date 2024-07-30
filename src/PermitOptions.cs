using System.Diagnostics.CodeAnalysis;

namespace PermitSDK.AspNet;

/// <summary>
/// Permit SDK options
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class PermitOptions
{
    /// <summary>
    /// Token to access Permit APIs
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The URL of the PDP
    /// </summary>
    public string PdpUrl { get; set; } = "http://localhost:7766";

    /// <summary>
    /// Default tenant
    /// </summary>
    public string DefaultTenant { get; set; } = "default";

    /// <summary>
    /// If true, the default tenant will be used if the tenant is not specified in the request 
    /// </summary>
    public bool UseDefaultTenantIfEmpty { get; set; } = true;

    /// <summary>
    /// Get or set the type of <see cref="IPermitValueProvider"/> to use to get the resource key
    /// </summary>
    public Type? GlobalResourceKeyProviderType { get; set; }

    /// <summary>
    /// Get or set the type of <see cref="IPermitValueProvider"/> to use to get the tenant
    /// </summary>
    public Type? GlobalTenantProviderType { get; set; }

    /// <summary>
    /// Get or set the type of <see cref="IPermitValuesProvider"/> to use to get the attributes
    /// </summary>
    public Type? GlobalAttributesProviderType { get; set; }

    /// <summary>
    /// Get or set the type of <see cref="IPermitValuesProvider"/> to use to get the context
    /// </summary>
    public Type? GlobalContextProviderType { get; set; }

    /// <summary>
    /// Get or set the type of <see cref="IPermitUserKeyProvider"/> to use to get the user key
    /// </summary>
    public Type? GlobalUserKeyProviderType { get; set; }

    /// <summary>
    /// Function called before any request is sent to the PDP
    /// </summary>
    public Func<HttpRequestMessage, Task>? BeforeSendCallbackAsync { get; set; }
}