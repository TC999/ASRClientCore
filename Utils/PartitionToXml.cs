using ASRClientCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ASRClientCore.Utils
{
    public static class PartitionToXml
    {
        public static void SavePartitionsToXml(List<Partition> partitions, Stream stream)
        {
            if (partitions[0].Name == "splloader") partitions.RemoveAt(0);
            var doc = new XDocument(
                new XElement("Partitions",
                    partitions.Select(p =>
                        new XElement("Partition",
                            new XAttribute("id", p.Name),
                            new XAttribute("size", p.Name == "userdata" ? "0xFFFFFFFF" : Math.Ceiling(p.Size / (double)(1 << p.IndicesToMB)))
                        )
                    )
                )
            );

            doc.Save(stream);
        }
        public static List<Partition> LoadPartitionsXml(string xmlContent)
        {
            Func<string, ulong> ParseSize = (sizeString) =>
            {
                if (sizeString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToUInt64(sizeString.Substring(2), 16);
                }
                return Convert.ToUInt64(sizeString);
            };
            var partitions = new List<Partition>();
            var doc = XDocument.Parse(xmlContent);

            var nodes = doc.XPathSelectElements("//Partitions/Partition");

            foreach (var node in nodes)
            {
                var nameAttr = node.Attribute("id");
                var sizeAttr = node.Attribute("size");

                if (nameAttr == null || sizeAttr == null) continue;

                try
                {
                    var partition = new Partition
                    {
                        Name = nameAttr.Value,
                        Size = ParseSize(sizeAttr.Value),
                        IndicesToMB = 0
                    };
                    partitions.Add(partition);
                }
                catch (FormatException)
                {
                }
            }

            return partitions;
        }

    }
}
