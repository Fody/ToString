using System;
using System.Collections.Generic;
using System.Reflection;
using Fody;
using TestResult = Fody.TestResult;

// weaving runs share the AssemblyToProcess files and the weaver test output folders
[NotInParallel]
public class IntegrationTests
{
    static Assembly assembly;
    static TestResult testResult;

    static IntegrationTests()
    {
        var weaver = new ModuleWeaver();
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll");
        assembly = testResult.Assembly;
    }

    [Test]
    public async Task NormalClassTest()
    {
        var instance = testResult.GetInstance("NormalClass");
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}");
    }

    [Test]
    public async Task NormalStructTest()
    {
        var instance = testResult.GetInstance("NormalStruct");
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"NormalStruct\", X: 1, Y: \"2\", Z: 4.5}");
    }

    [Test]
    public async Task NestedClassTest()
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

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: {T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"V\"}}");
    }

    [Test]
    public async Task ClassWithIgnoredPropertiesTest()
    {
        var type = assembly.GetType("ClassWithIgnoredProperties");
        dynamic instance = Activator.CreateInstance(type);
        instance.Username = "user";
        instance.Password = "pass";
        instance.Age = 18;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ClassWithIgnoredProperties\", Username: \"user\", Age: 18}");
    }

    [Test]
    public async Task NullTest()
    {
        var nestedType = assembly.GetType("NestedClass");
        dynamic nestedInstance = Activator.CreateInstance(nestedType);
        nestedInstance.A = 10;
        nestedInstance.B = "11";
        nestedInstance.C = 12.25;
        nestedInstance.D = null;

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"NestedClass\", A: 10, B: \"11\", C: 12.25, D: null}");
    }

    [Test]
    public async Task ClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("Child");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InParent = 10;
        instance.InChild = 5;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"Child\", InChild: 5, InParent: 10}");
    }

    [Test]
    public async Task ComplexClassWithParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("ComplexChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChildNumber = 1L;
        instance.InChildText = "2";
        instance.InChildCollection  = new[] {3};
        instance.InParentNumber = 4L;
        instance.InParentText = "5";
        instance.InParentCollection  = new[] {6};

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ComplexChild\", InChildNumber: 1, InChildText: \"2\", InChildCollection: [3], InParentNumber: 4, InParentText: \"5\", InParentCollection: [6]}");
    }

    [Test]
    public async Task ClassWithGenericParentInAnotherAssembly()
    {
        var derivedType = assembly.GetType("GenericChild");
        dynamic instance = Activator.CreateInstance(derivedType);
        instance.InChild = "5";
        instance.GenericInParent = 6;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"GenericChild\", InChild: \"5\", GenericInParent: 6}");
    }

    [Test]
    public async Task GuidErrorTest()
    {
        var type = assembly.GetType( "ReferenceObject" );
        dynamic instance = Activator.CreateInstance( type );
        instance.Id = Guid.Parse( "{f6ab1abe-5811-40e9-8154-35776d2e5106}" );
        instance.Name = "Test";

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ReferenceObject\", Name: \"Test\", Id: \"f6ab1abe-5811-40e9-8154-35776d2e5106\"}");
    }

    #region Collections

    [Test]
    public async Task IntArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new[] { 1, 2, 3, 4, 5, 6 };
        nestedInstance.Count = 2;

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6]}");
    }

    [Test]
    public async Task StringArray()
    {
        var type = assembly.GetType("StringCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new List<string> { "foo", "bar" };
        nestedInstance.Count = 2;

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"StringCollection\", Count: 2, Collection: [\"foo\", \"bar\"]}");
    }

    [Test]
    public async Task EmptyArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = new int[] {};
        nestedInstance.Count = 0;

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"IntCollection\", Count: 0, Collection: []}");
    }

    [Test]
    public async Task NullArray()
    {
        var type = assembly.GetType("IntCollection");
        dynamic nestedInstance = Activator.CreateInstance(type);
        nestedInstance.Collection = null;
        nestedInstance.Count = 0;

        string result = nestedInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"IntCollection\", Count: 0, Collection: null}");
    }

    [Test]
    public async Task ObjectArray()
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

        string result = arrayInstance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ObjectCollection\", Count: 2, Collection: [{T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"}, null]}");
    }

    [Test]
    public async Task GenericClassWithCollection()
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

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"GenericClass<GenericClassNormalClass>\", A: 1, B: [{T: \"GenericClassNormalClass\", D: 2, C: 3}]}");
    }

    [Test]
    public async Task WithoutGenericParameter()
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

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"WithoutGenericParameter\", Z: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: -4}]}");
    }

    [Test]
    public async Task WithGenericParameter()
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

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"WithGenericParameter<GenericClassNormalClass>\", X: 12, A: 1, B: [{T: \"GenericClassNormalClass\", D: 3, C: 4}]}");
    }

    [Test]
    public async Task WithGenericProperty()
    {
        var withGenericPropertyType = assembly.GetType("WithPropertyOfGenericType`1");
        var propType = assembly.GetType("GenericClassNormalClass");
        var instanceType = withGenericPropertyType.MakeGenericType(propType);

        dynamic instance = Activator.CreateInstance(instanceType);
        dynamic propInstance = Activator.CreateInstance(propType);
        instance.GP = propInstance;
        propInstance.C = 1;
        propInstance.D = 3;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"WithPropertyOfGenericType<GenericClassNormalClass>\", GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}");
    }

    [Test]
    public async Task WithInheritedGenericProperty()
    {
        var withGenericPropertyType = assembly.GetType("WithInheritedPropertyOfGenericType");

        dynamic instance = Activator.CreateInstance(withGenericPropertyType);
        var propType = assembly.GetType("GenericClassNormalClass");
        dynamic propInstance = Activator.CreateInstance(propType);
        instance.GP = propInstance;
        propInstance.C = 1;
        propInstance.D = 3;
        instance.X = 6;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"WithInheritedPropertyOfGenericType\", X: 6, GP: {T: \"GenericClassNormalClass\", D: 3, C: 1}}");
    }

    #endregion

    #region enums

    [Test]
    public async Task EmptyEnum()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type);

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"EnumClass\", NormalEnum: \"A\", FlagsEnum: \"G\"}");
    }

    [Test]
    public async Task EnumWithValues()
    {
        var type = assembly.GetType("EnumClass");
        dynamic instance = Activator.CreateInstance(type, 3, 6);

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"EnumClass\", NormalEnum: \"D\", FlagsEnum: \"I, J\"}");
    }


    #endregion

    [Test]
    public async Task TimeClassTest()
    {
        var type = assembly.GetType( "TimeClass" );
        dynamic instance = Activator.CreateInstance( type );
        instance.X = new DateTime(1988, 05, 23, 10, 30, 0, DateTimeKind.Utc);
        instance.Y = new TimeSpan(1, 2, 3, 4);

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"TimeClass\", X: \"1988-05-23T10:30:00.0000000Z\", Y: \"1.02:03:04\"}");
    }

    [Test]
    public async Task IndexerTest()
    {
        var type = assembly.GetType("ClassWithIndexer");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = 2;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ClassWithIndexer\", X: 1, Y: 2}");
    }

    [Test]
    public async Task RemoveToStringMethod()
    {
        var type = assembly.GetType("ClassWithToString");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = 2;

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ClassWithToString\", X: 1, Y: 2}");
    }

    [Test]
    public async Task GuidClassTest()
    {
        var type = assembly.GetType( "GuidClass" );
        dynamic instance = Activator.CreateInstance( type );
        instance.X = 1;
        instance.Y = new Guid(1,2,3,4,5,6,7,8,9,10,11);

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"GuidClass\", X: 1, Y: \"00000001-0002-0003-0405-060708090a0b\"}");
    }

    [Test]
    public async Task ClassWithDerivedPropertiesTest()
    {
        var type = assembly.GetType("ClassWithDerivedProperties");
        dynamic instance = Activator.CreateInstance(type);
        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{T: \"ClassWithDerivedProperties\", NormalProperty: \"New\", INormalProperty.NormalProperty: \"Interface\", VirtualProperty: \"Override Virtual\", AbstractProperty: \"Override Abstract\"}");
    }
}