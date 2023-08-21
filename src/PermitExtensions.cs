using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PermitSDK.Models;

namespace PermitSDK.AspNet;

/// <summary>
/// Extension methods for the Permit middleware.
/// </summary>
public static class PermitExtensions
{
    /// <summary>
    /// Register the Permit middleware.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configurationSection">Configuration section for Permit SDK options</param>
    /// <param name="configureProviders">Function to configure global providers</param>
    /// <returns></returns>
    public static IServiceCollection AddPermit(this IServiceCollection services,
        IConfigurationSection configurationSection,
        Action<PermitProvidersOptionsConfiguration>? configureProviders = null)
    {
        var options = new PermitOptions();
        configurationSection.Bind(options);
        return services.AddPermit(options, configureProviders);
    }

    /// <summary>
    /// Register the Permit middleware.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Permit SDK options</param>
    /// <param name="configureProviders">Function to configure global providers</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    public static IServiceCollection AddPermit(this IServiceCollection services,
        PermitOptions options,
        Action<PermitProvidersOptionsConfiguration>? configureProviders = null)
    {
        if (options.ApiKey == null)
        {
            throw new InvalidOperationException("API key not set.");
        }
        
        var providersOptions = new PermitProvidersOptions();
        configureProviders?.Invoke(new PermitProvidersOptionsConfiguration(providersOptions));
        var serviceOptions = new PermitServiceOptions(options, providersOptions);
        return services.AddSingleton(serviceOptions);
    }

    /// <summary>
    /// Use the Permit middleware.
    /// </summary>
    /// <param name="applicationBuilder">Application builder</param>
    /// <returns></returns>
    public static IApplicationBuilder UsePermit(
        this IApplicationBuilder applicationBuilder)
    {
        var serviceOptions = applicationBuilder.ApplicationServices.GetService<PermitServiceOptions>();
        if (serviceOptions == null)
        {
            throw new InvalidOperationException("Permit middleware not registered.");
        }
        
        return applicationBuilder.UseMiddleware<PermitMiddleware>(
            serviceOptions.Options, serviceOptions.ProvidersOptions);
    }
    
    internal static Task<UserKey?> GetProviderUserKey(this IServiceProvider serviceProvider,
        HttpContext httpContext, Type providerType)
    { 
        return serviceProvider.RunProviderAsync<IPermitUserKeyProvider, UserKey>(
            providerType, provider => provider.GetUserKeyAsync(httpContext));
    }
    
    internal static Task<string?> GetProviderValue(this IServiceProvider serviceProvider,
        HttpContext httpContext, Type providerType)
    {
        return serviceProvider.RunProviderAsync<IPermitValueProvider, string>(
            providerType, provider => provider.GetValueAsync(httpContext));
    }

    internal static Task<Dictionary<string, object>?> GetProviderValues(this IServiceProvider serviceProvider,
        HttpContext httpContext, Type providerType)
    {
        return serviceProvider.RunProviderAsync<IPermitValuesProvider, Dictionary<string, object>>(
            providerType, provider => provider.GetValues(httpContext));
    }
    
    private static async Task<TResult?> RunProviderAsync<TProvider, TResult>(
        this IServiceProvider serviceProvider,
        Type providerType,
        Func<TProvider, Task<TResult>> getValues)
        where TProvider : class
        where TResult : class
    {
        // If the provider type is not assignable to the interface, abort
        if (!typeof(TProvider).IsAssignableFrom(providerType))
        {
            return null;
        }

        var constructor = providerType.GetConstructor(Type.EmptyTypes);
        var hasParameterlessConstructor = constructor != null;

        // If the provider type has a parameterless constructor, use it directly
        // Otherwise, use the service provider to create an instance
        var provider = hasParameterlessConstructor
            ? Activator.CreateInstance(providerType) as TProvider
            : serviceProvider.GetService(providerType) as TProvider;
        if (provider == null)
        {
            return null;
        }

        try
        {
            return await getValues(provider);
        }
        catch (Exception)
        {
            return null;
        }
    }
}