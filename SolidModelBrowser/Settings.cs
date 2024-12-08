using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace SolidModelBrowser
{
    internal class Settings
    {
        [PropertyInfo(Category = "Interface", Description = "Use application light theme")]
        public bool LightTheme { get; set; } = false;

        [PropertyInfo(Category = "Main Window", Description = "Window state")]
        public WindowState WindowState { get; set; } = WindowState.Normal;

        [PropertyInfo(Category = "Main Window", Description = "Window X location")]
        public double LocationX { get; set; } = 100;

        [PropertyInfo(Category = "Main Window", Description = "Window Y location")]
        public double LocationY { get; set; } = 150;

        [PropertyInfo(Category = "Main Window", Description = "Window width")]
        public double Width { get; set; } = 800;

        [PropertyInfo(Category = "Main Window", Description = "Window height")]
        public double Height { get; set; } = 600;

        [PropertyInfo(Category = "Main Window", Description = "Max Window width, set 0 for screen limit")]
        public double MaxWidth { get; set; } = 0;

        [PropertyInfo(Category = "Main Window", Description = "Max Window height, set 0 for screen limit\r\n# this option is to manually limit window on non primary displays")]
        public double MaxHeight { get; set; } = 0;

        [PropertyInfo(Category = "Main Window", Description = "Width of file panel")]
        public double FilePanelWidth { get; set; } = 200;

        [PropertyInfo(Category = "Main Window", Description = "Opacity of file panel when it is not in use")]
        public double FilePanelIdleOpacity { get; set; } = 0.2;


        [PropertyInfo(Category = "Utils", Description = "External application to open current viewed file")]
        public string ExternalApp { get; set; } = "";

        [PropertyInfo(Category = "Utils", Description = "External application command line arguments\r\n# Variable $file$ = current filename")]
        public string ExternalAppArguments { get; set; } = "$file$";

        [PropertyInfo(Category = "Utils", Description = "Last selected path in file panel")]
        public string SelectedPath { get; set; } = @"c:\";

        [PropertyInfo(Category = "Utils", Description = "DPI to render image before save to file")]
        public int SaveImageDPI { get; set; } = 600;


        [PropertyInfo(Category = "Materials", Description = "Model diffuse color ARGB")]
        public SColor DiffuseColor { get; set; } = new SColor("#FFFFFF00");// Color.FromArgb(255, 255, 255, 0);

        [PropertyInfo(Category = "Materials", Description = "Model specular color ARGB")]
        public SColor SpecularColor { get; set; } = new SColor("#FFDCDCDC");// Color.FromArgb(255, 220, 220, 220);

        [PropertyInfo(Category = "Materials", Description = "Model emissive color ARGB")]
        public SColor EmissiveColor { get; set; } = new SColor("#FF000040");// Color.FromArgb(255, 0, 0, 64);

        [PropertyInfo(Category = "Materials", Description = "Model backside diffuse color ARGB")]
        public SColor BackDiffuseColor { get; set; } = new SColor("#FFFF0000");// Color.FromArgb(255, 255, 0, 0);

        [PropertyInfo(Category = "Materials", Description = "Value that specifies the degree to which a material applied to a 3-D model reflects the lighting model as shine")]
        public double SpecularPower { get; set; } = 40.0;

        [PropertyInfo(Category = "Materials", Description = "Diffuse material is enabled")]
        public bool IsDiffuseEnabled { get; set; } = true;

        [PropertyInfo(Category = "Materials", Description = "Specular material is enabled")]
        public bool IsSpecularEnabled { get; set; } = true;

        [PropertyInfo(Category = "Materials", Description = "Emissive material is enabled")]
        public bool IsEmissiveEnabled { get; set; } = true;

        [PropertyInfo(Category = "Materials", Description = "Backside diffuse material is enabled")]
        public bool IsBackDiffuseEnabled { get; set; } = true;


        [PropertyInfo(Category = "Interface", Description = "Orthographic camera mode is enabled")]
        public bool IsOrthoCameraEnabled { get; set; } = false;

        [PropertyInfo(Category = "Interface", Description = "Wide angle camera mode is enabled")]
        public bool IsFishEyeModeEnabled { get; set; } = false;

        [PropertyInfo(Category = "Interface", Description = "Show XYZ axes lines")]
        public bool IsAxesEnabled { get; set; } = false;

        [PropertyInfo(Category = "Interface", Description = "Show ground plane")]
        public bool IsGroundEnabled { get; set; } = false;

        [PropertyInfo(Category = "Interface", Description = "Use different colors for files in panel depending on file extensions")]
        public bool ColorizeFiles { get; set; } = true;


        [PropertyInfo(Category = "Camera", Description = "Camera field of view angle")]
        public double FOV { get; set; } = 45.0;

        [PropertyInfo(Category = "Camera", Description = "Camera field of view angle in fish eye mode")]
        public double FishEyeFOV { get; set; } = 120.0;

        [PropertyInfo(Category = "Camera", Description = "Camera basic shift from model in units relative to model overall size")]
        public double CameraInitialShift { get; set; } = 2.0;


        [PropertyInfo(Category = "Model", Description = "Model rotation angle: 45/90 deg options allowed")]
        public double ModelRotationAngle { get; set; } = 90.0;


        string defPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

        class PropertyInfoAttribute : Attribute
        {
            public string Category { get; set; }
            public string Description { get; set; }
        }


        // --- Save ---

        class PropInfo
        {
            public string Category;
            public string Name;
            public object Value;
            public string Description;
        }

        List<PropInfo> getPropInfoList(object obj, bool sort)
        {
            List<PropInfo> pi = new List<PropInfo>();
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var attr = p.GetCustomAttributes(true).OfType<PropertyInfoAttribute>().FirstOrDefault();
                pi.Add(new PropInfo() { Name = p.Name, Value = p.GetValue(this, null), Category = attr?.Category ?? "All", Description = attr?.Description });
            }
            if (sort) pi.Sort((a, b) => string.Compare(a.Category + a.Name, b.Category + b.Name));
            return pi;
        }

        public void Save() => Save(defPath, true);
        public void Save(string filename, bool writeDescriptions)
        {
            var pi = getPropInfoList(this, true);
            List<string> lines = new List<string>();

            string currentCatName = null;
            foreach (var p in pi)
            {
                if (p.Category != currentCatName)
                {
                    if (lines.Count > 0) lines.Add("");
                    lines.Add($"[{p.Category}]");
                    currentCatName = p.Category;
                }
                if (p.Value != null)
                {
                    if (writeDescriptions && p.Description != null) lines.Add("# " + p.Description);
                    lines.Add($"{p.Name}={escape(p.Value.ToString())}");
                }
            }
            File.WriteAllLines(filename, lines);
        }


        // --- Load ---

        List<string> parseErrors = new List<string>();
        public List<string> GetParseErrors() => parseErrors;
        public int GetParseErrorsCount() => parseErrors.Count;

        public void Load() => Load(defPath);
        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                parseErrors.Add($"No settings file {filename}, default one is created");
                Save();
                return;
            }
            var lines = File.ReadAllLines(filename);
            Load(lines);
        }

        public void Load(string[] lines)
        {
            parseErrors.Clear();
            var t = this.GetType();
            foreach (string line in lines)
            {
                if (line.Length > 0 && char.IsLetter(line[0]))
                {
                    int pos = line.IndexOf('=');
                    string name = pos >= 0 ? line.Substring(0, pos) : line;
                    string value = pos >= 0 ? unescape(line.Substring(pos + 1)) : "";
                    var p = t.GetProperty(name);
                    if (p != null)
                    {
                        Type pt = p.PropertyType;
                        object v = null;
                        try
                        {
                            if (pt == typeof(string)) v = value;
                            else if (pt.IsEnum) v = Enum.Parse(pt, value);
                            else
                            {
                                var mi = pt.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                                if (mi != null) v = mi.Invoke(null, new object[] { value });
                                else if (p.GetValue(this, null) != null)
                                {
                                    mi = pt.GetMethod("Parse", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
                                    if (mi != null) mi.Invoke(p.GetValue(this, null), new object[] { value });
                                    else parseErrors.Add($"No parser for {pt.Name}");
                                }
                            }

                            if (v != null) p.SetValue(this, v, null);
                        }
                        catch { parseErrors.Add($"Parsing failed for {name}={value}"); }
                    }
                    else parseErrors.Add($"No property in object with name {name}");
                }
            }
        }

        string escape(string s) => s.Replace("\r", "<CR>").Replace("\n", "<LF>");
        string unescape(string s) => s.Replace("<CR>", "\r").Replace("<LF>", "\n");

        public void StartProcess() => System.Diagnostics.Process.Start(defPath);
    }

    public class SColor
    {
        public Color Color { get; set; }
        public SColor(string s) => this.Parse(s);
        public override string ToString() => Color.ToString();
        public void Parse(string s) => Color = (Color)ColorConverter.ConvertFromString(s);
    }
}
