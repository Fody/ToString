﻿using System;

public class StatePrinter
{
    public string PrintObject( object o )
    {
        return o.ToString();
    }
}

public class DomainObject
{
    static readonly StatePrinter StatePrinter = new();

    public override string ToString()
    {
        return StatePrinter.PrintObject( this );
    }
}

public class IdObject : DomainObject
{
    public Guid Id { get; set; }

    public bool IsEmpty()
    {
        return Id == Guid.Empty;
    }
}

public class NameObject : IdObject
{
    public string Name { get; set; }
}