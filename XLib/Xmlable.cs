using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XLib
{
    public class Xmlable : IXmlable
    {
        public static int WhiteSpace { get; } = 2;
        // indent là số khoảng trắng sẽ thụt vào
        public string ToXml(int indent = 0)
        {
            string xml = "";
            // indent = 0 tức là nó là thẻ root
            if (indent == 0)
            {
                xml += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                // thẻ root sẽ in tên của class
                xml += $"<{GetType().Name}>\n";
            }

            var properties  = GetType().GetProperties();
            // còn property thì in ra tên property
            foreach (var property in properties)
            {
                // chỉ in ra các property kiểu nguyên thuỷ, kiểu string và kiểu ModelBase
                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string)) 
                {
                    // in property ra trong 1 dòng
                    xml += new string(' ', indent + WhiteSpace);
                    xml += $"<{property.Name}>";

                    // kiểu bool khi ToString nó sẽ viết True hoặc False nên phải ToLower nó
                    if (property.PropertyType == typeof(bool)) xml += $"{property.GetValue(this)?.ToString()?.ToLower()}";
                    else xml += $"{property.GetValue(this)}";

                    xml += $"</{property.Name}>\n";
                }
                else if (property.PropertyType.IsSubclassOf(typeof(Xmlable)))
                {
                    // nếu property là 1 Xmlable
                    // in property ra nhiều dòng
                    xml += new string(' ', indent + WhiteSpace);
                    xml += $"<{property.Name}>\n";
                    xml += $"{((Xmlable?)property.GetValue(this))?.ToXml(indent + WhiteSpace)}";
                    xml += new string(' ', indent + WhiteSpace);
                    xml += $"</{property.Name}>\n";
                }
                else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericArguments()[0].IsSubclassOf(typeof(Xmlable)))
                {
                    // nếu property là 1 danh sách các Xmlable
                    xml += new string(' ', indent + WhiteSpace);
                    xml += $"<{property.Name}>\n";

                    // lấy ra method ToXml trong class extension 
                    var toXmlMethod = typeof(XmlableExtension).GetMethod("ToXml", BindingFlags.Static | BindingFlags.Public);
                    // biến nó thành method thực sự dùng được, nguyên bản nó có parametter kiểu T
                    var toXml = toXmlMethod.MakeGenericMethod(property.PropertyType.GetGenericArguments()[0]);
                    // lấy ra object của property
                    var genericObj = Convert.ChangeType(property.GetValue(this), property.PropertyType);
                    // invoke ToXml
                    var genericXml = toXml.Invoke(genericObj, new object[] { genericObj, indent + WhiteSpace });
                    xml += $"{genericXml}";

                    xml += new string(' ', indent + WhiteSpace);
                    xml += $"</{property.Name}>\n";
                }
            }

            if (indent == 0) xml += $"</{GetType().Name}>";

            return xml;
        }

        public void ToXmlFile(string fileName)
        {
            File.WriteAllText(fileName, this.ToXml());
        }

    }

}
