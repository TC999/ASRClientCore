using ASRClientCore.DeviceManager;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Exceptions;
using ASRClientCore.Models.Interfaces;
using ASRClientCore.ProtocolHandlers;
using ASRClientCore.Utils;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using SPRDClientCore.Utils;
using ASRClientCore.Models;
using System.Net.WebSockets;
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
        public static void Log(string log, ConsoleColor color)
        {
            var origColor = Console.ForegroundColor;
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.Write("[Log] ");
                Console.WriteLine(log);
                Console.ForegroundColor = origColor;
            }
        }
        static void Main(string[] args)
        {
            ConnectionConfig cfg = ConnectionConfig.Parse(ref args);
            Console.WriteLine($"Waiting for device connecting ({cfg.WaitTime / 1000}s). Connect your device directly just after powering down");
            Console.WriteLine("ASRClientCore by YC, QQ:1145145343");
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
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    executor.CancelAction();
                };
                try { fm.ErasePartition("fuck_you_asr"); } catch (BadResponseException) { }
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
                Console.ForegroundColor = ConsoleColor.Yellow;
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
获取分区表：pl/partition_list <保存路径>（获取后设备永远卡死，必须手动重新重启）
重新分区：rp/repartition [分区表路径]
写入分区：w/write_part [分区名] [文件路径] (须先读取或擦除相应分区)
回读分区：r/read_part [分区名] <保存路径>
擦除分区：e/erase_part [分区名]
写入内存：s/w_mem [文件路径] [地址] [模式] <分区名称>
备份全机：backup [分区表文件路径] <保存路径> (保存路径为文件夹路径)
恢复全机：restore [备份文件夹路径]
读取内存：p/pull_mem/read_mem [读取大小] [内存地址] <保存路径> 
查找可读取的内存地址：p_loop
获取设备信息：info
关机: off/poweroff
开机(至XX模式): rst/reset <模式>

参数设置指令: 
设置块大小：blk_size/bs [大小]
设置最大超时限制: timeout [毫秒时间]

开机模式：
0:Normal
1:DisconnectUSB,
2:Normal1,
3:BootLoader,
4:Calibration,
5:Ata,
6:CurrentTest,
7:UDL,
0x14:PowerOff
0x15:ColdRebootToNormal

写入内存模式：
0:Write(Send)Only,
1:WriteAndExecute,
2:WritePartition";
            private static readonly HashSet<string> CommandKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                "repartition","rp",
                "pl","partition_list",
                "r","read_part",
                "w","write_part",
                "e","erase_part",
                "s","w_mem",
                "backup","restore",
                "p","pull_mem","read_mem",
                "p_loop",
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
                            case "pl" or "partition_list":
                                string path = args.Count > 1 ? args[1] : "partition.xml";
                                List<Partition> partitions = manager.GetPartitionList();
                                using (FileStream fs = File.Create(path))
                                    PartitionToXml.SavePartitionsToXml(partitions, fs);
                                foreach (var part in partitions)
                                    Console.WriteLine(part.ToString());
                                Log("please reboot your device by pressing power button for 10 more seconds");
                                status.HasExited = true;
                                break;
                            case "repartition" or "rp":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区表路径");
                                    break;
                                }
                                string repartitionPath = args[1];
                                if (!File.Exists(repartitionPath))
                                {
                                    Log($"{repartitionPath} not exist");
                                    break;
                                }
                                manager.Repartition(PartitionToXml.LoadPartitionsXml(File.ReadAllText(repartitionPath)));
                                break;
                            case "r" or "read_part":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区名");
                                    break;
                                }
                                string partName = args[1];
                                string? outputPath = args.Count > 2 ? args[2] : null;
                                try
                                {
                                    using (FileStream fs = new FileStream(outputPath ?? $"{partName}.img", FileMode.Create, FileAccess.Write))
                                    {
                                        manager.ReadPartition(partName, fs);
                                    }
                                }
                                catch (BadResponseException ex)
                                {
                                    if (ex.Response != ResponseStatus.PartitionNotFound) throw;
                                    Log($"{partName} not exist");
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
                                try
                                {
                                    using (FileStream fs = File.OpenRead(writeFilePath))
                                        manager.WritePartition(writePartName, fs);
                                }
                                catch (BadResponseException) { }
                                break;
                            case "e" or "erase_part":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区名");
                                    break;
                                }
                                try
                                {
                                    manager.ErasePartition(args[1]);
                                }
                                catch (BadResponseException)
                                {
                                    Log($"failed to erase {args[1]} partition");
                                }
                                break;
                            case "s" or "w_mem":
                                if (args.Count < 4)
                                {
                                    Log("s/w_mem [文件路径] [地址] [模式] <分区名称>");
                                    break;
                                }
                                if (!File.Exists(args[1]))
                                {
                                    Log($"file not exist");
                                    break;
                                }
                                try
                                {
                                    using (FileStream fs = File.OpenRead(args[1]))
                                        manager.WriteMemory(StrToSize.StringToSize(args[2]),
                                            (WriteMemoryMode)StrToSize.StringToSize(args[3]),
                                            fs, args.Count >= 5 ? args[4] : string.Empty);
                                }catch (BadResponseException) { }
                                break;
                            case "backup":
                                if (args.Count < 2)
                                {
                                    Log("请指定分区表路径");
                                    break;
                                }
                                string xmlPath = args[1];
                                if (!File.Exists(xmlPath))
                                {
                                    Log("file not exist");
                                    break;
                                }
                                string backupDir = args.Count > 2 ? args[2] : "ASRClientcore_backup";
                                if (!Directory.Exists(backupDir))
                                    Directory.CreateDirectory(backupDir);
                                List<Partition> parts = PartitionToXml.LoadPartitionsXml(File.ReadAllText(xmlPath));
                                Log("Press Ctrl+C to cancel backup action", ConsoleColor.Cyan);
                                var token = cts.Token;
                                foreach (var part in parts)
                                {
                                    if (token.IsCancellationRequested) break;
                                    if (part.Name is "userdata" or "cache")
                                    {
                                        Log($"skip {part.Name} partition");
                                        continue;
                                    }
                                    try
                                    {
                                        using (FileStream fs = new FileStream(Path.Combine(backupDir, $"{part.Name}.img"), FileMode.Create, FileAccess.Write))
                                            manager.ReadPartition(part.Name, fs);
                                    }
                                    catch (BadResponseException ex)
                                    {
                                        if (ex.Response != ResponseStatus.PartitionNotFound) throw;
                                        Log($"{part.Name} not exist");
                                    }
                                }
                                break;
                            case "restore":
                                if (args.Count < 2)
                                {
                                    Log("请指定备份文件夹路径");
                                    break;
                                }
                                string restoreDir = args[1];
                                if (!Directory.Exists(restoreDir))
                                {
                                    Log("文件夹不存在");
                                    break;
                                }
                                var imgFiles = Directory.GetFiles(restoreDir, "*.img");
                                Log("Press Ctrl+C to cancel restore action", ConsoleColor.Cyan);
                                var ctoken = cts.Token;
                                foreach (var img in imgFiles)
                                {
                                    if (ctoken.IsCancellationRequested) break;
                                    string partname = Path.GetFileNameWithoutExtension(img);
                                    if (partname.Contains("modem"))
                                    {
                                        Log($"skip {partname} part");
                                        continue;
                                    }
                                    try
                                    {
                                        using (FileStream fs = File.OpenRead(img))
                                            manager.WritePartition(partname, fs);
                                    }
                                    catch (BadResponseException)
                                    {
                                        Log($"failed to write {partname} partition");
                                    }
                                }
                                break;
                            case "p" or "pull_mem" or "read_mem":
                                if (args.Count < 3)
                                {
                                    Log("p [length] [addr] <path_to_save>");
                                    break;
                                }
                                ulong address = StrToSize.StringToSize(args[1]);
                                ulong length = StrToSize.StringToSize(args[2]);
                                string? savePath = args.Count > 3 ? args[3] : $"memdump_{address:x}.bin";
                                if (length == 0)
                                {
                                    Log("读取长度不能为0");
                                    break;
                                }
                                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                                {
                                    manager.ReadMemory(address, length, fs);
                                }
                                break;
                            case "p_loop":
                                void add(ref uint i)
                                {
                                    if (i < 0x20000) i += 0x2000;
                                    else if (i < 0x200000) i += 0x20000;
                                    else if (i < 0x2000000) i += 0x200000;
                                    else i += 0x2000000;
                                }
                                ulong read = 0;
                                for (uint i = 0x200; read == 0 && i < 0xfe000000; add(ref i))
                                {
                                    Log($"tried 0x{i:x}");
                                    using (MemoryStream stream = new())
                                        manager.ReadMemory(i, 8, stream);
                                }
                                break;
                            case "info":
                                Console.WriteLine(manager.DeviceInformation);
                                break;
                            case "off" or "poweroff":
                                status.HasExited = true;
                                manager.PowerDownDevice();
                                return true;
                            case "rst" or "reset":
                                status.HasExited = true;
                                BootMode mode = BootMode.Normal;
                                if (args.Count >= 2)
                                    mode = (BootMode)StrToSize.StringToSize(args[1]);
                                manager.RebootDeviceToCustomMode(mode);
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