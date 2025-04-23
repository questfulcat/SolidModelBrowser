using SolidModelBrowser.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shell;

namespace SolidModelBrowser
{
    internal static class Utils
    {
        public static MeshGeometry3D FillMeshFromImport()
        {
            var mesh = new MeshGeometry3D();
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
            return mesh;
        }

        public static void FillMesh(MeshGeometry3D src, MeshGeometry3D dest)
        {
            dest.Positions = src.Positions;
            dest.Normals = src.Normals;
            dest.TriangleIndices = src.TriangleIndices;
        }

        public static void GetModelCenterAndSize(MeshGeometry3D mesh, out Point3D center, out Point3D massCenter, out Vector3D size)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            double massX = 0.0, massY = 0.0, massZ = 0.0;
            foreach (var p in mesh.Positions)
            {
                double x = p.X, y = p.Y, z = p.Z;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
                massX += x; massY += y; massZ += z;
            }
            size = new Vector3D(maxX - minX, maxY - minY, maxZ - minZ);
            center = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
            int q = mesh.Positions.Count;
            massCenter = new Point3D(massX / q, massY / q, massZ / q);
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
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                v.Add(mesh.TriangleIndices[i + 1]);
                v.Add(mesh.TriangleIndices[i]);
                v.Add(mesh.TriangleIndices[i + 2]);
            }
            mesh.TriangleIndices = v;
        }

        public static MeshGeometry3D UnsmoothMesh(MeshGeometry3D mesh)
        {
            var r = new MeshGeometry3D();
            var p = new Point3DCollection(mesh.Positions.Count);
            var i = new Int32Collection(mesh.TriangleIndices.Count);
            for (int c = 0; c < mesh.TriangleIndices.Count; c++)
            {
                p.Add(mesh.Positions[mesh.TriangleIndices[c]]);
                i.Add(c);
            }
            r.Positions = p;
            r.TriangleIndices = i;
            //r.Normals.Clear();
            return r;
        }



        public static Vector3D Norm(this Vector3D v) { v.Normalize(); return v; }

        public static MeshGeometry3D ConvertToWireframe(MeshGeometry3D src, double edgeScale)
        {
            var dest = UnsmoothMesh(src);
            var p = new Point3DCollection(dest.Positions.Count * 3);
            var i = new Int32Collection(dest.TriangleIndices.Count * 3);
            int iq = 0;
            for (int c = 0; c < dest.Positions.Count; c += 3)
            {
                Point3D p1 = dest.Positions[c], p2 = dest.Positions[c + 1], p3 = dest.Positions[c + 2];
                p.Add(p1);
                p.Add(p2);
                p.Add((Point3D)(p2 + (p3 - p2) * edgeScale));
                i.Add(iq++);
                i.Add(iq++);
                i.Add(iq++);

                p.Add(p2);
                p.Add(p3);
                p.Add((Point3D)(p3 + (p1 - p3) * edgeScale));
                i.Add(iq++);
                i.Add(iq++);
                i.Add(iq++);

                p.Add(p3);
                p.Add(p1);
                p.Add((Point3D)(p1 + (p2 - p1) * edgeScale));
                i.Add(iq++);
                i.Add(iq++);
                i.Add(iq++);
            }
            dest.Positions = p;
            dest.TriangleIndices = i;
            return dest;
        }

        //public static void ConvertToVertexframe(MeshGeometry3D mesh, double vertexScale)
        //{
        //    RecreateUnsmoothed(mesh);
        //    var p = new Point3DCollection(mesh.Positions.Count * 3);
        //    var i = new Int32Collection(mesh.TriangleIndices.Count * 3);
        //    int iq = 0;
        //    for (int c = 0; c < mesh.Positions.Count; c += 3)
        //    {
        //        Point3D p1 = mesh.Positions[c], p2 = mesh.Positions[c + 1], p3 = mesh.Positions[c + 2];
        //        p.Add(p1);
        //        p.Add((Point3D)(p1 + (p2 - p1).Norm() * vertexScale));
        //        p.Add((Point3D)(p1 + (p3 - p1).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);

        //        p.Add(p2);
        //        p.Add((Point3D)(p2 + (p1 - p2).Norm() * vertexScale));
        //        p.Add((Point3D)(p2 + (p3 - p2).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);

        //        p.Add(p3);
        //        p.Add((Point3D)(p3 + (p1 - p3).Norm() * vertexScale));
        //        p.Add((Point3D)(p3 + (p2 - p3).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);
        //    }
        //    mesh.Positions = p;
        //    mesh.TriangleIndices = i;
        //}

        //public static Stopwatch sw;
        public static void SliceMesh(MeshGeometry3D src, MeshGeometry3D dest, double z)
        {
            if (src == null) return;
            //sw = Stopwatch.StartNew();
            int q = src.TriangleIndices.Count;
            Int32Collection indices = new Int32Collection(q);

            var ti = new int[src.TriangleIndices.Count];
            src.TriangleIndices.CopyTo(ti, 0);
            var pos = new Point3D[src.Positions.Count];
            src.Positions.CopyTo(pos, 0);

            for (int c = 0; c < q; c += 3)
            {
                int i1 = ti[c], i2 = ti[c+1], i3 = ti[c+2];
                if (pos[i1].Z <= z && pos[i2].Z <= z && pos[i3].Z <= z)
                {
                    indices.Add(i1);
                    indices.Add(i2);
                    indices.Add(i3);
                }
            }
            
            //dest.Positions = new Point3DCollection(src.Positions.Count);
            //foreach(var p in src.Positions) dest.Positions.Add(p);
            if (dest.Positions.Count != src.Positions.Count)
                dest.Positions = src.Positions.Clone(); 
            dest.Normals = src.Normals.Clone();
            dest.TriangleIndices = indices;

            //sw.Stop();
        }

        //static Point3DCollection storedMeshPositions;
        //static Int32Collection storedMeshTriangles;
        //static Vector3DCollection storedMeshNormals;
        //public static void StoreMesh(MeshGeometry3D mesh)
        //{
        //    storedMeshPositions = mesh.Positions;
        //    storedMeshTriangles = mesh.TriangleIndices;
        //    storedMeshNormals = mesh.Normals;
        //}

        //public static void RestoreMesh(MeshGeometry3D mesh)
        //{
        //    mesh.Positions = storedMeshPositions;
        //    mesh.TriangleIndices = storedMeshTriangles;
        //    mesh.Normals = storedMeshNormals;
        //}

        public static double MinMax(this double value, double min, double max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public static double SelectInRange(this double value, double[] options, double defvalue)
        {
            if (options.Contains(value)) return value;
            return defvalue;
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

        public static void SaveImagePNG(Viewport3D vp, int dpi)
        {
            var sd = new Microsoft.Win32.SaveFileDialog() { Filter = "PNG Image|*.png" };
            if (sd.ShowDialog().Value) RenderElementToPNG(vp, sd.FileName, dpi);
        }

        public static void RunExternalApp(string path, string args, string fname)
        {
            if (!File.Exists(fname)) { MessageWindow("Select file first"); return; }
            if (string.IsNullOrWhiteSpace(path)) { MessageWindow("No app specified in settings"); return; }
            System.Diagnostics.Process.Start(path, args.Replace("$file$", fname));
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

        public static Size GetCurrentScreenSize(System.Windows.Window w)
        {
            var scr = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(w).Handle);
            if (scr.Primary) return new Size(SystemParameters.MaximizedPrimaryScreenWidth, SystemParameters.MaximizedPrimaryScreenHeight);
            return new Size(scr.WorkingArea.Width + 16, scr.WorkingArea.Height + 16);
        }

        public static string GetKeyboardModifiers()
        {
            string CAS = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? "C" : "";
            CAS += Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ? "A" : "";
            CAS += Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? "S" : "";
            return CAS;
        }

        public static void Toggle(this ToggleButton b) => b.IsChecked = !b.IsChecked;

        public static string MessageWindow(string message) => MessageWindow(message, Application.Current.MainWindow, "OK", Orientation.Horizontal);
        public static string MessageWindow(string message, Window owner, string buttons, Orientation buttonsOrientation)
        {
            string result = "";
            Window mb = new Window() { SizeToContent = SizeToContent.WidthAndHeight, WindowStyle = WindowStyle.None, MinWidth = 200, MinHeight = 60, MaxWidth = 800, MaxHeight = 1000, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            if (owner != null && owner.Visibility == Visibility.Visible)
            {
                mb.Owner = owner;
                mb.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                owner.Effect = new BlurEffect() { Radius = 4.0 };
            }
            WindowChrome.SetWindowChrome(mb, new WindowChrome() { CaptionHeight = 0.0 });
            mb.SetResourceReference(Window.BackgroundProperty, "WindowBack");
            mb.SetResourceReference(Window.ForegroundProperty, "WindowFore");
            var spv = new StackPanel() { Margin = new Thickness(16) };
            var lab = new TextBlock() { Text = message, Margin = new Thickness(0, 0, 0, 16) };
            spv.Children.Add(lab);

            var spbtns = new StackPanel() { Orientation = buttonsOrientation, HorizontalAlignment = HorizontalAlignment.Center };
            var btns = buttons.Split(',');
            foreach (var b in btns)
            {
                var btn = new Button() { Content = b, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(30, 2, 30, 2) };
                btn.Click += (s, e) => { result = b; mb.Close(); };
                spbtns.Children.Add(btn);
            }
            spv.Children.Add(spbtns);
            mb.Content = spv;
            mb.ContentRendered += (s, e) => mb.InvalidateVisual();
            mb.KeyDown += (s, e) => { if (e.Key == Key.Escape) mb.Close(); };
            mb.ShowDialog();
            if (owner != null) owner.Effect = null;
            return result;
        }

        public static void SetWindowStyle(Window w, bool systemStyle, FrameworkElement elementToHide)
        {
            if (systemStyle)
            {
                w.Style = (Style)Application.Current.Resources["SimpleWindowStyle"];
                if (elementToHide != null) elementToHide.Visibility = Visibility.Collapsed;
            }
            else
            {
                w.Style = (Style)Application.Current.Resources["CustomWindowStyle"];
                if (elementToHide != null) elementToHide.Visibility = Visibility.Visible;
            }
        }

        public static Vector3D RotateVector(Vector3D v, double ax, double ay, double az)
        {
            // Convert angles from degrees to radians
            double radX = ax * (Math.PI / 180.0);
            double radY = ay * (Math.PI / 180.0);
            double radZ = az * (Math.PI / 180.0);

            // Rotation around X axis
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double y1 = v.Y * cosX - v.Z * sinX;
            double z1 = v.Y * sinX + v.Z * cosX;

            // Rotation around Y axis
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);
            double x2 = v.X * cosY + z1 * sinY;
            double z2 = -v.X * sinY + z1 * cosY;

            // Rotation around Z axis
            double cosZ = Math.Cos(radZ);
            double sinZ = Math.Sin(radZ);
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;

            return new Vector3D(x3, y3, z2);
        }

        public static Point3D RotatePoint(Point3D p, Point3D center, double ax, double ay, double az)
        {
            double vx = p.X - center.X;
            double vy = p.Y - center.Y;
            double vz = p.Z - center.Z;

            // Convert angles from degrees to radians
            double radX = ax * (Math.PI / 180.0);
            double radY = ay * (Math.PI / 180.0);
            double radZ = az * (Math.PI / 180.0);

            // Rotation around X axis
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double y1 = vy * cosX - vz * sinX;
            double z1 = vy * sinX + vz * cosX;

            // Rotation around Y axis
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);
            double x2 = vx * cosY + z1 * sinY;
            double z2 = -vx * sinY + z1 * cosY;

            // Rotation around Z axis
            double cosZ = Math.Cos(radZ);
            double sinZ = Math.Sin(radZ);
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;

            return new Point3D(center.X + x3, center.Y + y3, center.Z + z2);
        }

        public static void RotateMesh(MeshGeometry3D mesh, Point3D center, double ax, double ay, double az)
        {
            var positions = new Point3DCollection(mesh.Positions.Count);
            var normals = new Vector3DCollection(mesh.Normals.Count);
            foreach (var p in mesh.Positions) positions.Add(RotatePoint(p, center, ax, ay, az));
            foreach (var n in mesh.Normals) normals.Add(RotateVector(n, ax, ay, az));
            mesh.Positions = positions;
            mesh.Normals = normals;
        }


        public static Vector3D GenerateNormal(Point3D p1, Point3D p2, Point3D p3)
        {
            return Vector3D.CrossProduct(p2 - p1, p3 - p1).Norm();
        }
    }
}
