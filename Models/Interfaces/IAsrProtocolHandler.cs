using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Interfaces
{
    public interface IAsrProtocolHandler : IDisposable
    {
        uint Timeout { get; set; }
        uint LastErrorCode { get; }
        uint Read(byte[] buffer, int offset, int count);
        uint Write(byte[] buffer, int offset, int count);
    }
}
