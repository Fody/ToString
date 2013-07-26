## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Icon](https://raw.github.com/Fody/ToString/master/Icons/package_icon.png)

Generates ToString method from public properties for class decorated with a `[ToString]` Attribute.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).


## Nuget 

Nuget package http://nuget.org/packages/ToString.Fody/

To Install from the Nuget Package Manager Console 
    
    PM> Install-Package ToString.Fody
    
## Your Code

    [ToString]
    class TestClass
    {
        public int Foo { get; set; }

        public double Bar { get; set; }
        
        [IgnoreDuringToString]
        public string Baz { get; set; }
    }

## What gets compiled

    class TestClass
    {
        public int Foo { get; set; }

        public double Bar { get; set; }
        
        public string Baz { get; set; }
        
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                "{{T: TestClass, Foo: {0}, Bar: {1}}}",
                this.Foo,
                this.Bar);
        }
    }

## Icon

Icon courtesy of [The Noun Project](http://thenounproject.com)
