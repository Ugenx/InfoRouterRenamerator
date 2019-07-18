using System;
using System.Linq;
using System.Xml.Linq;

namespace InfoRouterRenamerator
{
    public class OurDocument
    {
        private const string FilenameFormat = "{0}_{1}_{2}";
        private const string OriginalFolderPath = "\\NRG_DYNAMICS";

        public string LoadNumber { get; set; }

        public string DriverName { get; set; }

        public string OriginalFileName { get; set; }

        public string RemoteDocumentPath => $"{OriginalFolderPath}\\{OriginalFileName}";

        public string LocalFileName => GetDownloadFileName();

        public string GetDownloadFileName()
        {
            // use "noload" if na else use whatever is in that field
            var prefix = LoadNumber.Equals("NA", StringComparison.OrdinalIgnoreCase)
                ? "NOLOAD"
                : LoadNumber;

            var filename = OriginalFileName.Contains(".tif")
                ? OriginalFileName.Replace(".tif", ".pdf")
                : OriginalFileName;

            return string.Format(FilenameFormat,
                prefix,
                DriverName,
                filename);
        }

        public static OurDocument LoadFromElement(XElement element)
        {
            var doc = new OurDocument();
            var ovs = element.Descendants("propertyset").SingleOrDefault(set => set.Attribute("Name")?.Value == "OVS");
            if (ovs == null) return null;
            var targetRow = ovs.Elements("propertyrow").FirstOrDefault(row => row.Attribute("LOADNUMBER") != null);
            if (targetRow == null) return null;

            doc.DriverName = targetRow.Attribute("DRIVERNAME")?.Value;
            doc.LoadNumber = targetRow.Attribute("LOADNUMBER")?.Value;
            doc.OriginalFileName = element.Attribute("Name")?.Value;
            return doc;
        }
    }
}