using System;

namespace IniParser.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IniIgnorePropertyAttribute : Attribute {}
}
