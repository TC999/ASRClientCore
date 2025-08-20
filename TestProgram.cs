using ASRClientCore.DeviceManager;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Interfaces;
using ASRClientCore.ProtocolHandlers;
using ASRClientCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Text;
namespace ASRClientCore
{
    class TestProgram
    {
        private static readonly object _lock = new object();
        public static void Log(string log)
        {
            Console.Write("[Log] ");
            Console.WriteLine(log);
        }
        static void Main(string[] args)
        {
            ConnectionConfig cfg = ConnectionConfig.Parse(ref args);
            Console.WriteLine($"waiting for device connecting ({cfg.WaitTime / 1000}s) . connect your device directly just after powering down");
            IAsrProtocolHandler device;
            try
            {
                device = AsrAdbProtocolHandler.FindAndOpen(cfg.WaitTime);
            }
            catch
            {
                Console.WriteLine("fail to connect to device");
                return;
            }
            device.Timeout = cfg.Timeout;
            RequestManager rm = new RequestManager(device);
            FlashManager fm = new FlashManager(rm);
            try
            {

                ConsoleProgressBar progressBar = new ConsoleProgressBar();
                fm.UpdatePercentage += progressBar.UpdateProgress;
                fm.Log += msg => Console.WriteLine(msg);
                DeviceStatus status = new DeviceStatus();
                CommandExecutor executor = new CommandExecutor(fm, status);
                executor.Execute(args.ToList());
                while (!status.HasExited)
                {
                    Console.Write("[ASR] >");
                    string? input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        status.HasExited = true;
                        continue;
                    }
                    executor.Execute(input);
                }
            }
            catch (Exception ex)
            {
                Log($"error :{ex.Message}");
            }
            finally
            {
                fm.Dispose();
            }
        }
        public class DeviceStatus
        {
            public bool HasExited { get; set; }
        }
        public class ConnectionConfig
        {
            public uint WaitTime { get; private set; } = 30000;
            public uint Timeout { get; private set; } = 5000;
            public static ConnectionConfig Parse(ref string[] args)
            {
                ConnectionConfig config = new();
                List<string> cmds = new();

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLowerInvariant();
                    bool matched = true;

                    switch (arg)
                    {
                        case "--wait":
                            if (i + 1 < args.Length && uint.TryParse(args[i + 1], out uint waitTime))
                            {
                                config.WaitTime = waitTime * 1000;
                                i++;
                            }
                            break;
                        case "--timeout":
                            if (i + 1 < args.Length && uint.TryParse(args[i + 1], out uint timeout))
                            {
                                config.Timeout = timeout;
                                i++;
                            }
                            break;
                        default:
                            matched = false;
                            break;
                    }

                    if (!matched)
                    {
                        cmds.Add(args[i]);
                    }
                }

                args = cmds.ToArray();
                return config;
            }

        }

        class ConsoleProgressBar
        {
            public int BarWidth { get; set; } = 25;
            public void UpdateProgress(int percentage)
            {
                if (percentage > 100) percentage = 100;
                else if (percentage < 0) percentage = 0;
                int progressWidth = (int)(percentage / 100.0 * BarWidth);
                var tmp = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                lock (_lock)
                {
                    Console.CursorLeft = 0;
                    Console.Write('[');
                    Console.Write(new string('#', progressWidth));
                    Console.Write(new string(' ', BarWidth - progressWidth));
                    Console.Write(']');
                    Console.Write($"{percentage}%");
                    Console.ForegroundColor = tmp;
                    if (percentage == 100) Console.WriteLine();
                }
            }
            public void UpdateSpeed(string speed)
            {
                lock (_lock)
                {
                    var tmp = Console.CursorLeft;
                    Console.CursorLeft = BarWidth + 10;
                    Console.Write(speed);
                    Console.CursorLeft = tmp;
                }
            }
        }
        public class CommandExecutor(FlashManager manager, DeviceStatus status)
        {
            private readonly FlashManager manager = manager;
            private readonly DeviceStatus status = status;
            private CancellationTokenSource cts = new();

            private string commandHelp =
                @"指令帮助
[]内的参数必填, <>内的参数选填
参数指令：
--timeout [毫秒时间]：设置最大超时限制
--wait [秒数]：设置等待设备连接的时间(默认30秒)

运行时指令：
写入分区（强制写入在文件路径后加参数force）：w/write_part [分区名] [文件路径] (尚未支持，敬请期待)
回读分区：r/read_part [分区名] <保存路径>
擦除分区：e/erase_part [分区名]
读取内存：p/pull_mem <保存路径> [读取大小] [偏移地址] (尚未支持，敬请期待)
获取设备信息：info
关机: off/poweroff
开机: rst/reset

参数设置指令: 
设置块大小：blk_size/bs [大小]
设置最大超时限制: timeout [毫秒时间]";
            private static readonly HashSet<string> CommandKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                "r","read_part",
                "w","write_part",
                "e","erase_part",
                "p","pull_mem",
                "info",
                "off","poweroff",
                "rst","reset",
                "blk_size","bs",
                "timeout",
            };


            private static List<string> ParseCommand(string command)
            {
                var result = new List<string>();
                var sb = new StringBuilder();
                bool inPath = false;

                foreach (char c in command)
                {
                    if (c == '"') inPath = !inPath;
                    else if (c == ' ' && !inPath)
                    {
                        if (sb.Length > 0)
                        {
                            result.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else sb.Append(c);
                }

                if (sb.Length > 0) result.Add(sb.ToString());

                return result;
            }
            public void Execute(string command) => Execute(ParseCommand(command));
            public void Execute(List<string> tokens)
            {
                var subCommands = new List<List<string>>();
                List<string>? current = null;
                foreach (var tok in tokens)
                {
                    if (CommandKeys.Contains(tok))
                    {
                        current = new List<string> { tok };
                        subCommands.Add(current);
                    }
                    else if (current != null)
                    {
                        current.Add(tok);
                    }
                    else
                    {
                        Log(commandHelp);
                        return;
                    }
                }

                foreach (var args in subCommands)
                {
                    bool shouldExit = ExecuteSingle(args);
                    if (shouldExit)
                        break;
                }
            }
            public bool ExecuteSingle(List<string> args)
            {
                try
                {
                    if (args.Count > 0)
                        switch (args[0])
                        {
                            default: Log(commandHelp); break;
                            case "r" or "read_part":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区名");
                                    break;
                                }
                                string partName = args[1];
                                string? outputPath = args.Count > 2 ? args[2] : null;
                                using (FileStream fs = new FileStream(outputPath ?? $"{partName}.img", FileMode.Create, FileAccess.Write))
                                {
                                    manager.ReadPartition(partName, fs);
                                }
                                break;
                            case "w" or "write_part":
                                if (args.Count < 3)
                                {
                                    Log("请指定分区名和文件路径");
                                    break;
                                }
                                string writePartName = args[1];
                                string writeFilePath = args[2];
                                if (!File.Exists(writeFilePath))
                                {
                                    Log($"文件 {writeFilePath} 不存在");
                                    break;
                                }
                                Console.WriteLine("尚未完成");
                                break;
                            case "e" or "erase_part":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区名");
                                    break;
                                }
                                manager.ErasePartition(args[1]);
                                break;
                            case "p" or "pull_mem":
                                break;
                            case "info":
                                Console.WriteLine(manager.DeviceInformation);
                                break;
                            case "off" or "poweroff":
                                status.HasExited = true;
                                manager.PowerdownDevice();
                                return true;
                            case "rst" or "reset":
                                status.HasExited = true;
                                manager.RebootDeviceToCustomMode(BootMode.Normal);
                                return true;
                            case "blk_size" or "bs":
                                if (args.Count < 2)
                                {
                                    Log("请指定块大小");
                                    break;
                                }
                                manager.PerBlockSize = (uint)StrToSize.StringToSize(args[1]);
                                break;
                            case "timeout":
                                if (args.Count < 2)
                                {
                                    Log("请指定超时时间");
                                    break;
                                }
                                manager.Timeout = (uint)StrToSize.StringToSize(args[1]);
                                break;
                        }
                }
                catch (OperationCanceledException) { }
                return false;
            }

            public void CancelAction()
            {
                cts.Cancel();
                cts.Dispose();
                cts = new();
            }
        }


    }
}