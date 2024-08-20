namespace PermitSDK.AspNet.Abstractions;

/// <summary>
/// Attribute to decorate controllers and actions with Permit permissions.
/// Check pass if any of the permissions are granted.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermitAnyMetadata(params PermitData[] policies) : PermitMetadataAttribute, IPermitAnyData
{
    /// <inheritdoc />
    public PermitData[] Policies => policies;
}