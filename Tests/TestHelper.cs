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

    public static XElement PrepareConfig(string propertyNameToValueSeparator, string propertiesSeparator, bool? wrapWithBrackets, bool? writeTypeName)
    {
        XElement config;

        var configXml = new StringBuilder();
        configXml.Append("<ToString ");
        if (!string.IsNullOrEmpty(propertyNameToValueSeparator))
        {
            configXml.AppendFormat("PropertyNameToValueSeparator=\"{0}\" ", propertyNameToValueSeparator);
        }
        if (!string.IsNullOrEmpty(propertiesSeparator))
        {
            configXml.AppendFormat("PropertiesSeparator=\"{0}\" ", propertiesSeparator);
        }
        if (wrapWithBrackets.HasValue)
        {
            configXml.AppendFormat("WrapWithBrackets=\"{0}\" ", wrapWithBrackets);
        }
        if (writeTypeName.HasValue)
        {
            configXml.AppendFormat("WriteTypeName=\"{0}\" ", writeTypeName);
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