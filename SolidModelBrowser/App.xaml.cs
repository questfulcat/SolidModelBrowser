using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace SolidModelBrowser
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
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
