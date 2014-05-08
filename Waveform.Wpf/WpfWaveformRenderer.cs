using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Graphics;
using Graphics.Wpf;

namespace Waveform.Wpf
{
        /// <summary>
    ///     A WPF implementation of <see cref="Renderer" />.
    /// </summary>
    /// <remarks><see cref="GetBitmap" /> will return an object of type <see cref="WriteableBitmap" />.</remarks>
        public class WpfWaveformRenderer:WaveformRenderer
   {
        private WriteableBitmap _bitmap;

    
       public override RendererContext GetContext()
        {
            return new WpfRendererContext(this);
        }

        /// <summary>
        ///     Gets the bitmap.
        /// </summary>
        /// <returns>A <see cref="WriteableBitmap" /> object.</returns>
        public override object GetBitmap()
        {
            return _bitmap;
        }

        public override void GetBitmapSize(out int bitmapWidth, out int bitmapHeight)
        {
            bitmapWidth = _bitmap.PixelWidth;
            bitmapHeight = _bitmap.PixelHeight;
        }

        public override void SetBitmapSize(int bitmapWidth, int bitmapHeight)
        {
            if (bitmapWidth <= 0.0d) throw new ArgumentOutOfRangeException("bitmapWidth");
            if (bitmapHeight <= 0.0d) throw new ArgumentOutOfRangeException("bitmapHeight");

            if (_bitmap == null)
            {
                _bitmap = new WriteableBitmap(bitmapWidth, bitmapHeight, 96, 96, PixelFormats.Pbgra32, null);
            }
            else
            {
                // Update bitmap only if size is different
                int currentWidth;
                int currentHeight;
                GetBitmapSize(out currentWidth, out currentHeight);
                if (bitmapWidth != currentWidth || bitmapHeight != currentHeight)
                {
                    _bitmap = new WriteableBitmap(bitmapWidth, bitmapHeight, 96, 96, PixelFormats.Pbgra32, null);
                }
            }
        }
    }
}
