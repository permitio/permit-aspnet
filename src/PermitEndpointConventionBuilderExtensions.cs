using Microsoft.AspNetCore.Builder;

namespace PermitSDK.AspNet;

/// <summary>
/// Permit extension methods for <see cref="IEndpointConventionBuilder"/>. 
/// </summary>
public static class PermitEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Add Permit authorization to the endpoint.
    /// </summary>
    /// <param name="builder">Endpoint builder</param>
    /// <param name="action">The key of the resource action</param>
    /// <param name="resourceType">The key of the resource type</param>
    /// <param name="configurePolicy">Action to configure the permissions check</param>
    /// <typeparam name="TBuilder"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static TBuilder RequirePermit<TBuilder>(this TBuilder builder,
        string action, string resourceType, Action<IPermitData>? configurePolicy = null) where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var attribute = new PermitAttribute(action, resourceType);
        configurePolicy?.Invoke(attribute);
        builder.RequirePermitCore(attribute);
        return builder;
    }

    private static void RequirePermitCore<TBuilder>(this TBuilder builder, IPermitData data)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(data);
        });
    }
}