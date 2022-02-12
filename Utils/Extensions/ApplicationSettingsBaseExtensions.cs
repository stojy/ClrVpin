using System;
using System.Configuration;

namespace Utils.Extensions
{
    public static class ApplicationSettingsBaseExtensions
    {
        public static TProperty GetDefault<TProperty>(this ApplicationSettingsBase value, string propertyName)
        {
            // using the native ApplicationSettings properties
            var stringValue = value.Properties[propertyName].DefaultValue;

            // alternatively use some attribute reflection
            //var property = value.GetType().GetProperty(propertyName);
            //var attribute = property?.GetCustomAttributes(false).OfType<DefaultSettingValueAttribute>().FirstOrDefault(); // should only be one
            //var stringValue = attribute?.Value;

            return (TProperty) Convert.ChangeType(stringValue, typeof(TProperty));
        }
    }
}