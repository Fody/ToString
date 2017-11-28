using System.Collections.Generic;

[ToString]
public class StringCollection
{
    public int Count { get; set; }

    public IEnumerable<string> Collection { get; set; }
}