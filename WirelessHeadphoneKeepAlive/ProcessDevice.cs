using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WirelessHeadphoneKeepAlive
{
    partial class Program
    {
        static readonly ConcurrentDictionary<string, bool> _running = new ConcurrentDictionary<string, bool>();

        static void ProcessDevice(MMDevice device, Options opts, byte[] audioData)
        {
            using (device)
            {
                if (device.DataFlow != DataFlow.Render)
                {
                    return;
                }
                var id = device.ID;
                string displayName;
                try
                {
                    displayName = device.FriendlyName;
                }
                catch (Exception)
                {
                    displayName = id;
                }
                if (!displayName.Contains(opts.DeviceName))
                {
                    Console.WriteLine("Skipping {0}", displayName);
                    return;
                }
                AudioMeterInformation meter;
                try
                {
                    meter = device.AudioMeterInformation;
                }
                catch (Exception)
                {
                    Console.WriteLine("{0} doesn't support meter", displayName);
                    return;
                }
                GC.Collect();
                if (!_running.TryAdd(id, true))
                {
                    Console.WriteLine("Already running: {0}", displayName);
                    return;
                }
                try
                {
                    Console.WriteLine("Starting: {0}", displayName);
                    var watch = Stopwatch.StartNew();
                    while (true)
                    {
                        Thread.Sleep(1000);
                        if (meter.MasterPeakValue > opts.SilentThreshold)
                        {
                            watch.Restart();
                            continue;
                        }
                        if (watch.Elapsed.TotalSeconds <= opts.Interval)
                        {
                            continue;
                        }
                        watch.Restart();
                        float masterVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                        if (masterVolume < 0.01)
                        {
                            continue;
                        }
                        try
                        {
                            using (var audioFile = new WaveFileReader(new MemoryStream(audioData)))
                            {
                                using (var player = new WasapiOut(device, AudioClientShareMode.Shared, false, 200))
                                {
                                    player.Init(audioFile);
                                    var volumes = new float[player.AudioStreamVolume.ChannelCount];
                                    for (var i = 0; i < volumes.Length; i++)
                                    {
                                        volumes[i] = (float)Math.Min(1, opts.TargetVolumeLevel / Math.Pow(masterVolume, opts.MasterVolumeNormalizationFactor));
                                    }
                                    player.AudioStreamVolume.SetAllVolumes(volumes);
                                    player.Play();
                                    while (player.PlaybackState == PlaybackState.Playing)
                                    {
                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                        }
                        catch (COMException ex) when ((uint)ex.ErrorCode == 0x8889000A) // AUDCLNT_E_DEVICE_IN_USE
                        {
                            Console.WriteLine("{0} is currently being used in exclusive mode", displayName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to play audio for {0}:", displayName);
                            Console.WriteLine(ex);
                        }
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(displayName);
                    Console.WriteLine(ex);
                }
                finally
                {
                    _running.TryRemove(id, out _);
                    Console.WriteLine("Stopped: {0}", displayName);
                }
            }
        }

    }
}
