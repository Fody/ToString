using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Fody;

public class ModuleWeaver : BaseModuleWeaver
{
    TypeReference stringBuilderType;
    MethodReference appendString;
    MethodReference moveNext;
    MethodReference current;
    MethodReference getEnumerator;
    MethodReference getInvariantCulture;
    MethodReference formatMethod;

    public IEnumerable<TypeDefinition> GetMatchingTypes()
    {
        return ModuleDefinition.GetTypes()
            .Where(_ => _.CustomAttributes.Any(a => a.AttributeType.Name == "ToStringAttribute"));
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Runtime";
        yield return "netstandard";
    }

    public override void Execute()
    {
        var stringBuildType = FindTypeDefinition("System.Text.StringBuilder");
        stringBuilderType = ModuleDefinition.ImportReference(stringBuildType);
        appendString = ModuleDefinition.ImportReference(stringBuildType.FindMethod("Append", "Object"));
        var enumeratorType = FindTypeDefinition("System.Collections.IEnumerator");
        moveNext = ModuleDefinition.ImportReference(enumeratorType.FindMethod("MoveNext"));
        current = ModuleDefinition.ImportReference(enumeratorType.Properties.Single(_ => _.Name == "Current").GetMethod);
        var enumerableType = FindTypeDefinition("System.Collections.IEnumerable");
        getEnumerator = ModuleDefinition.ImportReference(enumerableType.FindMethod("GetEnumerator"));
        formatMethod = ModuleDefinition.ImportReference(TypeSystem.StringDefinition.FindMethod("Format", "IFormatProvider", "String", "Object[]"));

        var cultureInfoType = FindTypeDefinition("System.Globalization.CultureInfo");
        var invariantCulture = cultureInfoType.Properties.Single(_ => _.Name == "InvariantCulture");
        getInvariantCulture = ModuleDefinition.ImportReference(invariantCulture.GetMethod);

        foreach (var type in GetMatchingTypes())
        {
            AddToString(type);
        }
    }

    void AddToString(TypeDefinition type)
    {
        var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;

        var method = new MethodDefinition("ToString", methodAttributes, TypeSystem.StringReference);
        var variables = method.Body.Variables;
        variables.Add(new(new ArrayType(TypeSystem.ObjectReference)));
        var allProperties = type.GetProperties().Where(x => !x.HasParameters).ToArray();
        var properties = RemoveIgnoredProperties(allProperties).Distinct(PropertyNameEqualityComparer.Default).ToArray();

        var format = GetFormatString(type, properties);

        var body = method.Body;
        body.InitLocals = true;
        var ins = body.Instructions;

        var hasCollections = properties.Any(x => !x.PropertyType.IsGenericParameter && x.PropertyType.Resolve().IsCollection());
        if (hasCollections)
        {
            variables.Add(new(stringBuilderType));

            var enumeratorType = ModuleDefinition.ImportReference(typeof(IEnumerator));
            variables.Add(new(enumeratorType));

            variables.Add(new(TypeSystem.BooleanReference));

            variables.Add(new(new ArrayType(TypeSystem.ObjectReference)));
        }

        var genericOffset = !type.HasGenericParameters ? 0 : type.GenericParameters.Count;
        AddInitCode(ins, format, properties, genericOffset);

        if (type.HasGenericParameters)
        {
            AddGenericParameterNames(type, ins);
        }

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            AddPropertyCode(method.Body, i + genericOffset, property, type, variables);
        }

        AddMethodAttributes(method);

        AddEndCode(body);
        body.OptimizeMacros();

        var toRemove = type.Methods.FirstOrDefault(_ => _.Name == method.Name &&
                                                        _.Parameters.Count == 0);
        if (toRemove != null)
        {
            type.Methods.Remove(toRemove);
        }

        type.Methods.Add(method);

        RemoveFodyAttributes(type, allProperties);
    }

    void AddGenericParameterNames(TypeDefinition type, Collection<Instruction> ins)
    {
        var typeType = ModuleDefinition.ImportReference(FindTypeDefinition(typeof(Type).FullName!)).Resolve();
        var memberInfoType = ModuleDefinition.ImportReference(FindTypeDefinition(typeof(System.Reflection.MemberInfo).FullName!)).Resolve();
        var getTypeMethod = ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition.FindMethod("GetType"));
        var getGenericArgumentsMethod = ModuleDefinition.ImportReference(typeType.FindMethod("GetGenericArguments"));
        var nameProperty = memberInfoType.Properties.Single(_ => _.Name == "Name");
        var nameGet = ModuleDefinition.ImportReference(nameProperty.GetMethod);

        for (var i = 0; i < type.GenericParameters.Count; i++)
        {
            ins.Add(Instruction.Create(OpCodes.Ldloc_0));
            ins.Add(Instruction.Create(OpCodes.Ldc_I4, i));

            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Callvirt, getTypeMethod));
            ins.Add(Instruction.Create(OpCodes.Callvirt, getGenericArgumentsMethod));
            ins.Add(Instruction.Create(OpCodes.Ldc_I4, i));
            ins.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            ins.Add(Instruction.Create(OpCodes.Callvirt, nameGet));

            ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }
    }

    void AddMethodAttributes(MethodDefinition method)
    {
        var generatedConstructor = ModuleDefinition.ImportReference(typeof(GeneratedCodeAttribute)
            .GetConstructor([typeof(string), typeof(string)]));

        var version = typeof(ModuleWeaver).Assembly.GetName().Version.ToString();

        var generatedAttribute = new CustomAttribute(generatedConstructor);
        generatedAttribute.ConstructorArguments.Add(new(TypeSystem.StringReference, "Fody.ToString"));
        generatedAttribute.ConstructorArguments.Add(new(TypeSystem.StringReference, version));
        method.CustomAttributes.Add(generatedAttribute);

        var debuggerConstructor = ModuleDefinition.ImportReference(typeof(DebuggerNonUserCodeAttribute).GetConstructor(Type.EmptyTypes));
        var debuggerAttribute = new CustomAttribute(debuggerConstructor);
        method.CustomAttributes.Add(debuggerAttribute);
    }

    void AddEndCode(MethodBody body)
    {
        body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        body.Instructions.Add(Instruction.Create(OpCodes.Call, formatMethod));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    void AddInitCode(Collection<Instruction> ins, string format, PropertyDefinition[] properties, int genericOffset)
    {
        ins.Add(Instruction.Create(OpCodes.Call, getInvariantCulture));
        ins.Add(Instruction.Create(OpCodes.Ldstr, format));
        ins.Add(Instruction.Create(OpCodes.Ldc_I4, properties.Length + genericOffset));
        ins.Add(Instruction.Create(OpCodes.Newarr, TypeSystem.ObjectReference));
        ins.Add(Instruction.Create(OpCodes.Stloc_0));
    }

    void AddPropertyCode(MethodBody body, int index, PropertyDefinition property, TypeDefinition targetType, Collection<VariableDefinition> variables)
    {
        var ins = body.Instructions;

        ins.Add(Instruction.Create(OpCodes.Ldloc_0));
        ins.Add(Instruction.Create(OpCodes.Ldc_I4, index));

        var get = ModuleDefinition.ImportReference(property.GetGetMethod(targetType));

        ins.Add(Instruction.Create(OpCodes.Ldarg_0));
        ins.Add(Instruction.Create(OpCodes.Call, get));

        if (get.ReturnType.IsValueType)
        {
            var returnType = ModuleDefinition.ImportReference(property.GetMethod.ReturnType);
            if (returnType.FullName == "System.DateTime")
            {
                var convertToUtc = ModuleDefinition.ImportReference(returnType.Resolve().FindMethod("ToUniversalTime"));

                var variable = new VariableDefinition(returnType);
                variables.Add(variable);
                ins.Add(Instruction.Create(OpCodes.Stloc, variable));
                ins.Add(Instruction.Create(OpCodes.Ldloca, variable));
                ins.Add(Instruction.Create(OpCodes.Call, convertToUtc));
            }

            ins.Add(Instruction.Create(OpCodes.Box, returnType));
        }
        else
        {
            var propType = property.PropertyType.Resolve();
            var isCollection = !property.PropertyType.IsGenericParameter && propType.IsCollection();

            if (isCollection)
            {
                AssignFalseToFirstFLag(ins);

                If(ins,
                    nc => nc.Add(Instruction.Create(OpCodes.Dup)),
                    nt =>
                    {
                        GetEnumerator(nt);

                        NewStringBuilder(nt);

                        AppendString(nt, ListStart);

                        While(nt,
                            c =>
                            {
                                c.Add(Instruction.Create(OpCodes.Ldloc_2));
                                c.Add(Instruction.Create(OpCodes.Callvirt, moveNext));
                            },
                            b =>
                            {
                                AppendSeparator(b);

                                ins.Add(Instruction.Create(OpCodes.Ldloc_1));
                                If(ins,
                                    c =>
                                    {
                                        c.Add(Instruction.Create(OpCodes.Ldloc_2));
                                        c.Add(Instruction.Create(OpCodes.Callvirt, current));
                                    },
                                    t =>
                                    {
                                        t.Add(Instruction.Create(OpCodes.Call, getInvariantCulture));

                                        string format;
                                        var collectionType = ((GenericInstanceType)property.PropertyType).GenericArguments[0];
                                        if (HaveToAddQuotes(collectionType))
                                        {
                                            format = "\"{0}\"";
                                        }
                                        else
                                        {
                                            format = "{0}";
                                        }

                                        t.Add(Instruction.Create(OpCodes.Ldstr, format));

                                        t.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
                                        t.Add(Instruction.Create(OpCodes.Newarr, TypeSystem.ObjectReference));
                                        t.Add(Instruction.Create(OpCodes.Stloc, body.Variables[4]));
                                        t.Add(Instruction.Create(OpCodes.Ldloc, body.Variables[4]));

                                        t.Add(Instruction.Create(OpCodes.Ldc_I4_0));

                                        t.Add(Instruction.Create(OpCodes.Ldloc_2));
                                        t.Add(Instruction.Create(OpCodes.Callvirt, current));


                                        t.Add(Instruction.Create(OpCodes.Stelem_Ref));
                                        t.Add(Instruction.Create(OpCodes.Ldloc, body.Variables[4]));

                                        t.Add(Instruction.Create(OpCodes.Call, formatMethod));
                                    },
                                    _ => _.Add(Instruction.Create(OpCodes.Ldstr, "null")));
                                ins.Add(Instruction.Create(OpCodes.Callvirt, appendString));
                                ins.Add(Instruction.Create(OpCodes.Pop));
                            });

                        AppendString(ins, ListEnd);
                        StringBuilderToString(ins);
                    },
                    nf =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Pop));
                        ins.Add(Instruction.Create(OpCodes.Ldstr, "null"));
                    });
            }
            else
            {
                If(ins,
                    c =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Dup));
                        AddBoxing(property, targetType, c);
                    },
                    t => AddBoxing(property, targetType, t),
                    e =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Pop));
                        ins.Add(Instruction.Create(OpCodes.Ldstr, "null"));
                    });
            }
        }

        ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
    }

    static void AddBoxing(PropertyDefinition property, TypeDefinition targetType, Collection<Instruction> ins)
    {
        if (property.PropertyType.IsValueType || property.PropertyType.IsGenericParameter)
        {
            var genericType = property.PropertyType.GetGenericInstanceType(targetType);
            ins.Add(Instruction.Create(OpCodes.Box, genericType));
        }
    }

    void NewStringBuilder(Collection<Instruction> ins)
    {
        var stringBuilderConstructor = ModuleDefinition.ImportReference(typeof(StringBuilder).GetConstructor([]));
        ins.Add(Instruction.Create(OpCodes.Newobj, stringBuilderConstructor));
        ins.Add(Instruction.Create(OpCodes.Stloc_1));
    }

    void GetEnumerator(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Callvirt, getEnumerator));
        ins.Add(Instruction.Create(OpCodes.Stloc_2));
    }

    static void AssignFalseToFirstFLag(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        ins.Add(Instruction.Create(OpCodes.Stloc_3));
    }

    static void While(
        Collection<Instruction> ins,
        Action<Collection<Instruction>> condition,
        Action<Collection<Instruction>> body)
    {
        var loopBegin = Instruction.Create(OpCodes.Nop);
        var loopEnd = Instruction.Create(OpCodes.Nop);

        ins.Add(loopBegin);

        condition(ins);

        ins.Add(Instruction.Create(OpCodes.Brfalse, loopEnd));

        body(ins);

        ins.Add(Instruction.Create(OpCodes.Br, loopBegin));
        ins.Add(loopEnd);
    }

    void AppendString(Collection<Instruction> ins, string str)
    {
        ins.Add(Instruction.Create(OpCodes.Ldloc_1));
        ins.Add(Instruction.Create(OpCodes.Ldstr, str));
        ins.Add(Instruction.Create(OpCodes.Callvirt, appendString));
        ins.Add(Instruction.Create(OpCodes.Pop));
    }

    void StringBuilderToString(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Ldloc_1));
        var toStringMethod = ModuleDefinition.ImportReference(stringBuilderType.Resolve().FindMethod("ToString"));
        ins.Add(Instruction.Create(OpCodes.Callvirt, toStringMethod));
    }

    static void If(Collection<Instruction> ins,
        Action<Collection<Instruction>> condition,
        Action<Collection<Instruction>> thenStatement,
        Action<Collection<Instruction>> elseStatement)
    {
        var ifEnd = Instruction.Create(OpCodes.Nop);
        var ifElse = Instruction.Create(OpCodes.Nop);

        condition(ins);

        ins.Add(Instruction.Create(OpCodes.Brfalse, ifElse));

        thenStatement(ins);

        ins.Add(Instruction.Create(OpCodes.Br, ifEnd));
        ins.Add(ifElse);

        elseStatement(ins);

        ins.Add(ifEnd);
    }

    void AppendSeparator(Collection<Instruction> ins)
    {
        If(ins,
            _ => _.Add(Instruction.Create(OpCodes.Ldloc_3)),
            t => AppendString(t, PropertiesSeparator),
            e =>
            {
                ins.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                ins.Add(Instruction.Create(OpCodes.Stloc_3));
            });
    }


    string GetFormatString(TypeDefinition type, PropertyDefinition[] properties)
    {
        var builder = new StringBuilder();
        var offset = 0;

        if (WrapWithBrackets)
        {
            builder.Append("{{");
        }

        if (WriteTypeName)
        {
            builder.AppendFormat("T{0}\"", PropertyNameToValueSeparator);

            if (!type.HasGenericParameters)
            {
                builder.Append(type.Name);
            }
            else
            {
                var name = type.Name.Remove(type.Name.IndexOf('`'));
                offset = type.GenericParameters.Count;
                builder.Append(name);
                builder.Append('<');
                for (var i = 0; i < offset; i++)
                {
                    builder.Append("{");
                    builder.Append(i);
                    builder.Append("}");
                    if (i + 1 != offset)
                    {
                        builder.Append(PropertiesSeparator);
                    }
                }

                builder.Append('>');
            }

            builder.Append("\"" + PropertiesSeparator);
        }

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            builder.Append(property.Name);
            builder.Append(PropertyNameToValueSeparator);

            if (HaveToAddQuotes(property.PropertyType))
            {
                builder.Append('"');
            }

            builder.Append('{');
            builder.Append(i + offset);

            if (property.PropertyType.FullName == "System.DateTime")
            {
                builder.Append(":O");
            }

            if (property.PropertyType.FullName == "System.TimeSpan")
            {
                builder.Append(":c");
            }

            builder.Append("}");

            if (HaveToAddQuotes(property.PropertyType))
            {
                builder.Append('"');
            }

            if (i != properties.Length - 1)
            {
                builder.Append(PropertiesSeparator);
            }
        }

        if (WrapWithBrackets)
        {
            builder.Append("}}");
        }

        return builder.ToString();
    }

    static bool HaveToAddQuotes(TypeReference type)
    {
        var name = type.FullName;
        if (name is "System.String" or "System.Char" or "System.DateTime" or "System.TimeSpan" or "System.Guid")
        {
            return true;
        }

        var resolved = type.Resolve();
        return resolved is {IsEnum: true};
    }

    public override bool ShouldCleanReference => true;

    static void RemoveFodyAttributes(TypeDefinition type, PropertyDefinition[] allProperties)
    {
        type.RemoveAttribute("ToStringAttribute");
        foreach (var property in allProperties)
        {
            property.RemoveAttribute("IgnoreDuringToStringAttribute");
        }
    }

    static IEnumerable<PropertyDefinition> RemoveIgnoredProperties(IEnumerable<PropertyDefinition> allProperties)
    {
        return allProperties
            .Where(_ => _.CustomAttributes.All(y => y.AttributeType.Name != "IgnoreDuringToStringAttribute"));
    }

    class PropertyNameEqualityComparer : IEqualityComparer<PropertyDefinition>
    {
        public bool Equals(PropertyDefinition x, PropertyDefinition y)
            => (x == null && y == null) || x?.Name == y?.Name;

        public int GetHashCode(PropertyDefinition obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.Name.GetHashCode();
        }

        public static readonly PropertyNameEqualityComparer Default = new();
    }

    string PropertyNameToValueSeparator => ReadStringValueFromConfig("PropertyNameToValueSeparator", ": ");
    string PropertiesSeparator => ReadStringValueFromConfig("PropertiesSeparator", ", ");
    string ListStart => ReadStringValueFromConfig("ListStart", "[");
    string ListEnd => ReadStringValueFromConfig("ListEnd", "]");
    bool WrapWithBrackets => ReadBoolValueFromConfig("WrapWithBrackets", true);
    bool WriteTypeName => ReadBoolValueFromConfig("WriteTypeName", true);

    string ReadStringValueFromConfig(string nodeName, string defaultValue)
    {
        var node = Config?.Attributes().FirstOrDefault(a => a.Name.LocalName == nodeName);
        return node?.Value ?? defaultValue;
    }

    bool ReadBoolValueFromConfig(string nodeName, bool defaultValue)
    {
        var node = Config?.Attributes().FirstOrDefault(a => a.Name.LocalName == nodeName);
        return node != null && bool.TryParse(node.Value, out var nodeValue)
            ? nodeValue
            : defaultValue;
    }
}