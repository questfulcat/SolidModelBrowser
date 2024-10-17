using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class Import3MF : Import
    {
        public Import3MF()
        {
            Extensions = new List<string> { "3mf" };
            InitialXRotationNeeded = true;
        }

        bool getToken(string s, string t, ref int p)
        {
            p = s.IndexOf(t, p);
            return p >= 0;
        }

        string getValue(string s, ref int p)
        {
            getToken(s, "\"", ref p);
            p++; int st = p;
            if (st < 0) return "0";
            getToken(s, "\"", ref p);
            if (p < 0) return "0";
            return s.Substring(st, p - st);
        }

        public override void Load(string filename)
        {
            using (var zfile = ZipFile.OpenRead(filename))
            {
                var zentry = zfile.Entries.First(e => e.Name.ToLower() == "3dmodel.model");
                using (var sr = new StreamReader(zentry.Open(), Encoding.UTF8))
                {
                    var strs = sr.ReadToEnd().Split(new string[] { "</mesh>" }, StringSplitOptions.None);
                    strs[strs.Length - 1] = "";
                    int prlen = strs.Sum((s) => s.Length); // progress len
                    int prpos = 0; // progress partial pos

                    int basepos = 0;
                    foreach (var s in strs) if (s != "")
                    {
                        int p = 0;
                        while (p >= 0)
                        {
                            double x = 0.0, y = 0.0, z = 0.0;
                            if (getToken(s, "<vertex", ref p))
                            {
                                if (getToken(s, "x=\"", ref p)) x = double.Parse(getValue(s, ref p));
                                if (getToken(s, "y=\"", ref p)) y = double.Parse(getValue(s, ref p));
                                if (getToken(s, "z=\"", ref p)) z = double.Parse(getValue(s, ref p));
                                Positions.Add(new Point3D(x, y, z));
                            }

                            if (p > 0) Progress = (int)((long)(prpos + p / 2) * 100 / prlen);
                            if (StopFlag) return;
                        }

                        prpos += s.Length / 2;
                        p = 0;
                        while (p >= 0)
                        {
                            int v1 = 0, v2 = 0, v3 = 0;
                            if (getToken(s, "<triangle", ref p))
                            {
                                if (getToken(s, "v1=\"", ref p)) v1 = int.Parse(getValue(s, ref p));
                                if (getToken(s, "v2=\"", ref p)) v2 = int.Parse(getValue(s, ref p));
                                if (getToken(s, "v3=\"", ref p)) v3 = int.Parse(getValue(s, ref p));
                                Indices.Add(v1 + basepos);
                                Indices.Add(v2 + basepos);
                                Indices.Add(v3 + basepos);
                            }

                            if (p > 0) Progress = (int)((long)(prpos + p / 2) * 100 / prlen);
                            if (StopFlag) return;
                        }

                        //// regex way (slower)
                        //var matches1 = Regex.Matches(s, @"<vertex\s+x=""(.*?)""\s+y=""(.*?)""\s+z=""(.*?)""");
                        //foreach (Match m in matches1) positions.Add(new Point3D(double.Parse(m.Groups[1].Value), double.Parse(m.Groups[2].Value), double.Parse(m.Groups[3].Value)));
                        //var matches2 = Regex.Matches(s, @"<triangle\s+v1=""(.*?)""\s+v2=""(.*?)""\s+v3=""(.*?)""");
                        //foreach (Match m in matches2)
                        //{
                        //    indices.Add(int.Parse(m.Groups[1].Value) + basepos);
                        //    indices.Add(int.Parse(m.Groups[2].Value) + basepos);
                        //    indices.Add(int.Parse(m.Groups[3].Value) + basepos);
                        //}

                        basepos = Positions.Count;

                        prpos += s.Length / 2;
                    }
                }
            } 
        }
    }
}
