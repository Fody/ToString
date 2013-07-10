using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
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
        instance.V = 'C';

        var result  = instance.ToString();

        Assert.AreEqual("{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}", result);
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

        Assert.AreEqual("{T: \"NormalStruct\", X: 1, Y: \"2\", Z: 4.5}", result);
    }

    [Test]
    public void NestedClassTest()
    {
        var normalType = assembly.GetType("NormalClass");
        dynamic noramlInstance = Activator.CreateInstance(normalType);
        noramlInstance.X = 1;
        noramlInstance.Y = "2";
        noramlInstance.Z = 4.5;
        noramlInstance.V = 'V';
        var nestedType = assembly.GetType("NestedClass");
        dynamic nestedInstance = Activator.CreateInstance(nestedType);
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = noramlInstance;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: {T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"V\"}}", result);
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

        Assert.AreEqual("{T: \"ClassWithIgnoredProperties\", Username: \"user\", Age: 18}", result);
    }

    [Test]
    public void NullTest()
    {
        var nestedType = assembly.GetType("NestedClass");
        dynamic nestedInstance = Activator.CreateInstance(nestedType);
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = null;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: null}", result);
    }

    #region Collections

    [Test]
    public void IntArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new int[] { 1, 2, 3, 4, 5, 6 };
        nestedInstance.Count = 2;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6]}", result);
    }

    [Test]
    public void StringArray()
    {
        var type = assembly.GetType("StringCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new List<string> { "foo", "bar" };
        nestedInstance.Count = 2;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"StringCollection\", Count: 2, Collection: [\"foo\", \"bar\"]}", result);
    }

    [Test]
    public void EmptyArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new int[] {};
        nestedInstance.Count = 0;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"IntCollection\", Count: 0, Collection: []}", result);
    }

    [Test]
    public void NullArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = null;
        nestedInstance.Count = 0;

        var result = nestedInstance.ToString();

        Assert.AreEqual("{T: \"IntCollection\", Count: 0, Collection: null}", result);
    }

    [Test]
    public void ObjectArray()
    {
        var arrayType = assembly.GetType("ObjectCollection");
        dynamic arrayInstance = Activator.CreateInstance(arrayType);
        arrayInstance.Count = 2;

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        dynamic array = Activator.CreateInstance(type.MakeArrayType(), new object[] { 2 });
        array[0] = instance;
        array[1] = null;

        arrayInstance.Collection = array;

        var result = arrayInstance.ToString();

        Assert.AreEqual("{T: \"ObjectCollection\", Count: 2, Collection: [{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}, null]}", result);
    }

    #endregion

    #region enums

    [Test]
    public void EmptyEnum()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type);

        var result = instance.ToString();

        Assert.AreEqual("{T: \"EnumClass\", NormalEnum: \"A\", FlagsEnum: \"G\"}", result);
    }

    [Test]
    public void EnumWithValues()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type,new object[]{3,6});

        var result = instance.ToString();

        Assert.AreEqual("{T: \"EnumClass\", NormalEnum: \"D\", FlagsEnum: \"I, J\"}", result);
    }


    #endregion
}