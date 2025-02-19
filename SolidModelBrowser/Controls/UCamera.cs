using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    public class UCamera
    {
        Viewport3D viewport;
        PerspectiveCamera camPerspective = new PerspectiveCamera() { NearPlaneDistance = 0.1, FarPlaneDistance = 1000000.0 };
        OrthographicCamera camOrthographic = new OrthographicCamera() { NearPlaneDistance = 0.1, FarPlaneDistance = 1000000.0 };


        public UCamera(Viewport3D vp)
        {
            viewport = vp;
            viewport.Camera = camPerspective;
        }

        public void SelectType(bool orthographic)
        {
            if (orthographic) viewport.Camera = camOrthographic;
            else viewport.Camera = camPerspective;
            validateCamera();
        }

        public void DefaultPosition(double modelLen, double cameraInitialShift)
        {
            Position = new Point3D(lookCenter.X, lookCenter.Y - modelLen * cameraInitialShift, lookCenter.Z + modelLen * cameraInitialShift / 2);
            validateCamera();
            camOrthographic.Width = modelLen * cameraInitialShift;
        }

        public void RelativePosition(double modelLen, double dx, double dy, double dz)
        {
            Position = new Point3D(lookCenter.X + dx * modelLen, lookCenter.Y + dy * modelLen, lookCenter.Z + dz * modelLen);
            validateCamera();
        }

        public void TurnAt(Point3D pos)
        {
            lookCenter = pos;
            validateCamera();
        }

        void validateCamera()
        {
            camPerspective.LookDirection = lookCenter - camPerspective.Position;
            camPerspective.UpDirection = new Vector3D(0, 0, 1);
            camOrthographic.LookDirection = lookCenter - camOrthographic.Position;
            camOrthographic.UpDirection = new Vector3D(0, 0, 1);
            camOrthographic.Width = (Position - lookCenter).Length;
        }

        public void Scale(double mult)
        {
            Position = lookCenter + (Position - lookCenter) * mult;
            validateCamera();
        }

        public void Move(double dx, double dy)
        {
            var dv = Utils.GetShiftVectorInNormalSurface(Position - lookCenter, -dx, -dy);
            Position += dv;
            lookCenter += dv;
            validateCamera();
        }

        public void Orbit(double dx, double dy)
        {
            var nv1 = Utils.RotateVectorInParallel(Position - lookCenter, -dx / 50);
            var nv2 = Utils.RotateVectorInMeridian(nv1, -dy / 50);
            Position = lookCenter + nv2;
            validateCamera();
        }

        public void MoveFOV(double dx, double dy)
        {
            var f = (camPerspective.FieldOfView - dy - dx).MinMax(10, 160);
            camPerspective.FieldOfView = f;
        }

        public double FOV
        {
            get { return camPerspective.FieldOfView; }
            set { camPerspective.FieldOfView = value; }
        }

        Point3D lookCenter = new Point3D();
        public Point3D LookCenter
        {
            get { return lookCenter; }
            set
            {
                lookCenter = value;
                validateCamera();
            }
        }

        public Point3D Position
        {
            get { return camPerspective.Position; }
            set
            {
                camPerspective.Position = value;
                camOrthographic.Position = value;
                camOrthographic.Width = (Position - lookCenter).Length;
            }
        }
    }
}
