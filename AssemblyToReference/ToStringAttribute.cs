using System;

/// <summary>
/// Adds ToString method to class.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ToStringAttribute : Attribute
{
}
