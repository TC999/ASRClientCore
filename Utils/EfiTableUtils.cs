using ASRClientCore.Models;
using System.Text;
using System.Xml;

namespace SPRDClientCore.Utils
{
    public class EfiTableUtils
    {
        public static List<Partition> GetPartitions(Stream ms)
        {
            using (BinaryReader reader = new BinaryReader(ms))
            {
                List<Partition> partList = new List<Partition>();

                // 搜索EFI分区头
                const int sectorSize = 512;
                int sectorIndex = 0;
                bool found = false;

                while (sectorIndex < 32)
                {
                    ms.Position = sectorIndex * sectorSize;
                    byte[] signatureBytes = reader.ReadBytes(8);
                    if (Encoding.ASCII.GetString(signatureBytes) == "EFI PART")
                    {
                        found = true;
                        break;
                    }
                    sectorIndex++;
                }

                if (!found)
                {
                    return partList;
                }

                ms.Position = sectorIndex * sectorSize;
                EfiHeader header = ReadEfiHeader(reader);

                ms.Position = header.PartitionEntryLba * sectorSize;
                List<EfiEntry> entries = ReadPartitionEntries(reader, header);
                foreach (var entry in entries)
                {
                    if (entry.StartingLba == 0 && entry.EndingLba == 0)
                        continue;

                    string name = Encoding.Unicode.GetString(entry.PartitionName);
                    name = name.Substring(0, name.IndexOf('\0'));

                    long sizeSectors = entry.EndingLba - entry.StartingLba + 1;
                    double sizeMB = sizeSectors * sectorSize;
                    partList.Add(new Partition { Name = name, Size = (ulong)sizeMB, IndicesToMB = 20 });
                }
                return partList;
            }
        }
        static EfiHeader ReadEfiHeader(BinaryReader reader)
        {
            return new EfiHeader
            {
                Signature = reader.ReadBytes(8),
                Revision = reader.ReadInt32(),
                HeaderSize = reader.ReadInt32(),
                HeaderCrc32 = reader.ReadInt32(),
                Reserved = reader.ReadInt32(),
                CurrentLba = reader.ReadInt64(),
                BackupLba = reader.ReadInt64(),
                FirstUsableLba = reader.ReadInt64(),
                LastUsableLba = reader.ReadInt64(),
                DiskGuid = reader.ReadBytes(16),
                PartitionEntryLba = reader.ReadInt64(),
                NumberOfPartitionEntries = reader.ReadInt32(),
                SizeOfPartitionEntry = reader.ReadInt32(),
                PartitionEntryArrayCrc32 = reader.ReadInt32()
            };
        }

        static List<EfiEntry> ReadPartitionEntries(BinaryReader reader, EfiHeader header)
        {
            List<EfiEntry> entries = new List<EfiEntry>();
            int entrySize = Math.Max(header.SizeOfPartitionEntry, 128);

            for (int i = 0; i < header.NumberOfPartitionEntries; i++)
            {
                long pos = reader.BaseStream.Position;
                EfiEntry entry = new EfiEntry
                {
                    PartitionTypeGuid = reader.ReadBytes(16),
                    UniquePartitionGuid = reader.ReadBytes(16),
                    StartingLba = reader.ReadInt64(),
                    EndingLba = reader.ReadInt64(),
                    Attributes = reader.ReadInt64(),
                    PartitionName = reader.ReadBytes(72)
                };

                // 跳过可能存在的填充字节
                if (entrySize > 128)
                    reader.ReadBytes(entrySize - 128);

                entries.Add(entry);
            }
            return entries;
        }

        static void GeneratePartitionXml(List<EfiEntry> entries, int sectorSize = 512)
        {
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create("partition.xml", settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Partitions");

                foreach (var entry in entries)
                {
                    if (entry.StartingLba == 0 && entry.EndingLba == 0)
                        continue;

                    string name = Encoding.Unicode.GetString(entry.PartitionName)
                        .TrimEnd('\0')
                        .Replace("\0", "");

                    long sizeSectors = entry.EndingLba - entry.StartingLba + 1;
                    double sizeMB = sizeSectors * sectorSize / (1024.0 * 1024.0);

                    writer.WriteStartElement("Partition");
                    writer.WriteAttributeString("id", name);
                    writer.WriteAttributeString("size",
                        name == "userdata" ? "0xFFFFFFFF" : ((int)sizeMB).ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
        static void GetPartitionList(List<EfiEntry> entries, int sectorSize)
        {
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create("partition.xml", settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Partitions");

                foreach (var entry in entries)
                {
                    if (entry.StartingLba == 0 && entry.EndingLba == 0)
                        continue;

                    string name = Encoding.Unicode.GetString(entry.PartitionName)
                        .TrimEnd('\0')
                        .Replace("\0", "");

                    long sizeSectors = entry.EndingLba - entry.StartingLba + 1;
                    double sizeMB = sizeSectors * sectorSize / (1024.0 * 1024.0);

                    writer.WriteStartElement("Partition");
                    writer.WriteAttributeString("id", name);
                    writer.WriteAttributeString("size",
                        name == "userdata" ? "0xFFFFFFFF" : ((int)sizeMB).ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        struct EfiHeader
        {
            public byte[]? Signature;
            public int Revision;
            public int HeaderSize;
            public int HeaderCrc32;
            public int Reserved;
            public long CurrentLba;
            public long BackupLba;
            public long FirstUsableLba;
            public long LastUsableLba;
            public byte[]? DiskGuid;
            public long PartitionEntryLba;
            public int NumberOfPartitionEntries;
            public int SizeOfPartitionEntry;
            public int PartitionEntryArrayCrc32;
        }
        struct EfiEntry
        {
            public byte[]? PartitionTypeGuid;
            public byte[]? UniquePartitionGuid;
            public long StartingLba;
            public long EndingLba;
            public long Attributes;
            public byte[] PartitionName;
        }
    }
}
