using IniParser.Extensions;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace IniParser
{
    public static class IniConvert
    {
        private static readonly string ArraySeporator = "|";

        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        public static object FromTo(Type type, string iniContent, string seporator = "\n")
        {
            if (!type.IsClass) throw new ArgumentException("Тип должен быть классом");
            
            var properties = type.GetProperties(Flags).GetMemberInfos();
            var fields = type.GetFields(Flags).GetMemberInfos();

            var instance = FormatterServices.GetUninitializedObject(type);

            var elements = iniContent.IniSplit(seporator);

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

            return instance;
        }
        
        public static T FromTo<T>(string iniContent, string seporator = "\n") where T: class
        {
            var type = typeof(T);
            
            return (T)FromTo(type, iniContent, seporator);
        }

        public static string ToFrom(object obj, string seporator = "\n")
        {
            var type = obj.GetType();

            var writer = new StringBuilder();

            var properties = type.GetProperties(Flags);
            var fields = type.GetFields(Flags);

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                if (property.IsSerializable())
                {
                    var value = property.GetValue(obj, null);

                    var iniValue = SerializableObject(value);

                    writer.AppendValue(property.Name, iniValue, i + 1 < properties.Length ? seporator : "");
                }
            }

            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                if (field.IsSerializable())
                {
                    var value = field.GetValue(obj);

                    var iniValue = SerializableObject(value);

                    writer.AppendValue(field.Name, iniValue, i + 1 < fields.Length ? seporator : "");
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
            else if (type == typeof(Color))
            {
                var color = (Color)obj;

                return string.Format("{0},{1},{2},{3}", color.A, color.R, color.G, color.B);
            }
            else if (type.IsArray)
            {
                var arrayType = type.GetElementType();
                var writer = new StringBuilder();
                var array = (Array)obj;
                
                if (arrayType.IsClass)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.Append(ToFrom(array.GetValue(i), ArraySeporator));
                        
                        if (i + 1 < array.Length) writer.Append(";");
                    }
                    
                    return writer.ToString();
                }
                else if (arrayType.IsPrimitive || arrayType == typeof(string))
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.Append(SerializableObject(array.GetValue(i)));
                        
                        if (i + 1 < array.Length) writer.Append(";");
                    }
                    
                    return writer.ToString();
                }
            }
            
            throw new SerializationException("Такой тип переменной не поддерживаеться: " + type.ToString());
        }

        public static object DeserializableObject(Type type, string iniValue)
        {
            if (type == typeof(string))
            {
                return iniValue.Substring(1, iniValue.Length - 2);
            }
            else if (type.IsPrimitive)
            {
                return Convert.ChangeType(iniValue, type);
            }
            else if (type == typeof(Point))
            {
                var pointParams = iniValue.Split(',');

                if (pointParams.Length >= 2)
                {
                    var x = int.Parse(pointParams[0]);
                    var y = int.Parse(pointParams[1]);

                    return new Point(x, y);
                }
                else
                {
                    throw new ArgumentException("Количество аргументов не достаточно: " + type.ToString());
                }
            }
            else if (type == typeof(Color))
            {
                var colorParams = iniValue.Split(',');

                if (colorParams.Length >= 4)
                {
                    var a = byte.Parse(colorParams[0]);
                    var r = byte.Parse(colorParams[1]);
                    var g = byte.Parse(colorParams[2]);
                    var b = byte.Parse(colorParams[3]);

                    return Color.FromArgb(a, r, g, b);
                }
                else
                {
                    throw new ArgumentException("Количество аргументов не достаточно: " + type.ToString());
                }
            }
            else if (type.IsArray)
            {
                var arrayType = type.GetElementType();
                var iniArray = iniValue.IniArraySplit();
                var array = Array.CreateInstance(arrayType, iniArray.Length);
                
                if (type.IsClass)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array.SetValue(FromTo(arrayType, iniArray[i], ArraySeporator), i);
                    }

                    return array;
                }
                else if (type.IsPrimitive)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array.SetValue(DeserializableObject(arrayType, iniArray[i]), i);
                    }
                    
                    return array;
                }
            }
            
            throw new SerializationException("Такой тип переменной не поддерживаеться: " + type.ToString());
        }
    }
}
