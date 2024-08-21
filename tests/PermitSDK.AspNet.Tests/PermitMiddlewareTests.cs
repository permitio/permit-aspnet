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
    private const string TestMultiAction = $"{FailTestAction},{TestAction}";
    private const string FailTestMultiAction = $"{FailTestAction},{FailTestAction}";
    private const string FailTestAction = "fail";
    private const string FailTestResourceType = "fail";
    private const string DefaultUserKey = "defaultUserKey";
    private const string ProviderUserKey = "testUserKey";
    private readonly Mock<ILogger<PermitMiddleware>> _loggerMock = new();
    private static PermitAttribute SuccessTestAttribute => new(TestAction, TestResourceType);
    private static PermitData SuccessTestData => new(TestAction, TestResourceType);
    private static PermitAttribute FailTestAttribute => new(FailTestAction, FailTestResourceType);
    private static PermitData FailTestData => new(FailTestAction, FailTestResourceType);
    private static PermitAttribute SuccessTestMultiAttribute => new(TestMultiAction, TestResourceType);
    private static PermitAttribute FailTestMultiAttribute => new(FailTestMultiAction, TestResourceType);

    [Fact]
    public async Task NoActionDescriptor_Ok()
    {
        // q
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

    [Theory]
    [InlineData(UserIdClaimTypes.Subject)]
    [InlineData(UserIdClaimTypes.NameIdentifier)]
    [InlineData(UserIdClaimTypes.FullyQualifiedId)]
    [InlineData(UserIdClaimTypes.ObjectIdentifier)]
    public async Task ActionOnResource_UserKeyFromClaims_Ok(string userIdClaimType)
    {
        await RunMiddlewareAsync(controllerAttributes: [SuccessTestAttribute], userIdClaimType: userIdClaimType);
    }

    [Fact]
    public async Task ActionOnResource_UserKeyFromProvider_Ok()
    {
        var options = new PermitOptions
        {
            GlobalUserKeyProviderType = typeof(TestUserKeyProvider)
        };
        await RunMiddlewareAsync(controllerAttributes: [SuccessTestAttribute], options: options,
            userKey: ProviderUserKey);
    }

    [Theory]
    [MemberData(nameof(MiddlewareTestData))]
    public async Task ActionOnResource_WithAttributes(string runName, bool isAllowed,
        PermitAttribute[]? controllerAttributes, PermitAttribute[]? actionAttributes)
    {
        await RunMiddlewareAsync(isAllowed, controllerAttributes, actionAttributes);
    }

    [Fact]
    public async Task ActionOnResource_WithMinimalApi()
    {
        await RunMiddlewareAsync(endpointMetadata: [SuccessTestData]);
    }
    
    [Fact]
    public async Task ActionOnResource_WithMinimalApi_Fail()
    {
        await RunMiddlewareAsync(false, endpointMetadata: [FailTestData]);
    }

    public static IEnumerable<object?[]> MiddlewareTestData =>
        new List<object?[]>
        {
            // name, expectedResponse, controllerAttributes, actionAttributes
            new object?[]
            {
                "Single controller wrong attribute: false",
                false, new[] { FailTestAttribute }, null
            },
            new object?[]
            {
                "Single action wrong attribute: false",
                false, null, new[] { FailTestAttribute }
            },
            new object?[]
            {
                "Single controller attribute: true",
                true, new[] { SuccessTestAttribute }, null
            },
            new object?[]
            {
                "Single action attribute: true",
                true, null, new[] { SuccessTestAttribute }
            },
            new object?[]
            {
                "Multiple controller attributes: true",
                true, new[] { SuccessTestAttribute, SuccessTestAttribute }, null
            },
            new object?[]
            {
                "Multiple controller attributes: false",
                false, new[] { SuccessTestAttribute, FailTestAttribute }, null
            },
            new object?[]
            {
                "Multiple action attributes: true",
                true, null, new[] { SuccessTestAttribute, SuccessTestAttribute }
            },
            new object?[]
            {
                "Multiple action attributes: false",
                false, null, new[] { SuccessTestAttribute, FailTestAttribute }
            },
            new object?[]
            {
                "Multiple attributes: true",
                true, new[] { SuccessTestAttribute }, new[] { SuccessTestAttribute }
            },
            new object?[]
            {
                "Multiple attributes: false",
                false, new[] { SuccessTestAttribute }, new[] { FailTestAttribute }
            },
            new object?[]
            {
                "Multiple attributes 2: false",
                false, new[] { FailTestAttribute }, new[] { SuccessTestAttribute }
            },
            new object?[]
            {
                "Multi action on controller: true",
                false,  new[] { SuccessTestMultiAttribute }, null
            },
            new object?[]
            {
                "Multi action on controller: false",
                false,  new[] { FailTestMultiAttribute }, null
            },
        };

    private async Task RunMiddlewareAsync(
        bool expected = true,
        PermitAttribute[]? controllerAttributes = null,
        PermitAttribute[]? actionAttributes = null,
        IPermitData[]? endpointMetadata = null,
        PermitOptions? options = null,
        string userIdClaimType = UserIdClaimTypes.NameIdentifier,
        string userKey = DefaultUserKey)
    {
        // Arrange
        var pdpService = GetPdpServiceFromAllowed(userKey, TestAction, TestResourceType, null);
        var resourceInputBuilderFactoryMock = new Mock<Func<IResourceInputBuilder>>();
        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(It.Is<IPermitData>(a =>
                a.ResourceType == TestResourceType && a.Action == TestAction), It.IsAny<HttpContext>()))
            .ReturnsAsync(new Resource(null, null, null, null, TestResourceType));
        
        resourceInputBuilderFactoryMock.Setup(m => m.Invoke().BuildAsync(It.Is<IPermitData>(a =>
                a.ResourceType == FailTestResourceType && a.Action == FailTestAction), It.IsAny<HttpContext>()))
            .ReturnsAsync(new Resource(null, null, null, null, FailTestResourceType));

        var middleware = new PermitMiddleware(Success,
            pdpService,
            resourceInputBuilderFactoryMock.Object,
            options ?? new PermitOptions(),
            _loggerMock.Object);

        var httpContext = GetContext(
            withUser: true,
            userIdClaimType,
            controllerAttributes,
            actionAttributes,
            endpointMetadata);

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        var expectedResponseCode = expected ? 200 : 403;
        Assert.Equal(expectedResponseCode, httpContext.Response.StatusCode);
    }

    private static DefaultHttpContext GetContext(
        bool withUser = true,
        string userIdClaimType = UserIdClaimTypes.NameIdentifier,
        PermitAttribute[]? controllerAttributes = null,
        PermitAttribute[]? actionAttributes = null,
        IPermitData[]? endpointMetadata = null)
    {
        EndpointMetadataCollection? endpointMetadataCollection = null;
        if (controllerAttributes?.Length > 0 || actionAttributes?.Length > 0)
        {
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerName = "MockedController",
                ActionName = "MockedAction",
                ControllerTypeInfo = new FakeTypeInfo(controllerAttributes ?? []),
                MethodInfo = new FakeMethodInfo(actionAttributes ?? [])
            };
            endpointMetadataCollection = new EndpointMetadataCollection(actionDescriptor);
        }
        else if (endpointMetadata?.Length > 0)
        {
            endpointMetadataCollection = new EndpointMetadataCollection(endpointMetadata.Cast<object>());
        }

        var endpoint = new Endpoint(
            _ => Task.CompletedTask, // Dummy request delegate
            endpointMetadataCollection,
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

    private static PdpService GetPdpServiceFromAllowed(string userKey, string action, string type, string? resourceKey)
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
                        allowedRequest.User.Key == userKey;

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