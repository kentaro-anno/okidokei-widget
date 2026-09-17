using System.Linq;
using System.Runtime.InteropServices;

namespace OkidokeiWidget.Core.Monitors;

public static class MonitorEnumerationService
{
    private const uint MONITORINFOF_PRIMARY = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    public static IReadOnlyList<ConnectedMonitor> GetConnectedMonitors()
    {
        var stableIds = MonitorIdentifier.GetStableIdsByAdapterDeviceName();
        var monitors = new List<ConnectedMonitor>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var info = default(MONITORINFOEX);
            info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (GetMonitorInfo(hMonitor, ref info))
            {
                var identifier = stableIds.TryGetValue(info.szDevice, out var stableId) ? stableId : info.szDevice;

                monitors.Add(new ConnectedMonitor(
                    identifier,
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0,
                    ParseDisplayNumber(info.szDevice),
                    info.rcWork.Left,
                    info.rcWork.Top,
                    info.rcWork.Right - info.rcWork.Left,
                    info.rcWork.Bottom - info.rcWork.Top));
            }

            return true;
        }, IntPtr.Zero);

        return monitors.OrderBy(m => m.DisplayNumber).ToList();
    }

    /// <summary>
    /// アダプターデバイス名 (`\\.\DISPLAY<n>`) から番号部分を取り出す。この番号は Windows の
    /// 「設定 > システム > ディスプレイ」の識別番号と一致する (UI 表示用の並び順・ラベルに使う)。
    /// </summary>
    private static int ParseDisplayNumber(string deviceName)
    {
        var digits = new string(deviceName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }
}
