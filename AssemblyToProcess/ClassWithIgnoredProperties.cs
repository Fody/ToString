[ToString]
public class ClassWithIgnoredProperties
{
    public string Username { get; set; }

    public int Age { get; set; }

    [IgnoreDuringToString]
    public string Password { get; set; }
}
