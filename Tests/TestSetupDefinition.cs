using Mono.Cecil;

public class TestSetupDefinition
{
    public string AfterAssemblyPath { get; set; }

    public ModuleDefinition ModuleDefinition { get; set; }

    public MockAssemblyResolver MockAssemblyResolver { get; set; }
}