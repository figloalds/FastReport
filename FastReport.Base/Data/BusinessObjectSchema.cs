using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FastReport.Utils;

namespace FastReport.Data
{
    // Discovery never advances an enumerable. Only indexed lists can be sampled, so
    // registering a streaming/one-shot source cannot consume rows before preparation.
    internal static class BusinessObjectSchema
    {
        internal static object GetList(object value) => value is IListSource source ? source.GetList() : value;

        internal static Type GetItemType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type == typeof(string)) return type;
            var enumerable = new[] { type }.Concat(type.GetInterfaces())
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerable?.GetGenericArguments()[0] ??
                (typeof(IEnumerable).IsAssignableFrom(type) ? typeof(object) : type);
        }

        internal static object Sample(object value)
        {
            value = GetList(value);
            if (value is IList list)
            {
                for (int i = 0; i < Math.Min(list.Count, 32); i++)
                    if (list[i] != null) return list[i];
                return null;
            }
            return value is IEnumerable ? null : value;
        }

        internal static PropertyDescriptorCollection GetProperties(Column column)
        {
            object source = GetList(column.Reference);
            Type type = GetItemType(source?.GetType() ?? column.DataType);
            object instance = Sample(source);
            object ownedInstance = null;
            try
            {
                // ITypedList is the authoritative schema, including empty lists.
                PropertyDescriptorCollection properties;
                if (source is ITypedList typedList)
                    properties = typedList.GetItemProperties(null);
                else
                {
                    if (instance == null && typeof(ICustomTypeDescriptor).IsAssignableFrom(type))
                    {
                        var args = new GetTypeInstanceEventArgs(type);
                        Config.ReportSettings.OnGetBusinessObjectTypeInstance(null, args);
                        instance = ownedInstance = args.Instance;
                    }
                    properties = instance is ICustomTypeDescriptor || type == typeof(object) && instance != null
                        ? TypeDescriptor.GetProperties(instance)
                        : TypeDescriptor.GetProperties(type);
                }
                var filtered = new PropertyDescriptorCollection(null);
                foreach (PropertyDescriptor property in properties)
                {
                    var args = new FilterPropertiesEventArgs(property);
                    Config.ReportSettings.OnFilterBusinessObjectProperties(source ?? instance ?? type, args);
                    if (!args.Skip) filtered.Add(args.Property);
                }
                return filtered;
            }
            finally
            {
                // Only instances requested through the factory hook are owned here.
                (ownedInstance as IDisposable)?.Dispose();
            }
        }
    }
}
