using Mono.Cecil;

public static class PropertyDefinitionExtensions
{
    public static MethodReference GetGetMethod(this PropertyDefinition property, TypeReference targetType)
    {
        MethodReference method = property.GetMethod;
        if (!method.DeclaringType.HasGenericParameters)
        {
            return method;
        }

        var genericInstanceType = property.DeclaringType.GetGenericInstanceType(targetType);
        return new MethodReference(method.Name, method.ReturnType)
        {
            DeclaringType = genericInstanceType,
            HasThis = true
        };
    }
}