[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg?style=flat)](https://gitter.im/Fody/Fody)
[![NuGet Status](http://img.shields.io/nuget/v/ToString.Fody.svg?style=flat)](https://www.nuget.org/packages/ToString.Fody/)


## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Icon](https://raw.github.com/Fody/ToString/master/package_icon.png)

Generates ToString method from public properties for class decorated with a `[ToString]` Attribute.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).


## Usage

See also [Fody usage](https://github.com/Fody/Fody#usage).


### NuGet installation

Install the [ToString.Fody NuGet package](https://nuget.org/packages/ToString.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package ToString.Fody
PM> Update-Package Fody
```

The `Update-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<ToString/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <ToString/>
</Weavers>
```


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
