# WirelessHeadphoneKeepAlive

Avoid wireless headphone turning off on idle

## Why

Sennheiser RS 175 (and quite a few other wireless headphones) has a "feature" which turns itself off when there is no sound for some time,
and I couldn't find a way to disable it.
This is annoying as I don't always have music playing, and from time to time I have to take my headphone off my head to turn it on again.

This tool removes this hassle by periodically playing some sound when no sound output is detected.
When there is actual sound output, the tool will pause itself to avoid disturbance.

## Usage

1. Find name of your playback device in sound setting. You may need to rename your device to make it unique.

2. Run the tool in command line to confirm that it is working: `WirelessHeadphoneKeepAlive.exe -d "DEVICE NAME"`.

3. When you are satisfied with the result, create a shortcut in your startup folder to run the tool automatically.

## Notes

* Tested on Windows 10 20H2, should be working on not-too-old Win10 installations

* Default settings are tested with optical output to RS 175 and 50% of system volume. You may need to tweak some parameters for your system.

* This tool may not work if system volume is too low, setting volume to 25% or higher is recommended.

* No UI/console is shown by default, please kill it from task manager if needed.

## Command line reference

```
  -d, --device             Required. Name of the output device. Any device which
                           name contains this argument will be handled

  -i, --interval           (Default: 270) Number of silent seconds before
                           playing keep-alive sound

  -f, --file               (Default: ) Specify a custom WAV file to play instead
                           of included beep sound

  --silentthreshold        (Default: 0.0025) Peak value lower than this will be
                           treated as silent

  --targetvolume           (Default: 0.005) Target volume level of the
                           keep-alive sound. Output volume is normalized to be
                           roughly the same regardless of master volume

  --normalizationfactor    (Default: 1.75) Factor of volume normalization

  --showconsole            (Default: false) Show debug console

  --help                   Display this help screen.

  --version                Display version information.
```
