using static ASRClientCore.Models.Enums.AsrResponseStatus;

namespace ASRClientCore.Models.Interfaces
{
    public interface IRequestManager
    {
        public event Action<string>? Log;
        public IAsrProtocolHandler Handler { get; }
        public uint Timeout { get; set; }
        public (ResponseStatus Response, string Info) SendGetInformationRequest();
        public ResponseStatus SendReadPartitionRequest(string partName,out ulong size);
    }
}
