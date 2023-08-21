using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermitSDK.Models;

namespace PermitSDK.AspNet;

internal class ResourceInputBuilder
{
    private readonly PermitProvidersOptions _permitProvidersOptions;
    private bool _isFailed;
    private string? _resourceKey;
    private string? _tenant;
    private Dictionary<string, object>? _attributes;
    private Dictionary<string, object>? _context;

    private readonly HttpContext _httpContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly PermitAttribute _attribute;

    public ResourceInputBuilder(
        PermitProvidersOptions permitProvidersOptions,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        PermitAttribute attribute)
    {
        _permitProvidersOptions = permitProvidersOptions;
        _httpContext = httpContext;
        _serviceProvider = serviceProvider;
        _attribute = attribute;
    }

    public async Task<ResourceInput?> BuildAsync()
    {
        await TryAppendResourceKeyAsync();
        await TryAppendTenantAsync();
        await TryAppendAttributesAsync();
        await TryAppendContextAsync();

        if (_isFailed)
        {
            return null;
        }

        return new ResourceInput(
            _attribute.ResourceType,
            _resourceKey,
            _tenant,
            _attributes,
            _context);
    }

    private async Task TryAppendResourceKeyAsync()
    {
        var (isSpecified, resourceKey) = await GetValueAsync(
            _attribute.ResourceKey,
            _attribute.ResourceKeyFromRoute,
            _attribute.ResourceKeyFromHeader,
            _attribute.ResourceKeyFromBody,
            _attribute.ResourceKeyProviderType,
            _permitProvidersOptions.GlobalResourceKeyProviderType);

        if (isSpecified && resourceKey == null)
        {
            _isFailed = true;
        }
        else if (resourceKey != null)
        {
            _resourceKey = resourceKey;
        }
    }

    private async Task TryAppendTenantAsync()
    {
        if (_isFailed)
        {
            return;
        }

        var (isSpecified, tenant) = await GetValueAsync(
            _attribute.Tenant,
            _attribute.TenantFromRoute,
            _attribute.TenantFromHeader,
            _attribute.TenantFromBody,
            _attribute.TenantProviderType,
            _permitProvidersOptions.GlobalTenantProviderType);

        if (isSpecified && tenant == null)
        {
            _isFailed = true;
        }
        else if (tenant != null)
        {
            _tenant = tenant;
        }
    }

    private async Task<(bool IsSpecified, string? Value)> GetValueAsync(
        string? staticValue,
        string? fromRoute,
        string? fromHeader,
        string? fromBody,
        Type? providerType,
        Type? globalProviderType)
    {
        if (!string.IsNullOrWhiteSpace(staticValue))
        {
            return (true, staticValue);
        }

        if (!string.IsNullOrWhiteSpace(fromRoute))
        {
            return (true, GetValueFromRoute(fromRoute));
        }

        if (!string.IsNullOrWhiteSpace(fromHeader))
        {
            var value = await GetValueFromBody(fromHeader);
            return (true, value);
        }

        if (!string.IsNullOrWhiteSpace(fromBody))
        {
            return (true, GetValueFromHeader(fromBody));
        }

        if (providerType != null)
        {
            var value = await _serviceProvider.GetProviderValue(_httpContext, providerType);
            return (true, value);
        }
        
        if (globalProviderType != null)
        {
            var value = await _serviceProvider.GetProviderValue(_httpContext, globalProviderType);
            return (true, value);
        }

        return (false, null);
    }

    private async Task TryAppendAttributesAsync()
    {
        if (_isFailed || (_attribute.AttributesProviderType == null && _permitProvidersOptions.GlobalAttributesProviderType == null))
        {
            return;
        }
        
        var providerType = _attribute.AttributesProviderType ?? _permitProvidersOptions.GlobalAttributesProviderType;
        var attributes = await _serviceProvider.GetProviderValues(_httpContext, providerType!);
        if (attributes == null)
        {
            _isFailed = true;
        }
        else
        {
            _attributes = attributes;
        }
    }

    private async Task TryAppendContextAsync()
    {
        if (_isFailed || (_attribute.ContextProviderType == null && _permitProvidersOptions.GlobalContextProviderType == null))
        {
            return;
        }
        
        var providerType = _attribute.ContextProviderType ?? _permitProvidersOptions.GlobalContextProviderType;
        var context = await _serviceProvider.GetProviderValues(_httpContext, providerType!);
        if (context == null)
        {
            _isFailed = true;
        }
        else
        {
            _context = context;
        }
    }

    private string? GetValueFromRoute(string routeName)
    {
        return _httpContext.GetRouteValue(routeName)?.ToString();
    }

    private string? GetValueFromHeader(string headerName)
    {
        _ = _httpContext.Request.Headers
            .TryGetValue(headerName, out var value);
        return value;
    }

    private async Task<string?> GetValueFromBody(string jsonPropertyPath)
    {
        string requestBody;
        using (var reader = new StreamReader(_httpContext.Request.Body))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        var jsonObject = JsonConvert.DeserializeObject<JObject>(requestBody);
        var propertyValue = jsonObject?.SelectToken(jsonPropertyPath);
        return propertyValue?.ToString();
    }
}