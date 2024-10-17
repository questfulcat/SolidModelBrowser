using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal static class Utils
    {
        public static void FillMesh(MeshGeometry3D mesh)
        {
            // don't try to optimize; mesh and 3Dcollections are single thread; also dont fill mesh objects directly in mesh cos it is too slow
            var normals = new Vector3DCollection(Import.Normals.Count);
            var positions = new Point3DCollection(Import.Positions.Count);
            var indices = new Int32Collection(Import.Indices.Count);
            foreach (var n in Import.Normals) normals.Add(n);
            foreach (var p in Import.Positions) positions.Add(p);
            foreach (var i in Import.Indices) indices.Add(i);
            mesh.Normals = normals;
            mesh.Positions = positions;
            mesh.TriangleIndices = indices;
        }

        public static void GetModelCenterAndSize(MeshGeometry3D mesh, out Point3D center, out Vector3D size)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            foreach (var p in mesh.Positions)
            {
                double x = p.X, y = p.Y, z = p.Z;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }
            size = new Vector3D(maxX - minX, maxY - minY, maxZ - minZ);
            center = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        }

        public static Vector3D GetShiftVectorInNormalSurface(Vector3D v, double dx, double dy)
        {
            var ap = Math.Atan2(v.Y, v.X) + Math.PI / 2;
            var v1 = new Vector3D(Math.Cos(ap), Math.Sin(ap), 0);

            var v2 = Vector3D.CrossProduct(v, v1);
            if (v2.Length > 0) v2.Normalize();

            var r = v.Length;
            return (v1 * dx + v2 * dy) * r / 1000;
        }

        public static Vector3D RotateVectorInParallel(Vector3D v, double angle)
        {
            var a = Math.Atan2(v.Y, v.X) + angle;
            var r = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            return new Vector3D(r * Math.Cos(a), r * Math.Sin(a), v.Z);
        }

        public static Vector3D RotateVectorInMeridian(Vector3D v, double angle)
        {
            var qx = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            var am = (Math.Atan2(v.Z, qx) + angle).MinMax(-Math.PI / 2 + 0.01, Math.PI / 2 - 0.01);
            var r = v.Length;
            var ap = Math.Atan2(v.Y, v.X);
            var rp = r * Math.Cos(am);
            return new Vector3D(rp * Math.Cos(ap), rp * Math.Sin(ap), r * Math.Sin(am));
        }

        //public static Transform3D GetRotation(Vector3D r, Point3D center)
        //{
        //    if (r.Length == 0) return null;
        //    var t = new Transform3DGroup();
        //    var tx = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), r.X), center);
        //    var ty = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), r.Y), center);
        //    var tz = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), r.Z), center);
        //    t.Children.Add(tx);
        //    t.Children.Add(ty);
        //    t.Children.Add(tz);
        //    return t;
        //}

        //public static void RotateMesh(MeshGeometry3D mesh)
        //{
        //    var t = new Point3DCollection();
        //    var mp = mesh.Positions;
        //    mesh.Positions = null;
        //    for (int c = 0; c < mp.Count; c++)
        //    {
        //        var p = mp[c];
        //        t.Add(new Point3D(-p.Y, p.X, p.Z));
        //    }
        //    mesh.Positions = t;
        //}

        public static void InvertNormals(MeshGeometry3D mesh)
        {
            if (mesh.Normals == null) return;
            var normals = new Vector3DCollection(mesh.Normals.Count);
            foreach (var normal in mesh.Normals) normals.Add(-normal);
            mesh.Normals = normals;
        }

        public static void InvertVertexOrder(MeshGeometry3D mesh)
        {
            if (mesh.TriangleIndices == null || mesh.TriangleIndices.Count % 3 != 0) return;
            var v = new Int32Collection(mesh.TriangleIndices.Count);
            for (int i = 0; i < mesh.TriangleIndices.Count; i+=3)
            {
                v.Add(mesh.TriangleIndices[i + 1]);
                v.Add(mesh.TriangleIndices[i]);
                v.Add(mesh.TriangleIndices[i + 2]);
            }
            mesh.TriangleIndices = v;
        }

        public static double MinMax(this double value, double min, double max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public static void RenderElementToPNG(FrameworkElement e, string filename, int dpi)
        {
            double scale = (double)dpi / 96; // scale DPI
            var img = new RenderTargetBitmap((int)(scale * (e.ActualWidth + 1)), (int)(scale * (e.ActualHeight + 1)), dpi, dpi, PixelFormats.Default);
            img.Render(e);

            var encoder = new PngBitmapEncoder();
            BitmapFrame frame = BitmapFrame.Create(img);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(filename)) encoder.Save(stream);
        }

        //public static GeometryModel3D GetAxesModel()
        //{
        //    var model = new GeometryModel3D();
        //    model.Geometry = new MeshGeometry3D() { Positions = new Point3DCollection() { 0, 0, 0,  100 0 0  0 0 1  100 0 1 } }
        //}

        //public static void saveTest(Point3DCollection positions, Vector3DCollection normals, Int32Collection indices)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var p in positions) sb.AppendLine($"p x={p.X} y={p.Y} z={p.Z}");
        //    sb.AppendLine();
        //    foreach (var n in normals) sb.AppendLine($"n x={n.X} y={n.Y} z={n.Z}");
        //    sb.AppendLine();
        //    for(int c = 0; c < indices.Count; c += 3) sb.AppendLine($"f {indices[c]} {indices[c + 1]} {indices[c + 2]}");
        //    File.WriteAllText(@"c:\downloads\text.txt", sb.ToString());
        //}
    }
}
