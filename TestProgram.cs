using ASRClientCore.DeviceManager;
using ASRClientCore.Models.Interfaces;
using ASRClientCore.ProtocolHandlers;
using System;
using System.Text;
namespace ASRClientCore
{
    class TestProgram
    {
        static void Main(string[] args)
        {
            IAsrProtocolHandler device = AsrAdbProtocolHandler.FindAndOpen(uint.MaxValue);
            RequestManager rm = new RequestManager(device);
            rm.SendGetInformationRequest(out var _);
            rm.SendWritePartitionRequest("boot");
        }

    }
}