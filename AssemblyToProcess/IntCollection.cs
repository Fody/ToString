using System.Collections.Generic;

[ToString]
public class IntCollection
{
    public int Count { get; set; }

    public IEnumerable<int> Collection { get; set; }
}