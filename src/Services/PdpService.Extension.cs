using System.Text.Json;
using System.Text.Json.Serialization;

namespace PermitSDK.AspNet.Services;

/// <summary>
/// Service to call the PDP endpoints. 
/// </summary>
public partial class PdpService: IDisposable
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
    
    /// <summary>
    /// Is Allowed
    /// </summary>
    /// <returns>Successful Response</returns>
    /// <exception cref="PdpCallException">A server side error occurred.</exception>
    public virtual System.Threading.Tasks.Task<AuthorizationResult> AllowedAsync(AuthorizationQuery body)
    {
        return AllowedAsync(body, System.Threading.CancellationToken.None);
    }

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Is Allowed
    /// </summary>
    /// <returns>Successful Response</returns>
    /// <exception cref="PdpCallException">A server side error occurred.</exception>
    public virtual async System.Threading.Tasks.Task<AuthorizationResult> AllowedAsync(AuthorizationQuery body,
        System.Threading.CancellationToken cancellationToken)
    {
        if (body == null)
            throw new System.ArgumentNullException("body");

        var client_ = _httpClient;
        var disposeClient_ = false;
        try
        {
            using (var request_ = new System.Net.Http.HttpRequestMessage())
            {
                var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, _settings.Value);
                var content_ = new System.Net.Http.ByteArrayContent(json_);
                content_.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
                request_.Content = content_;
                request_.Method = new System.Net.Http.HttpMethod("POST");
                request_.Headers.Accept.Add(
                    System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

                var urlBuilder_ = new System.Text.StringBuilder();

                // Operation Path: "allowed"
                urlBuilder_.Append("allowed");

                PrepareRequest(client_, request_, urlBuilder_);

                var url_ = urlBuilder_.ToString();
                request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);

                PrepareRequest(client_, request_, url_);

                var response_ = await client_
                    .SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                var disposeResponse_ = true;
                try
                {
                    var headers_ =
                        new System.Collections.Generic.Dictionary<string,
                            System.Collections.Generic.IEnumerable<string>>();
                    foreach (var item_ in response_.Headers)
                        headers_[item_.Key] = item_.Value;
                    if (response_.Content != null && response_.Content.Headers != null)
                    {
                        foreach (var item_ in response_.Content.Headers)
                            headers_[item_.Key] = item_.Value;
                    }

                    ProcessResponse(client_, response_);

                    var status_ = (int)response_.StatusCode;
                    if (status_ == 200)
                    {
                        var objectResponse_ =
                            await ReadObjectResponseAsync<AuthorizationResult>(response_, headers_, cancellationToken)
                                .ConfigureAwait(false);
                        if (objectResponse_.Object == null)
                        {
                            throw new PdpCallException("Response was null which was not expected.", status_,
                                objectResponse_.Text, headers_, null);
                        }

                        return objectResponse_.Object;
                    }
                    else if (status_ == 422)
                    {
                        var objectResponse_ =
                            await ReadObjectResponseAsync<HTTPValidationError>(response_, headers_, cancellationToken)
                                .ConfigureAwait(false);
                        if (objectResponse_.Object == null)
                        {
                            throw new PdpCallException("Response was null which was not expected.", status_,
                                objectResponse_.Text, headers_, null);
                        }

                        throw new PdpCallException<HTTPValidationError>("Validation Error", status_,
                            objectResponse_.Text, headers_, objectResponse_.Object, null);
                    }
                    else
                    {
                        var responseData_ = response_.Content == null
                            ? null
                            : await response_.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new PdpCallException(
                            "The HTTP status code of the response was not expected (" + status_ + ").", status_,
                            responseData_, headers_, null);
                    }
                }
                finally
                {
                    if (disposeResponse_)
                        response_.Dispose();
                }
            }
        }
        finally
        {
            if (disposeClient_)
                client_.Dispose();
        }
    }

    /// <summary>
    /// Dispose the service
    /// </summary>
    public void Dispose()
    {
       _httpClient?.Dispose(); 
    }
}