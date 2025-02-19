using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SolidModelBrowser
{
    public partial class DoubleValuesInput : UserControl
    {
        Thickness th1 = new Thickness(2, 0, 8, 0);

        public DoubleValuesInput()
        {
            InitializeComponent();
        }

        List<NumericBox> boxes = new List<NumericBox>();
        public DoubleValuesInput(string[] fieldNames, double min, double max, int roundDigits)
        {
            InitializeComponent();
            foreach (string field in fieldNames)
            {
                panel.Children.Add(new TextBlock { Text = field });
                var nb = new NumericBox() { MinValue = min, MaxValue = max, RoundDigits = roundDigits, Width = 100, Margin = th1 };
                nb.ValueChanged += (sender, e) => { ValueChanged?.Invoke(this, EventArgs.Empty); };
                panel.Children.Add(nb);
                boxes.Add(nb);
            }
        }

        public void SetMinValue(int boxindex, double min)
        {
            if (boxindex >=0 && boxindex < boxes.Count) boxes[boxindex].MinValue = min;
        }

        public void SetMaxValue(int boxindex, double max)
        {
            if (boxindex >= 0 && boxindex < boxes.Count) boxes[boxindex].MaxValue = max;
        }

        public event EventHandler ValueChanged;

        public double this [int index]
        {
            get { return index >= 0 && index < boxes.Count ? boxes[index].Value : double.NaN; }
            set { if (index >= 0 && index < boxes.Count) boxes[index].Value = value; }
        }
    }
}
