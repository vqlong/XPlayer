using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLib
{
    public interface IXmlable
    {
        string ToXml(int indent);
        void ToXmlFile(string fileName);
    }
}
