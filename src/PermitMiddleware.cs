using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using PermitSDK.AspNet.PdpClient;
using PermitSDK.AspNet.PdpClient.Models;

namespace PermitSDK.AspNet;

/// <summary>
/// Run the Permit authorization process.
/// </summary>
public sealed class PermitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PdpService _pdpService;
    private readonly IResourceInputBuilder _resourceInputBuilder;
    private readonly PermitProvidersOptions _permitProvidersOptions;
    private readonly ILogger<PermitMiddleware> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Request delegate</param>
    /// <param name="pdpService">Service to call PDP endpoints</param>
    /// <param name="resourceInputBuilder">Builder for resource input</param>
    /// <param name="permitProvidersOptions">Function to configure global providers</param>
    /// <param name="logger">Middleware logger</param>
    public PermitMiddleware(
        RequestDelegate next,
        PdpService pdpService, 
        IResourceInputBuilder resourceInputBuilder,
        PermitProvidersOptions permitProvidersOptions,
        ILogger<PermitMiddleware> logger)
    {
        _next = next;
        _pdpService = pdpService;
        _resourceInputBuilder = resourceInputBuilder;
        _permitProvidersOptions = permitProvidersOptions;        
        _logger = logger;
    }
    
    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="serviceProvider"></param>
    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        // Get the endpoint information
        var endpoint = httpContext.GetEndpoint();
        if (endpoint == null)
        {
            await _next(httpContext);
            return;
        }

        // Get user key
        var userKey = await GetUserKeyAsync(httpContext, serviceProvider);
        if (userKey == null)
        {
            _logger.LogTrace("User key not found.");
            await _next(httpContext);
            return;
        }
        
        var permitMedata = GetPermitEndpointMetadata(endpoint);
        foreach (var data in permitMedata)
        {
            var controllerPermitted = await IsAuthorizedAsync(httpContext, data, userKey);
            if (controllerPermitted) continue;
            httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        await _next(httpContext);
    }

    private static IEnumerable<IPermitData> GetPermitEndpointMetadata(Endpoint endpoint)
    {
        var permitData = endpoint.Metadata.GetOrderedMetadata<IPermitData>();
        if (permitData.Any())
        {
            return permitData;
        }

        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()
            is not { } actionDescriptor)
        {
            return Array.Empty<IPermitData>();
        }

        var controllerAttributes = actionDescriptor.ControllerTypeInfo
            .GetCustomAttributes<PermitAttribute>(inherit: true);
        var actionAttributes = actionDescriptor.MethodInfo
            .GetCustomAttributes<PermitAttribute>(inherit: true);
        return controllerAttributes.Concat(actionAttributes).ToArray();
    }

    private async Task<UserKey?> GetUserKeyAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        if (_permitProvidersOptions.GlobalUserKeyProviderType != null)
        {
            return await serviceProvider.GetProviderUserKey(httpContext, _permitProvidersOptions.GlobalUserKeyProviderType);
        }
        
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return null;
        }

        var firstName = httpContext.User.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = httpContext.User.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return new UserKey(userId, firstName, lastName, email);
    }

    private async Task<bool> IsAuthorizedAsync(HttpContext httpContext,
        IPermitData data, UserKey userKey)
    {
        if (string.IsNullOrWhiteSpace(data.ResourceType))
        {
            return false;
        }

        var resourceInput = await _resourceInputBuilder.BuildAsync(data, httpContext);
        
        // Call PDP
        return await _pdpService.AllowAsync(userKey, data.Action, resourceInput!);
    }
}