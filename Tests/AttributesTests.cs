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

    public Assembly PrepareAssembly(string name, string propertyNameToValueSeparator, string propertiesSeparator, bool? wrapWithBrackets, bool? writeTypeName)
    {
        var config = TestHelper.PrepareConfig(propertyNameToValueSeparator, propertiesSeparator, wrapWithBrackets, writeTypeName);
        
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
        var assembly = PrepareAssembly("test1", PropertyNameToValueSeparator, null, null, null);

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
        var assembly = PrepareAssembly("test2", null, PropertiesSeparator, null, null);

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
        var assembly = PrepareAssembly("test3", null, null, false, null);

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
        var assembly = PrepareAssembly("test4", null, null, null, false);

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
}