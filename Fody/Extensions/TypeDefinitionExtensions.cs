using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;

public static class TypeDefinitionExtensions
{
    public static MethodDefinition FindMethod(this TypeDefinition typeDefinition, string method, params string[] paramTypes)
    {
        return typeDefinition.Methods.First(x => x.Name == method && x.IsMatch(paramTypes));
    }
}
