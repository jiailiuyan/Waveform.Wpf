using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Waveform.Wpf.ValueConverters
{
    public class SamplerateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int)) return DependencyProperty.UnsetValue;
            CultureInfo cultureInfo = new CultureInfo(CultureInfo.InvariantCulture.LCID);
            var format = cultureInfo.NumberFormat;
            format.NumberGroupSeparator = " ";
            int i = (int)value;
            var s = i.ToString("N0", format);
            return string.Format("{0} Hz", s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}