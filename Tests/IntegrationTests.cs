using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;


[TestFixture]
public class IntegrationTests
{
    Assembly assembly;
    List<string> warnings = new List<string>();
    string beforeAssemblyPath;
    string afterAssemblyPath;

    public IntegrationTests()
    {
        beforeAssemblyPath = Path.GetFullPath(@"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)

        beforeAssemblyPath = beforeAssemblyPath.Replace("Debug", "Release");
#endif

        afterAssemblyPath = beforeAssemblyPath.Replace(".dll", "2.dll");
        File.Copy(beforeAssemblyPath, afterAssemblyPath, true);
        
        var assemblyResolver = new MockAssemblyResolver
            {
                Directory = Path.GetDirectoryName(beforeAssemblyPath)
            };
        var moduleDefinition = ModuleDefinition.ReadModule(afterAssemblyPath,new ReaderParameters
            {
                AssemblyResolver = assemblyResolver
            });
        var weavingTask = new ModuleWeaver
                              {
                                  ModuleDefinition = moduleDefinition,
                                  AssemblyResolver = assemblyResolver,
                              };

        weavingTask.Execute();
        moduleDefinition.Write(afterAssemblyPath);

        assembly = Assembly.LoadFile(afterAssemblyPath);
    }

    [Test]
    public void NormalClassTest()
    {
        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;

        var result  = instance.ToString();        

        Assert.AreEqual("{T: NormalClass, X: 1, Y: \"2\", Z: 4.5}", result);
    }

    [Test]
    public void NormalStructTest()
    {
        var type = assembly.GetType("NormalStruct");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;

        var result = instance.ToString();

        Assert.AreEqual("{T: NormalStruct, X: 1, Y: \"2\", Z: 4.5}", result);
    }

    [Test]
    public void NestedClassTest()
    {
        var normalType = assembly.GetType("NormalClass");
        dynamic noramlInstance = Activator.CreateInstance(normalType);
        noramlInstance.X = 1;
        noramlInstance.Y = "2";
        noramlInstance.Z = 4.5;
        var nestedType = assembly.GetType("NestedClass");
        dynamic nestedInstance = Activator.CreateInstance(nestedType);
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = noramlInstance;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: NestedClass, A: 10, B: \"11\", C: 12.25, D: {T: NormalClass, X: 1, Y: \"2\", Z: 4.5}}", result);
    }

    [Test]
    public void ClassWithIgnoredPropertiesTest()
    {
        var type = assembly.GetType("ClassWithIgnoredProperties");
        dynamic instance = Activator.CreateInstance(type);
        instance.Username = "user";
        instance.Password = "pass";
        instance.Age = 18;

        var result = instance.ToString();

        Assert.AreEqual("{T: ClassWithIgnoredProperties, Username: \"user\", Age: 18}", result);
    }
}