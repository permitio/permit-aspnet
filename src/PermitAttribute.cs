namespace PermitSDK.AspNet;

/// <summary>
/// Attribute to decorate controllers and actions with Permit permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermitAttribute : Attribute, IPermitData
{
    /// <summary>
    /// Create a Permit attribute.
    /// </summary>
    /// <param name="action">The key of the resource action</param>
    /// <param name="resourceType">The key of the resource type</param>
    public PermitAttribute(string action, string resourceType)
    {
        Action = action;
        ResourceType = resourceType;
    }

    /// <inheritdoc />
    public string Action { get; init; }
    
    /// <inheritdoc />
    public string ResourceType { get; }
    
    /// <inheritdoc />
    public string? Tenant { get; set; }
    
    /// <inheritdoc />
    public string? TenantFromRoute { get; set; }
    
    /// <inheritdoc />
    public string? TenantFromHeader { get; set; }
    
    /// <inheritdoc />
    public string? TenantFromBody { get; set; }
    
    /// <inheritdoc />
    public Type? TenantProviderType { get; set; }
    
    /// <inheritdoc />
    public string? ResourceKey { get; set; }
    
    /// <inheritdoc />
    public string? ResourceKeyFromRoute { get; set; }
    
    /// <inheritdoc />
    public string? ResourceKeyFromHeader { get; set; }
    
    /// <inheritdoc />
    public string? ResourceKeyFromBody { get; set; }
    
    /// <inheritdoc />
    public Type? ResourceKeyProviderType { get; set; }
    
    /// <inheritdoc />
    public Type? AttributesProviderType { get; set; }
    
    /// <inheritdoc />
    public Type? ContextProviderType { get; set; }
}