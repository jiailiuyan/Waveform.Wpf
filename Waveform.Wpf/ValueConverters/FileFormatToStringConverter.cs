using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Waveform.Wpf.ValueConverters
{
    public class FileFormatToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var b = values != null && values.Length == 2;
            if (b)
            {
                var o = values[0];
                var o1 = values[1];
                var b1 = o is int && o1 is int;
                if (b1)
                {
                    var channels = (int)o;
                    var bits = (int)o1;
                    var s = channels == 1 ? "Mono" : "Stereo";
                    var format = string.Format("{0} {1} bit", s, bits);
                    return format;

                }

            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}