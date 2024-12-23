using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal abstract class Import
    {
        public static bool StopFlag = false;
        public static int Progress = 0;
        public static string ExceptionMessage;

        // static buffers are common for all importers
        public static List<Vector3D> Normals = new List<Vector3D>();
        public static List<Point3D> Positions = new List<Point3D>();
        public static List<int> Indices = new List<int>();

        // extensions should be in lower case
        public List<string> Extensions { get; protected set; } = new List<string>();
        public Dictionary<string, SolidColorBrush> ExtensionsColors { get; protected set; } = new Dictionary<string, SolidColorBrush>();
        public Dictionary<string, SolidColorBrush> ExtensionsColorsLight { get; protected set; } = new Dictionary<string, SolidColorBrush>();
        public bool InitialXRotationNeeded { get; protected set; } = false;

        public abstract void Load(string filename);

        public void Initialize()
        {
            StopFlag = false;
            Progress = 0;
            ExceptionMessage = null;

            Normals.Clear();
            Positions.Clear();
            Indices.Clear();
        }
    }
}
