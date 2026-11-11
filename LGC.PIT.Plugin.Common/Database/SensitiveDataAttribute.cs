using System;

namespace LGC.PIT.Plugin.Common.Database;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute
{
}