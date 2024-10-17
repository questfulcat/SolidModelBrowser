using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ImportSTL : Import
    {
        public ImportSTL()
        {
            Extensions = new List<string> { "stl" };
        }

        char[] separator = { ' ' };

        public bool isBinaryFormat(string filename)
        {
            using (var f = File.OpenRead(filename))
            {
                if (f.Length < 84) return false;
                f.Seek(80, SeekOrigin.Begin);
                int trianglesCount = f.ReadByte() + (f.ReadByte() << 8) + (f.ReadByte() << 16) + (f.ReadByte() << 24);

                return trianglesCount * 50 + 84 == f.Length;
            }
        }

        public override void Load(string filename)
        {
            if (isBinaryFormat(filename))
            {
                using (BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Seek(80, SeekOrigin.Begin);
                    int q = br.ReadInt32();

                    for (int c = 0; c < q; c++)
                    {
                        var n = new Vector3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        Normals.Add(n);
                        Normals.Add(n);
                        Normals.Add(n);
                        Positions.Add(new Point3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                        Positions.Add(new Point3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                        Positions.Add(new Point3D(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                        br.ReadUInt16();
                        Indices.Add(c * 3);
                        Indices.Add(c * 3 + 1);
                        Indices.Add(c * 3 + 2);

                        Progress = c * 100 / q;
                        if (StopFlag) return;
                    }
                }
            }
            else // ASCII format
            {
                var lines = File.ReadAllLines(filename);
                int q = lines.Length; 

                if (q < 7 || !(lines[q - 1].Contains("endsolid") || lines[q - 2].Contains("endsolid"))) throw new Exception("Not valid STL format");

                int f = 0;
                for(int c = 0; c < q; c++)
                {
                    var line = lines[c].ToLower();
                    int i1 = line.IndexOf("facet normal");
                    if (i1 >= 0)
                    {
                        var s = line.Substring(i1 + 12).Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length != 3) throw new Exception($"Bad format, line {c} '{line}' should have 3 values");
                        var v = new Vector3D(double.Parse(s[0]), double.Parse(s[1]), double.Parse(s[2]));
                        Normals.Add(v);
                        Normals.Add(v);
                        Normals.Add(v);
                        Indices.Add(f * 3);
                        Indices.Add(f * 3 + 1);
                        Indices.Add(f * 3 + 2);
                        f++;
                    }
                    else
                    {
                        int i2 = line.IndexOf("vertex");
                        if (i2 >= 0)
                        {
                            var s = line.Substring(i2 + 6).Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            if (s.Length != 3) throw new Exception($"Bad format, line {c} '{line}' should have 3 values");
                            var p = new Point3D(double.Parse(s[0]), double.Parse(s[1]), double.Parse(s[2]));
                            Positions.Add(p);
                        }
                    }

                    Progress = c * 100 / q;
                    if (StopFlag) return;
                }
            }

        }
    }
}
