[ToString]
public class ClassWithToString
{
    public int X { get; set; }

    public int Y { get; set; }

    public override string ToString()
    {
        return "XY";
    }
}