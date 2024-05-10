# Permit AspNet

Easily protect APIs with Permit.io via attributes!

```csharp
[HttpGet("articles/{id}")]
[Permit("read", "article")]
public Article[] GetArticles()
{
    ...
}

[HttpPost("articles/{id}")]
[Permit("write", "article")]
public Article UpdateArticle([FromRoute] string id, [FromBody] Article article)
{
    ...
}
```

## Getting Started

* Install the package from `NuGet`
* Configure the middleware:
   ```json
  // appsettings.json
  {
    ...
    "Permit": {
      "ApiKey": "<API_KEY>", // Required
      "PdpUrl": "http://localhost:7760" // Optional
      "DefaultTenant": "default" // Optional
      "UseDefaultTenantIfEmpty": true // Optional 
    }
  }
  ```
  ```csharp  
  // Program.cs
  builder.Services.AddPermit(builder.Configuration);
  
  // Or directly
  var permitOptions = new PermitOptions
  {
      ApiKey = "<API_KEY>"
  };
  builder.Services.AddPermit(permitOptions);
  ```
  
* Enable the middleware:
  ```csharp
  app.UseAuthentication(); // Require by default
  app.UseAuthorization();
  
  // Add this line
  app.UsePermit();

  app.MapControllers();
  ```
* Use the `Permit` attribute to protect controllers or actions:
  ```csharp
  [Permit(action: "read", resourceType: "article")]
  public Article[] GetArticles()
  {
      return new Article[] { ... };
  }
  ```
  
## How it works

For each request, the middleware does:
* Extract the user ID from the *JWT* token claims (overridable)
  * The following claim types are checked in exact order `Sub`, `NameIdentifier`, `FullyQualifiedId`, `ObjectIdentifier`
* Extract `action` and `resourceType` from the `Permit` attribute
  * Multiple attributes are run sequentially. Controller first, then action
* Use the `HttpClientFactory` to get a `HttpClient` and call the PDP's `/allowed` endpoint 
* If the user is not allowed, a `403` is returned
* If the user is allowed, the request is processed

## Minimal API

This library works with the new *Minimal API* introduced in *ASP.NET Core 6* too:

```csharp
app.MapGetArticles()
    .WithName("GetArticles")
    .RequirePermit("read", "article");
```

## Resource instances

It's possible to specify a resource instance key in the `Permit` attribute:

```csharp
[HttpGet("articles/{id}")]
[Permit("read", "article", ResourceKeyFromRoute = "id")]
public Article GetArticle([FromRoute] string id)
{
    ...
}
```

There are many ways to specify the resource instance:

| Property                  | Description                                |
|---------------------------|--------------------------------------------| 
| `ResourceKey`             | Static value                               |
| `ResourceKeyFromRoute`    | From a route parameter                     | 
| `ResourceKeyFromHeader`   | From a header                              |
| `ResourceKeyFromQuery`    | From a query parameter                     |
| `ResourceKeyFromClaim`    | From a token claim                         |
| `ResourceKeyFromBody`     | From the body. Nesting supported with dots |
| `ResourceKeyProviderType` | Custom provider, explained below           |

## Tenant

The tenant can be specified with the `DefaultTenant` property in `PermitOptions`.

For more granular control, the same options as resource instances are available:

| Property             | Description                                |
|----------------------|--------------------------------------------|
| `Tenant`             | Static value                               |
| `TenantFromRoute`    | From a route parameter                     |
| `TenantFromHeader`   | From a header                              |
| `TenantFromQuery`    | From a query parameter                     |
| `TenantFromClaim`    | From a token claim                         |
| `TenantFromBody`     | From the body. Nesting supported with dots |
| `TenantProviderType` | Custom provider, explained below           |

## Permit providers

Providers are classes that can extract specific information from the request.

There are three types of providers for specific use cases
* `IPermitValueProvider`: Extract a single value
  * Used for *resource instance key*
  * Used for *tenant*
    ```csharp
    ResourceKeyProviderType = typeof(MyCustomValueProvider)
    TenantProviderType = typeof(MyCustomValueProvider)
    ```
* `IPermitValuesProvider`: Extract a dictionary
  * Used for *attributes*
  * Used for *context*
    ```csharp
    AttributesProviderType = typeof(MyCustomValuesProvider)
    ContextProviderType = typeof(MyCustomValuesProvider)
    ```
* `IPermitUserKeyProvider`: Extract the user key

### Example

```csharp
public class FakeUserKeyProvider: IPermitUserKeyProvider
{
    public Task<UserKey> GetUserKeyAsync(HttpContext httpContext)
    {
        return Task.FromResult(new UserKey("net@permit.io"));
    }
}
```

### Global providers

To apply these providers for each request, there is a second argument in the `AddPermit` method:

```csharp
builder.Services.AddPermit(builder.Configuration, options =>
    {
        conf.GlobalUserKeyProviderType = typeof(FakeUserKeyProvider);
    });


// Or directly
var permitOptions = new PermitOptions
{
    GlobalUserKeyProviderType = typeof(FakeUserKeyProvider);
}
builder.Services.AddPermit(permitOptions);
```

### Dependency injection

Providers support *DI* out the box, just register the class in the *DI* container:

```csharp
// Provider
public class RegionsContextProvider : IPermitValuesProvider
{
    private readonly IRegionsProvider _regionsProvider;

    public RegionsContextProvider(IRegionsProvider regionsProvider)
    {
        _regionsProvider = regionsProvider;
    }
    
    public Task<Dictionary<string, object>> GetValues(HttpContext httpContext)
    {
        return _regionsProvider.GetRegionsAsync();
    }
}

// Registration
builder.Services.AddSingleton<IRegionsProvider, RegionsProvider>();
builder.Services.AddScoped<RegionsContextProvider>();
```

## Custom attributes

The `Permit` attribute is not mandatory, you can create your own:

```csharp
// Custom attribute
public class ProtectArticleAttribute : PermitAttribute
{
    public ProtectArticleAttribute(string action)
      : base(action, "article",
        ResourceKeyFromRoute = "id",
        TenantFromHeader = "X-Org-Id") { }
}

// Usage
[HttpGet("articles/{id}")]
[ProtectArticle("read")]
public Article GetArticle([FromRoute] string id)
{
    ...
}

[HttpPost("articles/{id}")]
[ProtectArticle("write")]
public Article UpdateArticle([FromRoute] string id, [FromBody] Article article)
{
    ...
}
```

## Call the PDP

If you need to call the *PDP*, you can inject the `PdpService`, which is a wrapper around the `HttpClient` created by `NSwag`.