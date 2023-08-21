namespace PermitSDK.AspNet;

/// <summary>
/// Configuration for the Permit providers
/// </summary>
public class PermitProvidersOptions
{
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
}