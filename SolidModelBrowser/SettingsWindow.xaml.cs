using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolidModelBrowser
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            ButtonClose.Click += (s, e) => Close();
            BorderHeader.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };
            ScrollViewerBase.PreviewMouseWheel += ScrollViewerBase_PreviewMouseWheel;

            //ButtonLoadDefaults.Click += (s, e) =>
            //if (MessageBox.Show("Close app and open settings.ini in default editor?\r\n(You have to associate ini files with Notepad++ or other text editor before using this option)", "Request", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            //saveSettings();
            //settings.StartProcess();
            //Close();
        }

        private void ScrollViewerBase_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
