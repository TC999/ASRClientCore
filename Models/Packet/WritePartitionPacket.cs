using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Packet
{
    public struct WritePartitionPacket
    {
        public ulong Address { get; set; }
        public ulong TotalWriteSize { get; set; }
        public string PartitionName { get; set; }
        public WritePartitionPacket(string partName, ulong size) 
        {
            TotalWriteSize = size;
            PartitionName = partName;
        }
        public byte[] ToBytes()
        {
            byte[] result = new byte[32];
            ToBytes(result);
            return result;
        }
        public void ToBytes(Span<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException();
            BitConverter.TryWriteBytes(bytes, Address);
            BitConverter.TryWriteBytes(bytes.Slice(8), TotalWriteSize);
            Encoding.ASCII.GetBytes(PartitionName, bytes.Slice(16));
        }
    }
}
