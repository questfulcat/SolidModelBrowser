using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ExportSTLAscii : Export
    {
        public ExportSTLAscii()
        {
            Extension = "stl";
            Description = "STL Ascii";
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            string solidname = "SolidModelBrowserExport";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"solid {solidname}");

            int q = mesh.TriangleIndices.Count / 3;
            int nq = mesh.Normals.Count;

            for (int c = 0; c < q; c++)
            {
                Point3D p1 = mesh.Positions[mesh.TriangleIndices[c * 3]];
                Point3D p2 = mesh.Positions[mesh.TriangleIndices[c * 3 + 1]];
                Point3D p3 = mesh.Positions[mesh.TriangleIndices[c * 3 + 2]];
                Vector3D n = c * 3 < nq ? mesh.Normals[c * 3] : Utils.GenerateNormal(p1, p2, p3);

                sb.AppendLine($"facet normal {n.X} {n.Y} {n.Z}");
                sb.AppendLine("  outer loop");
                sb.AppendLine($"    vertex {p1.X} {p1.Y} {p1.Z}");
                sb.AppendLine($"    vertex {p2.X} {p2.Y} {p2.Z}");
                sb.AppendLine($"    vertex {p3.X} {p3.Y} {p3.Z}");
                sb.AppendLine("  endloop");
                sb.AppendLine("endfacet");
            }

            sb.AppendLine($"endsolid {solidname}");
            File.WriteAllText(filename, sb.ToString());
        }
    }
}
