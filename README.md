## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Icon](https://raw.github.com/Fody/ToString/master/Icons/package_icon.png)

Generates ToString method from public properties for class decorated with a `[ToString]` Attribute.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).

[![NuGet Status](https://img.shields.io/gitter/room/fody/fody.svg?style=flat)](https://gitter.im/Fody/Fody)

## The nuget package  [![NuGet Status](http://img.shields.io/nuget/v/ToString.Fody.svg?style=flat)](https://www.nuget.org/packages/ToString.Fody/)

https://nuget.org/packages/ToString.Fody/

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
