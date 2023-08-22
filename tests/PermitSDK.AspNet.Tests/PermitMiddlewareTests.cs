using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace PermitSDK.AspNet.Tests;

public class PermitMiddlewareTests
{
    [Fact]
    public async Task NoActionDescriptor_Ok()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var permitProxyMock = new Mock<IPermitProxy>();
        var middleware = new PermitMiddleware(Success,
            permitProxyMock.Object,
            new PermitProvidersOptions());

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }
    
    [Fact]
    public async Task NoUserKey_Ok()
    {
        // Arrange
        var httpContext = GetContext();
        var permitProxyMock = new Mock<IPermitProxy>();
        var middleware = new PermitMiddleware(Success,
            permitProxyMock.Object,
            new PermitProvidersOptions());

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    private static HttpContext GetContext()
    {
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "MockedController",
            ActionName = "MockedAction"
        };

        var metadata = new EndpointMetadataCollection(actionDescriptor);
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,  // Dummy request delegate
            metadata,
            "Test endpoint"
        );

        var endpointFeature = new EndpointFeature
        {
            Endpoint = endpoint
        };

        var featureCollection = new FeatureCollection();
        featureCollection.Set<IEndpointFeature>(endpointFeature);
        featureCollection.Set<IHttpRequestFeature>(new HttpRequestFeature());
        featureCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());
        featureCollection.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));

        var httpContext = new DefaultHttpContext(featureCollection)
        {
            Response = { StatusCode = 500 }
        };
        return httpContext;
    }
    
    private static Task Success(HttpContext context)
    {
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    }
    
    private class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
        public RouteValueDictionary? RouteValues { get; set; }
    }
}