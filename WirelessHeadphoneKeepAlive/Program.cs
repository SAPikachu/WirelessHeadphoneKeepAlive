using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using CommandLine;
using System.Runtime.InteropServices;
using CommandLine.Text;

namespace WirelessHeadphoneKeepAlive
{
    partial class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);

        delegate bool HandlerRoutine(UInt32 dwCtrlType);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);
        private const int STD_INPUT_HANDLE = -10;
        [DllImport("kernel32.dll", EntryPoint = "GetConsoleMode", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);
        [DllImport("kernel32.dll", EntryPoint = "SetConsoleMode", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);
        private const int ENABLE_PROCESSED_INPUT = 1;
        private const int ENABLE_LINE_INPUT = 2;


        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CHAR = 0x0102;
        const int VK_ENTER = 0x0D;


        static void Run(Options opts, bool haveExistingConsole)
        {
            if (!haveExistingConsole && opts.ShowConsole)
            {
                AllocConsole();
            }
            HandlerRoutine onCtrlC = (dwCtrlType) =>
            {
                Console.WriteLine("Received Ctrl-C, exiting");
                Environment.Exit(0);
                return true;
            };
            if (haveExistingConsole || opts.ShowConsole)
            {
                SetConsoleCtrlHandler(onCtrlC, true);
                var handle = GetStdHandle(STD_INPUT_HANDLE);
                GetConsoleMode(handle, out var mode);
                SetConsoleMode(handle, mode | ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT);
            }
            if (haveExistingConsole)
            {
                var thread = new Thread((_) =>
                {
                    IntPtr cw = GetConsoleWindow();
                    if (cw.ToInt64() != -1)
                    {
                        // Parent process is attached to input buffer, send a key event first so that we can get the next event (with better chance)
                        ThreadPool.QueueUserWorkItem(__ =>
                        {
                            Thread.Sleep(1);
                            SendMessage(cw, WM_CHAR, (IntPtr)VK_ENTER, IntPtr.Zero);
                        });
                    }
                    var key = Console.ReadKey(true);
                    Console.WriteLine("Key pressed, exiting...");
                    SendMessage(cw, WM_CHAR, (IntPtr)VK_ENTER, IntPtr.Zero);
                    Environment.Exit(0);
                });
                thread.IsBackground = true;
                thread.Start();
            }
            var mutex = new Mutex(true, Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly()).ToString(), out var isNew);
            if (!isNew)
            {
                Console.WriteLine("Already running");
                return;
            }
            byte[] audioData;
            if (!string.IsNullOrEmpty(opts.SoundFile))
            {
                audioData = File.ReadAllBytes(opts.SoundFile);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program).Namespace + ".beep.wav").CopyTo(ms);
                    audioData = ms.ToArray();
                }
            }
            try
            {
                new WaveFileReader(new MemoryStream(audioData));
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Invalid sound file: {0}", opts.SoundFile);
                Environment.Exit(1);
                return;
            }
            using (var enumerator = new MMDeviceEnumerator())
            {
                Action<string> addDevice = (id) =>
                {
                    try
                    {
                        var device = enumerator.GetDevice(id);
                        var thread = new Thread(() => ProcessDevice(device, opts, audioData));
                        thread.IsBackground = true;
                        thread.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to handle device {0}:", id);
                        Console.WriteLine(ex);
                    }
                };
                var notifier = new NotificationClient();
                notifier.Added += (sender, id) => addDevice(id);
                enumerator.RegisterEndpointNotificationCallback(notifier);

                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All);
                foreach (var device in devices)
                {
                    addDevice(device.ID);
                    device.Dispose();
                }
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                Thread.Sleep(-1);
                GC.KeepAlive(notifier);
            }
            GC.KeepAlive(onCtrlC);
            GC.KeepAlive(mutex);
        }
        static void Main(string[] args)
        {
            var haveConsole = AttachConsole(-1);
            var result = (new Parser(with =>
            {
                with.AutoHelp = true;
                with.AutoVersion = true;
                with.HelpWriter = null;
            })).ParseArguments<Options>(args).WithParsed((opts) => Run(opts, haveConsole));
            result.WithNotParsed(_ =>
            {
                if (!haveConsole)
                {
                    AllocConsole();
                }
                Console.Error.WriteLine(HelpText.AutoBuild(result));
                if (!haveConsole)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                Environment.Exit(1);
            });
        }
    }
}
