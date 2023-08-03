using System;
using System.ComponentModel;
using System.Reflection;

namespace Utils.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum enumType)
    {
        var type = enumType.GetType();
        var name = Enum.GetName(type, enumType);
            
        if (name != null && type.GetField(name) is MemberInfo field)
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                return attr.Description;
        }
        return null;
    }
}