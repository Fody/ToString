# <img src="/package_icon.png" height="30px"> ToString.Fody

[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg)](https://gitter.im/Fody/Fody)
[![NuGet Status](https://img.shields.io/nuget/v/ToString.Fody.svg)](https://www.nuget.org/packages/ToString.Fody/)

Generates ToString method from public properties for class decorated with a `[ToString]` Attribute.


### This is an add-in for [Fody](https://github.com/Fody/Home/)

**It is expected that all developers using Fody either [become a Patron on OpenCollective](https://opencollective.com/fody/contribute/patron-3059), or have a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-fody?utm_source=nuget-fody&utm_medium=referral&utm_campaign=enterprise). [See Licensing/Patron FAQ](https://github.com/Fody/Home/blob/master/pages/licensing-patron-faq.md) for more information.**


## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet installation

Install the [ToString.Fody NuGet package](https://nuget.org/packages/ToString.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package ToString.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<ToString/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <ToString/>
</Weavers>
```


## Your Code

```csharp
[ToString]
class TestClass
{
    public int Foo { get; set; }

    public double Bar { get; set; }
    
    [IgnoreDuringToString]
    public string Baz { get; set; }
}
```


## What gets compiled

```csharp
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
            Foo,
            Bar);
    }
}
```


## Options


### PropertyNameToValueSeparator

Default: `: `

For example:

```xml
<Weavers>
  <ToString PropertyNameToValueSeparator="->"/>
</Weavers>
```


### PropertiesSeparator

Default: `, `

For example:

```xml
<Weavers>
  <ToString PropertiesSeparator=". "/>
</Weavers>
```


### WrapWithBrackets

Default: `true`

For example:

```xml
<Weavers>
  <ToString WrapWithBrackets="false"/>
</Weavers>
```


### WriteTypeName

Default: `true`

For example:

```xml
<Weavers>
  <ToString WriteTypeName="false"/>
</Weavers>
```


### ListStart

Default: `[`

For example:

```xml
<Weavers>
  <ToString ListStart="("/>
</Weavers>
```


### ListEnd

Default: `]`

For example:

```xml
<Weavers>
  <ToString ListEnd=")"/>
</Weavers>
```


## Icon

Icon courtesy of [The Noun Project](https://thenounproject.com)
