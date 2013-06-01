using System.Linq;

using Mono.Cecil;

public static class ICustomAttributeProviderExtensions
{
    public static void RemoveToStringttribute(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;

        var attribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == "ToStringAttribute");

        if (attribute != null)
        {
            customAttributes.Remove(attribute);
        }
    }
}

