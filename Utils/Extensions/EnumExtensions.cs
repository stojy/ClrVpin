using System;
using System.ComponentModel;
using System.Reflection;

namespace Utils.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            
            if (name != null)
            {
                if (type.GetField(name) is MemberInfo field)
                {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                        return attr.Description;
                }
            }
            return null;
        }
    }
}
