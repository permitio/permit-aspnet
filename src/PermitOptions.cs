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
}