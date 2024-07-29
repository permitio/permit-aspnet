using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using PermitSDK.AspNet.Services;

namespace PermitSDK.AspNet;

internal class ResourceInputBuilder : IResourceInputBuilder
{
    private readonly PermitOptions _options;
    private bool _isFailed;
    private string? _resourceKey;
    private string? _tenant;
    private Dictionary<string, object>? _attributes;
    private Dictionary<string, object>? _context;

    private readonly IServiceProvider _serviceProvider;

    public ResourceInputBuilder(
        PermitOptions options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _tenant = options.UseDefaultTenantIfEmpty
            ? options.DefaultTenant
            : null;
    }

    public async Task<Resource?> BuildAsync(IPermitData data, HttpContext httpContext)
    {
        await TryAppendResourceKeyAsync();
        await TryAppendTenantAsync();
        await TryAppendAttributesAsync();
        await TryAppendContextAsync();

        if (_isFailed)
        {
            return null;
        }

        return new Resource(
            _attributes,
            _context,
            _resourceKey,
            _tenant,
            data.ResourceType);

        async Task TryAppendResourceKeyAsync()
        {
            var (isSpecified, resourceKey) = await GetValueAsync(
                data.ResourceKey,
                data.ResourceKeyFromRoute,
                data.ResourceKeyFromHeader,
                data.ResourceKeyFromQuery,
                data.ResourceKeyFromClaim,
                data.ResourceKeyFromBody,
                data.ResourceKeyProviderType,
                _options.GlobalResourceKeyProviderType);

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
                data.Tenant,
                data.TenantFromRoute,
                data.TenantFromHeader,
                data.TenantFromQuery,
                data.TenantFromClaim,
                data.TenantFromBody,
                data.TenantProviderType,
                _options.GlobalTenantProviderType);

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
            string? fromQuery,
            string? fromClaim,
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
                var value = GetValueFromHeader(fromHeader);
                return (true, value);
            }

            if (!string.IsNullOrWhiteSpace(fromQuery))
            {
                var value = GetValueFromQuery(fromQuery);
                return (true, value);
            }

            if (!string.IsNullOrWhiteSpace(fromClaim))
            {
                var value = GetValueFromClaim(fromClaim);
                return (true, value);
            }

            if (!string.IsNullOrWhiteSpace(fromBody))
            {
                return (true, await GetValueFromBody(fromBody));
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
            if (_isFailed || (data.AttributesProviderType == null && _options.GlobalAttributesProviderType == null))
            {
                return;
            }

            var providerType = data.AttributesProviderType ?? _options.GlobalAttributesProviderType;
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
            if (_isFailed || (data.ContextProviderType == null && _options.GlobalContextProviderType == null))
            {
                return;
            }

            var providerType = data.ContextProviderType ?? _options.GlobalContextProviderType;
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

        string? GetValueFromQuery(string queryParameter)
        {
            _ = httpContext.Request.Query
                .TryGetValue(queryParameter, out var value);
            return value;
        }

        string? GetValueFromClaim(string claimType)
        {
            return httpContext.User.FindFirst(claimType)?.Value;
        }

        async Task<string?> GetValueFromBody(string jsonPropertyPath)
        {
            string requestBody;

            using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;
            }

            var jsonDocument = JsonDocument.Parse(requestBody);
            var node = GetJsonElement(jsonDocument.RootElement, jsonPropertyPath);
            return GetJsonElementValue(node);
        }

        static JsonElement GetJsonElement(JsonElement jsonElement, string path)
        {
            if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return default;
            }

            var segments = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in segments)
            {
                jsonElement = jsonElement.TryGetProperty(t, out var value) ? value : default;

                if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    return default;
                }
            }

            return jsonElement;
        }

        static string? GetJsonElementValue(JsonElement jsonElement)
        {
            return
                jsonElement.ValueKind != JsonValueKind.Null &&
                jsonElement.ValueKind != JsonValueKind.Undefined
                    ? jsonElement.ToString()
                    : default;
        }
    }
}