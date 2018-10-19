public abstract class SuperClass
{
    public string NormalProperty => "Normal";
    public virtual string VirtualProperty => "Virtual";
    public abstract string AbstractProperty { get; }
}

public interface INormalProperty
{
    string NormalProperty { get; }
}

[ToString]
public class ClassWithDerivedProperties : SuperClass, INormalProperty
{
    public new string NormalProperty => "New";
    string INormalProperty.NormalProperty => "Interface";
    public override string VirtualProperty => "Override Virtual";
    public override string AbstractProperty => "Override Abstract";
}
