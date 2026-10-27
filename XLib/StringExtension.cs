using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLib
{
    public static class StringExtension
    {
        /// <summary>
        /// Chuyển chuỗi sang dạng không dấu.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToUnsigned(this string input)
        {
              string signed = "ăâđêôơưàảãạáằẳẵặắầẩẫậấèẻẽẹéềểễệế ìỉĩịíòỏõọóồổỗộốờởỡợớùủũụúừửữựứỳỷỹỵý ĂÂĐÊÔƠƯÀẢÃẠÁẰẲẴẶẮẦẨẪẬẤÈẺẼẸÉỀỂỄỆẾÌỈĨỊÍ ÒỎÕỌÓỒỔỖỘỐỜỞỠỢỚÙỦŨỤÚỪỬỮỰỨỲỶỸỴÝ";
            string unsigned = "aadeoouaaaaaaaaaaaaaaaeeeeeeeeee iiiiiooooooooooooooouuuuuuuuuuyyyyy AADEOOUAAAAAAAAAAAAAAAEEEEEEEEEEIIIII OOOOOOOOOOOOOOOUUUUUUUUUUYYYYY";

            for (int i = 0; i < input.Length; i++)
            {
                if (signed.Contains(input[i]))
                    input = input.Replace(input[i], unsigned[signed.IndexOf(input[i])]);
            }

            return input;
        }

        public static void ToFileText(this string text, string filePath = "error.txt")
        {
            using var writer = File.CreateText(filePath);
            writer.WriteLine(text);
        }
    }


    public static class XmlableExtension
    {
        public static string ToXml<T>(this IEnumerable<T> models, int indent = 0) where T : Xmlable
        {

            string xml = "";
            if (indent == 0)
            {
                xml += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                xml += $"<ArrayOf{typeof(T).Name}>\n";
            }

            foreach (var model in models)
            {
                xml += new string(' ', indent + Xmlable.WhiteSpace);
                xml += $"<{typeof(T).Name}>\n";

                xml += model.ToXml(indent + Xmlable.WhiteSpace);

                xml += new string(' ', indent + Xmlable.WhiteSpace);
                xml += $"</{typeof(T).Name}>\n";

            }


            if (indent == 0) xml += $"</ArrayOf{typeof(T).Name}>";

            return xml;
        }

        public static void ToXmlFile<T>(this IEnumerable<T> models, string fileName) where T : Xmlable
        {
            File.WriteAllText(fileName, models.ToXml());
        }
    }

}
