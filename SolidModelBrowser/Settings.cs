﻿using System;
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

        [PropertyInfo(Category = "Main Window", Description = "Max Window height, set 0 for screen limit\r\nthis option is to manually limit window on non primary displays")]
        public double MaxHeight { get; set; } = 0;

        [PropertyInfo(Category = "Main Window", Description = "Width of file panel")]
        public double FilePanelWidth { get; set; } = 200;

        [PropertyInfo(Category = "Main Window", Description = "Opacity of file panel when it is not in use", MenuLabel = "File panel idle opacity", Min = 0.0, Max = 1.0, Increment = 0.1)]
        public double FilePanelIdleOpacity { get; set; } = 0.2;


        [PropertyInfo(Category = "Utils", Description = "External application to open current viewed file", MenuLabel = "External Application")]
        public string ExternalApp { get; set; } = "";

        [PropertyInfo(Category = "Utils", Description = "External application command line arguments\r\nVariable $file$ = current filename", MenuLabel = "External Application Arguments")]
        public string ExternalAppArguments { get; set; } = "$file$";

        [PropertyInfo(Category = "Utils", Description = "Last selected path in file panel")]
        public string SelectedPath { get; set; } = @"c:\";

        [PropertyInfo(Category = "Utils", Description = "DPI to render image before save to file", MenuLabel = "Save image DPI")]
        public int SaveImageDPI { get; set; } = 600;


        [PropertyInfo(Category = "Materials", Description = "Model diffuse color ARGB", MenuLabel = "Diffuse color")]
        public SColor DiffuseColor { get; set; } = new SColor("#FFFFFF00");

        [PropertyInfo(Category = "Materials", Description = "Model specular color ARGB", MenuLabel = "Specular color")]
        public SColor SpecularColor { get; set; } = new SColor("#FFDCDCDC");

        [PropertyInfo(Category = "Materials", Description = "Model emissive color ARGB", MenuLabel = "Emissive color")]
        public SColor EmissiveColor { get; set; } = new SColor("#FF000040");

        [PropertyInfo(Category = "Materials", Description = "Model backside diffuse color ARGB", MenuLabel = "Inner diffuse color")]
        public SColor BackDiffuseColor { get; set; } = new SColor("#FFFF0000");

        [PropertyInfo(Category = "Materials", Description = "Value that specifies the degree to which a material applied to a 3-D model reflects the lighting model as shine", MenuLabel = "Specular power", Min = 0.0, Max = 10000.0)]
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

        [PropertyInfo(Category = "Interface", Description = "Use different colors for files in panel depending on file extensions", MenuLabel = "Different colors for files")]
        public bool ColorizeFiles { get; set; } = true;

        [PropertyInfo(Category = "Interface", Description = "Sort files by extensions in filepanel", MenuLabel = "Sort files by extensions")]
        public bool SortFilesByExtensions { get; set; } = false;


        [PropertyInfo(Category = "Camera", Description = "Camera field of view angle", MenuLabel = "Normal FOV", Min = 5.0, Max = 160.0)]
        public double FOV { get; set; } = 45.0;

        [PropertyInfo(Category = "Camera", Description = "Camera field of view angle in fish eye mode", MenuLabel = "FOV in fish eye mode", Min = 5.0, Max = 160.0)]
        public double FishEyeFOV { get; set; } = 120.0;

        [PropertyInfo(Category = "Camera", Description = "Camera basic shift from model in units relative to model overall size", MenuLabel = "Initial shift", Min = 0.5, Max = 10.0)]
        public double CameraInitialShift { get; set; } = 2.0;

        [PropertyInfo(Category = "Camera", Description = "Default camera look at model points average or geometric center", MenuLabel = "Default camera look at points average center")]
        public bool DefaultLookAtModelPointsAvgCenter { get; set; } = true;



        [PropertyInfo(Category = "Model", Description = "Model rotation angle: 45/90 deg options allowed")]
        public double ModelRotationAngle { get; set; } = 90.0;

        [PropertyInfo(Category = "Model", Description = "Wireframe mode edge scaling factor 0.001...0.9", MenuLabel = "Wireframe edge scale", Min = 0.001, Max = 0.9, Increment = 0.01)]
        public double WireframeEdgeScale { get; set; } = 0.03;

        [PropertyInfo(Category = "Model", Description = "Skip loading models original normals", MenuLabel = "Skip load normals")]
        public bool IgnoreOriginalNormals { get; set; } = false;

        [PropertyInfo(Category = "Model", Description = "Convert smoothed models to flat automatically", MenuLabel = "Unsmooth after loading")]
        public bool UnsmoothAfterLoading { get; set; } = false;



        string defPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

        


        // --- Save ---

        public void Save() => Save(defPath, true);
        public void Save(string filename, bool writeDescriptions)
        {
            var pi = PropertyInfoAttribute.GetPropertiesInfoList(this, true);
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
                    if (writeDescriptions && p.Description != null) lines.Add("# " + p.Description.Replace("\r\n", "\r\n# "));
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
        public SColor(Color c) => Color = c;
        public override string ToString() => Color.ToString();
        public void Parse(string s) => Color = (Color)ColorConverter.ConvertFromString(s);
    }
}
