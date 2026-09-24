using System;
using System.Reflection;
using Fody;

// weaving runs share the AssemblyToProcess files and the weaver test output folders
[NotInParallel]
public class AttributesTests
{
    string PropertyNameToValueSeparator = "$%^%$";
    string PropertiesSeparator = "$@#@$";
    bool WrapWithBrackets = false;
    bool WriteTypeName = false;
    string ListStart = "---[[[";
    string ListEnd = "]]]---";

    Assembly PrepareAssembly(string name, AttributesConfiguration configuration)
    {
        var config = TestHelper.PrepareConfig(configuration);
        var weaver = new ModuleWeaver
        {
            Config = config

        };
        var testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll",assemblyName:name);
        return testResult.Assembly;
    }

    [Test]
    public async Task NormalClassTest_ShouldUseCustomPropertyNameToValueSeparator()
    {
        var assembly = PrepareAssembly(
            "test1",
            new()
            {
                PropertyNameToValueSeparator = PropertyNameToValueSeparator
            });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo(string.Format("{{T{0}\"NormalClass\", X{0}1, Y{0}\"2\", Z{0}4.5, V{0}\"C\"}}", PropertyNameToValueSeparator));
    }

    [Test]
    public async Task NormalClassTest_ShouldUseCustomPropertiesSeparator()
    {
        var assembly = PrepareAssembly("test2",
            new()
            {
                PropertiesSeparator = PropertiesSeparator
            });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo(string.Format("{{T: \"NormalClass\"{0}X: 1{0}Y: \"2\"{0}Z: 4.5{0}V: \"C\"}}", PropertiesSeparator));
    }

    [Test]
    public async Task NormalClassTest_ShouldNotWrapInBrackets()
    {
        var assembly = PrepareAssembly("test3",
            new()
            {
                WrapWithBrackets = WrapWithBrackets
            });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"");
    }

    [Test]
    public async Task NormalClassTest_ShouldNotWriteClassName()
    {
        var assembly = PrepareAssembly("test4",
            new()
            {
                WriteTypeName = WriteTypeName
            });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        string result = instance.ToString();

        await Assert.That(result).IsEqualTo("{X: 1, Y: \"2\", Z: 4.5, V: \"C\"}");
    }

    [Test]
    public async Task NormalClassTest_ShouldStartListWithCustomSeparator()
    {
        var assembly = PrepareAssembly("test5",
            new()
            {
                ListStart = ListStart
            });

        var type = assembly.GetType("IntCollection");
        dynamic instance = Activator.CreateInstance(type);
        instance.Collection = new[] {1, 2, 3, 4, 5, 6};
        instance.Count = 2;

        string result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: {ListStart}1, 2, 3, 4, 5, 6]}}";

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NormalClassTest_ShouldEndListWithCustomSeparator()
    {
        var assembly = PrepareAssembly("test6",
            new()
            {
                ListEnd = ListEnd
            });

        var type = assembly.GetType("IntCollection");
        dynamic instance = Activator.CreateInstance(type);
        instance.Collection = new[] {1, 2, 3, 4, 5, 6};
        instance.Count = 2;

        string result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6{ListEnd}}}";

        await Assert.That(result).IsEqualTo(expected);
    }
}