using System;
using System.Reflection;

namespace Common.Helpers
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class StringValueAttribute : Attribute
    {
        public string Value { get; }

        public StringValueAttribute(string value) => (Value) = (value);

        public static string GetStringValue(Enum value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return value.GetType().GetRuntimeField(value.ToString()).GetCustomAttribute<StringValueAttribute>()?.Value;
        }
    }
}
