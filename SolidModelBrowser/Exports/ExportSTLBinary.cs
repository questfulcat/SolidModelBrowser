using System;
using System.IO;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ExportSTLBinary : Export
    {
        public ExportSTLBinary()
        {
            Extension = "stl";
            Description = "STL Binary";
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Create, FileAccess.Write)))
            {
                bw.Write(" STL Exported with SolidModelBrowser".PadRight(79));

                int q = mesh.TriangleIndices.Count / 3;
                bw.Write(q);
                int nq = mesh.Normals.Count;

                for (int c = 0; c < q; c++)
                {
                    Point3D p1 = mesh.Positions[mesh.TriangleIndices[c * 3]];
                    Point3D p2 = mesh.Positions[mesh.TriangleIndices[c * 3 + 1]];
                    Point3D p3 = mesh.Positions[mesh.TriangleIndices[c * 3 + 2]];

                    Vector3D n = c * 3 < nq ? mesh.Normals[c * 3] : Utils.GenerateNormal(p1, p2, p3);
                    bw.Write((float)n.X);
                    bw.Write((float)n.Y);
                    bw.Write((float)n.Z);
                    
                    bw.Write((float)p1.X);
                    bw.Write((float)p1.Y);
                    bw.Write((float)p1.Z);
                    bw.Write((float)p2.X);
                    bw.Write((float)p2.Y);
                    bw.Write((float)p2.Z);
                    bw.Write((float)p3.X);
                    bw.Write((float)p3.Y);
                    bw.Write((float)p3.Z);

                    bw.Write((UInt16)0);
                }

            }
        }
    }
}
