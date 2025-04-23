using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal abstract class Import
    {
        public static bool StopFlag = false;
        public static int Progress = 0;
        public static string ExceptionMessage;

        protected static Settings settings;

        // static buffers are common for all importers
        public static List<Vector3D> Normals = new List<Vector3D>();
        public static List<Point3D> Positions = new List<Point3D>();
        public static List<int> Indices = new List<int>();

        // extensions should be in lower case
        public List<string> Extensions { get; protected set; } = new List<string>();
        public Dictionary<string, SolidColorBrush> ExtensionsColors { get; protected set; } = new Dictionary<string, SolidColorBrush>();
        public Dictionary<string, SolidColorBrush> ExtensionsColorsLight { get; protected set; } = new Dictionary<string, SolidColorBrush>();
        public bool InitialXRotationNeeded { get; protected set; } = false;

        public abstract void Load(string filename);

        public void Initialize()
        {
            StopFlag = false;
            Progress = 0;
            ExceptionMessage = null;

            Normals.Clear();
            Positions.Clear();
            Indices.Clear();
        }

        public static void BindSettings(Settings s) => settings = s;

        public static List<Import> Imports = new List<Import>();
        public static Import CurrentImport = null;

        static Import()
        {
            Type thisT = MethodBase.GetCurrentMethod().DeclaringType;
            var importTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType != null && t.BaseType == thisT);
            foreach (var importType in importTypes) Imports.Add((Import)Activator.CreateInstance(importType));
        }

        public static bool SelectImporter(string filename)
        {
            string ext = Path.GetExtension(filename).Trim('.').ToLower();
            foreach (Import i in Imports)
                if (i.Extensions.Contains(ext))
                {
                    CurrentImport = i;
                    CurrentImport.Initialize();
                    return true;
                }
            return false;
        }

        public static void FillColorsDictionary(Dictionary<string, SolidColorBrush> cd, bool lightTheme)
        {
            cd.Clear();
            foreach(var i in Imports)
            {
                var d = lightTheme ? i.ExtensionsColorsLight : i.ExtensionsColors;
                foreach (var c in d) cd.Add(c.Key, c.Value);
            }
        }
    }
}
