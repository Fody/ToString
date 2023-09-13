using System;
using System.Reflection;
using Fody;
using Xunit;

public class AttributesTests
{
    string PropertyNameToValueSeparator = "$%^%$";
    string PropertiesSeparator = "$@#@$";
    bool WrapWithBrackets = false;
    bool WriteTypeName = false;
    string ListStart = "---[[[";
    string ListEnd = "]]]---";

    public Assembly PrepareAssembly(string name, AttributesConfiguration configuration)
    {
        var config = TestHelper.PrepareConfig(configuration);
        var weavingTask = new ModuleWeaver
        {
            Config = config

        };
        var testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll",assemblyName:name);
        return testResult.Assembly;
    }

    [Fact]
    public void NormalClassTest_ShouldUseCustomPropertyNameToValueSeparator()
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

        var result = instance.ToString();

        Assert.Equal(
            string.Format("{{T{0}\"NormalClass\", X{0}1, Y{0}\"2\", Z{0}4.5, V{0}\"C\"}}", PropertyNameToValueSeparator),
            result);
    }

    [Fact]
    public void NormalClassTest_ShouldUseCustomPropertiesSeparator()
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

        var result = instance.ToString();

        Assert.Equal(
            string.Format("{{T: \"NormalClass\"{0}X: 1{0}Y: \"2\"{0}Z: 4.5{0}V: \"C\"}}", PropertiesSeparator),
            result);
    }

    [Fact]
    public void NormalClassTest_ShouldNotWrapInBrackets()
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

        var result = instance.ToString();

        Assert.Equal(
            "T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"",
            result);
    }

    [Fact]
    public void NormalClassTest_ShouldNotWriteClassName()
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

        var result = instance.ToString();

        Assert.Equal(
            "{X: 1, Y: \"2\", Z: 4.5, V: \"C\"}",
            result);
    }

    [Fact]
    public void NormalClassTest_ShouldStartListWithCustomSeparator()
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

        var result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: {ListStart}1, 2, 3, 4, 5, 6]}}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalClassTest_ShouldEndListWithCustomSeparator()
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

        var result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6{ListEnd}}}";

        Assert.Equal(expected, result);
    }
}