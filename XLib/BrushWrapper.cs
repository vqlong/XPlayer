using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace XLib
{
    public class BrushWrapper
    {
        public string Name { get; set; } = "BlueViolet";
        public Brush Brush { get; set; } = Brushes.BlueViolet;
        static BrushWrapper()
        {
            var uri = new Uri("pack://application:,,,/XLib;component/Resources/Brushes.xaml");
            var resource = new ResourceDictionary() { Source = uri };
            foreach(var key in resource.Keys)
            {
                Wrappers.Add(new BrushWrapper { Name = (string)key, Brush = (Brush)resource[key] });
            }
        
        }
        public static List<BrushWrapper> Wrappers { get; } = new List<BrushWrapper>();
        public override string ToString() => Name;
    }
}
