using Microsoft.AspNetCore.Http;

namespace PermitSDK.AspNet.Tests;

public class PermitMiddlewareTests
{
    [Fact]
    public async Task Default_Ok()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var middleware = new PermitMiddleware(Success,
            new PermitOptions(),
            new PermitProvidersOptions());

        // Act
        await middleware.InvokeAsync(httpContext, null!);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    private static Task Success(HttpContext context)
    {
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    }
}