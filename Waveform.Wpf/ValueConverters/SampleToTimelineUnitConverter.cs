using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AudioObjects;

namespace Waveform.Wpf.ValueConverters
{
    public class SampleToTimelineUnitConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 3)
            {
                object v1 = values[0];
                object v2 = values[1];
                object v3 = values[2];
                if (v1 is TimelineUnit && v2 is long && v3 is IAudioStream)
                {
                    var timelineUnit = (TimelineUnit) v1;
                    var sample = (long) v2;
                    var audioStream = (IAudioStream) v3;
                    if (sample == -1)
                    {
                        return DependencyProperty.UnsetValue;
                    }
                    switch (timelineUnit)
                    {
                        case TimelineUnit.Samples:
                            return string.Format("{0} samples", sample);
                        case TimelineUnit.Time:
                            long samplesToMiliseconds = Converters.SamplesToMiliseconds(sample,
                                audioStream.Samplerate);
                            TimeSpan fromMilliseconds = TimeSpan.FromMilliseconds(samplesToMiliseconds);
                            string format = string.Format("{0:hh}h{0:mm}m{0:ss}s{0:fff}ms", fromMilliseconds);
                            return format;
                        case TimelineUnit.FileSize:
                            long length = Converters.SamplesToBytes(sample, audioStream.BitDepth,
                                audioStream.Channels);
                            return Converters.BytesToString(length, true);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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