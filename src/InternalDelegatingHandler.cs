namespace PermitSDK.AspNet;

internal class InternalDelegatingHandler(Func<HttpRequestMessage, Task> configureRequest) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await configureRequest(request);
        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}