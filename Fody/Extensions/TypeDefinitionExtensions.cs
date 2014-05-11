using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Collections.Generic;

public static class TypeDefinitionExtensions
{
    public static MethodDefinition FindMethod(this TypeDefinition typeDefinition, string method, params string[] paramTypes)
    {
        return typeDefinition.Methods.First(x => x.Name == method && x.IsMatch(paramTypes));
    }

    public static bool IsCollection(this TypeDefinition type)
    {
        return !type.Name.Equals("String") && (type.Interfaces.Any(i => i.Name.Equals("IEnumerable")));
    }

    public static PropertyDefinition[] GetProperties(this TypeDefinition type)
    {
        var properties = new List<PropertyDefinition>();

        var currentType = type;
        do
        {
            var currentProperties = currentType.Properties;
            properties.AddRange(currentProperties);
            currentType = currentType.BaseType.Resolve();
        } while (currentType.FullName != typeof(object).FullName);

        return properties.ToArray();
    }

    public static TypeReference GetGenericInstanceType(this TypeReference type, TypeReference targetType)
    {
        var genericInstance = targetType as GenericInstanceType;
        if (genericInstance != null)
        {
            return genericInstance;
        }

        if (type.IsGenericParameter)
        {
            var genericParameter = type as GenericParameter;

            var current = targetType;
            var currentResolved = current.Resolve();

            while (currentResolved.FullName != genericParameter.DeclaringType.FullName)
            {
                if (currentResolved.BaseType == null)
                {
                    return type;
                }
                current = currentResolved.BaseType;
                currentResolved = current.Resolve();
            }

            var genericInstanceType = current as GenericInstanceType;
            if (genericInstanceType != null)
            {
                var newType = genericInstanceType.GenericArguments[genericParameter.Position];
                return newType;
            }

            return type;
        }

        if (type.HasGenericParameters)
        {
            GenericInstanceType genericInstanceType;
            var parent = targetType;
            var parentReference = targetType;

            if (type.FullName == targetType.Resolve().FullName)
            {
                genericInstanceType = GetGenericInstanceType(type, type.GenericParameters);
            }
            else
            {
                var propertyType = type.Resolve();

                TypeDefinition parentResolved;
                while (parent != null && propertyType.FullName != (parentResolved = parent.Resolve()).FullName)
                {
                    parentReference = parentResolved.BaseType;
                    parent = parentResolved.BaseType != null ? parentResolved.BaseType.Resolve() : null;
                }

                genericInstanceType = parentReference as GenericInstanceType;
                if (genericInstanceType == null)
                {
                    genericInstanceType = GetGenericInstanceType(type, parentReference.GenericParameters);
                }
            }

            return genericInstanceType;
        }

        return type;
    }

    private static GenericInstanceType GetGenericInstanceType(TypeReference type, Collection<GenericParameter> parameters)
    {
        var genericInstanceType = new GenericInstanceType(type);
        foreach (var genericParameter in parameters)
        {
            genericInstanceType.GenericArguments.Add(genericParameter);
        }
        return genericInstanceType;
    }
}
