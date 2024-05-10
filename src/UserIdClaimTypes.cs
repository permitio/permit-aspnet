using System.Security.Claims;

namespace PermitSDK.AspNet;

/// <summary>
/// Provides constants for claim types used to extract user ID.
/// </summary>
public static class UserIdClaimTypes
{
    /// <summary>
    /// Represents the name identifier claim type.
    /// </summary>
    public const string NameIdentifier = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Represents the object identifier claim type.
    /// </summary>
    public const string ObjectIdentifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Represents the fully qualified ID claim type.
    /// </summary>
    public const string FullyQualifiedId = "fully_qualified_id";

    /// <summary>
    /// Represents the subject claim type.
    /// </summary>
    public const string Subject = "sub";
}