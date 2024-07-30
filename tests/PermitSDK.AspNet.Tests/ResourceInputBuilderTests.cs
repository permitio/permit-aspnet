using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace PermitSDK.AspNet.Tests;

public class ResourceInputBuilderTests
{
    private const string TestAction = "testAction";
    private const string TestType = "testType";
    private const string TestResourceKey = "testKey";
    private const string TestTenant = "testTenant";

    private static readonly Dictionary<string, object> TestAttributes = new()
    {
        { "testAttrKey", "testAttrValue" }
    };

    private static readonly Dictionary<string, object> TestContext = new()
    {
        { "testCxtKey", "testCtxValue" }
    };

    #region ResourceKey

    [Fact]
    public async Task ResourceKey_Static()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKey = TestResourceKey
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_FromRoute()
    {
        // Arrange
        const string routeKey = "testRouteKey";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKeyFromRoute = routeKey
        };
        var endpointFeature = new RouteValuesFeature
        {
            RouteValues = new RouteValueDictionary
            {
                { routeKey, TestResourceKey }
            }
        };
        var featureCollection = new FeatureCollection();
        featureCollection.Set<IRouteValuesFeature>(endpointFeature);
        var httpContext = new DefaultHttpContext(featureCollection);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_FromHeader()
    {
        // Arrange
        const string headerKey = "testHeaderKey";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKeyFromHeader = headerKey
        };
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    { headerKey, new StringValues(TestResourceKey) }
                }
            }
        };

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_FromBody()
    {
        // Arrange
        const string bodyKey = "response.testResourceKey";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKeyFromBody = bodyKey
        };

        var bodyObj = new
        {
            response = new
            {
                testResourceKey = TestResourceKey
            }
        };
        var bodyJson = JsonConvert.SerializeObject(bodyObj);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Body = memoryStream
            }
        };

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_Provider()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKeyProviderType = typeof(ResourceKeyProvider)
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_Provider_DependencyInjection()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ResourceKeyProviderType = typeof(ResourceKeyProviderDi)
        };
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<ResourceKeyProvider>()
            .AddSingleton<ResourceKeyProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    [Fact]
    public async Task ResourceKey_Provider_Global()
    {
        // Arrange
        var builder = GetBuilder(
            options: new PermitOptions
            {
                GlobalResourceKeyProviderType = typeof(ResourceKeyProviderDi)
            });
        var attribute = new PermitAttribute(TestAction, TestType);
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<ResourceKeyProvider>()
            .AddSingleton<ResourceKeyProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestResourceKey, result.Key);
    }

    private class ResourceKeyProvider : IPermitValueProvider
    {
        public Task<string> GetValueAsync(HttpContext httpContext)
        {
            return Task.FromResult(TestResourceKey);
        }
    }

    private class ResourceKeyProviderDi : IPermitValueProvider
    {
        private readonly ResourceKeyProvider _provider;

        public ResourceKeyProviderDi(ResourceKeyProvider provider)
        {
            _provider = provider;
        }

        public Task<string> GetValueAsync(HttpContext httpContext)
        {
            return _provider.GetValueAsync(httpContext);
        }
    }

    #endregion

    #region Tenant

    [Fact]
    public async Task Tenant_Static()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            Tenant = TestTenant
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_FromRoute()
    {
        // Arrange
        const string routeKey = "testRouteKey";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            TenantFromRoute = routeKey
        };
        var endpointFeature = new RouteValuesFeature
        {
            RouteValues = new RouteValueDictionary
            {
                { routeKey, TestTenant }
            }
        };
        var featureCollection = new FeatureCollection();
        featureCollection.Set<IRouteValuesFeature>(endpointFeature);
        var httpContext = new DefaultHttpContext(featureCollection);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_FromHeader()
    {
        // Arrange
        const string headerKey = "testHeaderKey";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            TenantFromHeader = headerKey
        };
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    { headerKey, new StringValues(TestTenant) }
                }
            }
        };

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_FromBody()
    {
        // Arrange
        const string bodyKey = "response.testTenant";
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            TenantFromBody = bodyKey
        };

        var bodyObj = new
        {
            response = new
            {
                testTenant = TestTenant
            }
        };
        var bodyJson = JsonConvert.SerializeObject(bodyObj);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Body = memoryStream
            }
        };

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_Provider()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            TenantProviderType = typeof(TenantProvider)
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_Provider_DependencyInjection()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            TenantProviderType = typeof(TenantProviderDi)
        };
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<TenantProvider>()
            .AddSingleton<TenantProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    [Fact]
    public async Task Tenant_Provider_Global()
    {
        // Arrange
        var builder = GetBuilder(
            options: new PermitOptions
            {
                GlobalTenantProviderType = typeof(TenantProviderDi)
            });
        var attribute = new PermitAttribute(TestAction, TestType);
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<TenantProvider>()
            .AddSingleton<TenantProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);
        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestTenant, result.Tenant);
    }

    private class TenantProvider : IPermitValueProvider
    {
        public Task<string> GetValueAsync(HttpContext httpContext)
        {
            return Task.FromResult(TestTenant);
        }
    }

    private class TenantProviderDi : IPermitValueProvider
    {
        private readonly TenantProvider _provider;

        public TenantProviderDi(TenantProvider provider)
        {
            _provider = provider;
        }

        public Task<string> GetValueAsync(HttpContext httpContext)
        {
            return _provider.GetValueAsync(httpContext);
        }
    }

    #endregion

    #region Attributes

    [Fact]
    public async Task Attributes_Provider()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            AttributesProviderType = typeof(AttributesProvider)
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestAttributes, result.Attributes);
    }

    [Fact]
    public async Task Attributes_Provider_DependencyInjection()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            AttributesProviderType = typeof(AttributesProviderDi)
        };
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<AttributesProvider>()
            .AddSingleton<AttributesProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestAttributes, result.Attributes);
    }

    [Fact]
    public async Task Attributes_Provider_Global()
    {
        // Arrange
        var builder = GetBuilder(
            options: new PermitOptions
            {
                GlobalAttributesProviderType = typeof(AttributesProviderDi)
            });
        var attribute = new PermitAttribute(TestAction, TestType);
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<AttributesProvider>()
            .AddSingleton<AttributesProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestAttributes, result.Attributes);
    }

    private class AttributesProvider : IPermitValuesProvider
    {
        public Task<Dictionary<string, object>> GetValues(HttpContext httpContext)
        {
            return Task.FromResult(TestAttributes);
        }
    }

    private class AttributesProviderDi : IPermitValuesProvider
    {
        private readonly AttributesProvider _provider;

        public AttributesProviderDi(AttributesProvider provider)
        {
            _provider = provider;
        }

        public Task<Dictionary<string, object>> GetValues(HttpContext httpContext)
        {
            return _provider.GetValues(httpContext);
        }
    }

    #endregion

    #region Context

    [Fact]
    public async Task Context_Provider()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ContextProviderType = typeof(ContextProvider)
        };
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestContext, result.Context);
    }

    [Fact]
    public async Task Context_Provider_DependencyInjection()
    {
        // Arrange
        var builder = GetBuilder();
        var attribute = new PermitAttribute(TestAction, TestType)
        {
            ContextProviderType = typeof(ContextProviderDi)
        };
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<ContextProvider>()
            .AddSingleton<ContextProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestContext, result.Context);
    }

    [Fact]
    public async Task Context_Provider_Global()
    {
        // Arrange
        var builder = GetBuilder(
            options: new PermitOptions
            {
                GlobalContextProviderType = typeof(ContextProviderDi)
            });
        var attribute = new PermitAttribute(TestAction, TestType);
        var httpContext = new DefaultHttpContext();

        var configureService = new Action<ServiceCollection>(services => services
            .AddSingleton<ContextProvider>()
            .AddSingleton<ContextProviderDi>());
        httpContext.RequestServices = BuildServiceProvider(configureService);

        // Act
        var result = await builder.BuildAsync(attribute, httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(TestContext, result.Context);
    }

    private class ContextProvider : IPermitValuesProvider
    {
        public Task<Dictionary<string, object>> GetValues(HttpContext httpContext)
        {
            return Task.FromResult(TestContext);
        }
    }

    private class ContextProviderDi : IPermitValuesProvider
    {
        private readonly ContextProvider _provider;

        public ContextProviderDi(ContextProvider provider)
        {
            _provider = provider;
        }

        public Task<Dictionary<string, object>> GetValues(HttpContext httpContext)
        {
            return _provider.GetValues(httpContext);
        }
    }

    #endregion

    #region Helpers

    private static ResourceInputBuilder GetBuilder(
        PermitOptions? options = null)
    {
        options ??= new PermitOptions();
        return new ResourceInputBuilder(options);
    }

    private static IServiceProvider BuildServiceProvider(Action<ServiceCollection>? configureService = null)
    {
        var services = new ServiceCollection();
        configureService?.Invoke(services);
        return services.BuildServiceProvider();
    }

    #endregion
}