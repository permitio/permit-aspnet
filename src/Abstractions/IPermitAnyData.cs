namespace PermitSDK.AspNet.Abstractions;

/// <summary>
/// Describe how a resource is protected
/// </summary>
public interface IPermitAnyData: IPermitMetadata
{
    /// <summary>
    /// Get the list of policies to check
    /// </summary>
    public PermitData[] Policies { get; }
}