using CommandLine;

namespace WirelessHeadphoneKeepAlive
{
    public class Options
    {
        [Option('d', "device", Required = true, HelpText = "Name of the output device. Any device which name contains this argument will be handled")]
        public string DeviceName { get; set; }
        [Option('i', "interval", Default = 270, HelpText = "Number of silent seconds before playing keep-alive sound")]
        public int Interval { get; set; }
        [Option('f', "file", Default = "", HelpText = "Specify a custom WAV file to play instead of included beep sound")]
        public string SoundFile { get; set; }
        [Option("silentthreshold", Default = 0.0025f, HelpText = "Peak value lower than this will be treated as silent")]
        public float SilentThreshold { get; set; }
        [Option("targetvolume", Default = 0.005f, HelpText = "Target volume level of the keep-alive sound. Output volume is normalized to be roughly the same regardless of master volume")]
        public float TargetVolumeLevel { get; set; }
        [Option("normalizationfactor", Default = 1.75f, HelpText = "Factor of volume normalization")]
        public float MasterVolumeNormalizationFactor { get; set; }
        [Option("showconsole", Default = false, HelpText = "Show debug console")]
        public bool ShowConsole { get; set; }
    }
}
