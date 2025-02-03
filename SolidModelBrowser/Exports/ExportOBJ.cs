using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ExportOBJ : Export
    {
        public ExportOBJ()
        {
            Extension = "obj";
            Description = "OBJ";
            InitialXRotationNeeded = true;
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"# Exported from SolidModelBrowser");
            sb.AppendLine("o default");

            int q = mesh.TriangleIndices.Count;
            int nq = mesh.Normals.Count;

            foreach (var p in mesh.Positions) sb.AppendLine($"v {p.X} {p.Y} {p.Z}");
            foreach (var n in mesh.Normals) sb.AppendLine($"vn {n.X} {n.Y} {n.Z}");
            for (int c = 0; c < q; c += 3) sb.AppendLine($"f {mesh.TriangleIndices[c] + 1} {mesh.TriangleIndices[c + 1] + 1} {mesh.TriangleIndices[c + 2] + 1}");

            File.WriteAllText(filename, sb.ToString());
        }
    }
}
