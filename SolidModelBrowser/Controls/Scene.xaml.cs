using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    public partial class Scene : UserControl
    {
        GeometryModel3D geometry = new GeometryModel3D();
        MeshGeometry3D mesh = new MeshGeometry3D();
        UCamera camera;

        public Scene()
        {
            InitializeComponent();
            geometry.Geometry = mesh;
            geometry.Material = materials;
            modelgroup.Children.Add(geometry);
            modelgroup.Children.Add(lights);

            camera = new UCamera(viewport3D);

            
        }

        
        // mouse controllers

        Point mLastPos;

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            camera.Scale(e.Delta > 0 ? 0.9 : 1.1);
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
            {
                Point pos = Mouse.GetPosition(viewport);
                Point actualPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
                double dx = actualPos.X - mLastPos.X, dy = actualPos.Y - mLastPos.Y;

                if (e.LeftButton == MouseButtonState.Pressed) camera.Move(dx, dy);
                if (e.RightButton == MouseButtonState.Pressed) camera.Orbit(dx, dy);

                mLastPos = actualPos;
            }
        }

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.LeftShift)) { ShiftAndDrag?.Invoke(this, EventArgs.Empty); return; }

            Point pos = Mouse.GetPosition(viewport);
            mLastPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
            ((IInputElement)sender).CaptureMouse();
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement)sender).ReleaseMouseCapture();
        }

        // materials

        DiffuseMaterial matDiffuse;
        SpecularMaterial matSpecular;
        EmissiveMaterial matEmissive;
        DiffuseMaterial matBackDiffuse;
        
        public void CreateMaterials(Color diffuse, Color specular, Color emissive, Color backDiffuse, double specularPower)
        {
            matDiffuse = new DiffuseMaterial(new SolidColorBrush(diffuse));
            //matDiffuse.AmbientColor = Color.FromArgb(255, 0, 255, 0);
            matSpecular = new SpecularMaterial(new SolidColorBrush(specular), specularPower);
            matEmissive = new EmissiveMaterial(new SolidColorBrush(emissive));
            matBackDiffuse = new DiffuseMaterial(new SolidColorBrush(backDiffuse));
        }

        MaterialGroup materials = new MaterialGroup();

        public void ApplyMaterials(bool diffuse, bool specular, bool emissive, bool backDiffuse)
        {
            materials.Children.Clear();
            if (diffuse) materials.Children.Add(matDiffuse);
            if (specular) materials.Children.Add(matSpecular);
            if (emissive) materials.Children.Add(matEmissive);
            geometry.Material = materials;

            geometry.BackMaterial = backDiffuse ? matBackDiffuse : null;
        }

        // lights

        Model3DGroup lights = new Model3DGroup();

        public void RemoveAllLights() => lights.Children.Clear();
        public void CreateAmbientLight(Color c) => lights.Children.Add(new AmbientLight(c));
        public void CreateDirectionalLight(Color c, Vector3D dir) => lights.Children.Add(new DirectionalLight(c, dir));

        // axes

        enum Axis { X, Y, Z }
        GeometryModel3D createAxis(Axis axis, double len, Color color)
        {
            Point3DCollection p = null;
            if (axis == Axis.X) p = new Point3DCollection() { new Point3D(0, 0, 0), new Point3D(len, 0, 0), new Point3D(0, 0, 1), new Point3D(len, 0, 1) };
            if (axis == Axis.Y) p = new Point3DCollection() { new Point3D(0, 0, 0), new Point3D(0, len, 0), new Point3D(0, 0, 1), new Point3D(0, len, 1) };
            if (axis == Axis.Z) p = new Point3DCollection() { new Point3D(0, 0, 1), new Point3D(0, 0, len), new Point3D(1, 0, 1), new Point3D(1, 0, len) };
            var i = new Int32Collection() { 0, 1, 2, 1, 3, 2 };
            var mesh = new MeshGeometry3D() { Positions = p, TriangleIndices = i };
            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            return new GeometryModel3D() { Geometry = mesh, Material = mat, BackMaterial = mat };
        }

        Model3DGroup axes = new Model3DGroup();
        public void CreateAxes(double len)
        {
            axes.Children.Clear();
            axes.Children.Add(createAxis(Axis.X, len, Color.FromArgb(160, 255, 0, 0)));
            axes.Children.Add(createAxis(Axis.Y, len, Color.FromArgb(160, 0, 255, 0)));
            axes.Children.Add(createAxis(Axis.Z, len, Color.FromArgb(160, 0, 0, 255)));
        }

        // ground

        GeometryModel3D ground = new GeometryModel3D();
        public void CreateGround(Rect r, double checkerSize, Color diffuseColor, Color emissiveColor)
        {
            if (r.Width == 0 || r.Height == 0 || checkerSize == 0) return;

            var p = new Point3DCollection() { new Point3D(r.Left, r.Top, 0), new Point3D(r.Right, r.Top, 0), new Point3D(r.Left, r.Bottom, 0), new Point3D(r.Right, r.Bottom, 0) };
            var texcoord = new PointCollection() { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };
            var i = new Int32Collection() { 0, 1, 2, 1, 3, 2 };
            var mesh = new MeshGeometry3D { Positions = p, TextureCoordinates = texcoord, TriangleIndices = i };

            var geomcoll = new GeometryCollection() { new RectangleGeometry(new Rect(0, 0, 50, 50)), new RectangleGeometry(new Rect(50, 50, 50, 50)) };
            var coloredDrawing = new GeometryDrawing() { Brush = new SolidColorBrush(diffuseColor), Geometry = new GeometryGroup() { Children = geomcoll } };
            var drawingBrush = new DrawingBrush { Viewport = new Rect(0, 0, 2 * checkerSize / r.Width, 2 * checkerSize / r.Height), TileMode = TileMode.Tile, Drawing = coloredDrawing };
            var diffuseMaterial = new DiffuseMaterial() { Brush = drawingBrush };
            var specularMaterial = new SpecularMaterial() { Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0)) };
            var emissiveMaterial = new EmissiveMaterial() { Brush = new SolidColorBrush(emissiveColor) };
            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(diffuseMaterial);
            materialGroup.Children.Add(specularMaterial);
            materialGroup.Children.Add(emissiveMaterial);
            var backMaterial = new DiffuseMaterial() { Brush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xA0, 0xA0)) };

            ground.Geometry = mesh;
            ground.Material = materialGroup;
            ground.BackMaterial = backMaterial;
        }


        // events and props

        public event EventHandler ShiftAndDrag;

        public MeshGeometry3D Mesh => mesh;
        public UCamera Camera => camera;
        public Viewport3D Viewport3D => viewport3D;

        bool isAxesVisible;
        public bool IsAxesVisible
        {
            get { return isAxesVisible; }
            set
            {
                isAxesVisible = value;
                if (modelgroup.Children.Contains(axes)) modelgroup.Children.Remove(axes);
                if (isAxesVisible) modelgroup.Children.Add(axes);
            }
        }

        bool isGroundVisible;
        public bool IsGroundVisible
        {
            get { return isGroundVisible; }
            set
            {
                isGroundVisible = value;
                if (modelgroup.Children.Contains(ground)) modelgroup.Children.Remove(ground);
                if (isGroundVisible) modelgroup.Children.Add(ground);
            }
        }

    }
}
