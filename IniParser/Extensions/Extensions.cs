using IniParser.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IniParser.Extensions
{
    public static class Extensions
    {
        public static string[] IniSplit(this string content, string seporator = "\n")
        {
            var lines = content.Split(new string[] { seporator }, StringSplitOptions.RemoveEmptyEntries);
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

        public static string[] IniArraySplit(this string content) => content.Split(';');

        public static bool IsSerializable<T>(this T member) where T: MemberInfo
        {
            return !member.IsDefined(typeof(IniIgnorePropertyAttribute), true);
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

        public static void AppendValue(this StringBuilder writer, string name, string value, string seporator = "\n")
        {
            writer.AppendFormat("{0}={1}{2}", name, value, seporator);
        }
    }
}