using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ExportPLYAscii : Export
    {
        public ExportPLYAscii()
        {
            Extension = "ply";
            Description = "PLY Ascii";
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            bool withnormals = mesh.Normals.Count == mesh.Positions.Count;
            int faces = mesh.TriangleIndices.Count / 3;
            int positions = mesh.Positions.Count;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ply");
            sb.AppendLine("format ascii 1.0");
            sb.AppendLine("comment Exported from SolidModelBrowser");
            sb.AppendLine($"element vertex {positions}");
            sb.AppendLine("property float x");
            sb.AppendLine("property float y");
            sb.AppendLine("property float z");
            if (withnormals)
            {
                sb.AppendLine("property float nx");
                sb.AppendLine("property float ny");
                sb.AppendLine("property float nz");
            }
            sb.AppendLine($"element face {faces}");
            sb.AppendLine("property list uchar uint vertex_indices");
            sb.AppendLine("end_header");

            for(int c = 0; c < positions; c++)
            {
                var p = mesh.Positions[c];
                sb.Append($"{(float)p.X} {(float)p.Y} {(float)p.Z}");
                if (withnormals) sb.Append($" {(float)mesh.Normals[c].X} {(float)mesh.Normals[c].Y} {(float)mesh.Normals[c].Z}");
                sb.AppendLine();
            }

            for (int c = 0; c < faces; c++)
            {
                sb.AppendLine($"3 {mesh.TriangleIndices[c * 3]} {mesh.TriangleIndices[c * 3 + 1]} {mesh.TriangleIndices[c * 3 + 2]}");
            }

            File.WriteAllText(filename, sb.ToString());
        }
    }
}
