using System;

[ToString]
public class TimeClass
{
    public DateTime X { get; set; }

    public TimeSpan Y { get; set; }

    public object z()
    {
        return X.ToUniversalTime();
    }
}
