using System.Runtime.InteropServices;

namespace OkidokeiWidget.Core.Monitors;

public static class MonitorIdentifier
{
    private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    /// <summary>
    /// アダプターのデバイス名 (例: \\.\DISPLAY1) をキーに、EDID 由来の安定したモニタ ID を返す。
    /// Screen.DeviceName は接続順序によって変わり得るため、永続化キーには使わない (research.md #2)。
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetStableIdsByAdapterDeviceName()
    {
        var result = new Dictionary<string, string>();

        var adapter = default(DISPLAY_DEVICE);
        adapter.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
        uint adapterIndex = 0;

        while (EnumDisplayDevices(null, adapterIndex, ref adapter, 0))
        {
            var monitor = default(DISPLAY_DEVICE);
            monitor.cb = Marshal.SizeOf<DISPLAY_DEVICE>();

            if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME))
            {
                result[adapter.DeviceName] = monitor.DeviceID;
            }

            adapterIndex++;
            adapter = default;
            adapter.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
        }

        return result;
    }
}
