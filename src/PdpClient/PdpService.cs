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
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Create a new instance of <see cref="PdpService"/>
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public PdpService( HttpClient httpClient, ILogger<PdpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
   
    /// <summary>
    /// Returns true if the user is allowed to perform the action on the resource.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="action"></param>
    /// <param name="resource"></param>
    /// <returns></returns>
    public async Task<bool> AllowAsync(UserKey user, string action, ResourceInput resource)
    {
        var body = new AllowedRequest(user, resource, action, null);
        var httpResponse = await _httpClient.PostAsJsonAsync("allowed", body, SerializerOptions);

        if (httpResponse is { IsSuccessStatusCode: true, StatusCode: HttpStatusCode.OK })
        {
            var response = await DeserializeAsync<AllowedResponse>(httpResponse);
            if (response?.Debug?.Rbac?.Reason != null)
            {
                _logger.LogTrace("RBAC reason: {Reason}", response.Debug.Rbac.Reason);
            }
            
            return response!.Allow;
        }

        var errorMessage = await httpResponse.Content.ReadAsStringAsync();
        _logger.LogError("Permit API returned {StatusCode}: {Message}", httpResponse.StatusCode, errorMessage);
        return false;
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient?.Dispose();

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions);
    }
}