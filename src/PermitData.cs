namespace PermitSDK.AspNet;

/// <inheritdoc cref="IPermitData" />
public class PermitData(string action, string resourceType) : IPermitData
{
    /// <inheritdoc />
    public string Action => action;

    /// <inheritdoc />
    public string ResourceType => resourceType;

    /// <inheritdoc />
    public string? Tenant { get; set; }

    /// <inheritdoc />
    public string? TenantFromRoute { get; set; }

    /// <inheritdoc />
    public string? TenantFromHeader { get; set; }

    /// <inheritdoc />
    public string? TenantFromQuery { get; set; }

    /// <inheritdoc />
    public string? TenantFromClaim { get; set; }

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
    public string? ResourceKeyFromQuery { get; set; }

    /// <inheritdoc />
    public string? ResourceKeyFromClaim { get; set; }

    /// <inheritdoc />
    public string? ResourceKeyFromBody { get; set; }

    /// <inheritdoc />
    public Type? ResourceKeyProviderType { get; set; }

    /// <inheritdoc />
    public Type? AttributesProviderType { get; set; }

    /// <inheritdoc />
    public Type? ContextProviderType { get; set; }
}