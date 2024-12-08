using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class Import3MF : Import
    {
        public Import3MF()
        {
            Extensions = new List<string> { "3mf" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "3mf", new SolidColorBrush(Color.FromRgb(255, 150, 150)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "3mf", new SolidColorBrush(Color.FromRgb(200, 50, 50)) } };
            InitialXRotationNeeded = false;
        }

        string readZipEntry(ZipArchive zfile, string entryName)
        {
            var zentry = zfile.Entries.First(e => e.FullName.ToLower() == entryName);
            using (var sr = new StreamReader(zentry.Open(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        string getTag(string s, string tag, bool shorttail, ref int pos)
        {
            string tail = shorttail ? "/>" : $"</{tag}>";
            int ps = s.IndexOf("<" + tag, pos, StringComparison.Ordinal);
            if (ps < 0) return null;
            int pe = s.IndexOf(tail, ps, StringComparison.Ordinal);
            if (pe < 0) return null;
            pos = pe + tail.Length;
            return s.Substring(ps, pe - ps + tail.Length);
        }

        string getTag(string s, string tag, bool shorttail)
        {
            int pos = 0;
            return getTag(s, tag, shorttail, ref pos);
        }

        List<string> getTags(string s, string tag, bool shorttail)
        {
            string tail = shorttail ? "/>" : $"</{tag}>";
            int pos = 0;
            var tags = new List<string>();
            string t;
            while((t = getTag(s, tag, shorttail, ref pos)) != null) tags.Add(t);
            return tags;
        }

        string getParam(string s, string param)
        {
            return Regex.Match(s, $"{param}=\"(.*?)\"", RegexOptions.IgnoreCase).Groups[1].Value;
        }

        Matrix3D buildMatrix(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Matrix3D();
            var p = s.Split(' ');
            var m = new Matrix3D();
            m.M11 = double.Parse(p[0]);
            m.M12 = double.Parse(p[1]);
            m.M13 = double.Parse(p[2]);
            m.M21 = double.Parse(p[3]);
            m.M22 = double.Parse(p[4]);
            m.M23 = double.Parse(p[5]);
            m.M31 = double.Parse(p[6]);
            m.M32 = double.Parse(p[7]);
            m.M33 = double.Parse(p[8]);
            m.OffsetX = double.Parse(p[9]);
            m.OffsetY = double.Parse(p[10]);
            m.OffsetZ = double.Parse(p[11]);
            m.M44 = 1D;
            return m;
        }

        Vector3D? getVertex(string s, ref int pos)
        {
            Vector3D v = new Vector3D();
            int p1 = s.IndexOf("<vertex", pos, StringComparison.Ordinal);
            if (p1 < 0) return null;
            int p2 = s.IndexOf('"', p1 + 7);
            if (p2 < 0) return null;
            int p3 = s.IndexOf('"', p2 + 1);
            if (p3 < 0) return null;
            v.X = double.Parse(s.Substring(p2 + 1, p3 - p2 - 1));
            int p4 = s.IndexOf('"', p3 + 1);
            if (p4 < 0) return null;
            int p5 = s.IndexOf('"', p4 + 1);
            if (p5 < 0) return null;
            v.Y = double.Parse(s.Substring(p4 + 1, p5 - p4 - 1));
            int p6 = s.IndexOf('"', p5 + 1);
            if (p6 < 0) return null;
            int p7 = s.IndexOf('"', p6 + 1);
            if (p7 < 0) return null;
            v.Z = double.Parse(s.Substring(p6 + 1, p7 - p6 - 1));
            pos = p7 + 1;
            return v;
        }

        struct Triangle { public int v1, v2, v3; }
        Triangle? getTriangle(string s, ref int pos)
        {
            Triangle t = new Triangle();
            int p1 = s.IndexOf("<triangle", pos, StringComparison.Ordinal);
            if (p1 < 0) return null;
            int p2 = s.IndexOf('"', p1 + 7);
            if (p2 < 0) return null;
            int p3 = s.IndexOf('"', p2 + 1);
            if (p3 < 0) return null;
            t.v1 = int.Parse(s.Substring(p2 + 1, p3 - p2 - 1));
            int p4 = s.IndexOf('"', p3 + 1);
            if (p4 < 0) return null;
            int p5 = s.IndexOf('"', p4 + 1);
            if (p5 < 0) return null;
            t.v2 = int.Parse(s.Substring(p4 + 1, p5 - p4 - 1));
            int p6 = s.IndexOf('"', p5 + 1);
            if (p6 < 0) return null;
            int p7 = s.IndexOf('"', p6 + 1);
            if (p7 < 0) return null;
            t.v3 = int.Parse(s.Substring(p6 + 1, p7 - p6 - 1));
            pos = p7 + 1;
            return t;
        }

        int countLines(string s)
        {
            int r = 0;
            //for(int c = 0; c < s.Length; c++) if (s[c] == '\n') ++r;
            foreach(char c in s) if (c == '\n') ++r;
            return r;
        }

        int basepos = 0;
        long pr_len, pr_avglinelen, pr_pos;
        void initProgress(string data)
        {
            pr_len = data.Length;
            pr_avglinelen = data.Length / countLines(data);// Regex.Match(data, "^.*?<triangle.*?$", RegexOptions.Multiline).Length;
            pr_pos = 0;
        }

        void appendProgress(string data)
        {
            pr_len += data.Length;
            pr_avglinelen = data.Length / countLines(data);// Regex.Match(data, "^.*?<triangle.*?$", RegexOptions.Multiline).Length;
        }

        void stepProgress()
        {
            pr_pos += pr_avglinelen;
            Progress = (int)(pr_pos * 100 / pr_len);
        }

        void parseMesh(string obj, Matrix3D mtr)
        {
            int pos = 0;
            Vector3D? v0;
            while ((v0 = getVertex(obj, ref pos)) != null)
            {
                Vector3D v = v0.Value * mtr;
                Positions.Add(new Point3D(v.X + mtr.OffsetX, v.Y + mtr.OffsetY, v.Z + mtr.OffsetZ));

                stepProgress();
                if (StopFlag) return;
            }

            pos = 0;
            Triangle? t;
            while ((t = getTriangle(obj, ref pos)) != null)
            {
                Indices.Add(t.Value.v1 + basepos);
                Indices.Add(t.Value.v2 + basepos);
                Indices.Add(t.Value.v3 + basepos);

                stepProgress();
                if (StopFlag) return;
            }
            basepos = Positions.Count;
        }


        Dictionary<string, List<string>> componentCache = new Dictionary<string, List<string>>();

        void loadComponent(string comp, string model, ZipArchive zfile, Matrix3D basemtr)
        {
            string id = getParam(comp, "objectid");
            string path = getParam(comp, "p:path").TrimStart('/').ToLower();
            string tr = getParam(comp, "transform");
            Matrix3D mtr = buildMatrix(tr) * basemtr;

            string resources;
            List<string> objects;
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (componentCache.ContainsKey(path)) objects = componentCache[path];
                else
                {
                    model = readZipEntry(zfile, path);
                    resources = getTag(model, "resources", false);
                    objects = getTags(resources, "object", false);
                    componentCache.Add(path, objects);
                }
            }
            else // if no path specified use local model
            {
                resources = getTag(model, "resources", false);
                objects = getTags(resources, "object", false);
            }

            string obj = objects.Where(o => o.Contains($" id=\"{id}\"")).FirstOrDefault();

            appendProgress(obj);

            parseMesh(obj, mtr);
        }

        public override void Load(string filename)
        {
            basepos = 0;
            string model;
            using (var zfile = ZipFile.OpenRead(filename))
            {
                model = readZipEntry(zfile, "3d/3dmodel.model");

                string resources = getTag(model, "resources", false);
                var objects = getTags(resources, "object", false);

                string build = getTag(model, "build", false);
                var items = getTags(build, "item", true);

                initProgress(model);

                foreach (string item in items)
                {
                    string id = getParam(item, "objectid");
                    string tr = getParam(item, "transform");
                    Matrix3D mtr = buildMatrix(tr);

                    string obj = objects.Where(o => o.Contains($" id=\"{id}\"")).FirstOrDefault();

                    //if (obj.Contains("<components>")) ExceptionMessage = "3MF mesh with external components is not supported yet";
                    string components = getTag(obj, "components", false);
                    if (components != null)
                    {
                        var comps = getTags(components, "component", true);
                        foreach (var comp in comps) loadComponent(comp, model, zfile, mtr);
                    }

                    parseMesh(obj, mtr);
                    if (StopFlag) return;
                }
            }
        }
    }
}
