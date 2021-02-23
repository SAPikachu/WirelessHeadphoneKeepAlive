using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace WirelessHeadphoneKeepAlive
{
    class NotificationClient : IMMNotificationClient
    {
        public event EventHandler<string> Added;

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
        }

        public void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId)
        {
            Added?.Invoke(this, pwstrDeviceId);
        }

        public void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId)
        {
        }

        public void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, DeviceState dwNewState)
        {
            Added?.Invoke(this, pwstrDeviceId);
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
        }
    }
}
