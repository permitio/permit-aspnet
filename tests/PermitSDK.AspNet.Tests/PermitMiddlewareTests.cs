using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PermitSDK.AspNet.Services;
using PermitSDK.AspNet.Tests.Mock;

namespace PermitSDK.AspNet.Tests;

public class PermitMiddlewareTests
{
    private const string TestResourceType = "article";
    private const string TestAction = "read";
    private const string DefaultUserKey = "defaultUserKey";
    private const string ProviderUserKey = "testUserKey";
    private readonly Mock<ILogger<PermitMiddleware>> _loggerMock = new();

    [Fact]
    public async Task NoActionDescriptor_Ok()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var pdpService = GetPdpService(_ => { });
        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions(),
            _loggerMock.Object);

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
        var pdpService = GetPdpService(_ => { });
        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions(),
            _loggerMock.Object);

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
        var pdpService = GetPdpService(_ => { });
        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions(),
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ActionOnResource_403()
    {
        // Arrange
        var pdpService = GetPdpServiceFromAllowed(DefaultUserKey, TestAction, TestResourceType, null, false);

        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(It.Is<PermitAttribute>(a =>
                a.ResourceType == TestResourceType && a.Action == TestAction), It.IsAny<HttpContext>()))
            .ReturnsAsync(new Resource(null, null, null, null, TestResourceType));

        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions(),
            _loggerMock.Object);

        var attribute = new PermitAttribute(TestAction, TestResourceType);
        var httpContext = GetContextWithControllerAttributes(attribute);

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(403, httpContext.Response.StatusCode);
    }

    [Theory]
    [InlineData(UserIdClaimTypes.Subject)]
    [InlineData(UserIdClaimTypes.NameIdentifier)]
    [InlineData(UserIdClaimTypes.FullyQualifiedId)]
    [InlineData(UserIdClaimTypes.ObjectIdentifier)]
    public async Task ActionOnResource_Ok_UserKeyFromClaims(string userIdClaimType)
    {
        // Arrange
        var pdpService = GetPdpServiceFromAllowed(DefaultUserKey, TestAction, TestResourceType, null, true);

        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(It.Is<PermitAttribute>(a =>
                a.ResourceType == TestResourceType && a.Action == TestAction), It.IsAny<HttpContext>()))
            .ReturnsAsync(new Resource(null, null, null, null, TestResourceType));

        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions(),
            _loggerMock.Object);

        var attribute = new PermitAttribute(TestAction, TestResourceType);
        var httpContext = GetContext(userIdClaimType: userIdClaimType, controllerAttributes: new[] { attribute });

        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(attribute, httpContext))
            .ReturnsAsync(new Resource(null, null, null, null, attribute.ResourceType));

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ActionOnResource_Ok_UserKeyProvider()
    {
        // Arrange
        var pdpService = GetPdpServiceFromAllowed(ProviderUserKey, TestAction, TestResourceType, null, true);

        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(It.Is<PermitAttribute>(a =>
                a.ResourceType == TestResourceType && a.Action == TestAction), It.IsAny<HttpContext>()))
            .ReturnsAsync(new Resource(null, null, null, null, TestResourceType));

        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            new PermitOptions
            {
                GlobalUserKeyProviderType = typeof(TestUserKeyProvider)
            },
            _loggerMock.Object);

        var attribute = new PermitAttribute(TestAction, TestResourceType);
        var httpContext = GetContextWithControllerAttributes(attribute);

        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(attribute, httpContext))
            .ReturnsAsync(new Resource(null, null, null, null, attribute.ResourceType));

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    private static HttpContext GetContextWithControllerAttributes(PermitAttribute attribute)
    {
        return GetContext(controllerAttributes: new[] { attribute });
    }

    private static HttpContext GetContext(
        bool withUser = true,
        string userIdClaimType = UserIdClaimTypes.NameIdentifier,
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
            _ => Task.CompletedTask, // Dummy request delegate
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
            new(userIdClaimType, DefaultUserKey)
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

    private class TestUserKeyProvider : IPermitUserKeyProvider
    {
        public Task<User> GetUserKeyAsync(HttpContext httpContext)
        {
            return Task.FromResult(new User(null, null, null, ProviderUserKey, null));
        }
    }

    private static PdpService GetPdpServiceFromAllowed(string userKey, string action, string type, string? resourceKey,
        bool allowed)
    {
        var serializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return GetPdpService(messageHandlerMock =>
        {
            messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    var body = await request.Content!.ReadAsStringAsync(token);
                    var allowedRequest = JsonSerializer.Deserialize<AuthorizationQuery>(body, serializationOptions)!;
                    var responseAllowed =
                        allowedRequest.Action == action &&
                        allowedRequest.Resource.Type == type &&
                        allowedRequest.Resource.Key == resourceKey &&
                        allowedRequest.User.Key == userKey && allowed;

                    var response = new HttpResponseMessage();
                    var content = JsonSerializer.Serialize(new AuthorizationResult(responseAllowed, null, null, null),
                        serializationOptions);
                    response.Content = new StringContent(content);
                    return response;
                })
                .Verifiable();
        });
    }

    private static PdpService GetPdpService(Action<Mock<HttpMessageHandler>> mockSetup)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        mockSetup(handlerMock);
        var httpClient = new HttpClient(handlerMock.Object);
        httpClient.BaseAddress = new Uri("http://localhost");
        return new PdpService(httpClient);
    }
}