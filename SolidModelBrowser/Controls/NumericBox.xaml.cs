using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolidModelBrowser
{
    public partial class NumericBox : UserControl
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;

        bool mousePressed;
        Point mousePressedPos;
        double mousePressedInitialValue;

        public NumericBox()
        {
            InitializeComponent();
            textbox.LostFocus += (sender, e) => applyValue();
            textbox.KeyDown += textbox_KeyDown;
            //textbox.SelectionChanged += textbox_SelectionChanged;

            textbox.PreviewMouseDown += textbox_PreviewMouseDown;
            textbox.PreviewMouseMove += textbox_PreviewMouseMove;
            textbox.PreviewMouseUp += textbox_PreviewMouseUp;

            this.PreviewMouseDown += NumericBox_PreviewMouseDown;
        }

        void textbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //textbox.SelectionLength = 0;
            //e.Handled = true;
        }


        void textbox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                mousePressed = true;
                mousePressedPos = e.GetPosition(textbox);
                mousePressedInitialValue = val;
            }
        }

        void textbox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mousePressed = false;
        }

        void textbox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double mult = 1.0;
            if (CtrlPressed()) mult = 10.0;
            if (ShiftPressed()) mult = 0.1;
            if (mousePressed)
            {
                Value = mousePressedInitialValue + (e.GetPosition(textbox) - mousePressedPos).X * mult;
            }

        }


        void NumericBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            applyValue();
            double posx = e.GetPosition(textbox).X;
            if (posx < 20) { val -= increment; updateValue(true); }
            if (posx > textbox.ActualWidth - 20) { val += increment; updateValue(true); }
        }

        void textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && (e.Key != Key.OemMinus) && (e.Key != Key.OemPeriod)) e.Handled = true;
            if (e.Key == Key.Enter) applyValue();
        }

        void applyValue()
        {
            {
                try
                {
                    string s = textbox.Text;
                    if (s == "" || s == "-") s = "0";
                    double d = double.Parse(s, culture);
                    val = d;
                    updateValue(true);
                }
                catch
                {
                    updateValue(true);
                }
            }
        }

        void updateValue(bool redraw)
        {
            double v = val;
            if (val < minvalue) val = minvalue;
            if (val > maxvalue) val = maxvalue;

            if (val != v) redraw = true;

            if (redraw)
            {
                int cp = textbox.CaretIndex;
                string s = val.ToString(culture);

                // validate fractional digits count
                int dotpos = s.LastIndexOf('.');
                if (dotpos >= 0)
                {
                    int fracdq = s.Length - dotpos - 1;
                    if (fracdq > rounddigits)
                    {
                        s = s.Substring(0, s.Length - (fracdq - rounddigits));
                    }
                }

                textbox.Text = s;

                if (cp > 0) textbox.CaretIndex = cp;

                if (ValueChanged != null) ValueChanged(this, new EventArgs());
            }
        }


        public static bool CtrlPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        public static bool ShiftPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        public static bool AltPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }


        // --- Events ---

        public event EventHandler ValueChanged;


        // --- Properties ---

        double val = 0.0;
        public double Value
        {
            get { return val; }
            set
            {
                val = value;
                updateValue(true);
            }
        }

        double minvalue = double.MinValue;
        public double MinValue
        {
            get { return minvalue; }
            set
            {
                minvalue = value;
                updateValue(true);
            }
        }

        double maxvalue = double.MaxValue;
        public double MaxValue
        {
            get { return maxvalue; }
            set
            {
                maxvalue = value;
                updateValue(true);
            }
        }

        int rounddigits = 3;
        public int RoundDigits
        {
            get { return rounddigits; }
            set
            {
                rounddigits = value;
                updateValue(true);
            }
        }

        double increment = 1.0;
        public double Increment
        {
            get { return increment; }
            set { increment = value; }
        }

        
        //public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(NumericBox));
        //public string Title
        //{
        //    get { return (string)GetValue(TitleProperty); }
        //    set { SetValue(TitleProperty, value); }
        //}

        //public static readonly DependencyProperty TitleMarginProperty = DependencyProperty.Register("TitleMargin", typeof(Thickness), typeof(NumericBox));
        //public Thickness TitleMargin
        //{
        //    get { return (Thickness)GetValue(TitleMarginProperty); }
        //    set { SetValue(TitleMarginProperty, value); }
        //}


    }
}
