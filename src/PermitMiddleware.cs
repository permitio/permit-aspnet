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
    private readonly Func<IResourceInputBuilder> _resourceInputBuilderFactory;
    private readonly PermitOptions _options;
    private readonly ILogger<PermitMiddleware> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Request delegate</param>
    /// <param name="pdpService">Service to call PDP endpoints</param>
    /// <param name="resourceInputBuilderFactory">Builder for resource input</param>
    /// <param name="options">Permit options</param>
    /// <param name="logger">Middleware logger</param>
    public PermitMiddleware(
        RequestDelegate next,
        PdpService pdpService,
        Func<IResourceInputBuilder> resourceInputBuilderFactory,
        PermitOptions options,
        ILogger<PermitMiddleware> logger)
    {
        _next = next;
        _pdpService = pdpService;
        _resourceInputBuilderFactory = resourceInputBuilderFactory;
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

        //With an intention to read request multiple times, enable buffering
        httpContext.Request.EnableBuffering();

        // Get user key
        var userKey = await GetUserKeyAsync(httpContext);
        if (userKey == null)
        {
            _logger.LogTrace("User key not found.");
            await _next(httpContext);
            return;
        }

        var permitMetadataList = GetPermitMetadataList(endpoint);
        if (permitMetadataList.Count > 0)
        {

            var isEndpointAuthorized = await IsAuthorizedListAsync(httpContext, userKey, permitMetadataList);
            if (!isEndpointAuthorized)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
        }

        await _next(httpContext);
    }

    private static IReadOnlyList<IPermitData> GetPermitMetadataList(Endpoint endpoint)
    {
        // Minimal API
        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()
            is not { } actionDescriptor)
        {
            return endpoint.Metadata.GetOrderedMetadata<PermitData>(); 
        }

        // Controller
        var controllerAttributes = actionDescriptor.ControllerTypeInfo
            .GetCustomAttributes<PermitAttribute>(inherit: true);
        var actionAttributes = actionDescriptor.MethodInfo
            .GetCustomAttributes<PermitAttribute>(inherit: true);
        return controllerAttributes.Concat(actionAttributes).ToArray();
    }

    private async Task<bool> IsAuthorizedListAsync(HttpContext httpContext, User userKey,
        IEnumerable<IPermitData> permitDataList)
    {
        foreach (var data in permitDataList)
        {

            var isAuthorised = await IsAuthorizedAsync(httpContext, data, userKey);
            if (!isAuthorised)
            {
                return false;
            }

        }

        return true;
    }

    private async Task<User?> GetUserKeyAsync(HttpContext httpContext)
    {
        if (_options.GlobalUserKeyProviderType != null)
        {
            return await httpContext.GetProviderUserKey(_options.GlobalUserKeyProviderType);
        }

        var userId = ExtractUserId(httpContext);
        if (userId == null)
        {
            return null;
        }

        var firstName = httpContext.User.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = httpContext.User.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return new User(null, email, firstName, userId, lastName);
    }

    private static string? ExtractUserId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(UserIdClaimTypes.Subject)?.Value;
        if (userId != null)
            return userId;

        userId = httpContext.User.FindFirst(UserIdClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            return userId;

        userId = httpContext.User.FindFirst(UserIdClaimTypes.FullyQualifiedId)?.Value;
        if (userId != null)
            return userId;

        userId = httpContext.User.FindFirst(UserIdClaimTypes.ObjectIdentifier)?.Value;

        return userId;
    }

    private async Task<bool> IsAuthorizedAsync(HttpContext httpContext,
        IPermitData data, User userKey)
    {
        if (string.IsNullOrWhiteSpace(data.ResourceType))
        {
            return false;
        }

        var resourceInputBuilder = _resourceInputBuilderFactory();
        var resourceInput = await resourceInputBuilder.BuildAsync(data, httpContext);
        if (resourceInput == null)
        {
            return false;
        }
        
        var actions = data.Action.Split(',');
        foreach (var action in actions)
        {
            var isAllowed = await IsAllowedAsync(action, resourceInput, userKey);
            if (isAllowed)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsAllowedAsync(string action, Resource resourceInput, User userKey)
    {
        var request = new AuthorizationQuery(action, null, resourceInput, null, userKey);
        var response = await _pdpService.AllowedAsync(request);
        if (response.Debug is JsonElement debugNode &&
            debugNode.TryGetProperty("rbac", out var rbacNode) &&
            rbacNode.TryGetProperty("reason", out var reasonNode))
        {
            var reason = reasonNode.GetString();
            _logger.LogDebug("RBAC reason: {Reason}", reason); // response.Debug.Rbac.Reason);
        }

        return response.Allow ?? false;
    }
}