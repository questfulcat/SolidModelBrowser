using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace SolidModelBrowser
{
    public partial class ColorSelector : UserControl
    {
        public ColorSelector()
        {
            InitializeComponent();
            NBAlpha.ValueChanged += NB_ValueChanged;
            NBRed.ValueChanged += NB_ValueChanged;
            NBGreen.ValueChanged += NB_ValueChanged;
            NBBlue.ValueChanged += NB_ValueChanged;
        }

        Color getColor()
        {
            return Color.FromArgb((byte)NBAlpha.Value, (byte)NBRed.Value, (byte)NBGreen.Value, (byte)NBBlue.Value);
        }

        void NB_ValueChanged(object sender, System.EventArgs e)
        {
            BorderColorPreview.Background = new SolidColorBrush(getColor());
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ValueChanged;

        public Color ColorValue
        {
            get
            {
                return getColor();
            }
            set
            {
                NBAlpha.Value = value.A;
                NBRed.Value = value.R;
                NBGreen.Value = value.G;
                NBBlue.Value = value.B;
            }
        }
    }
}
