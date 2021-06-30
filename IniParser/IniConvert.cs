using System;
using System.Drawing;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace IniParser
{
    public class IniConvert
    {
        public static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        public static T FromTo<T>(string iniContent) where T: class
        {
            var type = typeof(T);

            var properties = type.GetProperties(Flags).GetMemberInfos();
            var fields = type.GetFields(Flags).GetMemberInfos();

            var instance = FormatterServices.GetUninitializedObject(type);

            var elements = iniContent.IniSplit();

            for (int i = 0; i < elements.Length; i += 2)
            {
                var key = elements[i];
                var iniValue = elements[i + 1];

                if (properties.TryGetValue(key, out PropertyInfo property))
                {
                    property.SetValue(instance, DeserializableObject(property.PropertyType, iniValue), null);
                }
                else if (fields.TryGetValue(key, out FieldInfo field))
                {
                    field.SetValue(instance, DeserializableObject(field.FieldType, iniValue));
                }
            }

            return (T)instance;
        }

        public static string ToFrom(object obj)
        {
            var type = obj.GetType();

            var writer = new StringBuilder();

            var properties = type.GetProperties(Flags);
            var fields = type.GetFields(Flags);

            foreach (var property in properties)
            {
                if (property.IsSerializable())
                {
                    var value = property.GetValue(obj, null);

                    var iniValue = SerializableObject(value);

                    writer.AppendValue(property.Name, iniValue);
                }
            }

            foreach (var field in fields)
            {
                if (field.IsSerializable())
                {
                    var value = field.GetValue(obj);

                    var iniValue = SerializableObject(value);

                    writer.AppendValue(field.Name, iniValue);
                }
            }

            return writer.ToString();
        }

        public static string SerializableObject(object obj)
        {
            var type = obj.GetType();

            if (type == typeof(string))
            {
                return string.Format("\"{0}\"", obj.ToString());
            }
            else if (type.IsPrimitive)
            {
                return obj.ToString();
            }
            else if (type == typeof(Point))
            {
                var point = (Point)obj;

                return string.Format("{0},{1}", point.X, point.Y);
            }
            else
            {
                throw new SerializationException("Такой тип переменной не поддерживаеться: " + type.ToString());
            }
        }

        public static object DeserializableObject(Type type, string iniValue)
        {
            if (type == typeof(string))
            {
                var text = iniValue.Substring(1, iniValue.Length - 2);

                return text;
            }
            else if (type.IsPrimitive)
            {
                return Convert.ChangeType(iniValue, type);
            }
            else if (type == typeof(Point))
            {
                var pointLines = iniValue.Split(',');

                var x = int.Parse(pointLines[0]);
                var y = int.Parse(pointLines[1]);

                return new Point(x, y);
            }
            else
            {
                throw new SerializationException("Такой тип переменной не поддерживаеться: " + type.ToString());
            }
        }
    }

    public static class Utils
    {
        public static string[] IniSplit(this string content)
        {
			var lines = content.Split('\n');
			var elements = new List<string>();

			foreach (var line in lines)
			{
				var parts = line.Split('=');

				if (parts.Length >= 2)
				{
					elements.Add(parts[0]);
					elements.Add(parts[1]);
				}
			}

			return elements.ToArray();
        }

        public static bool IsSerializable<T>(this T member) where T: MemberInfo
        {
            return member.IsDefined(typeof(IniIgnoreProperty), true) == false;
        }

        public static Dictionary<string, T> GetMemberInfos<T>(this T[] members) where T: MemberInfo
        {
            var dictionary = new Dictionary<string, T>();

            foreach (var member in members)
            {
                if (member.IsSerializable())
                {
                    dictionary.Add(member.Name, member);
                }
            }

            return dictionary;
        }

        public static void AppendValue(this StringBuilder writer, string name, string value)
        {
            writer.AppendFormat("{0}={1}\n", name, value);
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IniIgnoreProperty : Attribute {}
}