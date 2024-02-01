using System.Diagnostics.CodeAnalysis;

namespace PermitSDK.AspNet;

[ExcludeFromCodeCoverage]
internal record PermitServiceOptions(PermitOptions Options, PermitProvidersOptions ProvidersOptions);

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
}