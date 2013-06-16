![Icon](https://raw.github.com/Fody/ToString/master/Icons/package_icon.png)

## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

Generates ToString method from public properties for class decorated with a `[ToString]` Attribute.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).

# Nuget package

There is a nuget package available here http://nuget.org/packages/ToString.Fody/

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
