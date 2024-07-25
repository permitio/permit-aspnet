namespace PermitSDK.AspNet;

internal class InternalDelegatingHandler : DelegatingHandler
{
    private readonly Action<HttpRequestMessage> _configureRequest;

    public InternalDelegatingHandler(Action<HttpRequestMessage> configureRequest)
    {
        _configureRequest = configureRequest;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        _configureRequest(request);
        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}