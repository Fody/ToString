using System.Collections.Generic;
using System.Globalization;
using System;

[ToString]
public class WithGenericParameter<T> : GenericClass<T> where T : GenericClassBaseClass
{
    public int X { get; set; }
}

[ToString]
public class WithoutGenericParameter : GenericClass<GenericClassBaseClass>
{
    public int Z { get; set; }
}

[ToString]
public class WithPropertyOfGenericType<T> where T : GenericClassBaseClass
{
    public T GP { get; set; }
}

[ToString]
public class WithInheritedPropertyOfGenericType : WithPropertyOfGenericType<GenericClassBaseClass>
{
    public int X { get; set; }
}

[ToString]
public class GenericClass<T> where T : GenericClassBaseClass
{
    public int a;

    public int A 
    {
        get
        {
            return a;
        }
        set
        {
            a = value;
        }
    }

    public IEnumerable<T> B { get; set; }
}

[ToString]
public abstract class GenericClassBaseClass
{
    public int C { get; set; }
}

[ToString]
public class GenericClassNormalClass : GenericClassBaseClass
{
    public int D { get; set; }
}