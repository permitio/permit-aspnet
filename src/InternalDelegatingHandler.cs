namespace PermitSDK.AspNet;

internal class InternalDelegatingHandler : DelegatingHandler
{
	private readonly Func<HttpRequestMessage, Task> _configureRequest;

	public InternalDelegatingHandler(Func<HttpRequestMessage, Task> configureRequest)
	{
		_configureRequest = configureRequest;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		await _configureRequest(request);
		var response = await base.SendAsync(request, cancellationToken);
		return response;
	}
}