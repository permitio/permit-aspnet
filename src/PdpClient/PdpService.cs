using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PermitSDK.AspNet.PdpClient.Models;

namespace PermitSDK.AspNet.PdpClient;

/// <summary>
/// A client to call PDP endpoints
/// </summary>
public sealed class PdpService: IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdpService> _logger;
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Create a new instance of <see cref="PdpService"/>
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public PdpService(HttpClient httpClient, ILogger<PdpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
   
    /// <summary>
    /// Returns true if the user is allowed to perform the action on the resource.
    /// </summary>
    /// <param name="user">User info</param>
    /// <param name="action">The action</param>
    /// <param name="resource">The resource</param>
    /// <returns></returns>
    public async Task<AllowedResponse?> IsAllowedAsync(UserKey user, string action, ResourceInput resource)
    {
        var request = new AllowedRequest(user, resource, action, null);
        var httpResponse = await _httpClient.PostAsJsonAsync("allowed", request, SerializerOptions);
        return await HandleResponseAsync<AllowedResponse>(httpResponse);
    }

    /// <summary>
    /// Get the permissions a user has
    /// </summary>
    /// <param name="user">User info</param>
    /// <param name="tenants">Filter by tenants</param>
    /// <param name="resources">Filter by resources</param>
    /// <param name="resourceTypes">Filter by resource types</param>
    public async Task<Dictionary<string, UserPermissionResponseItem>?> GetUserPermissionsAsync(UserKey user, 
        IEnumerable<string>? tenants = null,
        IEnumerable<string>? resources = null,
        IEnumerable<string>? resourceTypes = null)
    {
        var request = new GetUserPermissionsRequest(user, tenants?.ToArray(), resources?.ToArray(), resourceTypes?.ToArray());
        var httpResponse = await _httpClient.PostAsJsonAsync("user-permissions", request, SerializerOptions);
        return await HandleResponseAsync<Dictionary<string, UserPermissionResponseItem>>(httpResponse);
    }

    /// <summary>
    /// Get the tenants a user can access to
    /// </summary>
    /// <param name="user">User info</param>
    public async Task<GetTenantResponse[]?> GetUserTenantsAsync(UserKey user)
    {
        var request = new GetUserTenantsRequest(user);
        var httpResponse = await _httpClient.PostAsJsonAsync("user-tenants", request, SerializerOptions);
        return await HandleResponseAsync<GetTenantResponse[]>(httpResponse);
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
 
    private async Task<TResponse?> HandleResponseAsync<TResponse>(HttpResponseMessage httpResponse)
        where TResponse : class
    {
        if (httpResponse is { IsSuccessStatusCode: true, StatusCode: HttpStatusCode.OK })
        {
            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(jsonResponse, SerializerOptions);
        }

        var errorMessage = await httpResponse.Content.ReadAsStringAsync();
        _logger.LogError("{Url} returned {StatusCode}: {Message}", 
            httpResponse.RequestMessage?.RequestUri?.AbsolutePath,
            httpResponse.StatusCode, errorMessage);
        return default;
    }
}