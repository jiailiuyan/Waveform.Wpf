using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioObjects;

namespace Waveform.Wpf
{
    /// <summary>
    ///     Interaction logic for WaveformControl.xaml
    /// </summary>
    public partial class WaveformControl : INotifyPropertyChanged
    {
        #region Private constants

        private const int DefaultScale = 1;
        private const int SliderSmallChange = 1;
        private const int SliderLargeChange = 32;
        // ReSharper disable MemberCanBePrivate.Global
        public const int HorizontalMovementFast = 1024;
        public const int HorizontalMovementNormal = 256;
        public const int HorizontalMovementSlow = 1;
        public const string HorizontalZoomInParameter = "HorizontalZoomInParameter";
        public const string HorizontalZoomOutParameter = "HorizontalZoomOutParameter";
        public const string HorizontalZoomResetParameter = "HorizontalZoomResetParameter";
        public const string VerticalZoomInParameter = "VerticalZoomInParameter";
        public const string VerticalZoomOutParameter = "VerticalZoomOutParameter";
        public const string VerticalZoomResetParameter = "VerticalZoomResetParameter";
        // ReSharper restore MemberCanBePrivate.Global

        #endregion

        #region Private fields
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IAudioStream _audioStream;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private long _sampleOverMouse;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimelineUnit _timelineUnit;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _zoomInteger;

        private readonly WaveformRenderer _renderer;

        #endregion

        public WaveformControl()
        {
            InitializeComponent();

            VerticalZoom = DefaultScale;
            ZoomInteger = 0; // yeah :D
            Slider1.SmallChange = SliderSmallChange;
            Slider1.LargeChange = SliderLargeChange;
            _renderer = new WpfWaveformRenderer();
            _renderer.Theme = new WaveformThemeWavelab();

            Loaded += OnLoaded;
        }

        #region Private properties

        private double VerticalZoom { get; set; }

        #endregion

        #region Public properties

        public IAudioStream AudioStream
        {
            get { return _audioStream; }
            set
            {
                if (Equals(value, _audioStream)) return;
                _audioStream = value;

                if (value != null)
                {
                    IWaveformCache cache = new WaveformCache(value, 128);
                    _renderer.Cache = cache;

                    long samples = AudioObjects.Converters.BytesToSamples(value.Length, value.BitDepth, value.Channels);
                    Slider1.Maximum = samples - 1;

                }
                else
                {
                    _renderer.Cache = null;
                    Slider1.Maximum = 0;
                }
                Refresh();
                
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the position in samples over the mouse cursor.
        /// </summary>
        public long SampleOverMouse
        {
            get { return _sampleOverMouse; }
            private set
            {
                if (value == _sampleOverMouse) return;
                _sampleOverMouse = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the current timeline unit.
        /// </summary>
        public TimelineUnit TimelineUnit
        {
            get { return _timelineUnit; }
            set
            {
                if (value == _timelineUnit) return;
                _timelineUnit = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the current zoom integer. 0 means 1:1, 1 means 1:2, -1 means 2:1 etc ...
        /// </summary>
        public int ZoomInteger
        {
            get { return _zoomInteger; }
            set
            {
                if (value == _zoomInteger) return;
                _zoomInteger = value;
                OnPropertyChanged();
                OnPropertyChanged("ZoomRatio");
                OnPropertyChanged("ZoomRatioString");
            }
        }

        /// <summary>
        ///     Gets the current zoom ratio denominator.
        /// </summary>
        public int ZoomRatio
        {
            get
            {
                int zoom = ZoomInteger;
                int ratio = (int)Math.Pow(2.0, Math.Abs(zoom)) * (Math.Sign(zoom) | 1);
                return ratio;
            }
        }

        public string ZoomRatioString
        {
            get
            {
                int ratioFromZoom = ZoomRatio;
                int a;
                int b;
                if (ratioFromZoom < 0)
                {
                    a = -ratioFromZoom;
                    b = 1;
                }
                else
                {
                    a = 1;
                    b = ratioFromZoom;
                }
                string s = string.Format("{0}:{1}", a, b);
                return s;
            }
        }

        #endregion

        #region Controls events

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isInDesignMode)
            {
                return;
            }

            Refresh();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            bool focusable = Focusable;
            bool isFocused = IsFocused;
            Focus();
        }

        private void GridImage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                var width = (int)e.NewSize.Width;
                var height = (int)e.NewSize.Height;
                //_bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                //Image1.Source = _bitmap;

                _renderer.SetBitmapSize(width, height);
                ImageSource imageSource = (ImageSource)_renderer.GetBitmap();
                Image1.Source = imageSource;

                Refresh();
            }
            else
            {
                //_bitmap = null;
            }
        }

        private void GridImage_OnMouseEnter(object sender, MouseEventArgs e)
        {
            UpdateSampleOverMouse();
        }

        private void GridImage_OnMouseMove(object sender, MouseEventArgs e)
        {
            UpdateSampleOverMouse();
        }

        private void ButtonRefreshOnClick(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Slider1_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Refresh();
            UpdateSampleOverMouse();

        }

        #endregion

        #region Private methods

        private void Refresh()
        {
            
            if (AudioStream != null)
            {
                var samples = (int)Slider1.Value;
                int bytes = AudioObjects.Converters.SamplesToBytes(samples, AudioStream.BitDepth, AudioStream.Channels);
                AudioStream.Position = bytes;

                var ratio = ZoomRatio;
                var zoom = VerticalZoom;
                // Draw(AudioStream, Theme, _bitmap, ratio, samples, _waveformCache, zoom);

                _renderer.Draw(samples, ratio, zoom);
                //WpfDrawingSurface Renderer = new WpfDrawingSurface(_waveformCache,new Size());

                //Renderer.Draw(samples,ratio,zoom);

            }
            else
            {
                using (var rendererContext = _renderer.GetContext())
                {
                    rendererContext.Clear(_renderer.Theme.ColorBackground);
                }
            }
        }

        private void UpdateSampleOverMouse()
        {
            Point position = Mouse.GetPosition(GridImage);
            Rect rect = new Rect(GridImage.RenderSize);
            var contains = rect.Contains(position);
            if (!contains)
            {
                // display nothing then
                SampleOverMouse=-1;//TODO fix
                return;
            }
            int zoomRatio = ZoomRatio;
            int abs = Math.Abs(zoomRatio);
            var value = (long)Slider1.Value;
            var x = (int)position.X;
            long sample = zoomRatio < 0 ? x / abs : x * abs;
            long arg0 = value + sample;
            SampleOverMouse = arg0;
        }

        /*
                private static void Draw(AudioStream AudioStream, IWaveformTheme theme, WriteableBitmap bitmap, int ratio, int positionSamples, IWaveformCache waveformCache, double verticalZoom)
                {
                    int bitmapWidth = bitmap.PixelWidth;
                    int bitmapHeight = bitmap.PixelHeight;
                    int samplePixels = Math.Abs(ratio);
                    var samples = (int)Math.Ceiling((double)bitmapWidth / samplePixels);

                    const int xMin = 0;
                    const int yMin = 0;
                    int xMax = bitmapWidth - 1;
                    int yMax = bitmapHeight - 1;
                    var color6dBLevel = theme.Color6dBLevel;
                    int colorBackground = theme.ColorBackground;
                    int colorDCLevel = theme.ColorDCLevel;
                    int colorForm = theme.ColorForm;
                    int colorEnvelope = theme.ColorEnvelope;
                    int colorSeparationLine = theme.ColorSeparationLine;
                    bool draw6dBLevel = theme.Draw6dBLevel;
                    bool drawBackground = theme.DrawBackground;
                    bool drawDCLevel = theme.DrawDCLevel;
                    bool drawEnvelope = theme.DrawEnvelope;
                    bool drawForm = theme.DrawForm;
                    bool drawSeparationLine = theme.DrawSeparationLine;

                    int channels = AudioStream.Channels;
                    int channelHeight = bitmapHeight / channels;
                    int channelCenter = channelHeight / 2;

                    int positionX = positionSamples / ratio;

                    const int minMax = 2;
                    int hop = channels * minMax;

                    #region Background

                    bitmap.Clear(drawBackground ? colorBackground : Colors.Transparent.ToInt32());

                    #endregion

                    #region 6dB, DC and separation lines

                    if (draw6dBLevel)
                    {
                        LineDash[] dashes6dBLevel =
                        {
                            new LineDash(3, color6dBLevel),
                            new LineDash(3, Colors.Transparent.ToInt32())
                        };
                        for (int c = 0; c < channels; c++)
                        {
                            int y1 = Transform(+0.5f, channelHeight, c, 1.0d);
                            int y2 = Transform(-0.5f, channelHeight, c, 1.0d);
                            bitmap.DrawLineBresenham(xMin, y1, xMax, y1, dashes6dBLevel);
                            bitmap.DrawLineBresenham(xMin, y2, xMax, y2, dashes6dBLevel);
                        }
                    }

                    if (drawDCLevel)
                    {
                        LineDash[] lineDashesDcLevel =
                        {
                            new LineDash(3, colorDCLevel),
                            new LineDash(3, Colors.Transparent.ToInt32())
                        };
                        for (int c = 0; c < channels; c++)
                        {
                            int y = channelHeight * c + channelCenter;
                            bitmap.DrawLineBresenham(xMin, y, xMax, y, lineDashesDcLevel);
                        }
                    }

                    if (drawSeparationLine)
                    {
                        if (channels > 1)
                        {
                            int y = bitmapHeight / 2 - 1;
                            bitmap.DrawLineBresenham(xMin, y, xMax, y, colorSeparationLine);
                        }
                    }

                    #endregion

                    #region Wave form

                    LineDash[] lineDashesEndIndicator =
                    {
                        new LineDash(3, theme.ColorEndIndicator),
                        new LineDash(3, Color.FromRgb(0xF0, 0xF0, 0xF0).ToInt32())
                    };

                    bool drawEndIndicator = theme.DrawEndIndicator;
                    if (ratio < 0) // OK !
                    {
                        #region Draw N:1

                        //var buffer1 = new float[channels]; // previous sample for correct drawing
                        //if (position > 0) AudioStream.ReadSamples(position - 1, buffer1, 1);
                        var buffer3 = new float[samples * channels]; // visible samples
                        int readSamples = AudioStream.ReadSamples(positionSamples, buffer3, samples);
                        var buffer2 = new float[channels]; // next sample for correct drawing
                        if (positionSamples + readSamples < AudioStream.Samples)
                            AudioStream.ReadSamples(positionSamples + readSamples, buffer2, 1);
                        for (int c = 0; c < channels; c++) // draw !
                        {
                            float f1;
                            //f1 = buffer1[c];
                            f1 = buffer3[c];
                            for (int x = 0; x < readSamples - 1; x++)
                            {
                                float f2 = buffer3[(x + 1) * channels + c];
                                int x1 = x * samplePixels;
                                int y1 = Transform(f1, channelHeight, c, verticalZoom);
                                int x2 = (x + 1) * samplePixels;
                                int y2 = Transform(f2, channelHeight, c, verticalZoom);
                                bitmap.DrawLine(x1, y1, x2, y1, colorEnvelope); // horizontal
                                bitmap.DrawLine(x2, y1, x2, y2, colorEnvelope); // vertical
                                f1 = f2;
                            }
                            int x3 = (readSamples - 1) * samplePixels; // last sample !
                            int y3 = Transform(f1, channelHeight, c, verticalZoom);
                            bitmap.DrawLineBresenham(x3, y3, x3 + samplePixels, y3, colorEnvelope);
                            bitmap.DrawLineBresenham(x3 + samplePixels, y3, x3 + samplePixels,
                                Transform(buffer2[c], channelHeight, c, verticalZoom), colorEnvelope);
                        }
                        if (readSamples < samples) // draw end lines
                        {
                            int x = (readSamples) * samplePixels + 1;
                            if (drawEndIndicator)
                            {
                                bitmap.DrawLineBresenham(x, 0, x, bitmapHeight - 1, lineDashesEndIndicator);
                            }
                            if (drawDCLevel)
                            {
                                for (int i = 0; i < channels; i++)
                                {
                                    int y = channelHeight * i + channelCenter;
                                    bitmap.DrawLineBresenham(x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
                                }
                            }
                        }

                        #endregion
                    }
                    else if (ratio == 1) // OK !
                    {
                        #region Draw 1:1

                        //float[] data = AudioStream.ReadSamples(samples);

                        var buffer3 = new float[samples * channels]; // visible samples
                        int readSamples = AudioStream.ReadSamples(positionSamples, buffer3, samples);
                        using (var bitmapContext = bitmap.GetBitmapContext())
                        {
                            var int32 = colorEnvelope;
                            for (int c = 0; c < channels; c++)
                            {
                                float f1 = buffer3[c];
                                for (int x = 0; x < readSamples; x++)
                                {
                                    float f2 = buffer3[x * channels + c];
                                    int x1 = (x - 1) * samplePixels;
                                    int y1 = Transform(f1, channelHeight, c, verticalZoom);
                                    int x2 = x * samplePixels;
                                    int y2 = Transform(f2, channelHeight, c, verticalZoom);
                                    bitmap.DrawLineBresenham(x1, y1, x2, y2, int32, bitmapContext);
                                    f1 = f2;
                                }
                            }
                        }

                        if (readSamples < samples) // draw end lines
                        {
                            int x = (readSamples) * samplePixels + 1;
                            if (drawEndIndicator)
                            {
                                LineDash[] lineDashes =
                                {
                                    new LineDash(3, theme.ColorEndIndicator),
                                    new LineDash(3, Color.FromRgb(0xF0, 0xF0, 0xF0).ToInt32())
                                };
                                bitmap.DrawLineBresenham(x, 0, x, bitmapHeight - 1, lineDashes);
                            }
                            if (drawDCLevel)
                            {
                                for (int i = 0; i < channels; i++)
                                {
                                    int y = channelHeight * i + channelCenter;
                                    bitmap.DrawLineBresenham(x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
                                }
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        var buffer = new float[bitmapWidth * hop];
                        int peaksWidth; // pixels
                        if (ratio < waveformCache.InitialRatio) // low overhead, just grab samples at this ratio
                        {
                            int position = positionSamples;
                            int numberOfSamples = bitmapWidth * ratio;
                            float[] floats = AudioStream.GetPeaks(ratio, numberOfSamples, position);
                            Array.Copy(floats, 0, buffer, 0, floats.Length);
                            peaksWidth = floats.Length / hop;
                        }
                        else // high overhead, grab portion of samples to draw from cache array
                        {
                            // OK !
                            float[] peaks = waveformCache.GetPeaks(ratio);
                            int sourceIndex = positionX * hop;
                            int sourceLength = bitmapWidth * hop;
                            if (sourceIndex + sourceLength > peaks.Length)
                                sourceLength = peaks.Length - sourceIndex;
                            Array.Copy(peaks, sourceIndex, buffer, 0, sourceLength);
                            peaksWidth = sourceLength / hop;
                        }
                        for (int c = 0; c < channels; c++)
                        {
                            int offsetMinPrev = c * channels + 0;
                            int offsetMaxPrev = c * channels + 1;
                            float minPrev = buffer[offsetMinPrev];
                            float maxPrev = buffer[offsetMaxPrev];
                            for (int x = 0; x < bitmapWidth; x++) // draw only vicible pixels
                            {
                                int offsetMin = x * channels * minMax + c * channels + 0;
                                int offsetMax = x * channels * minMax + c * channels + 1;
                                float min = buffer[offsetMin];
                                float max = buffer[offsetMax];
                                int y1 = Transform(minPrev, channelHeight, c, verticalZoom);
                                int y2 = Transform(maxPrev, channelHeight, c, verticalZoom);
                                int y3 = Transform(min, channelHeight, c, verticalZoom);
                                int y4 = Transform(max, channelHeight, c, verticalZoom);
                                if (drawForm)
                                {
                                    if (!drawEnvelope) bitmap.DrawLineBresenham(x - 1, y2, x, y4, colorEnvelope);
                                    bitmap.DrawLineBresenham(x, y3, x, y4, colorForm);
                                }
                                if (drawEnvelope)
                                {
                                    bitmap.DrawLineBresenham(x - 1, y1, x, y3, colorEnvelope);
                                    bitmap.DrawLineBresenham(x - 1, y2, x, y4, colorEnvelope);
                                }
                                maxPrev = max;
                                minPrev = min;
                            }
                        }
                        if (peaksWidth < bitmapWidth) // end indicator
                        {
                            int x = peaksWidth + 1;
                            bitmap.DrawLineBresenham(x, yMin, x, yMax, lineDashesEndIndicator);
                        }
                    }

                    #endregion
                }
        */

/*
        private static int Transform(float peak, int channelHeight, int channelIndex, double zoom)
        {
            return (int)((0.5d + 0.5d * -peak * zoom) * channelHeight + channelHeight * channelIndex);
        }
*/

        #endregion

        #region Commands handlers

        private void MoveLeftOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MoveLeftOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var increment = (int)e.Parameter;
            int ratio = ZoomRatio <= 0 ? 1 : ZoomRatio;
            Slider1.Value -= ratio * increment;
        }

        private void MoveRightOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MoveRightOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var increment = (int)e.Parameter;
            int ratio = ZoomRatio <= 0 ? 1 : ZoomRatio;
            Slider1.Value += ratio * increment;
        }

        private void MoveToHomeOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MoveToHomeOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Slider1.Value = Slider1.Minimum;
        }

        private void MoveToEndOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MoveToEndOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Slider1.Value = Slider1.Maximum;
        }

        private void ZoomOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ZoomOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // todo cleanup
            var parameter = (string)e.Parameter;
            switch (parameter)
            {
                case HorizontalZoomInParameter:
                    ZoomHorizontal(+1);
                    break;
                case HorizontalZoomOutParameter:
                    ZoomHorizontal(-1);
                    break;
                case HorizontalZoomResetParameter:
                    ZoomHorizontal(0);
                    break;
                case VerticalZoomInParameter:
                    ZoomVertical(+1);
                    break;
                case VerticalZoomOutParameter:
                    ZoomVertical(-1);
                    break;
                case VerticalZoomResetParameter:
                    ZoomVertical(0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("e", @"Invalid parameter");
            }
        }

        private void ZoomHorizontal(int i)
        {
            // todo behavior is ok but needs cleanup
            // todo move zoom bounds to property
            // Update zoom
            if (i > 0)
            {
                if (ZoomInteger > -5)
                {
                    ZoomInteger--;
                }
            }
            else if (i < 0)
            {
                if (ZoomInteger < 16)
                {
                    ZoomInteger++;
                }
            }
            else if (i == 0)
            {
                ZoomInteger = 0;
            }

            // Update slider
            int ratio = ZoomRatio;
            Slider1.SmallChange = ratio <= 0 ? 1 : Math.Abs(ratio);
            Slider1.LargeChange = Slider1.SmallChange * SliderLargeChange;
            Slider1.TickFrequency = ratio < 0 ? 1 : ratio;
            if (ratio > 1)
            {
                // clamp to ratio
                var value = (int)Slider1.Value;
                int mod = value % ratio;
                int newValue = value - mod;
                Slider1.Value = newValue;
            }
            // TODO do refresh one time only
            Refresh();
        }

        private void ZoomVertical(int value)
        {
            if (value > 0)
            {
                VerticalZoom *= 2.0d;
            }
            else if (value < 0)
            {
                VerticalZoom /= 2.0d;
            }
            else if (value == 0)
            {
                VerticalZoom = 1.0d;
            }
            Refresh();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /*
        private float[] GetPeaks(int ratio)
        {
            // this is wrtong it grabs too much
            if (ratio % 2 != 0) throw new ArgumentOutOfRangeException("ratio");
            long samples = Converters.BytesToSamples(AudioStream.Length, AudioStream.Bits, AudioStream.Channels);
            var chunks = (int)Math.Ceiling((double)samples / ratio);
            int channels = AudioStream.Channels;
            const int minmax = 2;
            var peaks = new float[chunks * channels * minmax];
            int z = 0;
            for (int x = 0; x < chunks; x++)
            {
                float[] readFrames = AudioStream.ReadSamples(ratio);
                for (int c = 0; c < channels; c++)
                {
                    float min = readFrames[c];
                    float max = readFrames[c];
                    for (int j = 0; j < ratio; j++)
                    {
                        int offset = j * channels + c;
                        float s = readFrames[offset];
                        if (s < min) min = s;
                        else if (s > max) max = s;
                    }
                    int offsetMin = x * channels * minmax + c * channels + 0;
                    int offsetMax = x * channels * minmax + c * channels + 1;
                    peaks[offsetMin] = min;
                    peaks[offsetMax] = max;
                    z = offsetMax;
                }
            }
            return peaks;
        }
*/
        /*
                private float[] GetPeaksSubset(float[] peaks, int channels, int inputRatio, int outputRatio)
                {
                    if (inputRatio % 2 != 0) throw new ArgumentOutOfRangeException("inputRatio");
                    if (outputRatio % 2 != 0) throw new ArgumentOutOfRangeException("outputRatio");
                    if (outputRatio <= inputRatio) throw new ArgumentOutOfRangeException("outputRatio");
                    int ratio = outputRatio / inputRatio;
                    const int minmax = 2;
                    int inputBlocks = peaks.Length / channels / minmax;
                    int outputBlocks = inputBlocks / ratio;
                    var subset = new float[peaks.Length / ratio];
                    for (int i = 0; i < outputBlocks; i++) // stereo not verified
                    {
                        for (int j = 0; j < channels; j++)
                        {
                            float min = peaks[i * ratio * channels];
                            float max = peaks[i * ratio * channels];
                            for (int k = 0; k < ratio; k++)
                            {
                                float s = peaks[i * ratio * channels + k];
                                if (s < min) min = s;
                                else if (s > max) max = s;
                            }
                            int m = i * channels * minmax + j * minmax;
                            subset[m] = min;
                            subset[m + 1] = max;
                        }
                    }
                    return subset;
                }
        */


        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
            var textBlock = (MenuItem)sender;
            var timelineUnit = (TimelineUnit)textBlock.DataContext;
            TimelineUnit = timelineUnit;
        }
    }
}