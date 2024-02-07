namespace PermitSDK.AspNet;

/// <summary>
/// Describe how a resource is protected 
/// </summary>
public interface IPermitData
{
    /// <summary>
    /// Get the key of the resource action
    /// </summary>
    public string Action { get; }
    
    /// <summary>
    /// Get the key of the resource type
    /// </summary>
    public string ResourceType { get; }
    
    /// <summary>
    /// Get or set tenant to use for the authorization.
    /// </summary>
    public string? Tenant { get; set; }
    
    /// <summary>
    /// Get or set the route parameter to use as tenant.
    /// </summary>
    public string? TenantFromRoute { get; set; }
    
    /// <summary>
    /// Get or set the header to use as tenant.
    /// </summary>
    public string? TenantFromHeader { get; set; }
    
    /// <summary>
    /// Get or set the property path to get the tenant
    /// <example><code>tenant.key</code></example>
    /// </summary>
    public string? TenantFromBody { get; set; }
    
    /// <summary>
    /// Get or set the type of <see cref="IPermitValueProvider"/> to use to get the tenant
    /// </summary>
    public Type? TenantProviderType { get; set; }
    
    /// <summary>
    /// Get or set resource key to use for the authorization.
    /// </summary>
    public string? ResourceKey { get; set; }
    
    /// <summary>
    /// Get or set the route parameter to use as resource key.
    /// </summary>
    public string? ResourceKeyFromRoute { get; set; }
    
    /// <summary>
    /// Get or set the header to use as resource key.
    /// </summary>
    public string? ResourceKeyFromHeader { get; set; }
    
    /// <summary>
    /// Get or set the property path to get the resource key
    /// <example><code>resource.key</code></example>
    /// </summary>
    public string? ResourceKeyFromBody { get; set; }
    
    /// <summary>
    /// Get or set the type of <see cref="IPermitValueProvider"/> to use to get the resource key
    /// </summary>
    public Type? ResourceKeyProviderType { get; set; }
    
    /// <summary>
    /// Get or set the type of <see cref="IPermitValuesProvider"/> to use to get the attributes
    /// </summary>
    public Type? AttributesProviderType { get; set; }
    
    /// <summary>
    /// Get or set the type of <see cref="IPermitValuesProvider"/> to use to get the context
    /// </summary>
    public Type? ContextProviderType { get; set; }
}