using ReferencedDependency;
using System.Collections.Generic;

[ToString]
public class Child : Parent
{
    public long InChild { get; set; }
}

[ToString]
public class ComplexChild : ComplexParent
{
    public long InChildNumber { get; set; }

    public string InChildText { get; set; }

    public IEnumerable<int> InChildCollection { get; set; }
}

[ToString]
public class GenericChild : GenericParent<int>
{
    public string InChild { get; set; }
}