using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolidModelBrowser
{
    public partial class FileNavigationPanel : UserControl
    {
        public event SelectionChangedEventHandler SelectionChanged;

        public delegate void FileNavigationEventHandler(object Sender, string filename);
        public event FileNavigationEventHandler FileNavigated;
         

        public FileNavigationPanel()
        {
            InitializeComponent();

            listBoxFiles.SelectionChanged += ListBoxFiles_SelectionChanged;
            listBoxFiles.PreviewMouseDoubleClick += ListBoxFiles_PreviewMouseDoubleClick;
            listBoxFiles.PreviewKeyDown += ListBoxFiles_PreviewKeyDown;
            
            fill();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                RadioButton rb = new RadioButton();
                rb.Content = d.Name[0].ToString().ToUpper();
                //rb.Margin = new Thickness(4);
                rb.GroupName = "drivesgroup";
                rb.Checked += (sender, args) => { path = rb.Content as string + @":\"; fill(); };
                rb.BorderThickness = new Thickness(0);
                rb.MinWidth = 30;
                rb.MinHeight = 30;
                drivesWrapPanel.Children.Add(rb);
            }

            Path = @"c:\";
            watchForDirectory();
        }

        private string getSelectedItemPath()
        {
            if (listBoxFiles.SelectedItem == null) return null;
            TextBlock t = listBoxFiles.SelectedItem as TextBlock;
            if (t == null) return null;
            return t.Tag as string;
        }

        private void ListBoxFiles_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject temp = (DependencyObject)e.OriginalSource;
            while ((temp = VisualTreeHelper.GetParent(temp)) != null) if (temp is ListBoxItem) TryEnterSelectedFolder();
        }

        bool dontRaiseSelectionChanged = false;
        private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dontRaiseSelectionChanged) return;
            SelectionChanged?.Invoke(this, e);

            string p = getSelectedItemPath();
            if (p == null) return;
            string ext = System.IO.Path.GetExtension(p).Trim('.').ToLower();
            if (!UseExtensionFilter || extensions.Contains(ext)) FileNavigated?.Invoke(this, p);
        }

        private void ListBoxFiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryEnterSelectedFolder();
            if (e.Key == Key.Back && listBoxFiles.Items.Count > 0)
            {
                if ((listBoxFiles.Items[0] as TextBlock)?.Text == "..")
                {
                    listBoxFiles.SelectedIndex = 0;
                    TryEnterSelectedFolder();
                }
            }

            if (IsIgnoringLetterKeysNavigation && e.Key >= Key.A && e.Key <= Key.Z) e.Handled = true;
        }

        private void TryEnterSelectedFolder()
        {
            try
            {
                TextBlock t = listBoxFiles.SelectedItem as TextBlock;
                if (t != null)
                {
                    if (t.Text == "..")
                    {
                        DirectoryInfo dir = new DirectoryInfo(path);
                        string dirname = dir.Name;
                        path = dir.Parent.FullName;
                        fill();

                        foreach (var li in listBoxFiles.Items) if ((li as TextBlock)?.Text == dirname) listBoxFiles.SelectedItem = li;
                    }
                    else
                    {
                        string testpath = System.IO.Path.Combine(path, t.Text);
                        if (Directory.Exists(testpath))
                        {
                            path = testpath;
                            fill();
                        }
                    }
                }
                if (listBoxFiles.Items.Count > 0)
                {
                    if (listBoxFiles.SelectedItem == null) listBoxFiles.SelectedIndex = 0;
                    listBoxFiles.UpdateLayout();
                    listBoxFiles.ScrollIntoView(listBoxFiles.SelectedItem);
                    ListBoxItem temp = (listBoxFiles.ItemContainerGenerator.ContainerFromItem(listBoxFiles.SelectedItem) as ListBoxItem);
                    if (temp != null) temp.Focus();
                }
            }
            catch { }
        }

        
        //SolidColorBrush folderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 200, 240));

        private void fill() => fill(false);
        private void fill(bool keepSelectedItem)
        {
            if (!Directory.Exists(path)) return;

            string si = getSelectedItemPath();

            listBoxFiles.Items.Clear();

            if ((new DirectoryInfo(path)).Parent != null) listBoxFiles.Items.Add(new TextBlock() { Text = ".." });

            string[] dirs = Directory.GetDirectories(path);
            for (int c = 0; c < dirs.Length; c++)
            {
                TextBlock t = new TextBlock();
                t.Text = System.IO.Path.GetFileName(dirs[c]);
                t.Tag = dirs[c];
                t.FontWeight = FontWeights.Bold;
                //t.Foreground = folderBrush;
                listBoxFiles.Items.Add(t);
            }

            string[] files = Directory.GetFiles(path);
            for (int c = 0; c < files.Length; c++)
                if (extensions.Contains(System.IO.Path.GetExtension(files[c]).Trim('.').ToLower()) || !UseExtensionFilter)
                {
                    TextBlock t = new TextBlock();
                    t.Text = System.IO.Path.GetFileName(files[c]);
                    t.Tag = files[c];
                    listBoxFiles.Items.Add(t);
                }

            if (keepSelectedItem)
            {
                var li = listBoxFiles.Items.OfType<TextBlock>().FirstOrDefault(t => t.Tag?.ToString() == si);
                listBoxFiles.SelectedItem = li;
            }
        }

        public void Reselect()
        {
            if (listBoxFiles.SelectedItem != null)
            {
                object sel = listBoxFiles.SelectedItem;
                listBoxFiles.SelectedItem = null;
                listBoxFiles.SelectedItem = sel;
            }
        }

        public void Refresh()
        {
            dontRaiseSelectionChanged = true;
            fill(true);
            dontRaiseSelectionChanged = false;
            if (listBoxFiles.SelectedItem == null) ListBoxFiles_SelectionChanged(this, null);
        }

        FileSystemWatcher watcher = new FileSystemWatcher();
        void watchForDirectory()
        {
            watcher.Path = path;
            watcher.Filter = "*.*";
            watcher.Changed += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Created += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Deleted += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Renamed += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.EnableRaisingEvents = true;
        }

        string path = @"c:\";
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                if (!Directory.Exists(value)) return;
                
                string drv = value.Substring(0, 1).ToUpper();
                var rbutton = drivesWrapPanel.Children.OfType<RadioButton>().FirstOrDefault(rb => (rb.Content as string).StartsWith(drv));
                if (rbutton != null) rbutton.IsChecked = true;
                
                path = value;
                fill();
                watcher.Path = path;
            }
        }

        public string FullName
        {
            get
            {
                string fname = (listBoxFiles.SelectedItem as TextBlock)?.Text;
                if (fname != null && fname != "..") return System.IO.Path.Combine(path, fname);
                return null;
            }
        }

        public bool UseExtensionFilter { get; set; } = true;

        List<string> extensions = new List<string> { "avi" };
        public List<string> Extensions
        {
            get
            {
                return extensions;
            }
            set
            {
                extensions = value;
                fill(true);
            }
        }

        public bool IsIgnoringLetterKeysNavigation { get; set; }
    }
}
