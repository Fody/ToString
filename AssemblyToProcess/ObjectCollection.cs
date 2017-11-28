using System.Collections.Generic;

[ToString]
public class ObjectCollection
{
    public int Count { get; set; }

    public IEnumerable<NormalClass> Collection { get; set; }
}