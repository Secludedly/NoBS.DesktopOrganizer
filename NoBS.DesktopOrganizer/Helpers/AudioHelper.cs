using System;
using System.Runtime.InteropServices;

namespace NoBS.DesktopOrganizer.Core.Helpers
{
    public static class AudioHelper
    {
        // -----------------------------
        // PUBLIC API
        // -----------------------------

        public static void SetSystemVolume(int percent)
        {
            percent = Math.Clamp(percent, 0, 100);
            float scalar = percent / 100f;

            var volume = GetEndpointVolume();
            volume.SetMasterVolumeLevelScalar(scalar, Guid.Empty);
        }

        public static int GetSystemVolume()
        {
            var volume = GetEndpointVolume();
            volume.GetMasterVolumeLevelScalar(out float level);
            return (int)Math.Round(level * 100);
        }

        // -----------------------------
        // CORE AUDIO PLUMBING
        // -----------------------------

        private static IAudioEndpointVolume GetEndpointVolume()
        {
            IMMDeviceEnumerator enumerator =
                (IMMDeviceEnumerator)new MMDeviceEnumerator();

            enumerator.GetDefaultAudioEndpoint(
                EDataFlow.eRender,
                ERole.eMultimedia,
                out IMMDevice device);

            Guid iid = typeof(IAudioEndpointVolume).GUID;

            device.Activate(
                ref iid,
                CLSCTX.CLSCTX_ALL,
                IntPtr.Zero,
                out object volumeObj);

            return (IAudioEndpointVolume)volumeObj;
        }

        // -----------------------------
        // COM INTERFACES
        // -----------------------------

        private enum EDataFlow
        {
            eRender,
            eCapture,
            eAll
        }

        private enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications
        }

        [Flags]
        private enum CLSCTX : uint
        {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_ALL = CLSCTX_INPROC_SERVER
                        | CLSCTX_INPROC_HANDLER
                        | CLSCTX_LOCAL_SERVER
                        | CLSCTX_REMOTE_SERVER
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();

            int GetDefaultAudioEndpoint(
                EDataFlow dataFlow,
                ERole role,
                out IMMDevice ppDevice);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(
                ref Guid iid,
                CLSCTX dwClsCtx,
                IntPtr pActivationParams,
                [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr pNotify);
            int UnregisterControlChangeNotify(IntPtr pNotify);
            int GetChannelCount(out uint channelCount);
            int SetMasterVolumeLevel(float level, Guid eventContext);
            int SetMasterVolumeLevelScalar(float level, Guid eventContext);
            int GetMasterVolumeLevel(out float level);
            int GetMasterVolumeLevelScalar(out float level);
            int SetChannelVolumeLevel(uint channel, float level, Guid eventContext);
            int SetChannelVolumeLevelScalar(uint channel, float level, Guid eventContext);
            int GetChannelVolumeLevel(uint channel, out float level);
            int GetChannelVolumeLevelScalar(uint channel, out float level);
            int SetMute(bool isMuted, Guid eventContext);
            int GetMute(out bool isMuted);
            int GetVolumeStepInfo(out uint step, out uint stepCount);
            int VolumeStepUp(Guid eventContext);
            int VolumeStepDown(Guid eventContext);
            int QueryHardwareSupport(out uint hardwareSupportMask);
            int GetVolumeRange(out float minDb, out float maxDb, out float incrementDb);
        }
    }
}
