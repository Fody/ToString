using Mono.Cecil;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Xml.Linq;

public static class TestHelper
{
    public static TestSetupDefinition PrepareDll(string assemblyNameSuffix)
    {

        string beforeAssemblyPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.dll"));
#if (!DEBUG)

        beforeAssemblyPath = beforeAssemblyPath.Replace("Debug", "Release");
#endif

        string afterAssemblyPath = beforeAssemblyPath.Replace(".dll", assemblyNameSuffix + ".dll");
        File.Copy(beforeAssemblyPath, afterAssemblyPath, true);

        var assemblyResolver = new MockAssemblyResolver
        {
            Directory = Path.GetDirectoryName(beforeAssemblyPath)
        };
        var moduleDefinition = ModuleDefinition.ReadModule(afterAssemblyPath, new ReaderParameters
        {
            AssemblyResolver = assemblyResolver
        });

        return new TestSetupDefinition
        {
            AfterAssemblyPath = afterAssemblyPath,
            ModuleDefinition = moduleDefinition,
            MockAssemblyResolver = assemblyResolver
        };
    }

    public static XElement PrepareConfig(AttributesConfiguration configuration)
    {
        XElement config;

        var configXml = new StringBuilder();
        configXml.Append("<ToString ");
        if (!string.IsNullOrEmpty(configuration.PropertyNameToValueSeparator))
        {
            configXml.AppendFormat("PropertyNameToValueSeparator=\"{0}\" ", configuration.PropertyNameToValueSeparator);
        }
        if (!string.IsNullOrEmpty(configuration.PropertiesSeparator))
        {
            configXml.AppendFormat("PropertiesSeparator=\"{0}\" ", configuration.PropertiesSeparator);
        }
        if (configuration.WrapWithBrackets.HasValue)
        {
            configXml.AppendFormat("WrapWithBrackets=\"{0}\" ", configuration.WrapWithBrackets);
        }
        if (configuration.WriteTypeName.HasValue)
        {
            configXml.AppendFormat("WriteTypeName=\"{0}\" ", configuration.WriteTypeName);
        }
        if (!string.IsNullOrEmpty(configuration.ListStart))
        {
            configXml.AppendFormat("ListStart=\"{0}\" ", configuration.ListStart);
        }
        if (!string.IsNullOrEmpty(configuration.ListEnd))
        {
            configXml.AppendFormat("ListEnd=\"{0}\" ", configuration.ListEnd);
        }
        configXml.Append("/>");

        using (var configStream = GenerateStreamFromString(configXml.ToString()))
        {
            config = XElement.Load(configStream);
        }

        return config;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}