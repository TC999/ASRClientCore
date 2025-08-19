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
            FlashManager fm = new FlashManager(rm);
            rm.Log += (message) => Console.WriteLine($"[Request Log]: {message}");
            fm.Log += (message) => Console.WriteLine($"[Common Log]: {message}");
            fm.UpdatePercentage += (message) => Console.WriteLine($"[Percentage]: {message}");
            using var fileStream = new FileStream("boot.img", FileMode.Create, FileAccess.Write);
            fm.ReadPartition("boot",fileStream);
        }

    }
}