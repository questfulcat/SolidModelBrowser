using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ImportPLY : Import
    {
        public ImportPLY()
        {
            Extensions = new List<string> { "ply" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "ply", new SolidColorBrush(Color.FromRgb(220, 150, 220)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "ply", new SolidColorBrush(Color.FromRgb(120, 20, 120)) } };
        }

        enum PLYFormat { Undef, ASCII, BinaryLE, BinaryBE }
        PLYFormat plyFormat = PLYFormat.Undef;
        int vertices = 0;
        int faces = 0;

        List<string> vertexTypes = new List<string>();
        List<string> vertexValues = new List<string>();
        
        int headerLinesCount = 0;

        char[] separator = new char[] { ' ' };

        void initialize()
        {
            plyFormat = PLYFormat.Undef;
            vertices = 0;
            faces = 0;
            vertexTypes.Clear();
            vertexValues.Clear();
            headerLinesCount = 0;
        }

        void loadHeader(string filename)
        {
            headerLinesCount = 0;
            using (var fileStream = File.OpenRead(filename))
            using (var streamReader = new StreamReader(fileStream, Encoding.ASCII, true, 1024))
            {
                string s;
                string currElement = "";
                while ((s = streamReader.ReadLine()) != null && !s.Trim().ToLower().StartsWith("end_header"))
                {
                    string t = s.Trim().ToLower();
                    string[] parts = t.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    int tq = parts.Length - 1;
                    if (parts[0] == "format")
                    {
                        if (parts[1] == "ascii") plyFormat = PLYFormat.ASCII;
                        if (parts[1] == "binary_little_endian") plyFormat |= PLYFormat.BinaryLE;
                        if (parts[1] == "binary_big_endian") plyFormat |= PLYFormat.BinaryBE;
                    }
                    if (parts[0] == "element")
                    {
                        if ((parts[1] == "vertex")) { currElement = "v"; vertices = int.Parse(parts[2]); }
                        else if ((parts[1] == "face")) { currElement = "f"; faces = int.Parse(parts[2]); }
                        else if (vertices == 0 || faces == 0) throw new Exception("Not supported PLY structure (elements order)");
                    }
                    if (parts[0] == "property")
                    {
                        if (currElement == "v")
                        {
                            vertexTypes.Add(parts[1]);
                            vertexValues.Add(parts[2]);
                        }
                        if (currElement == "f")
                        {
                            if (!(parts[1] == "list" && (parts[2] == "char" || parts[2] == "uchar") && (parts[3] == "int" || parts[3] == "uint"))) throw new Exception("Not supported PLY structure (faces property description)");
                        }
                    }
                    headerLinesCount++;
                }
                headerLinesCount++;
            }
        }

        public override void Load(string filename)
        {
            initialize();
            loadHeader(filename);
            int vq = vertexValues.Count;

            if (plyFormat == PLYFormat.ASCII)
            {
                string[] lines = File.ReadAllLines(filename);
                int linescount = lines.Length;
                for (int c = 0; c < vertices; c++)
                {
                    int linenum = headerLinesCount + c;
                    string[] parts = lines[linenum].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    double x = 0D, y = 0D, z = 0D;
                    for (int v = 0; v < vq; v++)
                    {
                        if (vertexValues[v] == "x") x = double.Parse(parts[v]);
                        if (vertexValues[v] == "y") y = double.Parse(parts[v]);
                        if (vertexValues[v] == "z") z = double.Parse(parts[v]);
                    }
                    Positions.Add(new Point3D(x, y, z));

                    Progress = linenum * 100 / linescount;
                    if (StopFlag) return;
                }
                for (int c = 0; c < faces; c++)
                {
                    int linenum = headerLinesCount + vertices + c;
                    string[] parts = lines[linenum].Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    int q = int.Parse(parts[0]);
                    if (q < 3) throw new Exception("Not supported PLY structure (faces should have at leat 3 vertices)");

                    int i1 = int.Parse(parts[1]);
                    int i2 = int.Parse(parts[2]);
                    for (int i = 0; i < q - 2; i++)
                    {
                        int i3 = int.Parse(parts[i + 3]);
                        Indices.Add(i1);
                        Indices.Add(i2);
                        Indices.Add(i3);
                        i2 = i3;
                    }

                    Progress = linenum * 100 / linescount;
                    if (StopFlag) return;
                }
            }
            else if (plyFormat == PLYFormat.BinaryLE)
            {
                using (var fileStream = File.OpenRead(filename))
                using (var br = new BinaryReader(fileStream))
                {
                    string endh = "end_header";
                    int cc = 0;
                    int cq = endh.Length;
                    while (cc < cq) cc = char.ToLower(br.ReadChar()) == endh[cc] ? cc + 1 : 0;
                    while (br.PeekChar() == 0x0D || br.PeekChar() == 0x0A) br.ReadChar();

                    int datacount = vertices + faces;
                    for (int c = 0; c < vertices; c++)
                    {
                        double x = 0D, y = 0D, z = 0D;
                        for (int v = 0; v < vq; v++)
                        {
                            if (vertexTypes[v] == "float")
                            {
                                float t = br.ReadSingle();
                                string vv = vertexValues[v];
                                if (vv == "x") x = t;
                                else if (vv == "y") y = t;
                                else if (vv == "z") z = t;
                            }
                            else if (vertexTypes[v] == "double")
                            {
                                double t = br.ReadDouble();
                                string vv = vertexValues[v];
                                if (vv == "x") x = t;
                                else if (vv == "y") y = t;
                                else if (vv == "z") z = t;
                            }
                            else if (vertexTypes[v] == "char" || vertexTypes[v] == "uchar") br.ReadByte();
                            else if (vertexTypes[v] == "short" || vertexTypes[v] == "ushort") br.ReadUInt16();
                            else if (vertexTypes[v] == "int" || vertexTypes[v] == "uint") br.ReadUInt32();
                            else throw new Exception("Not supported PLY structure (unknown data type)");
                        }
                        Positions.Add(new Point3D(x, y, z));

                        Progress = c * 100 / datacount;
                        if (StopFlag) return;
                    }

                    for (int c = 0; c < faces; c++)
                    {
                        int q = br.ReadByte();

                        if (q < 3) throw new Exception("Not supported PLY structure (faces should have at leat 3 vertices)");

                        int i1 = br.ReadInt32();
                        int i2 = br.ReadInt32();
                        for (int i = 0; i < q - 2; i++)
                        {
                            int i3 = br.ReadInt32();
                            Indices.Add(i1);
                            Indices.Add(i2);
                            Indices.Add(i3);
                            i2 = i3;
                        }

                        Progress = (vertices + c) * 100 / datacount;
                        if (StopFlag) return;
                    }
                }
            }
            else if (plyFormat == PLYFormat.BinaryBE) throw new Exception("Not supported PLY structure (Big Endian Binary PLY is not supported)");
            else throw new Exception("Not supported PLY structure (Unknown format)");
        }
    }
}
