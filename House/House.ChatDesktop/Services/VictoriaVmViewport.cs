using System.Runtime.InteropServices;
using System.Text;

namespace House.ChatDesktop.Services;

/// <summary>
/// Finds VirtualBox's 1:1 picture child under the embed parent. Click coordinates are
/// that parent's client pixels, so child positions are measured from the same origin.
/// </summary>
internal static class VictoriaVmViewport
{
    public static VictoriaBrowserCoordMap.EmbedViewport? Measure(
        nint root,
        int guestWidth,
        int guestHeight)
    {
        if (!OperatingSystem.IsWindows() || root == 0 || guestWidth <= 0 || guestHeight <= 0)
            return null;

        var origin = new POINT();
        if (!ClientToScreen(root, ref origin))
            return null;

        var children = new List<VictoriaBrowserCoordMap.ViewportChild>();
        EnumChildWindows(root, (child, _) =>
        {
            if (!GetClientRect(child, out var rect))
                return true;

            var width = rect.R - rect.L;
            var height = rect.B - rect.T;
            if (width <= 0 || height <= 0)
                return true;

            var screen = new POINT();
            if (!ClientToScreen(child, ref screen))
                return true;

            var cls = new StringBuilder(128);
            _ = GetClassName(child, cls, cls.Capacity);
            var title = new StringBuilder(128);
            _ = GetWindowText(child, title, title.Capacity);
            var overlay = cls.ToString().Contains("HerScreen", StringComparison.Ordinal)
                || title.ToString().StartsWith("HerScreen", StringComparison.Ordinal);
            children.Add(new VictoriaBrowserCoordMap.ViewportChild(
                screen.X - origin.X,
                screen.Y - origin.Y,
                width,
                height,
                overlay));
            return true;
        }, IntPtr.Zero);

        return VictoriaBrowserCoordMap.ChooseGuestViewport(children, guestWidth, guestHeight);
    }

    private delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint parent, EnumProc callback, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(nint hwnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(nint hwnd, ref POINT point);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hwnd, StringBuilder name, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, StringBuilder text, int capacity);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int L, T, R, B;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }
}
