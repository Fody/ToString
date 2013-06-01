using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

public class ModuleWeaver
{
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarning { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }
    public XElement Config { get; set; }

    public ModuleWeaver()
    {
        LogInfo = Console.WriteLine;
        LogWarning = Console.WriteLine;
    }

    public IEnumerable<TypeDefinition> GetMachingTypes()
    {
        return ModuleDefinition.GetTypes().Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "ToStringAttribute"));
    }

    public void Execute()
    {
        foreach (var type in GetMachingTypes())
        {
            AddToString(type);
        }
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
        var properties = GetPublicProperties(type);

        var format = GetFormatString(type, properties);

        var body = method.Body;
        var ins = method.Body.Instructions;
        this.AddInitCode(ins, format, properties);

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            AddPropertyCode(body, i, property);
        }

        this.AddEndCode(body);

        type.Methods.Add(method);
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
        var cultureInfoType = ModuleDefinition.Import(typeof(System.Globalization.CultureInfo)).Resolve();
        var invariantCulture = cultureInfoType.Properties.Single(x => x.Name == "InvariantCulture");
        var getInvariantCulture = ModuleDefinition.Import(invariantCulture.GetMethod);
        ins.Add(Instruction.Create(OpCodes.Call, getInvariantCulture));
        ins.Add(Instruction.Create(OpCodes.Ldstr, format));
        ins.Add(Instruction.Create(OpCodes.Ldc_I4, properties.Length));
        ins.Add(Instruction.Create(OpCodes.Newarr, this.ModuleDefinition.TypeSystem.Object));
        ins.Add(Instruction.Create(OpCodes.Stloc_0));
    }

    private static void AddPropertyCode(MethodBody body, int i, PropertyDefinition property)
    {
        body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));

        var get = property.GetMethod;
        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        body.Instructions.Add(Instruction.Create(OpCodes.Call, get));
        if (!get.ReturnType.IsByReference)
        {
            body.Instructions.Add(Instruction.Create(OpCodes.Box, property.GetMethod.ReturnType));
        }

        body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
    }

    private string GetFormatString(TypeDefinition type, PropertyDefinition[] properties)
    {
        var sb = new StringBuilder();
        sb.Append("{{T: ");
        sb.Append(type.Name);
        sb.Append(", ");
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            sb.Append(property.Name);
            sb.Append(": ");

            if (AddQuotes(property))
            {
                sb.Append('"');
            }

            sb.Append('{');
            sb.Append(i);
            sb.Append("}");

            if (AddQuotes(property))
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

    private static bool AddQuotes(PropertyDefinition property)
    {
        return property.PropertyType.Name == "String";
    }
}