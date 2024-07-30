using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PermitSDK.AspNet.Services;

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
    /// <param name="configuration">Configuration reference</param>
    /// <param name="configureOptions">Function to configure global providers</param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddPermit(this IServiceCollection services,
        IConfiguration configuration,
        Action<PermitOptions>? configureOptions = null)
    {
        var options = new PermitOptions();
        configuration.GetSection("Permit").Bind(options);
        configureOptions?.Invoke(options);
        return services.AddPermit(options);
    }

    /// <summary>
    /// Register the Permit middleware.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Permit SDK options</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddPermit(this IServiceCollection services,
        PermitOptions options)
    {
        if (options.ApiKey == null)
        {
            throw new InvalidOperationException("API key not set.");
        }

        services
            .AddSingleton(options);

        var httpClientBuilder = services.AddHttpClient<PdpService>(client =>
        {
            client.BaseAddress = new Uri(options.PdpUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            client.DefaultRequestHeaders.Add("x-permit-sdk-language", "permitio-aspnet-sdk");
        });

        if (options.BeforeSendCallbackAsync != null)
        {
            httpClientBuilder.AddHttpMessageHandler(_ =>
                new InternalDelegatingHandler(options.BeforeSendCallbackAsync));
        }

        return services;
    }

    /// <summary>
    /// Use the Permit middleware.
    /// </summary>
    /// <param name="applicationBuilder">Application builder</param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UsePermit(
        this IApplicationBuilder applicationBuilder)
    {
        var options = applicationBuilder.ApplicationServices.GetService<PermitOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("Permit middleware not registered.");
        }

        Func<IResourceInputBuilder> resourceInputBuilderFactory =
            () => new ResourceInputBuilder(options);
        var pdp = applicationBuilder.ApplicationServices.GetService<PdpService>();
        var logger = applicationBuilder.ApplicationServices.GetService<ILogger<PermitMiddleware>>();

        return applicationBuilder.UseMiddleware<PermitMiddleware>(
            pdp, resourceInputBuilderFactory, options, logger);
    }

    internal static Task<User?> GetProviderUserKey(this HttpContext httpContext, Type providerType)
    {
        return RunProviderAsync<IPermitUserKeyProvider, User>(
            providerType, provider => provider.GetUserKeyAsync(httpContext), httpContext);
    }

    internal static Task<string?> GetProviderValue(this HttpContext httpContext, Type providerType)
    {
        return RunProviderAsync<IPermitValueProvider, string>(
            providerType, provider => provider.GetValueAsync(httpContext), httpContext);
    }

    internal static Task<Dictionary<string, object>?> GetProviderValues(this HttpContext httpContext, Type providerType)
    {
        return RunProviderAsync<IPermitValuesProvider, Dictionary<string, object>>(
            providerType, provider => provider.GetValues(httpContext), httpContext);
    }

    private static async Task<TResult?> RunProviderAsync<TProvider, TResult>(Type providerType,
        Func<TProvider, Task<TResult>> getValues, HttpContext httpContext)
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
            : httpContext.RequestServices.GetService(providerType) as TProvider;

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