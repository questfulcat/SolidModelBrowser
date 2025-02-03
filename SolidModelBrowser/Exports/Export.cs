using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal abstract class Export
    {
        public string Extension { get; protected set; } = "";
        public string Description { get; protected set; } = "";
        public bool InitialXRotationNeeded { get; protected set; }

        public abstract void Save(MeshGeometry3D mesh, string filename);

        public static List<Export> Exports = new List<Export>();

        static Export()
        {
            Type thisT = MethodBase.GetCurrentMethod().DeclaringType;
            var importTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType != null && t.BaseType == thisT);
            foreach (var importType in importTypes) Exports.Add((Export)Activator.CreateInstance(importType));
        }
    }
}
