using System.Globalization;
using System.Reflection;

namespace PermitSDK.AspNet.Tests.Mock;

public class FakeMethodInfo : MethodInfo
{
    private readonly object[] _attributes;

    public FakeMethodInfo(params PermitAttribute[] attributes)
    {
        _attributes = attributes;
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        throw new NotImplementedException();
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return _attributes;
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        throw new NotImplementedException();
    }

    public override Type? DeclaringType { get; }
    public override string Name { get; }
    public override Type? ReflectedType { get; }
    public override MethodImplAttributes GetMethodImplementationFlags()
    {
        throw new NotImplementedException();
    }

    public override ParameterInfo[] GetParameters()
    {
        throw new NotImplementedException();
    }

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }

    public override MethodAttributes Attributes { get; }
    public override RuntimeMethodHandle MethodHandle { get; }
    public override MethodInfo GetBaseDefinition()
    {
        throw new NotImplementedException();
    }

    public override ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
}