namespace PermitSDK.AspNet;

internal record PermitServiceOptions(PermitOptions Options, PermitProvidersOptions ProvidersOptions);

/// <summary>
/// Permit SDK options
/// </summary>
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
    /// Default tenant
    /// </summary>
    public string DefaultTenant { get; set; } = "default";
    /// <summary>
    /// If true, the default tenant will be used if the tenant is not specified in the request
    /// </summary>
    public bool UseDefaultTenantIfEmpty { get; set; } = true;
    /// <summary>
    /// Enable debug mode to get more information about the request
    /// </summary>
    public bool DebugMode { get; set; } = false;
    /// <summary>
    /// The URL of the Permit API
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.permit.io";
    /// <summary>
    /// The log level
    /// </summary>
    public string Level { get; set; } = "info";
    /// <summary>
    /// The label on the logger
    /// </summary>
    public string Label { get; set; } = "permitio-sdk";
    /// <summary>
    /// If true, the logger will log as JSON
    /// </summary>
    public bool LogAsJson { get; set; } = false;
}