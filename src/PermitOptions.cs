using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace PermitSDK.AspNet;

[ExcludeFromCodeCoverage]
internal record PermitServiceOptions(PermitOptions Options, PermitProvidersOptions ProvidersOptions);

/// <summary>
/// Permit SDK options
/// </summary>
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
    public string PDP { get; set; } = "http://localhost:7766";
    
    /// <summary>
    /// Enable debug mode to get more information about the request
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// The log level
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Information;
}