using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SolidModelBrowser
{
    public class TextEncodedImage : Image
    {
        WriteableBitmap stringToBitmap(SolidColorBrush pen, string s)
        {
            if (pen == null || s == null) return null;
            try
            {
                //var watch = System.Diagnostics.Stopwatch.StartNew();
                uint ppen = ((uint)pen.Color.A << 24) + ((uint)pen.Color.R << 16) + ((uint)pen.Color.G << 8) + pen.Color.B;
                int basecode = s[0];
                int w = s[1] - basecode;
                int h = s[2] - basecode;
                var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);

                uint[,] data = new uint[h, w];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (((s[3 + (y * w + x) / 6] - basecode) & (1 << (y * w + x) % 6)) > 0) data[y, x] = ppen;

                // antialiasing
                uint phalf = ((uint)(pen.Color.A / 4) << 24) + ((uint)pen.Color.R << 16) + ((uint)pen.Color.G << 8) + pen.Color.B;
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        int q = 0;
                        if (data[y - 1, x] == ppen) q++;
                        if (data[y + 1, x] == ppen) q++;
                        if (data[y, x - 1] == ppen) q++;
                        if (data[y, x + 1] == ppen) q++;
                        if ((q == 2 || q == 3) && data[y, x] == 0) data[y, x] = phalf;
                    }

                bmp.WritePixels(new System.Windows.Int32Rect(0, 0, w, h), data, bmp.Format.BitsPerPixel * w / 8, 0);

                //watch.Stop();
                //MessageBox.Show(watch.ElapsedTicks.ToString());
                return bmp;
            }
            catch { return null; }
        }

        public static readonly DependencyProperty EncodedImageProperty = DependencyProperty.Register("EncodedImage", typeof(string), typeof(TextEncodedImage), new PropertyMetadata(null, propertyChangedCallback));
        public string EncodedImage
        {
            get { return (string)GetValue(EncodedImageProperty); }
            set { SetValue(EncodedImageProperty, value); }
        }

        public static readonly DependencyProperty EncodedImageColorProperty = DependencyProperty.Register("EncodedImageColor", typeof(SolidColorBrush), typeof(TextEncodedImage), new PropertyMetadata(null, propertyChangedCallback));
        public SolidColorBrush EncodedImageColor
        {
            get { return (SolidColorBrush)GetValue(EncodedImageColorProperty); }
            set { SetValue(EncodedImageColorProperty, value); }
        }

        private static void propertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEncodedImage o = (TextEncodedImage)obj;
            o.Source = o.stringToBitmap((SolidColorBrush)o.GetValue(EncodedImageColorProperty) /*?? Brushes.White*/, (string)o.GetValue(EncodedImageProperty));
        }
    }
}
