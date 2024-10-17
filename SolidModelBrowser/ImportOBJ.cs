using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ImportOBJ : Import
    {
        public ImportOBJ()
        {
            Extensions = new List<string> { "obj" };
            InitialXRotationNeeded = true;
        }

        char[] separator = { ' ' };

        string[] getParts(string line, int linenum)
        {
            var p = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 4) throw new Exception($"OBJ parse failed at line {linenum}, '{line}' should have at least 3 values");
            return p;
        }

        public override void Load(string filename)
        {
            var lines = File.ReadAllLines(filename);
            int q = lines.Length;

            var pre_normals = new List<Vector3D>(4096);
            var pre_positions = new List<Point3D>(4096);

            int index = 0;

            for (int c = 0; c < q; c++)
            {
                var line = lines[c].TrimStart().ToLower();
                if (line.StartsWith("v ")) // vertex
                {
                    var p = getParts(line, c);
                    pre_positions.Add(new Point3D(double.Parse(p[1]), double.Parse(p[2]), double.Parse(p[3])));
                }
                else if (line.StartsWith("vn ")) // normal
                {
                    var p = getParts(line, c);
                    pre_normals.Add(new Vector3D(double.Parse(p[1]), double.Parse(p[2]), double.Parse(p[3])));
                }
                else if (line.StartsWith("f ")) // face
                {
                    var p = getParts(line, c);
                    if (p.Length != 4) throw new Exception($"OBJ contains non triangle faces at line {c}, {line}");
                    for (int i = 1; i < p.Length; i++)
                    {
                        var p2 = p[i].Split('/');
                        int pos = int.Parse(p2[0]) - 1;
                        Positions.Add(pre_positions[pos]);
                        Indices.Add(index++);

                        if (p2.Length == 3)
                        {
                            int n = int.Parse(p2[2]) - 1;
                            //if (n >= 0 && pre_normals.Count > n) normals.Add(pre_normals[n]);
                        }
                    }
                }

                Progress = c * 100 / q;
                if (StopFlag) return;
            }
        }
    }
}
