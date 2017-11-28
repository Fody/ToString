﻿// ReSharper disable ValueParameterNotUsed
[ToString]
public class ClassWithIndexer
{
    public int X { get; set; }

    public byte Y { get; set; }

    public int this[int index]
    {
        get => X;
        set => X = index;
    }
}