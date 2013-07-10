using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System.Globalization;

public class ModuleWeaver
{
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }
    public XElement Config { get; set; }

    private TypeReference stringBuilderType;

    private MethodReference appendString;
    private MethodReference moveNext;
    private MethodReference currrent;
    private MethodReference getEnumerator;
    private MethodReference getInvariantCulture;
    private MethodReference formatMethod;

    public IEnumerable<TypeDefinition> GetMachingTypes()
    {
        return ModuleDefinition.GetTypes().Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "ToStringAttribute"));
    }

    public void Execute()
    {
        stringBuilderType = ModuleDefinition.Import(typeof (StringBuilder));
        appendString = ModuleDefinition.Import(typeof(StringBuilder).GetMethod("Append", new[] { typeof(object) }));
        moveNext = ModuleDefinition.Import(typeof(IEnumerator).GetMethod("MoveNext"));
        currrent = ModuleDefinition.Import(typeof(IEnumerator).GetProperty("Current").GetGetMethod());
        getEnumerator = ModuleDefinition.Import(typeof(IEnumerable).GetMethod("GetEnumerator"));
        formatMethod = this.ModuleDefinition.Import(this.ModuleDefinition.TypeSystem.String.Resolve().FindMethod("Format", "IFormatProvider", "String", "Object[]"));

        var cultureInfoType = ModuleDefinition.Import(typeof(CultureInfo)).Resolve();
        var invariantCulture = cultureInfoType.Properties.Single(x => x.Name == "InvariantCulture");
        getInvariantCulture = ModuleDefinition.Import(invariantCulture.GetMethod);

        foreach (var type in GetMachingTypes())
        {
            AddToString(type);
        }

        this.RemoveReference();
    }

    private PropertyDefinition[] GetPublicProperties(TypeDefinition type)
    {
        return type.Properties.Where(x => x.GetMethod != null).ToArray();
    }

    private void AddToString(TypeDefinition type)
    {
        var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
        var strType = ModuleDefinition.TypeSystem.String;
        var method = new MethodDefinition("ToString", methodAttributes, strType);
        method.Body.Variables.Add(new VariableDefinition(new ArrayType(ModuleDefinition.TypeSystem.Object)));
        var allProperties = GetPublicProperties(type);
        var properties = RemoveIgnoredProperties(allProperties);

        var format = GetFormatString(type, properties);

        var body = method.Body;
        var ins = body.Instructions;

        var hasCollections = properties.Any(x => x.PropertyType.Resolve().IsCollection());
        if (hasCollections)
        {
            method.Body.Variables.Add(new VariableDefinition(stringBuilderType));

            var enumeratorType = this.ModuleDefinition.Import(typeof (IEnumerator));
            method.Body.Variables.Add(new VariableDefinition(enumeratorType));

            method.Body.Variables.Add(new VariableDefinition(ModuleDefinition.TypeSystem.Boolean));

            method.Body.Variables.Add(new VariableDefinition(new ArrayType(ModuleDefinition.TypeSystem.Object)));
        }

        this.AddInitCode(ins, format, properties);

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            AddPropertyCode(method.Body, i, property);
        }

        this.AddEndCode(body);
        body.OptimizeMacros();

        type.Methods.Add(method);

        this.RemoveFodyAttributes(type, allProperties);
    }

    private void AddEndCode(MethodBody body)
    {
        var stringType = this.ModuleDefinition.TypeSystem.String.Resolve();
        var formatMethod = this.ModuleDefinition.Import(stringType.FindMethod("Format", "IFormatProvider", "String", "Object[]"));
        body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        body.Instructions.Add(Instruction.Create(OpCodes.Call, formatMethod));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    private void AddInitCode(Collection<Instruction> ins, string format, PropertyDefinition[] properties)
    {
        var cultureInfoType = ModuleDefinition.Import(typeof(CultureInfo)).Resolve();
        var invariantCulture = cultureInfoType.Properties.Single(x => x.Name == "InvariantCulture");
        var getInvariantCulture = ModuleDefinition.Import(invariantCulture.GetMethod);
        ins.Add(Instruction.Create(OpCodes.Call, getInvariantCulture));
        ins.Add(Instruction.Create(OpCodes.Ldstr, format));
        ins.Add(Instruction.Create(OpCodes.Ldc_I4, properties.Length));
        ins.Add(Instruction.Create(OpCodes.Newarr, this.ModuleDefinition.TypeSystem.Object));
        ins.Add(Instruction.Create(OpCodes.Stloc_0));
    }

    private void AddPropertyCode(MethodBody body, int index, PropertyDefinition property)
    {
        var ins = body.Instructions;

        ins.Add(Instruction.Create(OpCodes.Ldloc_0));
        ins.Add(Instruction.Create(OpCodes.Ldc_I4, index));

        var get = property.GetMethod;
        ins.Add(Instruction.Create(OpCodes.Ldarg_0));
        ins.Add(Instruction.Create(OpCodes.Call, get));

        if ( get.ReturnType.IsValueType)
        {
            ins.Add(Instruction.Create(OpCodes.Box, property.GetMethod.ReturnType));
        }
        else
        {
            var propType = property.PropertyType.Resolve();
            var isCollection = propType.IsCollection();

            if (isCollection)
            {
                AssignFalseToFirstFLag(ins);

                this.If(ins, 
                    nc =>
                    {
                        nc.Add(Instruction.Create(OpCodes.Dup));
                    },
                    nt =>
                    {
                        this.GetEnumerator(nt);

                        this.NewStringBuilder(nt);

                        this.AppendString(nt, "[");

                        this.While(nt,
                            c =>
                            {
                                c.Add(Instruction.Create(OpCodes.Ldloc_2));
                                c.Add(Instruction.Create(OpCodes.Callvirt, moveNext));
                            },
                            b =>
                            {
                                AppendSeparator(b, appendString);

                                ins.Add(Instruction.Create(OpCodes.Ldloc_1));
                                this.If(ins,
                                    c =>
                                    {
                                        c.Add(Instruction.Create(OpCodes.Ldloc_2));
                                        c.Add(Instruction.Create(OpCodes.Callvirt, currrent));
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
                                        t.Add(Instruction.Create(OpCodes.Newarr, this.ModuleDefinition.TypeSystem.Object)); 
                                        t.Add(Instruction.Create(OpCodes.Stloc, body.Variables[4])); 
                                        t.Add(Instruction.Create(OpCodes.Ldloc, body.Variables[4])); 

                                        t.Add(Instruction.Create(OpCodes.Ldc_I4_0)); 

                                        t.Add(Instruction.Create(OpCodes.Ldloc_2)); 
                                        t.Add(Instruction.Create(OpCodes.Callvirt, currrent)); 


                                        t.Add(Instruction.Create(OpCodes.Stelem_Ref));
                                        t.Add(Instruction.Create(OpCodes.Ldloc, body.Variables[4])); 

                                        t.Add(Instruction.Create(OpCodes.Call, formatMethod)); 
                                    },
                                    e =>
                                    {
                                        e.Add(Instruction.Create(OpCodes.Ldstr, "null"));
                                    });
                                ins.Add(Instruction.Create(OpCodes.Callvirt, appendString));
                                ins.Add(Instruction.Create(OpCodes.Pop));
                            });

                        this.AppendString(ins, "]");
                        this.StringBuilderToString(ins);       
                    },
                    nf =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Pop));
                        ins.Add(Instruction.Create(OpCodes.Ldstr, "null")); 
                    });              
            }
            else
            {
                this.If(ins, 
                    c =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Dup));  
                    },
                    t => {},
                    e =>
                    {
                        ins.Add(Instruction.Create(OpCodes.Pop));
                        ins.Add(Instruction.Create(OpCodes.Ldstr, "null"));   
                    });
            }
        }

        ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
    }

    private void NewStringBuilder(Collection<Instruction> ins)
    {
        var stringBuilderConstructor = this.ModuleDefinition.Import(typeof (StringBuilder).GetConstructor(new Type[] {}));
        ins.Add(Instruction.Create(OpCodes.Newobj, stringBuilderConstructor));
        ins.Add(Instruction.Create(OpCodes.Stloc_1));
    }

    private void GetEnumerator(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Callvirt, this.getEnumerator));
        ins.Add(Instruction.Create(OpCodes.Stloc_2));
    }

    private static void AssignFalseToFirstFLag(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        ins.Add(Instruction.Create(OpCodes.Stloc_3));
    }

    private void While(
        Collection<Instruction> ins,
        Action<Collection<Instruction>> condition,
        Action<Collection<Instruction>> body )
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

    private void AppendString(Collection<Instruction> ins, string str)
    {
        ins.Add(Instruction.Create(OpCodes.Ldloc_1));
        ins.Add(Instruction.Create(OpCodes.Ldstr, str));
        ins.Add(Instruction.Create(OpCodes.Callvirt, appendString));
        ins.Add(Instruction.Create(OpCodes.Pop));
    }

    private void StringBuilderToString(Collection<Instruction> ins)
    {
        ins.Add(Instruction.Create(OpCodes.Ldloc_1));
        var toStringMethod = this.ModuleDefinition.Import(stringBuilderType.Resolve().FindMethod("ToString"));
        ins.Add(Instruction.Create(OpCodes.Callvirt, toStringMethod));
    }

    private void If(Collection<Instruction> ins,
                    Action<Collection<Instruction>> condition,
                    Action<Collection<Instruction>> thenStatment,
                    Action<Collection<Instruction>> elseStetment)
    {
        var ifEnd = Instruction.Create(OpCodes.Nop);
        var ifElse = Instruction.Create(OpCodes.Nop);

        condition(ins);

        ins.Add(Instruction.Create(OpCodes.Brfalse, ifElse));

        thenStatment(ins);

        ins.Add(Instruction.Create(OpCodes.Br, ifEnd));
        ins.Add(ifElse);

        elseStetment(ins);

        ins.Add(ifEnd);
    }

    private void AppendSeparator(Collection<Instruction> ins, MethodReference appendString)
    {
        If(ins,
           c =>
               {
                   c.Add(Instruction.Create(OpCodes.Ldloc_3));
               },
           t =>
               {
                   AppendString(t, ", ");
               },
           e =>
               {
                   ins.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                   ins.Add(Instruction.Create(OpCodes.Stloc_3));
               });
    }

    private string GetFormatString(TypeDefinition type, PropertyDefinition[] properties)
    {
        var sb = new StringBuilder();
        sb.Append("{{T: \"");
        sb.Append(type.Name);
        sb.Append("\", ");
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            sb.Append(property.Name);
            sb.Append(": ");

            if (HaveToAddQuotes(property.PropertyType))
            {
                sb.Append('"');
            }

            sb.Append('{');
            sb.Append(i);
            sb.Append("}");

            if (HaveToAddQuotes(property.PropertyType))
            {
                sb.Append('"');
            }

            if (i != properties.Length - 1)
            {
                sb.Append(", ");
            }
        }
        sb.Append("}}");
        var format = sb.ToString();
        return format;
    }

    private static bool HaveToAddQuotes(TypeReference type)
    {
        var name = type.FullName;
        return name == "System.String" || name == "System.Char" || type.Resolve().IsEnum;
    }

    private void RemoveReference()
    {
        var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "ToString");
        if (referenceToRemove != null)
        {
            ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
        }
    }

    private void RemoveFodyAttributes(TypeDefinition type, PropertyDefinition[] allProperties)
    {
        type.RemoveAttribute("ToStringAttribute");
        foreach (var property in allProperties)
        {
            property.RemoveAttribute("IgnoreDuringToStringAttribute");
        }
    }

    private PropertyDefinition[] RemoveIgnoredProperties(PropertyDefinition[] allProperties)
    {
        return allProperties.Where(x => x.CustomAttributes.All(y => y.AttributeType.Name != "IgnoreDuringToStringAttribute")).ToArray();
    }
}