using System;
using System.Windows;

namespace SolidModelBrowser
{
    public partial class App : Application
    {
        public static void SetTheme(string theme)
        {
            Current.Resources.Clear();

            ResourceDictionary rdColors = new ResourceDictionary();
            rdColors.Source = new Uri("pack://application:,,,/SolidModelBrowser;component/Styles/" + theme);
            Current.Resources.MergedDictionaries.Add(rdColors);

            ResourceDictionary rdStyles = new ResourceDictionary();
            rdStyles.Source = new Uri("pack://application:,,,/SolidModelBrowser;component/Styles/Styles.xaml");
            Current.Resources.MergedDictionaries.Add(rdStyles);
        }
    }
}
