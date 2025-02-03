using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolidModelBrowser
{
    public enum SettingsWindowResult { None, ResetDefaults, OpenEditor };

    public partial class SettingsWindow : Window
    {
        public SettingsWindowResult WindowResult { get; set; } = SettingsWindowResult.None;

        public SettingsWindow()
        {
            InitializeComponent();

            ButtonClose.Click += (s, e) => Close();
            BorderHeader.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };
            ScrollViewerBase.PreviewMouseWheel += ScrollViewerBase_PreviewMouseWheel;

            ButtonLoadDefaults.Click += ButtonLoadDefaults_Click;
            ButtonOpenInTextEditor.Click += ButtonOpenInTextEditor_Click;
        }

        private void ButtonOpenInTextEditor_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.MessageWindow("Close app and open settings.ini in default editor?\r\n(You have to associate ini files with Notepad++ or other text editor before using this option)", this, "YES,Cancel", Orientation.Horizontal) != "YES") return;
            WindowResult = SettingsWindowResult.OpenEditor;
            Close();
        }

        private void ButtonLoadDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.MessageWindow("Are you sure you want to reset all settings to default values?", this, "YES,Cancel", Orientation.Horizontal) != "YES") return;
            WindowResult = SettingsWindowResult.ResetDefaults;
            Close();
        }

        private void ScrollViewerBase_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
