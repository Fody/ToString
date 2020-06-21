using System;
using System.Collections.Generic;
using System.Reflection;
using Fody;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class IntegrationTests
{
    static Assembly assembly;
    static TestResult testResult;

    static IntegrationTests()
    {
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        assembly = testResult.Assembly;
    }

    [Fact]
    public void NormalClassTest()
    {
        var instance = testResult.GetInstance("NormalClass");
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        var result  = instance.ToString();

        Assert.Equal("{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}", result);
    }

    [Fact]
    public void NormalStructTest()
    {
        var instance = testResult.GetInstance("NormalStruct");
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;

        var result = instance.ToString();

        Assert.Equal("{T: \"NormalStruct\", X: 1, Y: \"2\", Z: 4.5}", result);
    }

    [Fact]
    public void NestedClassTest()
    {
        var normalInstance = testResult.GetInstance("NormalClass");
        normalInstance.X = 1;
        normalInstance.Y = "2";
        normalInstance.Z = 4.5;
        normalInstance.V = 'V';
        var nestedInstance = testResult.GetInstance("NestedClass");
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = normalInstance;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: {T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"V\"}}", result);
    }

    [Fact]
    public void ClassWithIgnoredPropertiesTest()
    {
        var type = assembly.GetType("ClassWithIgnoredProperties");
        dynamic instance = Activator.CreateInstance(type);
        instance.Username = "user";
        instance.Password = "pass";
        instance.Age = 18;

        var result = instance.ToString();

        Assert.Equal("{T: \"ClassWithIgnoredProperties\", Username: \"user\", Age: 18}", result);
    }

    [Fact]
    public void NullTest()
    {
        var nestedType = assembly.GetType("NestedClass");
        dynamic nestedInstance = Activator.CreateInstance(nestedType);
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = null;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: null}", result);
    }

    [Fact]
    public void ClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("Child");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InParent = 10;
        instance.InChild = 5;

        var result = instance.ToString();

        Assert.Equal(result, "{T: \"Child\", InChild: 5, InParent: 10}");
    }

    [Fact]
    public void ComplexClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("ComplexChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChildNumber = 1L;
        instance.InChildText = "2";
        instance.InChildCollection  = new[] {3};
        instance.InParentNumber = 4L;
        instance.InParentText = "5";
        instance.InParentCollection  = new[] {6};

        var result = instance.ToString();

        Assert.Equal(result, "{T: \"ComplexChild\", InChildNumber: 1, InChildText: \"2\", InChildCollection: [3], InParentNumber: 4, InParentText: \"5\", InParentCollection: [6]}");
    }

    [Fact]
    public void ClassWithGenericParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("GenericChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChild = "5";
        instance.GenericInParent = 6;

        var result = instance.ToString();

        Assert.Equal(result, "{T: \"GenericChild\", InChild: \"5\", GenericInParent: 6}");
    }

    [Fact]
    public void GuidErrorTest()
    {
        var type = assembly.GetType( "ReferenceObject" );
        dynamic instance = Activator.CreateInstance( type );
        instance.Id = Guid.Parse( "{f6ab1abe-5811-40e9-8154-35776d2e5106}" );
        instance.Name = "Test";

        var result = instance.ToString();

        Assert.Equal( "{T: \"ReferenceObject\", Name: \"Test\", Id: \"f6ab1abe-5811-40e9-8154-35776d2e5106\"}", result );
    }

    #region Collections

    [Fact]
    public void IntArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new[] { 1, 2, 3, 4, 5, 6 };
        nestedInstance.Count = 2;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6]}", result);
    }

    [Fact]
    public void StringArray()
    {
        var type = assembly.GetType("StringCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new List<string> { "foo", "bar" };
        nestedInstance.Count = 2;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"StringCollection\", Count: 2, Collection: [\"foo\", \"bar\"]}", result);
    }

    [Fact]
    public void EmptyArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new int[] {};
        nestedInstance.Count = 0;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"IntCollection\", Count: 0, Collection: []}", result);
    }

    [Fact]
    public void NullArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = null;
        nestedInstance.Count = 0;

        var result = nestedInstance.ToString();

        Assert.Equal("{T: \"IntCollection\", Count: 0, Collection: null}", result);
    }

    [Fact]
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

        dynamic array = Activator.CreateInstance(type.MakeArrayType(), 2);
        array[0] = instance;
        array[1] = null;

        arrayInstance.Collection = array;

        var result = arrayInstance.ToString();

        Assert.Equal("{T: \"ObjectCollection\", Count: 2, Collection: [{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}, null]}", result);
    }

    [Fact]
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

        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), 1);
        array[0] = propInstance;

        instance.B = array;

        var result = instance.ToString();

        Assert.Equal("{T: \"GenericClass<GenericClassNormalClass>\", A: 1, B: [{T: \"GenericClassNormalClass\", D: 2, C: 3}]}", result);
    }

    [Fact]
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
        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), 1);
        array[0] = propInstance;
        instance.B = array;

        var result = instance.ToString();

        Assert.Equal("{T: \"WithoutGenericParameter\", Z: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: -4}]}", result);
    }

    [Fact]
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
        dynamic array = Activator.CreateInstance(propType.MakeArrayType(), 1);
        array[0] = propInstance;
        instance.B = array;

        var result = instance.ToString();

        Assert.Equal("{T: \"WithGenericParameter<GenericClassNormalClass>\", X: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: 4}]}", result);
    }

    [Fact]
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

        Assert.Equal(result, "{T: \"WithPropertyOfGenericType<GenericClassNormalClass>\", GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}");
    }

    [Fact]
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

        Assert.Equal(result, "{T: \"WithInheritedPropertyOfGenericType\", X: 6, GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}");
    }

    #endregion

    #region enums

    [Fact]
    public void EmptyEnum()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type);

        var result = instance.ToString();

        Assert.Equal("{T: \"EnumClass\", NormalEnum: \"A\", FlagsEnum: \"G\"}", result);
    }

    [Fact]
    public void EnumWithValues()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type, 3, 6);

        var result = instance.ToString();

        Assert.Equal("{T: \"EnumClass\", NormalEnum: \"D\", FlagsEnum: \"I, J\"}", result);
    }


    #endregion

    [Fact]
    public void TimeClassTest()
    {
        var type = assembly.GetType( "TimeClass" );
        dynamic instance = Activator.CreateInstance( type );
        instance.X = new DateTime(1988, 05, 23, 10, 30, 0, DateTimeKind.Utc);
        instance.Y = new TimeSpan(1, 2, 3, 4);

        var result = instance.ToString();

        Assert.Equal( "{T: \"TimeClass\", X: \"1988-05-23T10:30:00.0000000Z\", Y: \"1.02:03:04\"}", result );
    }

    [Fact]
    public void IndexerTest()
    {
        var type = assembly.GetType("ClassWithIndexer");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = 2;

        var result = instance.ToString();

        Assert.Equal("{T: \"ClassWithIndexer\", X: 1, Y: 2}", result);
    }

    [Fact]
    public void RemoveToStringMethod()
    {
        var type = assembly.GetType("ClassWithToString");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = 2;

        var result = instance.ToString();

        Assert.Equal("{T: \"ClassWithToString\", X: 1, Y: 2}", result);
    }

    [Fact]
    public void GuidClassTest()
    {
        var type = assembly.GetType( "GuidClass" );
        dynamic instance = Activator.CreateInstance( type );
        instance.X = 1;
        instance.Y = new Guid(1,2,3,4,5,6,7,8,9,10,11);

        var result = instance.ToString();

        Assert.Equal( "{T: \"GuidClass\", X: 1, Y: \"00000001-0002-0003-0405-060708090a0b\"}", result );
    }

    [Fact]
    public void ClassWithDerivedPropertiesTest()
    {
        var type = assembly.GetType("ClassWithDerivedProperties");
        dynamic instance = Activator.CreateInstance(type);
        var result = instance.ToString();

        Assert.Equal("{T: \"ClassWithDerivedProperties\", NormalProperty: \"New\", INormalProperty.NormalProperty: \"Interface\", VirtualProperty: \"Override Virtual\", AbstractProperty: \"Override Abstract\"}", result);
    }

    public IntegrationTests(ITestOutputHelper output) : 
        base(output)
    {
    }
}