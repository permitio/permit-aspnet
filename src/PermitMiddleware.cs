using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using PermitSDK.AspNet.Services;

namespace PermitSDK.AspNet;

/// <summary>
/// Run the Permit authorization process.
/// </summary>
public sealed class PermitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PdpService _pdpService;
    private readonly IResourceInputBuilder _resourceInputBuilder;
    private readonly PermitOptions _options;
    private readonly ILogger<PermitMiddleware> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Request delegate</param>
    /// <param name="pdpService">Service to call PDP endpoints</param>
    /// <param name="resourceInputBuilder">Builder for resource input</param>
    /// <param name="options">Permit options</param>
    /// <param name="logger">Middleware logger</param>
    public PermitMiddleware(
        RequestDelegate next,
        PdpService pdpService, 
        IResourceInputBuilder resourceInputBuilder,
        PermitOptions options,
        ILogger<PermitMiddleware> logger)
    {
        _next = next;
        _pdpService = pdpService;
        _resourceInputBuilder = resourceInputBuilder;
        _options = options;
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

    private async Task<User?> GetUserKeyAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        if (_options.GlobalUserKeyProviderType != null)
        {
            return await serviceProvider.GetProviderUserKey(httpContext, _options.GlobalUserKeyProviderType);
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return null;
        }

        var firstName = httpContext.User.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = httpContext.User.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return new User(null, email, firstName, userId, lastName);
    }

    private async Task<bool> IsAuthorizedAsync(HttpContext httpContext,
        IPermitData data, User userKey)
    {
        if (string.IsNullOrWhiteSpace(data.ResourceType))
        {
            return false;
        }

        var resourceInput = await _resourceInputBuilder.BuildAsync(data, httpContext);
        
        // Call PDP
        var request = new AuthorizationQuery(data.Action, null, resourceInput, null, userKey);
        var response = await _pdpService.AllowedAsync(request);
        if (response?.Debug is JsonElement debugNode && 
            debugNode.TryGetProperty("rbac", out var rbacNode) &&
            rbacNode.TryGetProperty("reason", out var reasonNode))
        {
            var reason = reasonNode.GetString();
            _logger.LogDebug("RBAC reason: {Reason}", reason); // response.Debug.Rbac.Reason);
        }
        return response?.Allow ?? false;
    }
}