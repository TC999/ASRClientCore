using ASRClientCore.Models.Enums;

namespace ASRClientCore.Models.Interfaces
{
    public interface IRequestManager
    {
        public event Action<string>? Log;
        public IAsrProtocolHandler Handler { get; }
        public uint Timeout { get; set; }
        public ResponseStatus SendGetInformationRequest(out string? deviceInfo);
        public ResponseStatus SendGetDeviceInfoRequest(out string? deviceInfo);
        public ResponseStatus SendReadPartitionRequest(string partName, out ulong size);
        public ResponseStatus SendWritePartitionRequest(string partName, ulong size);
        public ResponseStatus SendErasePartitionRequest(string partName);
        public ResponseStatus SendPullMemoryRequest(ulong address, ulong len, out ulong size);
        public ResponseStatus SendRebootDeviceRequest(BootMode bootMode);
        public ResponseStatus SendPowerDownDeviceRequest();

        /* public ResponseStatus SendWritePartitionRequest(string partName);
         public ResponseStatus SendErasePartitionRequest(string partName);
         public ResponseStatus SendRebootDeviceToCustomModeRequest(BootMode bootMode);*/
    }
}
