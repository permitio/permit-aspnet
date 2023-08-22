using System.Diagnostics.CodeAnalysis;

namespace PermitSDK.AspNet;

/// <summary>
/// Class to configure global providers
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PermitProvidersOptionsConfiguration
{
    private readonly PermitProvidersOptions _options;

    internal PermitProvidersOptionsConfiguration(PermitProvidersOptions options)
    {
        _options = options;
    }
    
    /// <summary>
    /// Set the type of <see cref="IPermitValueProvider"/> to use to get the resource key
    /// </summary>
    public PermitProvidersOptionsConfiguration WithGlobalResourceKeyProvider<TProvider>()
        where TProvider : class, IPermitValueProvider
    {
        _options.GlobalResourceKeyProviderType = typeof(TProvider);
        return this;
    }
    
    /// <summary>
    /// Set the type of <see cref="IPermitValueProvider"/> to use to get the tenant
    /// </summary>
    public PermitProvidersOptionsConfiguration WithGlobalTenantProvider<TProvider>()
        where TProvider : class, IPermitValueProvider
    {
        _options.GlobalTenantProviderType = typeof(TProvider);
        return this;
    }
    
    /// <summary>
    /// Set the type of <see cref="IPermitValuesProvider"/> to use to get the attributes
    /// </summary>
    public PermitProvidersOptionsConfiguration WithGlobalAttributesProvider<TProvider>()
        where TProvider : class, IPermitValuesProvider
    {
        _options.GlobalAttributesProviderType = typeof(TProvider);
        return this;
    }
    
    /// <summary>
    /// Set the type of <see cref="IPermitValuesProvider"/> to use to get the context
    /// </summary>
    public PermitProvidersOptionsConfiguration WithGlobalContextProvider<TProvider>()
        where TProvider : class, IPermitValuesProvider
    {
        _options.GlobalContextProviderType = typeof(TProvider);
        return this;
    }
    
    /// <summary>
    /// Set the type of <see cref="IPermitUserKeyProvider"/> to use to get the user key
    /// </summary>
    public PermitProvidersOptionsConfiguration WithGlobalUserKeyProvider<TProvider>()
        where TProvider : class, IPermitUserKeyProvider
    {
        _options.GlobalUserKeyProviderType = typeof(TProvider);
        return this;
    }
}