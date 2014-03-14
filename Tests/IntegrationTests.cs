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

    [Test]
    public void ClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("Child");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InParent = 10;
        instance.InChild = 5;

        var result = instance.ToString();

        Assert.That(result, Is.EqualTo("{T: \"Child\", InChild: 5, InParent: 10}"));
    }

    [Test]
    public void ComplexClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("ComplexChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChildNumber = 1L;
        instance.InChildText = "2";
        instance.InChildCollection  = new int[] {3};
        instance.InParentNumber = 4L;
        instance.InParentText = "5";
        instance.InParentCollection  = new int[] {6};

        var result = instance.ToString();

        Assert.That(result, Is.EqualTo("{T: \"ComplexChild\", InChildNumber: 1, InChildText: \"2\", InChildCollection: [3], InParentNumber: 4, InParentText: \"5\", InParentCollection: [6]}"));
    }

    [Test]
    public void ClassWithGenericParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("GenericChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChild = "5";
        instance.GenericInParent = 6;
            
        var result = instance.ToString();

        Assert.That(result, Is.EqualTo("{T: \"GenericChild\", InChild: \"5\", GenericInParent: 6}"));
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

    [Test]
    public void GenericClassWithCollection()
    {
        var genericClassType = assembly.GetType("GenericClass`1");
        var propType = assembly.GetType("GenericClassNormalClass");
        var instanceType = genericClassType.MakeGenericType(propType);

        dynamic instance = Activator.CreateInstance(instanceType);
        instance.A = 1;

        dynamic propInstance = Activator.CreateInstance(propType);
        propInstance.D = 2;
        propInstance.C = 3;

        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), new object[] { 1 });
        array[0] = propInstance;

        instance.B = array;

        var result = instance.ToString();

        Assert.AreEqual("{T: \"GenericClass<GenericClassNormalClass>\", A: 1, B: [{T: \"GenericClassNormalClass\", D: 2, C: 3}]}", result);
    }

    [Test]
    public void WithoutGenericParameter()
    {
        var withoutGenericParameterType = assembly.GetType("WithoutGenericParameter");
        var propType = assembly.GetType("GenericClassNormalClass");

        dynamic instance = Activator.CreateInstance(withoutGenericParameterType);
        instance.Z = 12;
        instance.A = 1;
        dynamic propInstance = Activator.CreateInstance(propType);
        propInstance.D = 3;
        propInstance.C = -4;
        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), new object[] { 1 });
        array[0] = propInstance;
        instance.B = array;

        var result = instance.ToString();

        Assert.AreEqual("{T: \"WithoutGenericParameter\", Z: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: -4}]}", result);
    }

    [Test]
    public void WithGenericParameter()
    {
        var withGenericParameterType = assembly.GetType("WithGenericParameter`1");
        var propType = assembly.GetType("GenericClassNormalClass");
        var instanceType = withGenericParameterType.MakeGenericType(propType);

        dynamic instance = Activator.CreateInstance(instanceType);
        instance.X = 12;
        instance.A = 1;
        dynamic propInstance = Activator.CreateInstance(propType);
        propInstance.D = 3;
        propInstance.C = 4;
        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), new object[] { 1 });
        array[0] = propInstance;
        instance.B = array;

        var result = instance.ToString();

        Assert.AreEqual("{T: \"WithGenericParameter<GenericClassNormalClass>\", X: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: 4}]}", result);
    }

    [Test]
    public void WithGenericProperty()
    {
        var withGenericPropertyType = assembly.GetType("WithPropertyOfGenericType`1");
        var propType = assembly.GetType("GenericClassNormalClass");
        var instanceType = withGenericPropertyType.MakeGenericType(propType);

        dynamic instance = Activator.CreateInstance(instanceType);
        dynamic propInstance = Activator.CreateInstance(propType);
        instance.GP = propInstance;
        propInstance.C = 1;
        propInstance.D = 3;

        var result = instance.ToString();

        Assert.That(result, Is.EqualTo("{T: \"WithPropertyOfGenericType<GenericClassNormalClass>\", GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}"));
    }

    [Test]
    public void WithInheritedGenericProperty()
    {
        var withGenericPropertyType = assembly.GetType("WithInheritedPropertyOfGenericType");

        dynamic instance = Activator.CreateInstance(withGenericPropertyType);
        var propType = assembly.GetType("GenericClassNormalClass");
        dynamic propInstance = Activator.CreateInstance(propType);
        instance.GP = propInstance;
        propInstance.C = 1;
        propInstance.D = 3;
        instance.X = 6;

        var result = instance.ToString();

        Assert.That(result, Is.EqualTo("{T: \"WithInheritedPropertyOfGenericType\", X: 6, GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}"));
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

    [Test]
    public void TimeClassTest()
    {
        var type = assembly.GetType( "TimeClass" );
        dynamic instance = Activator.CreateInstance( type );
        instance.X = new DateTime(1988, 05, 23, 10, 30, 0, DateTimeKind.Utc);
        instance.Y = new TimeSpan(1, 2, 3, 4);

        var result = instance.ToString();

        Assert.AreEqual( "{T: \"TimeClass\", X: \"1988-05-23T10:30:00.0000000Z\", Y: \"1.02:03:04\"}", result );
    }

    [Test]
    public void IndexerTest()
    {
        var type = assembly.GetType("ClassWithIndexer");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = 2;

        var result = instance.ToString();

        Assert.AreEqual("{T: \"ClassWithIndexer\", X: 1, Y: 2}", result);
    }
}