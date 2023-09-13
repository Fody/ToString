using System;

/// <summary>
/// Property will be ignored during generating ToString method.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreDuringToStringAttribute : Attribute;