using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Moq;
using PermitSDK.AspNet.Tests.Mock;

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
        var httpContext = GetContext(withUser: false);
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
    public async Task NoAttributes_Ok()
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
    
    private static HttpContext GetContextWithControllerAttributes(params PermitAttribute[] attributes)
    {
        return GetContext(controllerAttributes: attributes);
    }

    private static HttpContext GetContext(
        bool withUser = true,
        PermitAttribute[]? controllerAttributes = null,
        PermitAttribute[]? actionAttributes = null)
    {
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "MockedController",
            ActionName = "MockedAction",
            ControllerTypeInfo = new FakeTypeInfo(controllerAttributes ?? Array.Empty<PermitAttribute>()),
            MethodInfo = new FakeMethodInfo(actionAttributes ?? Array.Empty<PermitAttribute>())
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

        if (!withUser)
        {
            return httpContext;
        }
        
        var claims = new List<Claim>
        { 
            new(ClaimTypes.NameIdentifier, "bob")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
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
    }
}