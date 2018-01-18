using Mono.Cecil;

public static class PropertyDefinitionExtensions
{
    public static MethodReference GetGetMethod(this PropertyDefinition property, TypeReference targetType)
    {
        MethodReference method = property.GetMethod;
        if (method.DeclaringType.HasGenericParameters)
        {
            var genericInstanceType = property.DeclaringType.GetGenericInstanceType(targetType);
            var newRef = new MethodReference(method.Name, method.ReturnType)
            {
                DeclaringType = genericInstanceType,
                HasThis = true
            };

            return newRef;
        }

        return method;
    }
}