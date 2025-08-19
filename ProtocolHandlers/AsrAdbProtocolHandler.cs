using ASRClientCore.Models.Interfaces;
using AsrClientWrapper;

namespace ASRClientCore.ProtocolHandlers
{
    public class AsrAdbProtocolHandler : IAsrProtocolHandler
    {
        private readonly AsrNativeAdbDevice device;
        public AsrAdbProtocolHandler(AsrNativeAdbDevice device)
        {
            this.device = device;
        }
        public uint Timeout
        {
            get => device.Timeout;
            set => device.Timeout = value;
        }
        public uint LastErrorCode => device.LastErrorCode;
        public uint Read(byte[] buffer, int offset, int count)
        {
            return device.Read(buffer, offset, count);
        }
        public uint Write(byte[] buffer, int offset, int count)
        {
            return device.Write(buffer, offset, count);
        }
        public static AsrAdbProtocolHandler FindAndOpen(uint timeout)
        {
            var adbDevice = AsrNativeAdbDevice.FindAndOpen(timeout);
            return new AsrAdbProtocolHandler(adbDevice);
        }
    }
}
