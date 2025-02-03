using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class Export3MF : Export
    {
        public Export3MF()
        {
            Extension = "3mf";
            Description = "3MF";
        }

        void addZipEntry(ZipArchive zip, string entryname, string content)
        {
            var ze = zip.CreateEntry(entryname);
            using (StreamWriter writer = new StreamWriter(ze.Open()))
            {
                writer.Write(content);
            }
        }

        public override void Save(MeshGeometry3D mesh, string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, true))
                {
                    addZipEntry(zip, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?> \r\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\" /><Default Extension=\"png\" ContentType=\"image/png\" /></Types>");
                    addZipEntry(zip, "_rels/.rels", "  <?xml version=\"1.0\" encoding=\"UTF-8\" ?> \r\n- <Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n  <Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" /> \r\n  </Relationships>");

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<?xml version=\"1.0\"?>\r\n<model unit=\"millimeter\" xmlns=\"http://schemas.microsoft.com/3dmanufacturing/core/2015/02\"  xml:lang=\"en-US\">\r\n\t<resources>\r\n\t\t<object id=\"1\" name=\"SolidModelBrowser_export\" type=\"model\">\r\n\t\t\t<mesh>\r\n\t\t\t\t<vertices>");
                    foreach (var p in mesh.Positions) sb.AppendLine($"\t\t\t\t\t<vertex x=\"{p.X}\" y=\"{p.Y}\" z=\"{p.Z}\" />");
                    sb.AppendLine("\t\t\t\t</vertices>\r\n\t\t\t\t<triangles>");
                    int q = mesh.TriangleIndices.Count / 3;
                    for (int c = 0; c < q; c++) sb.AppendLine($"\t\t\t\t\t<triangle v1=\"{mesh.TriangleIndices[c * 3]}\" v2=\"{mesh.TriangleIndices[c * 3 + 1]}\" v3=\"{mesh.TriangleIndices[c * 3 + 2]}\" />");
                    sb.AppendLine("\t\t\t\t</triangles>\r\n\t\t\t</mesh>\r\n\t\t</object>\r\n\t</resources>\r\n\t<build>\r\n\t\t<item objectid=\"1\" />\r\n\t</build>\r\n</model>\r\n");

                    addZipEntry(zip, "3D/3dmodel.model", sb.ToString());
                }
            }
        }
    }
}
