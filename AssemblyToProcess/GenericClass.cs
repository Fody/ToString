using System.Collections.Generic;
using System.Globalization;
using System;

[ToString]
public class GenericClass<T> where T : GenericClassBaseClass
{
    public int a;

    public GenericClass()
    {

    }

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


    public string ToString2323232()
      {
        return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{{T: \"GenericClass`1\", A: {0}}}", new object[1]
        {
          (object) this.A
        });
      }
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