using NUnit.Framework;
using System;
using System.Reflection;

[TestFixture]
public class AttributesTests
{
    private string PropertyNameToValueSeparator = "$%^%$";
    private string PropertiesSeparator = "$@#@$";
    private bool WrapWithBrackets = false;
    private bool WriteTypeName = false;
    private string ListStart = "---[[[";
    private string ListEnd = "]]]---";

    public Assembly PrepareAssembly(string name, AttributesConfiguration configuration)
    {
        var config = TestHelper.PrepareConfig(configuration);

        var testSetup = TestHelper.PrepareDll(name);

        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = testSetup.ModuleDefinition,
            AssemblyResolver = testSetup.MockAssemblyResolver,
            Config = config
        };

        weavingTask.Execute();
        testSetup.ModuleDefinition.Write(testSetup.AfterAssemblyPath);

        return Assembly.LoadFile(testSetup.AfterAssemblyPath);
    }

    [Test]
    public void NormalClassTest_ShouldUseCustomPropertyNameToValueSeparator()
    {
        var assembly = PrepareAssembly("test1", new AttributesConfiguration { PropertyNameToValueSeparator = PropertyNameToValueSeparator });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        var result = instance.ToString();

        Assert.AreEqual(
            string.Format("{{T{0}\"NormalClass\", X{0}1, Y{0}\"2\", Z{0}4.5, V{0}\"C\"}}", PropertyNameToValueSeparator),
            result);
    }

    [Test]
    public void NormalClassTest_ShouldUseCustomPropertiesSeparator()
    {
        var assembly = PrepareAssembly("test2", new AttributesConfiguration { PropertiesSeparator = PropertiesSeparator });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        var result = instance.ToString();

        Assert.AreEqual(
            string.Format("{{T: \"NormalClass\"{0}X: 1{0}Y: \"2\"{0}Z: 4.5{0}V: \"C\"}}", PropertiesSeparator),
            result);
    }

    [Test]
    public void NormalClassTest_ShouldNotWrapInBrackets()
    {
        var assembly = PrepareAssembly("test3", new AttributesConfiguration { WrapWithBrackets = WrapWithBrackets });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        var result = instance.ToString();

        Assert.AreEqual(
            "T: \"NormalClass\", X: 1, Y: \"2\", Z: 4.5, V: \"C\"",
            result);
    }

    [Test]
    public void NormalClassTest_ShouldNotWriteClassName()
    {
        var assembly = PrepareAssembly("test4", new AttributesConfiguration { WriteTypeName = WriteTypeName });

        var type = assembly.GetType("NormalClass");
        dynamic instance = Activator.CreateInstance(type);
        instance.X = 1;
        instance.Y = "2";
        instance.Z = 4.5;
        instance.V = 'C';

        var result = instance.ToString();

        Assert.AreEqual(
            "{X: 1, Y: \"2\", Z: 4.5, V: \"C\"}",
            result);
    }

    [Test]
    public void NormalClassTest_ShouldStartListWithCustomSeparator()
    {
        var assembly = PrepareAssembly("test5", new AttributesConfiguration { ListStart = ListStart });

        var type = assembly.GetType("IntCollection");
        dynamic instance = Activator.CreateInstance(type);
        instance.Collection = new[] { 1, 2, 3, 4, 5, 6 };
        instance.Count = 2;

        var result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: {ListStart}1, 2, 3, 4, 5, 6]}}";

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void NormalClassTest_ShouldEndListWithCustomSeparator()
    {
        var assembly = PrepareAssembly("test6", new AttributesConfiguration { ListEnd = ListEnd });

        var type = assembly.GetType("IntCollection");
        dynamic instance = Activator.CreateInstance(type);
        instance.Collection = new[] { 1, 2, 3, 4, 5, 6 };
        instance.Count = 2;

        var result = instance.ToString();

        var expected = $"{{T: \"IntCollection\", Count: 2, Collection: [1, 2, 3, 4, 5, 6{ListEnd}}}";

        Assert.AreEqual(expected, result);
    }
}