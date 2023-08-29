using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermitSDK.Models;

namespace PermitSDK.AspNet;

internal class ResourceInputBuilder : IResourceInputBuilder
{
    private readonly PermitProvidersOptions _permitProvidersOptions;
    private bool _isFailed;
    private string? _resourceKey;
    private string? _tenant;
    private Dictionary<string, object>? _attributes;
    private Dictionary<string, object>? _context;

    private readonly IServiceProvider _serviceProvider;

    public ResourceInputBuilder(
        PermitProvidersOptions permitProvidersOptions,
        IServiceProvider serviceProvider)
    {
        _permitProvidersOptions = permitProvidersOptions;
        _serviceProvider = serviceProvider;
    }

    public async Task<ResourceInput?> BuildAsync(PermitAttribute attribute, HttpContext httpContext)
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
            attribute.ResourceType,
            _resourceKey,
            _tenant,
            _attributes,
            _context);

        async Task TryAppendResourceKeyAsync()
        {
            var (isSpecified, resourceKey) = await GetValueAsync(
                attribute.ResourceKey,
                attribute.ResourceKeyFromRoute,
                attribute.ResourceKeyFromHeader,
                attribute.ResourceKeyFromBody,
                attribute.ResourceKeyProviderType,
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

        async Task TryAppendTenantAsync()
        {
            if (_isFailed)
            {
                return;
            }

            var (isSpecified, tenant) = await GetValueAsync(
                attribute.Tenant,
                attribute.TenantFromRoute,
                attribute.TenantFromHeader,
                attribute.TenantFromBody,
                attribute.TenantProviderType,
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

        async Task<(bool IsSpecified, string? Value)> GetValueAsync(
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
                var value = await _serviceProvider.GetProviderValue(httpContext, providerType);
                return (true, value);
            }

            if (globalProviderType != null)
            {
                var value = await _serviceProvider.GetProviderValue(httpContext, globalProviderType);
                return (true, value);
            }

            return (false, null);
        }

        async Task TryAppendAttributesAsync()
        {
            if (_isFailed || (attribute.AttributesProviderType == null && _permitProvidersOptions.GlobalAttributesProviderType == null))
            {
                return;
            }

            var providerType = attribute.AttributesProviderType ?? _permitProvidersOptions.GlobalAttributesProviderType;
            var attributes = await _serviceProvider.GetProviderValues(httpContext, providerType!);
            if (attributes == null)
            {
                _isFailed = true;
            }
            else
            {
                _attributes = attributes;
            }
        }

        async Task TryAppendContextAsync()
        {
            if (_isFailed || (attribute.ContextProviderType == null && _permitProvidersOptions.GlobalContextProviderType == null))
            {
                return;
            }

            var providerType = attribute.ContextProviderType ?? _permitProvidersOptions.GlobalContextProviderType;
            var context = await _serviceProvider.GetProviderValues(httpContext, providerType!);
            if (context == null)
            {
                _isFailed = true;
            }
            else
            {
                _context = context;
            }
        }

        string? GetValueFromRoute(string routeName)
        {
            return httpContext.GetRouteValue(routeName)?.ToString();
        }

        string? GetValueFromHeader(string headerName)
        {
            _ = httpContext.Request.Headers
                .TryGetValue(headerName, out var value);
            return value;
        }

        async Task<string?> GetValueFromBody(string jsonPropertyPath)
        {
            string requestBody;
            using (var reader = new StreamReader(httpContext.Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var jsonObject = JsonConvert.DeserializeObject<JObject>(requestBody);
            var propertyValue = jsonObject?.SelectToken(jsonPropertyPath);
            return propertyValue?.ToString();
        }
    }
}