using System;
using System.Collections.Generic;
using System.Reflection;

namespace SolidModelBrowser
{
    public class PropertyInfoAttribute : Attribute
    {
        public string Name;
        public object Value;
        public object Object;
        public PropertyInfo Property;

        public string Category { get; set; }
        public string Description { get; set; }
        public string MenuLabel { get; set; }
        public double Min { get; set; } = 0.0;
        public double Max { get; set; } = double.MaxValue;
        public double Increment { get; set; } = 1.0;

        public static List<PropertyInfoAttribute> GetPropertiesInfoList(object obj, bool sort)
        {
            var pi = new List<PropertyInfoAttribute>();
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var attr = p.GetCustomAttribute<PropertyInfoAttribute>();
                if (attr != null) pi.Add(new PropertyInfoAttribute()
                {
                    Name = p.Name,
                    Value = p.GetValue(obj, null),
                    Object = obj,
                    Property = p,
                    Category = attr.Category ?? "All",
                    Description = attr.Description,
                    MenuLabel = attr.MenuLabel,
                    Min = attr.Min,
                    Max = attr.Max,
                    Increment = attr.Increment
                });
            }
            if (sort) pi.Sort((a, b) => string.Compare(a.Category + a.Name, b.Category + b.Name));
            return pi;
        }
    }
}
