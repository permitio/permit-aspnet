using PermitSDK.AspNet.Abstractions;

namespace PermitSDK.AspNet;

/// <summary>
/// Attribute to decorate controllers and actions with Permit permissions.
/// Check pass if any of the permissions are granted.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermitAnyAttribute(params PermitData[] policies) : PermitMetadataAttribute, IPermitAnyData
{
    /// <inheritdoc />
    public PermitData[] Policies => policies;
}