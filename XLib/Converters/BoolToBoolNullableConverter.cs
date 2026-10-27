using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XLib.Converters
{
    public class BoolToBoolNullableConverter : IValueConverter
    {
        public static BoolToBoolNullableConverter Default { get; } = new BoolToBoolNullableConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return Binding.DoNothing;
            }
            else
            {
                if (value is true) return true;
                if (value is false) return false;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
