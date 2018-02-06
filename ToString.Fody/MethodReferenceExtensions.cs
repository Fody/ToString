using Mono.Cecil;

public static class MethodReferenceExtensions
{
    public static bool IsMatch(this MethodReference methodReference, params string[] paramTypes)
    {
        var parameters = methodReference.Parameters;
        if (parameters.Count != paramTypes.Length)
        {
            return false;
        }

        var methodReferenceParameters = parameters;
        for (var index = 0; index < methodReferenceParameters.Count; index++)
        {
            var parameterDefinition = methodReferenceParameters[index];
            var paramType = paramTypes[index];
            if (parameterDefinition.ParameterType.Name != paramType)
            {
                return false;
            }
        }

        return true;
    }
}