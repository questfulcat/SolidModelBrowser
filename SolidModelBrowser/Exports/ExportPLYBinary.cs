using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ExportPLYBinary : Export
    {
        public ExportPLYBinary()
        {
            Extension = "ply";
            Description = "PLY Binary";
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            bool withnormals = mesh.Normals.Count == mesh.Positions.Count;
            int faces = mesh.TriangleIndices.Count / 3;
            int positions = mesh.Positions.Count;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ply");
            sb.AppendLine("format binary_little_endian 1.0");
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

            File.WriteAllText(filename, sb.ToString());
            using (BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Append, FileAccess.Write)))
            {
                for (int c = 0; c < positions; c++)
                {
                    var p = mesh.Positions[c];
                    bw.Write((float)p.X);
                    bw.Write((float)p.Y);
                    bw.Write((float)p.Z);
                    
                    if (withnormals)
                    {
                        bw.Write((float)mesh.Normals[c].X);
                        bw.Write((float)mesh.Normals[c].Y);
                        bw.Write((float)mesh.Normals[c].Z);
                    }
                }

                for (int c = 0; c < faces; c++)
                {
                    bw.Write((byte)3);
                    bw.Write(mesh.TriangleIndices[c * 3]);
                    bw.Write(mesh.TriangleIndices[c * 3 + 1]);
                    bw.Write(mesh.TriangleIndices[c * 3 + 2]);
                }
            }
        }
    }
}
